# ADR-0003 — Clean Architecture + explicit CQRS with Minimal APIs

Status: **Implemented for the ST-04 admission vertical slice**; migration of the older auth/org/user endpoint handlers remains tracked as follow-up.

## Boundaries and flow
- `Hemo.Domain`: patient/org/security entities and pure identity rules; no HTTP, SQL or Angular dependency.
- `Hemo.Application`: DTOs, CQRS `ICommand<T>`/`IQuery<T>` requests, strongly typed `IRequestHandler<T,R>`, domain orchestration, and `IAdmissionStore`/`IResourcePermission` ports. No DbContext/HTTP dependency.
- `Hemo.Infrastructure`: SQL Server EF mappings and persistence adapters, database transactions, BFF session/permission checks, audit writer.
- `Hemo.Api`: Minimal API route groups, cookie/antiforgery, HTTP result mapping and explicit composition root.
- No mediation package is required: directly injected handlers are explicit and easy to debug. No reflection-based dispatcher, no service locator.

## Request sequence
```
Angular (cookie BFF)
  -> POST /api/v1/facilities/{f}/departments/{d}/admissions/
  -> Minimal API (auth + CSRF)
  -> CreateAdmissionCommand
  -> CreateAdmissionHandler (real resource permission)
  -> IAdmissionStore (SQL Server implementation)
  -> Patient/Encounter/Location, audit, transaction
  -> AdmissionOperation<T> -> Minimal API HTTP response
```

## Guarantees and review points
- Facility+department grants checked by handler before touching patient data.
- Commands do not call read-query handlers; query methods are side-effect-free.
- SQL mappings/88-table baseline are unchanged by namespace-only relocations.
- Existing serialized JSON, endpoints, role strings and UI clients remain backward compatible.
- SQL integration tests and Chromium E2E must pass before merging.
- Older Auth/Org/UserAdmin Minimal endpoints still contain some EF work; a dedicated follow-up extracts them behind application use cases. Do not mislabel them migrated.
