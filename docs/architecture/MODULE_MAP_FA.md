# Hemo | مرزبندی ماژول‌ها، جریان و وابستگی‌ها
**نوع معماری:** Modular Monolith مستقل از Foundation؛ ماژول‌ها مفاهیم دامنه‌اند، نه تعداد صفحات یا نقش‌ها.

## نقشه منطقی
```text
Angular 22 (Persian RTL, same-origin BFF)
   | /api/v1/* (auth cookie + antiforgery on mutations)
ASP.NET Core 10 API (auth/authz, validation, tracing)
   | Application use cases (commands/queries)
   | Domain (patients, clinical, blood, equipment, hv)
   | Infrastructure (EF Core / SQL Server / integration)
SQL Server: org / sec / patient / clinical / blood / equip / hv /
            integration / report / cfg / audit
```

## چه کسی مالک چیست؟
- `org`: مرجع Facility/Department/Bed و محدوده کاربر. همه Queryهای بیمارمحور از این دامنه عبور می‌کنند.
- `sec`: حساب/role/permission و grant scoped؛ Domain پزشکی نباید از claims سمت مرورگر استنتاج کند.
- `patient`: Patient یک شخص؛ Encounter یک بستری مستقل؛ Wristband/Identity Verification زیرمجموعه بستری.
- `clinical`: BloodRequest/RequestItem/Approval/Task و Transfusion؛ RequestNo در همه نقش‌ها مشترک است.
- `blood`: هر `BloodUnit` یک دارایی قابل رهگیری، Reservation/Crossmatch/Issue/Movement/Return واحد؛ موجودی توسط هر پنل دوباره تعریف نمی‌شود.
- `equip`: Equipment/Temperature/PM/Alarm و اثر محدودیت روی BloodUnit؛ وضعیت یخچال در BloodBank/Equipment/Supervisor از یک داده مشترک.
- `hv`: AdverseEvent/Assessment/RCA/Audit/Training/Committee؛ از Transfusion و EquipmentIncident رویدادها را می‌گیرد.
- `integration`: مرز HIS/IBTO/HL7 با Idempotency، Secret References و Retry.
- `report`: read-model خروجی گزارش از جداول موضوعی، نه ثبت مجدد داده مرجع.

## قوانین ارتباط
1. یک بیمار می‌تواند چند بستری؛ یک بستری چند درخواست؛ هر درخواست چند item؛ هر item امکان اتصال چند Unit/Transfusion بر اساس اعتبارسنجی مصوب.
2. درخواست بین پرستار/پزشک/بانک خون باید شناسه ثابت داشته باشد؛ وضعیت از یک Application Service تغییر کند.
3. یک واحد خون باید در هر لحظه دارای وضعیت معتبر یکتا باشد و تغییرهایش در Movement و Audit ثبت شوند.
4. مجوز منو برای UX است. هر Endpoint و Query با UserId + Permission + Facility/Department + resource باید مجدداً authorize شود.
5. در Commands از transaction و idempotency برای داده حساس استفاده شود؛ Queryها صرفاً read-only.
6. Audit بیمارمحور PHI را در لاگ عمومی ذخیره نکند و در گزارش‌ها فیلتر سطح دسترسی مجدد اعمال شود.

## تصمیم‌های غیرقطعی
- تفکیک SUPERVISOR بالینی از Head Blood Bank هنوز در [تصمیم‌ها](../management/DECISIONS_RISKS_FA.md) باز است.
- قوانین حساس سازگاری خون/تأیید دو نفره و مسیر اضطراری باید با SOP رسمی و افراد مجاز نهایی شوند.
- HIS/IBTO واقعی در دسترس/تأیید نیست؛ برای آن API شبیه‌ساز production نسازید.
