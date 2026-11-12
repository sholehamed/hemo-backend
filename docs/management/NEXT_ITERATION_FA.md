# Hemo | دستور کار اجرایی مرحله بعد: ST-02B سپس ST-03
**وضعیت:** مصوب به‌عنوان برنامه، **هنوز پیاده‌سازی نشده**.  
**قانون:** این سند قبل از هر PR جدید خوانده و به‌روز شود. هیچ دکمه Angular نباید صرفاً پیام «ثبت موفق» بدون ثبت واقعی نشان دهد.

## بخش ۱ — ST-02B: تثبیت قرارداد SQL و مدل پایه
### PR پیشنهادی Backend: `HEMO-ST02B-DB-CONTRACT`
- [ ] از آخرین `main` Branch جدید بساز؛ SQL baseline و فایل‌های `HemoDbContext`/`IdentityEntities` را بررسی کن.
- [ ] جداول فعلی ۱۱ Schema، ۸۸ جدول، ۲۰۳ FK و ۲۵۹ ایندکس اعلام‌شده را در یک تست خودکار شمارش کن؛ اختلاف عدد را به‌جای مخفی‌کردن ثبت کن.
- [ ] ۵ Entity هویت فعلی (`AccountUser`, `Role`, `Permission`, `RolePermission`, `UserRole`) را از نظر اسم/نوع/طول ستون، RowVersion، Defaults و رابطه با فایل SQL تطبیق بده.
- [ ] جدول `sec.UserSession` را بررسی کن؛ تصمیم مستند بگیر آیا Ledger/Revocation از همین جدول پیاده می‌شود یا تغییر Schema لازم دارد.
- [ ] قرارداد واحد رویداد Audit، کد مجوز، نام‌گذاری migration و Backward Compatibility را بنویس.
- [ ] پیش از افزودن Clinical Entities، مدل EF Core `org` را طراحی و وجود Facility/Department/Bed را با constraintها اعتبارسنجی کن.
- [ ] مکانیزم نصب **fresh-only** در محیط disposable و نسخه‌بندی migration پس از baseline را پیاده کن؛ هر migration روی اسکیما واقعی تست شود، نه فقط SQLite یا InMemory.
- [ ] CI: اجرای DDL، تأیید metadata جدول‌ها، seed دوباره، بررسی unique/index/constraintها و Fail-fast برای schema mismatch.
- [ ] مستندات README و ADR و وضعیت Roadmap را در همان PR اصلاح کن.
- [ ] تا سبزشدن CI SQL و مرور ریسک تخریب داده Merge نکن.

### PR پیشنهادی Frontend: `HEMO-ST02B-UI-CONTRACT`
- [ ] مدل واحد `ApiError`/`ProblemDetails` و نمایش `traceId` بدون اطلاعات حساس را تعریف کن.
- [ ] HttpClient interceptor خطا و Handler وضعیت‌های 401/403/409/422 را با تست معرفی کن.
- [ ] Componentهای مشترک فرم، جدول، Loading/Empty/Error؛ RTL و تاریخ شمسی فقط در UI.
- [ ] DTOهای فعلی Session/RoleGrant را با خروجی واقعی Backend همخوان نگه دار.
- [ ] برای مراحل بالینی هنوز فرم ذخیره واقعی/Mock اضافه نکن.
- [ ] Frontend smoke/build و تست browser برای Login/Logout با نمونه SQL پس از فراهم‌شدن محیط.

### معیار خروج ST-02B
- [ ] مستند رسمی `schema version` و نقش DDL/migration مشخص است.
- [ ] اجرای baseline فقط روی دیتابیس خالی اجازه دارد و CI دستکاری ناخواسته را رد می‌کند.
- [ ] قراردادهای Identity و org در SQL/EF و API یکسان هستند.
- [ ] دو PR و CI مربوط به **آخرین Commit SHA** سبز؛ نتیجه در [Journal](PROJECT_JOURNAL_FA.md) ثبت شده.

## بخش ۲ — ST-03: ایمن‌سازی پیش از اولین API بیمار
### PR پیشنهادی Backend: `HEMO-ST03-ORG-RBAC`
1. org.Facility / Department / Bed و تخصیص scoped roles به API معرفی شود.
2. سطح دسترسی بر اساس **Scope منابع**، نه صرفاً وجود نقش و منوی Angular تعیین شود.
3. QueryFilter/Specification برای فهرست‌ها و authorize(resource) برای جزئیات اعمال شود؛ فقط `TenantId` یا `FacilityId` داخل DTO کاربر معیار اعتبار نیست.
4. تست‌ها: کاربر بخش A از مرکز ۱، کاربر بخش B از مرکز ۱ و کاربر مرکز ۲؛ تغییر ID، search filter، report، export و duplicate resource را پوشش بده.
5. جلوگیری از mass assignment؛ optimistic concurrency برای UserRole و حساب.
6. Audit موفق/ناموفق ورود، تخصیص نقش، ویرایش و دسترسی حساس؛ حذف یا پنهان‌سازی داده محرمانه از log.
7. تصمیم درباره lockout، session revocation، password reset/forced change، MFA و DataProtection key persistence ثبت شود.

### PR پیشنهادی Frontend: `HEMO-ST03-ACCESS-UI`
1. صفحه مدیریت کاربران، نمایش role grants فعلی، مرکز/بخش، غیرفعالسازی و صف تغییر رمز با permission سرور.
2. منوی نمایش نقش + permission و نمایش خطاهای 403/Session expired؛ هیچ گاردی جای API permission را نمی‌گیرد.
3. تست با دو حساب مستقل و چند Scope: مشاهده، ویرایش مجاز و جلوگیری از نمایش داده خارج Scope.
4. Sidebar و Profile و Dashboard فارسی RTL مطابق نمونه بدون وابستگی به Foundation.

### معیار خروج ST-03
- [ ] Cross-facility/department access tests سبز؛ کنترل‌های 401/403 و session rollback/revocation درست‌اند.
- [ ] Seed نقش‌ها/Permissionها و first admin تکرارپذیر و امن‌اند.
- [ ] لاگ عملیاتی و امنیتی بدون اطلاعات پزشکی حساس.
- [ ] مرور امنیتی مستقل یا دستی مستند شده؛ وضعیت موانع با علامت BLOCKED باقی نمانده.

## بخش ۳ — پس از تأیید: ST-04 اولین Slice بالینی
ترتیب: `Patient` → `Identifier` → `Encounter` → `EncounterLocation` → `Wristband` → `IdentityVerification` → Frontend pages → Integration/E2E.  
Acceptance: ثبت بیمار و بستری بعد از Refresh باقی بماند؛ کاربر خارج از مرکز نتواند همان پرونده را با تغییر ID مشاهده کند؛ مغایرت هویتی بلاک و Audit شود؛ تاریخ شمسی به UTC اشتباه تبدیل نشود.

**فعلاً شروع ST-04 قبل از بسته‌شدن ST-02B و ST-03 ممنوع است.**
