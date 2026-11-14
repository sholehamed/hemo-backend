using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Hemo.Application.Patients;
using Hemo.Domain.Patients;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Patients;

/// <summary>Persistent admission use-cases. Resource authorization is performed at API boundary.</summary>
public sealed class AdmissionService(HemoDbContext db, HemoAuditWriter audit)
{
    private IQueryable<AdmissionRow> Rows(long facilityId, long departmentId) =>
        from encounter in db.Encounters
        join location in db.EncounterLocations on encounter.EncounterId equals location.EncounterId
        join patient in db.Patients on encounter.PatientId equals patient.PatientId
        join activeBand in db.Wristbands.Where(x => x.Status == "Active")
            on encounter.EncounterId equals activeBand.EncounterId into bands
        from band in bands.DefaultIfEmpty()
        where encounter.FacilityId == facilityId
            && location.DepartmentId == departmentId && location.ToAt == null
            && encounter.Status != "Cancelled"
        select new AdmissionRow(encounter, location, patient, band);

    private sealed record AdmissionRow(Encounter Encounter, EncounterLocation Location,
        Patient Patient, Wristband? Band);

    private static AdmissionView ToView(AdmissionRow row) => new(
        row.Encounter.EncounterId,
        row.Patient.PatientId,
        row.Encounter.FacilityId,
        row.Location.DepartmentId,
        row.Location.BedId,
        row.Patient.FirstName,
        row.Patient.LastName,
        row.Patient.NationalId,
        row.Patient.UnknownIdentityCode,
        row.Patient.IsUnknown,
        row.Patient.BirthDate,
        row.Patient.Sex,
        row.Encounter.AdmissionType,
        row.Encounter.Status,
        row.Encounter.AdmittedAt,
        Convert.ToBase64String(row.Patient.RowVersion),
        row.Band?.Barcode);

