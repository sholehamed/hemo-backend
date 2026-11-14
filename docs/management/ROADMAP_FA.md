# Hemo | نقشه راه مرحله‌بندی‌شده (واحد Backend + Frontend)
**نسخه 1.0 · وضعیت‌ها:** `DONE` = ادغام و تست اثبات‌شده؛ `NEXT` = در صف بعدی؛ `PLANNED` = بدون پیاده‌سازی؛ `BLOCKED` = نیازمند تصمیم/پیش‌نیاز.  
**ترتیب اجرا:** ST-00 → ST-01 → ST-02A → ST-02B → ST-03 → ST-04 → ... → ST-13. هر گام شامل مستندات، تست و PR مستقل است. **ساختن همه Backend و سپس Frontend ممنوع نیست ولی راهبرد پروژه نیست**؛ هر گام باید همزمان قرارداد API و UI مصرف‌کننده را تعیین کند.

## تصویر کلان
| مرحله | عنوان | وضعیت 2026-11-12 | خروجی قابل تحویل |
|---|---|---|---|
| ST-00 | استخراج و تحلیل پروتوتایپ | DONE (تحلیل سورس) | فهرست 62 صفحه / 15 جریان |
| ST-01 | Bootstrap زیرساخت | DONE | ASP.NET Core + Angular + CI |
| ST-02A | هویت و BFF پایه | DONE (محدود) | Cookie/CSRF/Login و CRUD **هنوز نه** |
| ST-02B | تثبیت SQL baseline و قرارداد داده | IMPLEMENTED (contract; migrations evolution pending) | نصب schema تکرارپذیر، EF mappings، Seed و migration policy |
| ST-03 | مجوز، امنیت و سازمان | NEXT | Facility/Department authorization + مدیریت حساب + Audit |
| ST-04 | پذیرش و پرونده | PLANNED | Patient / Encounter / Wristband / IdentityVerification |
| ST-05 | درخواست خون و تأیید | PLANNED | Request / Items / Approval / Task با نقش‌های مشترک |
| ST-06 | موجودی و Crossmatch | PLANNED | Stock / Reservation / Crossmatch / Compatibility workflow |
| ST-07 | صدور و لجستیک | PLANNED | Issue / Dispatch / Receipt / Return / Waste / Thaw / Transfers |
| ST-08 | تزریق و پایش | PLANNED | Transfusion / Vitals / Checklist / Signoff |
| ST-09 | واکنش و پیگیری بالینی | PLANNED | Reaction / MedicalAssessment / Investigation / RCA linkage |
| ST-10 | تجهیزات و زنجیره سرما | PLANNED | Equipment / PM / Calibration / Readings / Excursions |
| ST-11 | کیفیت، آموزش و کمیته | PLANNED | QC / RCA-CAPA / Audit / Meeting / Training |
| ST-12 | ترخیص، HIS و گزارش‌های رسمی | PLANNED | Discharge / Integration / Reports و Analytics همه نقش‌ها |
| ST-13 | Hardening, UAT, Staging و Release | PLANNED | تست امنیت، داده، بار، Backup/Restore، ابلاغ بالینی |

## قانون هر مرحله
- **ورود:** PR قبلی و تست‌های اجباری سبز؛ تصمیم‌های حیاتی آن مرحله ثبت شده؛ قرارداد دیتای مرتبط موجود.
- **تحویل:** DB/Backend/Frontend مربوط به همان سناریو کار می‌کنند، سند OpenAPI/DTO و Mapping صفحه/رول اصلاح شده، Audit/Logs و تست مثبت/منفی وجود دارد.
- **خروج:** Unit + Integration + E2E متناسب، RBAC سناریوهای دورزدن، کنترل همزمانی و تست مهاجرت برای تغییر DB؛ همگی سبز؛ سپس Merge با SHA مشخص.
- **نکته بالینی:** عبور CI به معنی تأیید درمانی نیست؛ قواعد سازگاری فرآورده، مقدار و زمان پایش، واکنش، توقف و استثنای اورژانسی نیاز به SOP و تصویب صاحب‌صلاحیت دارند.

