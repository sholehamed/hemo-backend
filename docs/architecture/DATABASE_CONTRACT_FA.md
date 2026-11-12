# Hemo | قرارداد دیتابیس SQL Server و چرخه تغییرات
**Source of truth for physical baseline:** [Hemo_Database_SQLServer_2022.sql](../../database/reference/Hemo_Database_SQLServer_2022.sql) (۸۸ جدول / ۱۱ Schema طبق تحلیل اولیه).  
**Scope:** یک SQL Server واحد برای همه نقش‌ها و فرایندها؛ Schemaها مرز موضوعی هستند، نه دیتابیس مستقل.

## ۱. مدل فیزیکی و مالکیت داده
| Schema | نقش اصلی | موجودیت‌های نمونه |
|---|---|---|
| `org` | سازمان و بخش | Facility, Department, Bed, DutyShift |
| `sec` | حساب و مجوز | AccountUser, Role, Permission, RolePermission, UserRole, UserSession |
| `patient` | پرونده/بستری | Patient, Identifier, Encounter, Wristband, IdentityVerification, DischargeReview |
| `clinical` | درخواست و تزریق | BloodRequest, RequestItem, Approval, Task, Transfusion, Vitals, Signoff |
| `blood` | واحد فرآورده و گردش انبار | BloodUnit, StockReceipt, Reservation, Crossmatch, Issue, Dispatch, Return, Waste, Movement |
| `equip` | تجهیزات و دما | Equipment, Maintenance, TemperatureReading, Excursion, Incident |
| `hv` | واکنش/کیفیت/آموزش | AdverseEvent, RCA, CorrectiveAction, Audit, Committee, Training |
| `integration` | HIS و پیام | Endpoint, Message, MessageAttempt, SyncJob |
| `report` | خروجی گزارش | ReportDefinition, ReportRun, Submission |
| `cfg` | پارامتر | Setting, AlertThreshold |
| `audit` | رهگیری تغییرات | AuditEvent |

## ۲. وضعیت مستند، نه فرض‌شده
- GitHub Actions در ST-02A روی SQL Server 2022 موقت، اسکریپت کامل را اجرا کرده و Login/Permission/Logout را با DB واقعی تست کرده است.
- هنوز همه ۸۸ جدول به EF Core نگاشت نشده‌اند؛ فعلاً پنج جدول sec.* در `HemoDbContext` نگاشت شده و `ExcludeFromMigrations` دارند.
- `sec.UserSession` در Baseline هست ولی نشست عملیاتی فعلاً Cookie است و ledger/revocation کامل اجرا نشده.
- هیچ Migration کامل بالینی، سنجش query plan زیر بار واقعی، قواعد تاییدشده Crossmatch، یا استقرار production وجود ندارد.
- متن تحلیل قبلی که می‌گفت «SQL اجرا نشده» فقط وضعیت تاریخی آن سند است؛ گزارش به‌روز اینجاست.

## ۳. سیاست تغییر Schema
1. اجرای اسکریپت اولیه صرفاً روی یک **Database خالی** با کنترل preflight؛ ممنوعیت `EnsureCreated` و اجرای مجدد script در Startup.
2. پس از تثبیت Baseline، تغییرات بعدی با EF Core Migration یا SQL migration نسخه‌بندی‌شده (با مالک مشخص) و تست روی همان SQL Server انجام شود؛ **هیچ DDL دوتایی/هم‌پوشان**.
3. `DatabaseVersion` / migration ledger رسمی قبل از اولین ارتقای DB ساخته شود؛ تغییر destructive نیازمند ADR، backup+rollback.
4. Seed مرجع (Role, Permission, ProductType, BloodGroup, Facilities نمونه) باید idempotent و نسخه‌بندی‌شده باشد؛ credential و بیمار واقعی Seed نشود.
5. همه زمان‌ها UTC/Gregorian در DB؛ ورودی/خروجی جلالی و ارقام فارسی فقط در Angular/DTO validation کنترل می‌شوند.
6. Primary key BIGINT identity، RowVersion concurrency، FK واقعی، فیلتر ایندکس Active، unique unit code و RequestNo طبق مبنا.
7. داده بالینی/واحد خون Hard Delete نشود مگر سیاست رسمی retention/حریم خصوصی. Audit Event append-only در منطق برنامه.
8. برای هر تغییر DB: Verify Index, metadata, constraints و run migration/rollback test روی دیتابیس Clone‌شده غیرحساس.

## ۴. محافظت از تراکنش‌های حساس
- `BloodUnit` ↔ `UnitReservation`: فقط یک رزرو معتبر در یک زمان؛ SQL unique filtered index + atomic operation + concurrency handling.
- `StockReceipt`/`InventoryMovement`/`Issue`/`Dispatch`/`Return` باید در transaction واحد سازگار بمانند.
- شروع Transfusion باید Patient/Encounter/Unit/Issue/RequestItem و Scope بخش را کنار هم چک کند.
- رویداد واکنش و Assessment نباید فقط از روی PatientName قابل اتصال باشند؛ FKهای واقعی لازم است.
- گزارش‌ها Source-of-truth عملیاتی دارند؛ جدول Summary/Cache بعداً و با قوانین refresh مشخص.

## ۵. تست‌های SQL که باید قبل از ST-04 اضافه شوند
- Baseline DDL روی SQL Server 2022 تازه؛ شمارش 88 table و 203 FK و 259 index با روش شمارش **توضیح‌داده‌شده** (از تغییر تعریف ایندکس جلوگیری کن).
- بررسی موجودیت‌ها و Indexهای unique filtered برای active reservation/identification.
- Seed دوباره بدون duplicate؛ جلوگیری از نام نقش/Permission تکراری؛ ایجاد admin فقط با setup صریح.
- Column/EF mapping contract: max length, unicode, rowversion, computed/default, nullable, FK.
- Migration upgrade/downgrade در دیتابیس Ephemeral و اجرای دوباره Startup بدون DDL ناخواسته.
- دو actor و دو Facility برای تست محدودیت سازمانی قبل از ورود داده بیمار.

## ۶. اتصال و Secrets
- Development LocalDB یا SQL Server container با credential محیطی، prod با login حداقل‌دسترسی و TLS.
- هیچ رمز و ConnectionString واقعی یا داده/شناسه پزشکی در public repository.
- Backup، Restore، Retention، TDE/Encryption و Monitoring کوئری‌ها قبل از production باید آزمایش شوند.
