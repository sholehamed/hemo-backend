# Hemo ST-04 | شواهد تحویل فنی و مرز بهره‌برداری
**تاریخ:** 2026-11-14

## Pull Requests ادغام‌شده
- [Backend PR #6](https://github.com/sholehamed/hemo-backend/pull/6) → main، SQL-backed Patient / Identifier / Encounter / Location / Wristband / IdentityVerification، authorized API، واحد audit.
- [Frontend PR #6](https://github.com/sholehamed/hemo-frontend/pull/6) → main، Angular پذیرش، فهرست، جست‌وجو، فرم شمسی، مچ‌بند، تطبیق دو شناسه و اصلاح پرونده.
- پشتیبانی از Global admin و role scoped در API، رفع اشکال Guard پس از Refresh.

## تست‌های اجراشده و نتیجه
- Backend Release build / unit و API smoke: **۲۳/۲۳ passed** در آخرین PR Backend.
- SQL Server 2022 disposable / Full 88-table baseline + Scenario: **۱/۱ passed**.
- Frontend Production build و Smoke: **موفق**.
- [Chromium Playwright Browser E2E](https://github.com/sholehamed/hemo-frontend/actions/workflows/admission-browser-e2e.yml): **۱/۱ passed**. مسیر Login → Admission مجهول‌الهویه → Reload → Barcode issuance → Identity Verified → Search.
- SQL Integration آزمون Facility/Department authorization، duplicate National ID، occupied bed و optimistic concurrency را پوشش می‌دهد.
- Build Main Backend پس از Merge سبز و تست Frontend آخرین Commit PR سبز است.

## محدودیت‌های مهم که هنوز مانده‌اند
- نمایش رشته بارکد و چاپ عمومی مرورگر، **استاندارد تأیید‌شده چاپ یا اسکن مچ‌بند بیمارستانی نیست**.
- تشخیص هویت ناشناس و reconciliation بیمار تکراری، اعتبار شناسه دوم، و فرآیندهای emergency به SOP مصوب نیاز دارند.
- MFA، lockout persist، تغییر اجباری رمز اولیه، PenTest و داده حساس log/export باید قبل از بهره‌برداری تکمیل شوند.
- Migration ارتقایی schema، Disaster Recovery و UAT هنوز پیش‌نیاز انتشار هستند.
- تست مرورگر یک مسیر Happy Path را آزمایش می‌کند؛ این به معنای صحت تمام عملیات بالینی یا تست گسترده UI نیست.

**نتیجه:** ST-04 از نظر توسعه فنی و تست‌های CI تکمیل و Merge شده است؛ **آمادگی بالینی/Production ادعا نمی‌شود**.
