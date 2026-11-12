# Hemo Backend

Independent ASP.NET Core 10 backend (no Foundation dependencies).

## Local run

```bash
dotnet restore Hemo.slnx
dotnet build Hemo.slnx
dotnet test Hemo.slnx
dotnet run --project src/Hemo.Api
```

Development API: http://localhost:5080. Main endpoints:
- GET /health/live (process liveness without SQL)
- GET /health/ready (SQL connectivity)
- GET /api/v1/system/status (non-sensitive API status)
- GET /api/v1/auth/csrf (bootstrap antiforgery cookies)
- POST /api/v1/auth/login (body userName/password and X-XSRF-TOKEN header)
- GET /api/v1/auth/session (authenticated current scopes/permissions)
- POST /api/v1/auth/logout (authenticated and antiforgery header)
- GET /api/v1/system/access-check (authenticated global users.manage permission required)

## Database and initial admin

The original proposed 88-table Hemo SQL Server baseline is a **prerequisite** for identity endpoints. Its sec.* tables are the DDL source of truth; EF excludes those from migrations to prevent duplicate objects. The baseline is intended ONLY for a clean test database and has not yet been validated on a live SQL Server. Do not use EnsureCreated or modify existing production schemas. Set ConnectionStrings__Hemo securely (user secrets / environment, least-privileged application identity).

There is **no default admin password**. After applying the baseline, set HEMO_BOOTSTRAP_USERNAME, HEMO_BOOTSTRAP_DISPLAY_NAME, HEMO_BOOTSTRAP_PASSWORD (minimum 16 chars including upper, lower, digit, symbol) in your secure shell environment. Then explicitly run:

```bash
dotnet run --project src/Hemo.Api -- --bootstrap-system-admin
```

This CLI operation refuses when an active SYSTEM_ADMIN already exists. Never commit credentials or patient data. Existing `compose.sqlserver.yaml` is an optional local SQL Server container; it does not create the schema.

For architecture, security tradeoffs and next steps see `docs/ADR-0001-architecture.md`, `docs/ADR-0002-authentication.md` and `docs/STAGE2A-ACCEPTANCE.md`.