    public async Task<AdmissionPage> ListAsync(long facilityId, long departmentId,
        string? search, int page, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 10000);
        var query = Rows(facilityId, departmentId).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            if (q.Length > 100) q = q[..100];
            query = query.Where(x => x.Patient.FirstName.Contains(q) ||
                x.Patient.LastName.Contains(q) || (x.Patient.NationalId != null
                && x.Patient.NationalId.Contains(q)));
        }

        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Encounter.AdmittedAt)
            .ThenByDescending(x => x.Encounter.EncounterId)
            .Skip((page - 1) * 20).Take(20).ToListAsync(ct);
        return new AdmissionPage(rows.Select(ToView).ToArray(), page, 20, count);
    }

    public async Task<AdmissionView?> GetAsync(long facilityId, long departmentId,
        long encounterId, CancellationToken ct)
    {
        var row = await Rows(facilityId, departmentId).AsNoTracking()
            .SingleOrDefaultAsync(x => x.Encounter.EncounterId == encounterId, ct);
        return row is null ? null : ToView(row);
    }

    public async Task<AdmissionOperation<AdmissionView>> CreateAsync(
        long facilityId, long departmentId, long actor, CreateAdmissionRequest input,
        CancellationToken ct)
    {
        var nationalId = input.NationalId?.Trim();
        var first = PatientIdentityRules.Normalize(input.FirstName);
        var last = PatientIdentityRules.Normalize(input.LastName);
        if (input.IsUnknown)
        {
            nationalId = null;
            if (first.Length == 0) first = "نامشخص";
            if (last.Length == 0) last = "نامشخص";
        }
        if ((!input.IsUnknown && !PatientIdentityRules.ValidNationalId(nationalId)) ||
            first.Length is < 1 or > 100 || last.Length is < 1 or > 120 ||
            (input.FatherName?.Length ?? 0) > 100 || (input.Mobile?.Length ?? 0) > 30 ||
            (input.AdmissionNote?.Length ?? 0) > 1000 ||
            (input.BirthDate.HasValue &&
                input.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow)) ||
            input.Sex is not ("M" or "F" or "U") ||
            input.AdmissionType is not ("Routine" or "Emergency" or "Transfer" or "Other"))
            return AdmissionOperation<AdmissionView>.Fail("invalid_input");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var activeDept = await db.Departments.AnyAsync(x =>
            x.DepartmentId == departmentId && x.FacilityId == facilityId
            && x.IsActive, ct);
        if (!activeDept)
            return AdmissionOperation<AdmissionView>.Fail("invalid_department");

        if (input.BedId.HasValue)
        {
            var validBed = await db.Beds.AnyAsync(x => x.BedId == input.BedId &&
                x.DepartmentId == departmentId && x.Status == "Active", ct);
            if (!validBed)
                return AdmissionOperation<AdmissionView>.Fail("invalid_bed");
            if (await db.EncounterLocations.AnyAsync(x => x.BedId == input.BedId &&
                x.ToAt == null, ct))
                return AdmissionOperation<AdmissionView>.Fail("bed_occupied");
        }

        // This workflow deliberately does not link an existing patient across facilities
        // without an explicit hospital-approved reconciliation policy.
        if (nationalId is not null &&
            await db.Patients.AnyAsync(x => x.NationalId == nationalId, ct))
            return AdmissionOperation<AdmissionView>.Fail("duplicate_identifier");

        var now = DateTime.UtcNow;
        var patient = new Patient
        {
            NationalId = nationalId,
            UnknownIdentityCode = input.IsUnknown ? "UNK-" + Guid.NewGuid().ToString("N") : null,
            FirstName = first, LastName = last,
            FatherName = PatientIdentityRules.Normalize(input.FatherName) is { Length: > 0 } f
                ? f : null,
            BirthDate = input.BirthDate,
            Sex = input.Sex,
            Mobile = input.Mobile?.Trim(),
            IsUnknown = input.IsUnknown
        };
        db.Patients.Add(patient);
        try
        {
            await db.SaveChangesAsync(ct);
            db.PatientIdentifiers.Add(new PatientIdentifier
            {
                PatientId = patient.PatientId,
                FacilityId = input.IsUnknown ? facilityId : null,
                IdentifierType = input.IsUnknown ? "UnknownIdentity" : "NationalId",
                IdentifierValue = input.IsUnknown ? patient.UnknownIdentityCode! : nationalId!,
                IsVerified = false
            });
            var encounter = new Encounter
            {
                PatientId = patient.PatientId, FacilityId = facilityId,
                AdmissionType = input.AdmissionType, AdmittedAt = now,
                AdmissionNote = input.AdmissionNote?.Trim(), Status = "Admitted"
            };
            db.Encounters.Add(encounter);
            await db.SaveChangesAsync(ct);

            db.EncounterLocations.Add(new EncounterLocation
            {
                EncounterId = encounter.EncounterId, DepartmentId = departmentId,
                BedId = input.BedId, FromAt = now, RecordedByUserId = actor
            });
            audit.Record(actor, facilityId, "Encounter", encounter.EncounterId, "Admitted");
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            var view = await GetAsync(facilityId, departmentId, encounter.EncounterId, ct);
            return AdmissionOperation<AdmissionView>.Ok(view!);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            return AdmissionOperation<AdmissionView>.Fail("constraint_conflict");
        }
    }

    public async Task<AdmissionOperation<AdmissionView>> UpdatePatientAsync(
        long facilityId, long departmentId, long encounterId, long actor,
        UpdatePatientRequest input, CancellationToken ct)
    {
        var first = PatientIdentityRules.Normalize(input.FirstName);
        var last = PatientIdentityRules.Normalize(input.LastName);
        if (first.Length is < 1 or > 100 || last.Length is < 1 or > 120 ||
            (input.FatherName?.Length ?? 0) > 100 || (input.Mobile?.Length ?? 0) > 30 ||
            input.Sex is not ("M" or "F" or "U") ||
            (input.BirthDate.HasValue && input.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow)))
            return AdmissionOperation<AdmissionView>.Fail("invalid_input");

        var row = await Rows(facilityId, departmentId)
            .SingleOrDefaultAsync(x => x.Encounter.EncounterId == encounterId, ct);
        if (row is null) return AdmissionOperation<AdmissionView>.Fail("not_found");

        byte[] expected;
        try { expected = Convert.FromBase64String(input.RowVersion); }
        catch (FormatException) { return AdmissionOperation<AdmissionView>.Fail("invalid_rowversion"); }
        if (expected.Length != row.Patient.RowVersion.Length ||
            !CryptographicOperations.FixedTimeEquals(expected, row.Patient.RowVersion))
            return AdmissionOperation<AdmissionView>.Fail("concurrency_conflict");

        row.Patient.FirstName = first;
        row.Patient.LastName = last;
        row.Patient.FatherName = input.FatherName?.Trim();
        row.Patient.BirthDate = input.BirthDate;
        row.Patient.Mobile = input.Mobile?.Trim();
        row.Patient.Sex = input.Sex;
        audit.Record(actor, facilityId, "Patient", row.Patient.PatientId, "Updated");
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { return AdmissionOperation<AdmissionView>.Fail("concurrency_conflict"); }

        return AdmissionOperation<AdmissionView>.Ok(
            (await GetAsync(facilityId, departmentId, encounterId, ct))!);
    }

    public async Task<AdmissionOperation<WristbandView>> IssueWristbandAsync(
        long facilityId, long departmentId, long encounterId, long actor,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var row = await Rows(facilityId, departmentId)
            .SingleOrDefaultAsync(x => x.Encounter.EncounterId == encounterId, ct);
        if (row is null) return AdmissionOperation<WristbandView>.Fail("not_found");
        if (row.Encounter.Status != "Admitted")
            return AdmissionOperation<WristbandView>.Fail("invalid_state");
        if (row.Band is not null)
            return AdmissionOperation<WristbandView>.Ok(new WristbandView(
                row.Band.WristbandId, row.Band.Barcode, row.Band.PrintedAt, row.Band.Status));

        var band = new Wristband
        {
            EncounterId = encounterId, PrintedByUserId = actor,
            PrintedAt = DateTime.UtcNow, Barcode = "HEMO-" + Guid.NewGuid().ToString("N")
        };
        db.Wristbands.Add(band);
        audit.Record(actor, facilityId, "Wristband", encounterId, "Issued");
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            return AdmissionOperation<WristbandView>.Fail("constraint_conflict");
        }
        return AdmissionOperation<WristbandView>.Ok(new WristbandView(
            band.WristbandId, band.Barcode, band.PrintedAt, band.Status));
    }

    public async Task<AdmissionOperation<IdentityVerificationView>> VerifyIdentityAsync(
        long facilityId, long departmentId, long encounterId, long actor,
        IdentityVerificationRequest input, CancellationToken ct)
    {
        if (input.Barcode?.Length is < 5 or > 100 ||
            input.FirstIdentifier?.Length is < 1 or > 100 ||
            input.SecondIdentifier?.Length is < 1 or > 100 ||
            (input.MismatchReason?.Length ?? 0) > 800)
            return AdmissionOperation<IdentityVerificationView>.Fail("invalid_input");

        var row = await Rows(facilityId, departmentId)
            .SingleOrDefaultAsync(x => x.Encounter.EncounterId == encounterId, ct);
        if (row is null) return AdmissionOperation<IdentityVerificationView>.Fail("not_found");
        if (row.Encounter.Status != "Admitted")
            return AdmissionOperation<IdentityVerificationView>.Fail("invalid_state");
        if (row.Band is null) return AdmissionOperation<IdentityVerificationView>.Fail("no_wristband");

        var barcodeMatches = PatientIdentityRules.Matches(row.Band.Barcode, input.Barcode);
        var idMatches = PatientIdentityRules.Matches(
            row.Patient.IsUnknown ? row.Patient.UnknownIdentityCode : row.Patient.NationalId,
            input.FirstIdentifier);
        var nameMatches = PatientIdentityRules.Matches(
            row.Patient.FirstName + " " + row.Patient.LastName, input.SecondIdentifier);
        var successful = barcodeMatches && idMatches && nameMatches && input.ChecklistConfirmed;
        var now = DateTime.UtcNow;
        var check = new IdentityVerification
        {
            EncounterId = encounterId, WristbandId = row.Band.WristbandId,
            VerifiedByUserId = actor, VerifiedAt = now,
            FirstIdentifier = input.FirstIdentifier.Trim(),
            SecondIdentifier = input.SecondIdentifier.Trim(),
            WristbandMatched = barcodeMatches,
            Status = successful ? "Verified" : "Mismatch",
            MismatchReason = successful ? null : (string.IsNullOrWhiteSpace(input.MismatchReason)
                ? "Identity details or required confirmation did not match."
                : input.MismatchReason.Trim()),
            ChecklistJson = JsonSerializer.Serialize(new
            {
                checklistConfirmed = input.ChecklistConfirmed,
                idMatches, nameMatches, barcodeMatches
            })
        };
        db.IdentityVerifications.Add(check);
        audit.Record(actor, facilityId, "IdentityVerification", encounterId, check.Status);
        await db.SaveChangesAsync(ct);
        return AdmissionOperation<IdentityVerificationView>.Ok(new IdentityVerificationView(
            check.IdentityVerificationId, encounterId, check.Status,
            check.WristbandMatched, check.VerifiedAt));
    }
}
