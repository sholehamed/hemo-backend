# Hemo — وضعیت واقعی و محل ادامه کار
**به‌روزرسانی:** 2026-11-14 · **وضعیت:** ST-02B و ST-03 در main ادغام شده‌اند؛ ST-04 در PR #6 با Build و SQL Integration سبز و در انتظار تأیید Merge است.

## وضعیت مستند و قابل استناد
| شناسه | وضع | شاهد GitHub | توضیح |
|---|---|---|---|
| ST-00 — بررسی پروتوتایپ | مستند شده | 62 فایل HTML، تحلیل [مرجع](../reference/HEMO_WORKFLOWS_AND_DATA_MODEL_FA.md) | تحلیل بر اساس سورس، نه مرورگر/E2E |
| ST-01 — زیرساخت مستقل | **Merged** | Backend [PR #1](https://github.com/sholehamed/hemo-backend/pull/1)، Frontend [PR #1](https://github.com/sholehamed/hemo-frontend/pull/1) | .NET 10 چهارلایه، Angular 22، health/status، CI |
| ST-02A — BFF و هویت پایه | **Merged** | Backend [PR #2](https://github.com/sholehamed/hemo-backend/pull/2)، Frontend [PR #2](https://github.com/sholehamed/hemo-frontend/pull/2) | Cookie، CSRF، login/logout/session، sec.*، منوی مبتنی بر role |
| ST-02B — قرارداد پایگاه داده | **Merged** | Backend/Frontend PR #4 | تطبیق نگاشت org/sec و SQL Baseline، Version/preflight؛ Migration ارتقایی هنوز جداگانه لازم است |
| ST-03 — مجوز و سازمان | **Merged (foundation)** | Backend/Frontend PR #5 | کنترل scope بخش/مرکز و revocation SQL و مدیریت اولیه؛ MFA، lockout و تغییر رمز اجباری مانده‌اند |
| ST-04 — پذیرش و هویت | **Implemented; CI green, PR #6 open** | Backend/Frontend PR #6 | ثبت/جست‌وجو/ویرایش، Wristband و تطبیق هویت در SQL؛ Browser E2E و تأیید SOP بالینی مانده‌اند |
| ST-05 ... ST-13 | **Not started** | [Roadmap](ROADMAP_FA.md) | جریان‌های درخواست خون تا استقرار |

## نتیجه تست ثبت‌شده قبل از این سند
- Backend main: Release build و **10 تست معمول** موفق؛ شاخه PR #2 نیز **تست یکپارچه SQL Server 2022 (1/1)** را با اجرای Baseline کامل و Bootstrap/Login/Permission/Logout گذرانده است؛ [Workflow SQL](https://github.com/sholehamed/hemo-backend/actions/workflows/sql-server-integration.yml).
- Frontend main: Production build و **7 تست smoke** موفق؛ [Frontend CI](https://github.com/sholehamed/hemo-frontend/actions/workflows/frontend-ci.yml).
- تست مرورگر با Backend و SQL در یک محیط واقعی، تست امن‌سازی داده بین دو مرکز، و بارگذاری/کارایی **هنوز انجام نشده**.
- اجرای SQL Server در Workflow به معنی صحت همه قوانین بالینی یا نهایی‌بودن کل 88 جدول نیست.

## کار بعدی بدون تداخل
1. اول مدل فیزیکی موجود و نگاشت EF Core را خط‌به‌خط تطبیق بده؛ Audit و Migration policy را تثبیت کن. هیچ دومین جدول Identity نساز.
2. در همان زمان Frontend را فقط در حد Layout مشترک، HttpClient قرارداد ثابت و فرم‌های پایه ارتقا بده؛ **هنوز فرم بالینی را به داده Mock محدود نکن**.
3. بعد در ST-03 scope سازمانی و API authorization را تست کن. قبل از تکمیل این مرحله، هیچ Endpoint بیمارمحور را عمومی نکن.
4. سپس ST-04 را با Patient / Encounter / Identity Verification به‌عنوان اولین Vertical Slice واقعی شروع کن.

## گیت قبل از رفتن به پذیرش واقعی
- [ ] DDL reference و EF entity mapping تطبیق و در CI SQL آزمون شود.
- [ ] Permissionهای API و دامنه Facility/Department با داده حداقل دو مرکز و نقش‌های متفاوت تست شوند.
- [ ] Account/bootstrap/seed امن و audit لایه هویت تأیید شوند.
- [ ] خطاها بدون افشای PHI و با TraceId پاسخ داده شوند.
- [ ] شکل قرارداد DTO / اعتبارسنجی / خطا / Pagination / concurrency مشخص و مستند شود.

**هشدار:** Hemo هنوز برای استفاده با داده واقعی بیمار یا عملیات درمانی آماده نیست. گیت بالینی و امنیتی قبل از هر استقرار لازم است.
