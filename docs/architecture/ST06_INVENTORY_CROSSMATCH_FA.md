# ST-06 — inventory and crossmatch
Scope: facility-scoped blood-bank staff, never automatic blood group compatibility.
- Manual stock entry has unique unit code, product/group reference, expiry, facility and audit/movement.
- Reservation requires a physician-approved request for the **same facility**, correct product, unexpired unquarantined stock, and requested-quantity capacity; SQL serializable transaction blocks competing allocations.
- Crossmatch requires an active reservation and manual test evidence; status PendingReview. An independent second authorized specialist finalizes a result supplied by laboratory staff. Compatible is *not* inferred from ABO/Rh.
- ST-07 issue will require both a live reservation and independently Final/Compatible test.
- Clinical SOP, laboratory instrumentation validation and barcode scanning remain release gates.
