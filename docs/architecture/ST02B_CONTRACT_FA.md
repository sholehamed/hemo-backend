# ST-02B | تطبیق مدل داده و محدوده اسکریپت
- مرجع اولیه: `database/reference/Hemo_Database_SQLServer_2022.sql`.
- ۱۱ Schema / ۸۸ Table / ۲۰۳ FK / ۲۵۹ Index غیرخوشه‌ای در SQL موقت CI بررسی می‌شود.
- موجودیت‌های فعلی sec.AccountUser, Role, Permission, RolePermission, UserRole و org.Facility, Department, Bed با EF Core نگاشت شده‌اند.
- تمام جدول‌ها فعلاً `ExcludeFromMigrations` هستند؛ اجرای SQL اولیه فقط روی دیتابیس خالی مجاز است؛ پس از تثبیت نسخه یک، migrationهای تکمیلی به‌صورت جداگانه اعمال می‌شوند.
- مقدارهای Code و Scope از SQL Server گرفته می‌شوند؛ برای Hemo Identity موازی یا Seed کاربر با رمز ثابت تولید نمی‌شود.
- اعتبار و تثبیت نسخه‌بندی Migrationهای ارتقایی، مدیریت Session و فرم‌های بالینی در مرحله‌های بعد هستند.
- اطلاعات بیمار یا رمز واقعی وارد CI و Git نمی‌شود.