## ST-00 | تحلیل و کشف جریان (DONE / همیشه قابل به‌روزرسانی)
1. فهرست 62 HTML + menu.js را ثبت و 7 دامنه اصلی + نسخه‌های تکراری را تفکیک کن.
2. برای هر صفحه ورودی، خروجی، عملیات، نقش، وضعیت فعلی Mock و جداول مشترک را استخراج کن.
3. 15 جریان end-to-end، نقاط هم‌پوشان، تصمیم‌های نامشخص و قوانین پیشنهادی را مستند کن.
4. خروجی: [سند مرجع](../reference/HEMO_WORKFLOWS_AND_DATA_MODEL_FA.md)؛ هنگام پیاده‌سازی دوباره بررسی کاربردی انجام می‌شود.

## ST-01 | پایه فنی (DONE)
1. Solution .NET 10 در چهار پروژه، تست API و Health/Status را بساز.
2. Angular 22 Standalone RTL، Shell، Routeها و اتصال same-origin API را بساز.
3. GitHub Actions build/test، README و Docker نمونه را اضافه کن.
4. تحویل: PRهای #1 دو ریپو، CI موفق و Merge.

## ST-02A | ورود واقعی پایه (DONE با محدودیت)
1. نگاشت EF به 5 جدول موجود sec.*؛ خودداری از AspNetUsers موازی.
2. Cookie HttpOnly + SameSite، Antiforgery، rate limit، endpoint login/session/logout و permission probe.
3. Angular Login + session refresh + route guard و منوی role-aware.
4. Bootstrap صریح SYSTEM_ADMIN بدون رمز پیش‌فرض؛ اجرای SQL Server موقت و E2E auth در CI.
5. تحویل: PRهای #2، تست SQL سبز؛ محدودیت‌های امنیتی ST-03 هنوز باز.

## ST-02B | قرارداد دیتابیس و Seed (NEXT)
1. Baseline 88 جدول را به inventory ستون، FK، check، unique/filtered index و dependency تبدیل کن.
2. بررسی عمیق EF mapping پنج جدول sec با SQL واقعی و سیاست ownership تغییرات: DDL مبنا + migrations بعدی، بدون EnsureCreated.
3. تصمیم مستند برای SQL bootstrap در محیط خالی و migrations نسخه‌های بعد؛ گارد اجرا روی دیتابیس غیرخالی.
4. Seed versioned برای نقش، permission، product type، مراکز پایه **بدون کاربر/بیمار واقعی**؛ seed idempotent.
5. قرارداد مشترک تاریخ UTC، نمایش جلالی، soft delete/retention، RowVersion و شناسه پرونده مرکز.
6. CI SQL: اجرای baseline، seed دوباره، duplicate prevention، integrity، migration drift و fail-fast در نبود schema.
7. در Frontend: API contracts، typed DTO/errors، جست‌وجوی پایه و کنترل‌های فرم قابل استفاده مجدد.
8. معیار خروج: اختلافات EF/DDL صفر یا در ADR ثبت شده، تست SQL و تست Build هر دو سبز.

## ST-03 | سازمان، حساب، مجوز و Audit
1. موجودیت Facility/Department/Bed/Shift و ارتباط و اعتبارسنجی scope را تعریف کن.
2. سیاست Permission+Resource: هر endpoint روی واقعی‌ترین شناسه Facility/Department/Encounter بررسی شود.
3. Endpointهای Admin برای کاربران و تخصیص RoleGrant، فعال/غیرفعال، تغییر رمز اولیه/اجباری، قفل و audit ایجاد کن.
4. Revocation/Session ledger/lockout/security events؛ سیاست MFA و DataProtection keys در استقرار تعیین شود.
5. Frontend: پنل مدیریت کاربر/نقش، صفحات 401/403، منوی مبتنی بر permission واقعی؛ UI فقط نمای مجوزهاست.
6. با دو مرکز، دو بخش و نقش هم‌زمان تست کن: دسترسی بین‌مرکزی با URL/ID جعلی حتماً 403/404.
7. معیار خروج: عدم افشای داده بین scopeها؛ Audit actor/time/action؛ تست CI+browser موفق.

