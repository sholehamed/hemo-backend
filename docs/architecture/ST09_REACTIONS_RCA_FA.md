# ST-09 | Reaction reporting, independent physician assessment & HV/CAPA

## Linked data and permissions
- A real `Transfusion` in the current Facility/Department is the only allowed parent for an adverse event. Role-scoped `reaction.report` captures detection time, observed symptoms, immediate actions, physical-stop attestation and immutable symptom rows.
- For active transfusions, first follow the physical stop and ST-08 Stop record. The report endpoint **does not pretend to stop an infusion at the bedside** and rejects ongoing infusions with `stop_before_report`. Late-discovered events after Completed may still be documented.
- A separate physician with `reaction.diagnose` records a human-chosen diagnosis, severity 0..4, causality and recommendations. Code never calculates severity, causality or treatment.
- HV manager `rca.manage` adds append-only investigations; verified finding moves Reported→DoctorReview→Investigation→Confirmed→Closed with explicit review notes and optimistic concurrency.
- RCA cases link to an Event. Root causes and corrective actions are append-only. Responsible HV staff mark actions completed; another authorized reviewer verifies; RCA closure requires root cause, at least one verified action and an actor different from the case opener. Closing the event while RCA is open is blocked.
- All writes emit SQL audit events and use serializable transactions on state transitions, with resource joins for Facility/Department. Current baseline DDL is unchanged.

## Release boundaries
- National-reporting and ISBT classifications are recorded only as clinician choices; never auto-submit national reports.
- Hospital SOP, escalation/on-call notifications, national reporting authority agreement, biological coding review and clinical UAT remain open. This technical stage is not a medical device validation.
