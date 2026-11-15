# ST-07 — issue, transport, receive and return
- Stock cannot be issued unless an approved request matches the same **active encounter department**, a single active reservation exists and independently approved Final/Compatible Crossmatch with a recorded test exists.
- Bank staff with issue.create may issue once. No duplicate issuance of the same unit.
- Supervisor with dispatch.manage may record container/exit temperature and dispatch an issued unit; no automatic approval of cold-chain compliance.
- Ward worker with issue.receive and matching department receives using RowVersion (with a different identity from issuer). Dispatch, if present, becomes Delivered.
- Return is physically received by a bank supervisor with return.manage, identifies original ward receiver as return sender. Unit is quarantined, never automatically restocked.
- A **different** supervisor must finalize the return with Quarantine or Waste; Restock is blocked until hospital SOP supports it. Waste creates a separate record.
- Race safety uses serializable transactions, rowversion and SQL unique keys. Real-world scanning/temperature validation and signatures require clinical/quality approval.
