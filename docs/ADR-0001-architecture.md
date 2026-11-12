# ADR-0001: Independent Hemo modular monolith

Status: Accepted for Stage 1.

Hemo is independent from Foundation. Backend uses ASP.NET Core 10 LTS with Domain, Application, Infrastructure, Api. Modules will grow by clinical business capability rather than by UI page. No direct ASP.NET/EF dependency in Domain. Angular is deployed separately; BFF secure session / authorization will be designed and implemented in Stage 2 (never fake a login).

## Database rule
The 88-table SQL Server baseline from Hemo analysis is a **design reference**, not an approved schema migration. Do not execute the baseline against production, invoke `EnsureCreated`, or generate an empty EF migration. Reconcile names, identities, FKs, constraints, concurrency and audit requirements before initial EF migration. Health readiness checks the configured SQL database availability only.

## Safety / operational constraints
- Never commit medical or real patient data, credentials, or auth tokens.
- Frontend route visibility is not security; backend must enforce authorization before clinical endpoints exist.
- No clinical validation (ABO, crossmatch, reactions) is implemented in Stage 1.
- Production must use trusted TLS certificates, connection-string secret storage and least-privilege SQL accounts.
- `/health/live` is process-only; `/health/ready` requires SQL; `/api/v1/system/status` is a non-sensitive API contract.
