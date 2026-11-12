# ADR-0002 — Hemo independent cookie BFF on existing sec schema

Status: Implemented scaffold; SQL-backed positive-path tests pending an isolated SQL Server instance.

## Identity source of truth
The previously designed SQL Server baseline owns sec.AccountUser, sec.Role, sec.Permission, sec.UserRole, sec.RolePermission. Hemo's EF models map those exact tables and ExcludeFromMigrations. No ASP.NET Identity parallel AspNetUsers tables. Role codes are seeded by the baseline: SYSTEM_ADMIN, NURSE, DOCTOR, BLOOD_TRANSFUSION_EXPERT, ADMISSION, BIOMED_ENGINEER, HV_MANAGER, SUPERVISOR. Password hashes use Microsoft's ASP.NET Core PasswordHasher; passwords never go in Git.

## Session/CSRF
- Authentication is the server-issued encrypted/signed HttpOnly cookie Hemo.Session; no tokens in localStorage.
- CSRF bootstrap: GET /api/v1/auth/csrf returns a JS-readable XSRF-TOKEN cookie and an HttpOnly antiforgery cookie. Angular sends X-XSRF-TOKEN on relative mutating requests.
- Login and logout validate antiforgery tokens, with throttling on login. Logout invalidates the current browser cookie. Revocation of *all* sessions, persistent session ledger and anomaly monitoring are future work.
- Session validation queries AccountUser.IsActive on every authenticated request.
- Cookie SameSite Strict. Production Secure, local HTTP Development SameAsRequest.
- Production HTTPS/TLS, correctly scoped proxy forwarded headers, persisted protected Data Protection keys, SQL least privilege and IP rate-limit proxy configuration must be reviewed before launch.

## Authorization
An authenticated principal contains a user ID and username, **not trusted client-selected roles**. GET /api/v1/auth/session retrieves effective grants from sec.* including role, facility and department scope. Example /api/v1/system/access-check requires a current global users.manage grant. Future clinical endpoints must check their permission **and** their actual patient's facility/department ownership, not just union permissions. Guard/menu visibility is not a security boundary.

## Bootstrap
No default credentials and no self-registration. After applying the SQL baseline on a clean dedicated database, explicitly run:
`dotnet run --project src/Hemo.Api -- --bootstrap-system-admin`
with environment variables HEMO_BOOTSTRAP_USERNAME, HEMO_BOOTSTRAP_DISPLAY_NAME, HEMO_BOOTSTRAP_PASSWORD (16+ mixed characters). Refuses if active system admin already exists and never logs the password. Scope is global only for SYSTEM_ADMIN; clinical users require approved facility grants. Don't run the baseline against an existing Hemo database or a production DB without a migration review.

## Limitations
- The SQL baseline is a proposed physical design, not verified by medical specialists.
- Current CI tests use an unreachable SQL endpoint for negative security contract tests; SQL login/role/bootstrap positive-path integration tests remain before security go-live.
- No users UI, account lockout ledger, password reset, 2FA, permission editing, facility/department API enforcement or full login audit.
- Readiness only checks connectivity; it does not verify baseline tables. A future SQL schema readiness check must validate required objects.
