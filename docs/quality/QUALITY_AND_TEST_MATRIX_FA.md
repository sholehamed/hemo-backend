# Hemo | راهبرد تست، شواهد و گیت‌های انتشار

## تست‌های افقی در هر PR
- Unit: قوانین pure و validation؛ بدون InMemory به جای SQL برای constraints/transactions.
- Integration: ASP.NET WebApplicationFactory + SQL Server 2022 disposable؛ FK/Unique/RowVersion/permission و خطا.
- Contract: DTO/endpoint/OpenAPI و رفتار 401/403/409/422، TraceId و پاسخ‌های عدم افشای داده.
- Browser: Playwright با Angular + API + SQL و داده کاملاً مصنوعی؛ Login/Logout، Refresh، RTL، تاریخ/فرم، empty/error.
- CI: Restore/Build/Test در هر دو repo + SQL DDL/integration در Backend؛ آخرین Head SHA و بعد Main.
- Security: CSRF، Cookie، forged scope، IDOR، عدم وجود secrets/PHI در Log/Export؛ ابزارها و داده تست ایمن.

## ماتریس سناریوهای کسب‌وکار
| ID | اولویت | مرحله | سناریو و انتظار | نوع آزمون |
|---|---|---|---|---|
| QA-01 | P0 | ST-02A | Login با hash واقعی، Session، Permission، Logout و 401 پس از خروج | SQL Integration (انجام شده) |
| QA-02 | P0 | ST-03 | مرکز A نتواند Patient/Request/Report مرکز B را با ID یا URL دریافت کند | SQL + API + Browser |
| QA-03 | P0 | ST-03 | Role از Session سرور محاسبه شود؛ انتخاب role در مرورگر اختیاری نیست | API/Authorization |
| QA-04 | P0 | ST-04 | ثبت بیمار/بستری، Reload، جست‌وجو و تطبیق مچ‌بند پایدار و idempotent | SQL + E2E |
| QA-05 | P0 | ST-04 | بیمار مجهول‌الهویه/شناسه تکراری و مغایرت هویتی طبق سیاست مصوب ثبت شود | Unit + API |
| QA-06 | P0 | ST-05 | Nurse creates → Doctor approves → BloodBank sees same RequestNo | End-to-end بین نقش‌ها |
| QA-07 | P0 | ST-05 | approve/reject متناقض و double-submit همزمان مجاز نباشد | Concurrency integration |
| QA-08 | P0 | ST-06 | BloodUnit واحد فقط یک reservation فعال و یک issue معتبر داشته باشد | SQL constraint + concurrency |
| QA-09 | P0 | ST-06 | نتیجه ناسازگار Crossmatch طبق SOP مصوب صدور را مسدود کند | Unit + SQL |
| QA-10 | P0 | ST-07 | Issue/Dispatch/Return/Waste/Movement با یک تراکنش و یک تاریخچه سازگار ثبت شوند | SQL transaction |
| QA-11 | P0 | ST-08 | تزریق فقط به Patient/Encounter/Unit/Issue درست وصل شود | SQL + E2E |
| QA-12 | P0 | ST-09 | Nurse reaction به Doctor + HV ارجاع و Audit شود؛ بدون نشت بین بخش‌ها | E2E + resource auth |
| QA-13 | P1 | ST-10 | alarm دما اثر روی قرنطینه و تصمیم سرپرست داشته باشد | Integration |
| QA-14 | P1 | ST-11 | QC / Audit / CAPA / Committee / Training history از دست نرود | Integration |
| QA-15 | P1 | ST-12 | HIS failed/duplicate events با CorrelationId و retry idempotent پردازش شوند | Contract + Integration |
| QA-16 | P1 | ST-12 | گزارش رسمی با داده transaction یکی باشد؛ export فقط scope مجاز | Data reconciliation + Security |
| QA-17 | P0 | ST-13 | Backup/Restore, disaster recovery, E2E whole workflow, clinical signoff | Rehearsal/UAT |

## وضعیت تست اثبات‌شده تا 2026-11-12
| دسته | اجرا شده | نتیجه |
|---|---|---|
| API Smoke/Negative | GitHub Backend PR #2 | ۱۰ تست عادی گذشت |
| SQL 2022 Baseline/Auth | GitHub Backend PR #2 | یک تست کامل SQL E2E گذشت |
| Angular Smoke/Production Build | GitHub Frontend PR #2 | ۷ تست + build موفق |
| Browser E2E Login/Patient | خیر | **Not tested** |
| Clinical End-to-End | خیر | **Not implemented** |
| UAT / بیمارستان / بار واقعی | خیر | **Not tested** |

## خط قرمزها
- سبز بودن Build/Smoke مجوز استفاده بالینی نیست.
- تست‌های بیزینسی باید با schema واقعی SQL اجرا شوند؛ Fake/Test Doubles فقط برای منطق pure کافی‌اند.
- داده ساختگی و شناسه Test فقط؛ کپی اطلاعات بیمار واقعی یا Credentials حقیقی ممنوع.
- تغییرات حساس بدون Test Case و تصمیم بالینی «Blocked» باقی بمانند.
