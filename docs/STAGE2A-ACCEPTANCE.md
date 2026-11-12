# Stage 2A delivery checklist

- [x] Keep Hemo wholly independent from Foundation
- [x] Map sec.AccountUser, Role, Permission, UserRole, RolePermission into EF Core
- [x] Login/logout/session/csrf endpoints and cookie security
- [x] Require server-side global permission on protected example endpoint
- [x] One-time explicit administrator bootstrap using secret env vars
- [x] Negative-path integration tests without SQL
- [ ] Verify login and bootstrap against clean SQL Server with full baseline applied
- [ ] Test RBAC fixture facility/department isolation
- [ ] Account lockout, audit events, password change, revocation, 2FA
- [ ] Production TLS/proxy/data protection key persistence deployment hardening
- [ ] Reconcile medical entity mappings and approve business rules

Do not claim clinical production readiness based on these tests.