## ST-04 | پذیرش بیمار و هویت
1. مدل Patient/Identifier/Encounter/EncounterLocation/Wristband/IdentityVerification + قوانین uniqueness در مرکز.
2. Use-case پذیرش جدید، پرونده بیمار مجهول‌الهویه، اصلاح اطلاعات و ارجاع از HIS (Mock نداریم؛ در نبود integration منبع دستی ثبت می‌شود).
3. Endpointهای ثبت/ویرایش/جست‌وجو/نمایش جزئیات؛ Pagination و concurrency.
4. Angular: admission-dashboard، admission-new-patient، admission-patient-list، admission-identity-check.
5. Data validation و UI جلالی و بارکد/چاپ به صورت پیش‌نیازهای مستقل از سخت‌افزار.
6. Transaction test، گزارش مغایرت هویتی، ممنوعیت دسترسی مرکز دیگر و E2E پذیرش→تطبیق.
7. خروج: داده ایجادشده پس از Reload و بین حساب‌های مجاز پایدار و قابل مشاهده باشد.

## ST-05 | درخواست و تأیید بین نقش‌ها
1. BloodRequest/Item/ClinicalData/Approval/Task؛ انواع فرآورده و فیلد شرطی را بر اساس مرجع تحلیل تعریف کن.
2. وضعیت Draft→Submitted→DoctorReview→Approved/Rejected را فقط با قوانین مصوب فعال کن.
3. پرستار/پزشک/بانک خون روی یک RequestNo مشترک کار کنند؛ ثبت History/Audit.
4. Angular: nurse-request/tasks/stats، doctor-orders/approval/dashboard، bloodbank-requests.
5. تست چندنقشیِ ثبت، مشاهده کارتابل، تأیید، بازگشت/رد و idempotency/Concurrency.
6. خروج: یک Request واحد و پیام خطا در شکست authorization، بدون دوباره‌کاری فرم‌ها.

## ST-06 | واحد خون، موجودی، Crossmatch
1. StockOrder/Receipt/BloodUnit/InventoryMovement/Reservation/Crossmatch و testها را با FK واقعی راه‌اندازی کن.
2. منطق Reserve/Release و FEFO/انقضا/قرنطینه را Atomic و با ایندکس یکتا پیاده کن.
3. Crossmatch با جزئیات آزمایش و نسخه‌های مطرح در پروتوتایپ؛ **قواعد AB0/Rh فقط پس از تأیید تخصصی**.
4. Angular: bloodbank-inventory/crossmatch/dashboard، supervisor-inventory/crossmatch/expiry.
5. تست جلوگیری از دو رزرو هم‌زمان، ناسازگاری داده در درخواست و اقلام و Audit.
6. خروج: Trace کامل UnitCode تا RequestItem و وضعیت واحد.

## ST-07 | تحویل و چرخه لجستیک
1. UnitIssue/Dispatch/DispatchItem/UnitReturn/UnitWaste/ThawProcess/InterfacilityTransfer.
2. ثبت در هر جابه‌جایی با transaction یکپارچه + InventoryMovement + امضای نقش مربوط.
3. تأیید تحویل/عودت، بررسی دما، خروج از قرنطینه و اعزام بحران طبق SOP مصوب.
4. Angular: supervisor-dispatch/receive/return/waste، bloodbank-thawing، supervisor-dashboard logistics.
5. تست عدم صدور مضاعف، عدم بازگشت اشتباه به موجودی، مانع دریافت مغایر و عبور از مرکز دیگر.

## ST-08 | تزریق و پایش
1. Transfusion/Checklist/VitalObservation/Action/Signoff به Encounter + Issue + Unit وصل شود.
2. ثبت پایش بر پایه Timepoint با تاریخ UTC و نسخه تغییرناپذیر بعد از Signoff.
3. Angular: nurse-transfusion-form/vitals/tasks، doctor-monitoring/transfusion-record/patients.
4. تست تطبیق واحد و بیمار، آغاز/توقف/اتمام، هشدار مبتنی بر Threshold مصوب، data integrity.
5. خروج: چرخه درخواست تا تزریق قابل Trace با شناسه واحد و actor باشد.

