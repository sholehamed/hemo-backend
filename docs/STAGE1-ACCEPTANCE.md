# Stage 1 acceptance / implementation record

- [x] .NET 10 API and Domain/Application/Infrastructure layering
- [x] SQL Server EF Core connection scaffold, no entity mappings
- [x] Liveness, readiness, API status, trace-friendly errors, Dev OpenAPI
- [x] Integration smoke tests for no-DB infrastructure endpoints
- [x] Dockerfile, local SQL Server compose and PR CI workflow
- [ ] GitHub CI must pass on PR (not claimed before confirmed)
- [ ] SQL Server readiness must be validated against a real test instance
- [ ] Schema-baseline/EF mapping reconciliation (Stage 2)
- [ ] BFF login, permissions, audit/security design (Stage 2)
- [ ] Real patient/workflow endpoints (subsequent vertical slices)

The legacy Hemo schema/flow analysis and SQL reference are maintained as project planning artifacts and should be reviewed before entity mappings are introduced.
