# ADR-0003 — Clean Architecture + explicit CQRS with Minimal APIs

Status: **Implemented for all current ST-01 to ST-04 business flows**, including authentication, session/revocation/global permission, CLI bootstrap, organization, user administration and admission.

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
- Auth/Org/UserAdmin Minimal API endpoints now delegate to typed CQRS handlers; cookie issuance, CSRF, HTTP status mapping, health checks, rate limiting and authorization middleware remain in the transport layer by design. No controller business flow performs direct EF operations.
- `AuthenticationSqlStore` handles account password hashing, signed-cookie session ledger validation, logout revocation and one-time admin provisioning. EF SQL integration and standalone handler tests cover regression.
- EF permission resolution is an Infrastructure adapter. The HTTP security middleware invokes the CQRS `ValidateSessionQuery` and `HasGlobalPermissionQuery` handlers.
