# ST-04 | پذیرش بیمار و کنترل هویت
**Status:** implementation in development branch; hospital clinical sign-off required before production.

## Route contract
- `GET /api/v1/organization/facilities` and `GET /api/v1/organization/facilities/{id}/departments`: only authorized scopes.
- `GET /api/v1/facilities/{facilityId}/departments/{departmentId}/admissions/?page=&search=`: paged/search admission list.
- `GET /.../admissions/{encounterId}`: scoped detail with RowVersion and optional active barcode.
- `POST /.../admissions/`: new patient+identifier+encounter+location in a SQL Serializable transaction; no fabricated HIS number.
- `PUT /.../admissions/{encounterId}/patient`: update patient with RowVersion.
- `POST /.../admissions/{encounterId}/wristbands`: idempotent issue of active barcode; print UI may still fail physically.
- `POST /.../admissions/{encounterId}/identity-verifications`: record Verified/Mismatch with actor/time/audit. Verified requires active barcode match, identifier match, full-name match AND manual checklist confirmation.

## Model and isolation
Tables: patient.Patient, PatientIdentifier, Encounter, EncounterLocation, Wristband, IdentityVerification, org.Department/Bed, sec.AccountUser, audit.AuditEvent.
- **Never trust facility/department provided by the browser**: authorize the persisted Encounter.FacilityId and active Location.DepartmentId.
- Department grant cannot read another department or center even by guessing an Encounter ID. Role access is checked for every route, including search and dashboard.
- NationalId must be exactly ten ASCII digits; this is syntactic validation only, not proof of a person's identity. Clinical identity proof is separately governed by approved SOP.
- A new admission with an existing NationalId is a conflict until duplicate-person reconciliation policy is approved; do not automatically reveal or attach cross-hospital records.
- Unknown patient gets opaque `UNK-{guid}` identifier. One active EncounterLocation/Bed occupancy and Wristband per Encounter are backed by SQL filtered unique indexes.
- All timestamps persisted as UTC/Gregorian; Persian/Jalali conversion is presentation-only.

## Acceptance
- Persisted admission after reload; facility/dept scope checks; two simultaneous bed allocations prevented by DB; national ID uniqueness; mismatched verification recorded, not falsely successful.
- Release gate remains closed until hospital clinical authority approves wristband workflow, two-identifier policy and patient-identity exception handling.