## ST-09 | واکنش و پیگیری
1. AdverseEvent/EventSymptom/PhysicianEventAssessment/EventInvestigation و ارجاع RCA.
2. ثبت توسط پرستار، ارزیابی پزشک، مدیریت واکنش توسط HV؛ وضعیت و Alert ناظر.
3. Angular: nurse-reaction، doctor-reactions، hemovigilance-reactions/rca.
4. تست واکنش روی واحد واقعی، اطلاع‌رسانی به نقش مجاز، عدم تغییر تاریخچه بدون Audit.
5. تعاریف شدت/علیت/اقدامات بالینی فقط با امضای متخصص تصویب شود.

## ST-10 | تجهیزات و زنجیره سرما
1. EquipmentCategory/Equipment/PM/Calibration/Readings/Excursion/Incident/Action.
2. Validation دما و کالیبراسیون، مدیریت بررسی سرپرست و اثر رخداد روی Unitها.
3. Angular: 5 صفحه equipment + bloodbank-temperature + supervisor-temperature.
4. تست رخداد خارج محدوده، ثبت و رفع، امضا و اتصال به محدودیت موجودی.

## ST-11 | کیفیت و حاکمیت
1. DailyQC/QualityControlResult، RCA/Cause/CAPA، AuditQuestion/Answer، CommitteeMeeting/Decision، Training/Course/Enrollment.
2. Frontend: supervisor-qc/committee، hemovigilance-audit/committee/training/rca/settings.
3. گزارش مصوبات، تکلیف با Owner و DueDate، آموزش و آزمون، نسخه SOP.
4. تست integrity، گردش تأیید، Audit و جلوگیری از دستکاری مصوبات/امضا.

## ST-12 | HIS، ترخیص و گزارش‌ها
1. Integration Endpoint/Message/MessageAttempt/SyncJob + CorrelationId و retry idempotent؛ اتصال HIS فقط با قرارداد واقعی.
2. پایان پرونده/ترخیص با چک‌لیست کامل و باطل نکردن سوابق تزریق/واکنش.
3. ReportDefinition/Run/Submission و گزارش‌های پذیرش، پرستار، پزشک، بانک خون، سرپرست، HV؛ خروجی کمینه‌شده PHI.
4. Angular: admission-his-sync/discharge-clearance/blood-history/stats و همه صفحات گزارش/داشبورد.
5. تست پیام تکراری، خطا/Retry، انطباق گزارش با source-of-truth، Export و scope.

## ST-13 | آماده‌سازی بهره‌برداری
1. مرور نهایی تمام 62 صفحه و انتخاب نسخه canonical برای duplicates.
2. تست E2E در مرورگر با SQL، RBAC، چندمرکز، concurrency و استثناهای بالینی مصوب.
3. Security review: secrets, audit, login lockout/MFA, session revoke, TLS, DataProtection, backup/restore, data-retention, disaster recovery.
4. Performance/operational: query plan و شاخص‌ها، monitoring/trace، alert، staging pipeline، نسخه release و rollback plan.
5. UAT و امضای بالینی توسط افراد صاحب‌صلاحیت؛ هیچ ادعای clinical readiness پیش از آن.
6. تدوین runbook نصب اولیه، seed مجوزها، مهاجرت، نگهداری و گزارش تغییرات.

## مواردی که به‌صورت افقی در *تمام* مراحل همراه کارند
- Frontend + Backend + DB یک جریان، نه پیاده‌سازی یک‌طرفه.
- مستندسازی در همان PR، شاخص TraceId و Audit، تست مجوز/Scope، خطاهای سازگار، فارسی/RTL/شمسی.
- کنترل Patient PHI، Backward compatibility، migration safety و rollback.
- هر مرحله در [Journal](PROJECT_JOURNAL_FA.md) بسته شود و «مانده‌ها» به Backlog بعدی منتقل شوند.
