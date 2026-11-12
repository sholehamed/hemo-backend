# Hemo Backend

Independent ASP.NET Core 10 LTS backend. **No dependency on Foundation.**

## Run locally

Install .NET SDK 10. Run from repository root:

```bash
dotnet restore Hemo.slnx
dotnet build Hemo.slnx
dotnet test Hemo.slnx
dotnet run --project src/Hemo.Api
```

API (Development): `http://localhost:5080/api/v1/system/status`, `/health/live`, `/health/ready`, `/openapi/v1.json`.

`/health/live` should be OK even when SQL is down. `/health/ready` is 503 until configured SQL Server is reachable.

On Windows, Development defaults to a local SQL Server LocalDB connection (not a real clinical DB). For Docker SQL Server: set a strong `MSSQL_SA_PASSWORD` locally and run `docker compose -f compose.sqlserver.yaml up -d`. Set `ConnectionStrings__Hemo` in your environment (or user-secrets) with **a separate least-privilege app login**, never `sa`. The Docker port is loopback-only for local development. Do not commit credentials.

## Architecture

`Hemo.Domain` -> `Hemo.Application` -> `Hemo.Infrastructure` -> `Hemo.Api`. Stage 1 is **infrastructure only**: no login, patient data, business workflows, or fake SQL persistence. Schema reference must be reconciled with EF entities before initial migration; never run `EnsureCreated` on a clinical system.

See `docs/ADR-0001-architecture.md` and `docs/STAGE1-ACCEPTANCE.md`. Frontend is in the separate `sholehamed/hemo-frontend` repository.
