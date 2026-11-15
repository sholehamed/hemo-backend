# ST-08 | Transfusion safety contract

Backend CQRS: Prepare (verified encounter + received issue + final independently-reviewed Compatible crossmatch) → Baseline Vital → All-positive bedside Checklist → Start → During Vital → Complete or Stop → Independent Signoff.

- A transfusion links one Encounter, RequestItem and BloodUnit; source issue is a separately validated existing issued/received record.
- Clinical observations and action events are **append-only**. Changes after signoff are rejected. RowVersion detects stale start/stop/complete/signoff.
- All times saved as UTC. Only the UI displays Jalali time. No automatic severity, alarms, disease diagnosis or blood-group decision is inferred from numbers.
- A stopped infusion **cannot resume** automatically or become Complete. A completed or stopped transfusion cannot be edited after an independent signoff. Staff must follow hospital escalation protocol when reactions occur.
- Clinical SOP approval (including two-person bedside identity check and threshold/timepoint selection), end-to-end ward UAT and approved scanner/barcode hardware remain outstanding. Code alone is not a permission to initiate a clinical transfusion.
- Authorization enforced by actor + Facility/Department and linked Encounter/Issue on SQL queries and commands; UI guards are supplemental.
