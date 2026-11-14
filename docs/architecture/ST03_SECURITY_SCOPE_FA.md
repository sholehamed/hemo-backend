# Hemo ST-03 | امنیت سازمانی و مدیریت حساب
- `ResourceAccessService.CanAsync(user, permission, facility, department)` Scope واقعی سازمان را بررسی می‌کند. Grant مرکز دسترسی تمام بخش‌های همان مرکز را دارد؛ Grant بخش فقط همان بخش؛ Global تنها برای نقش SYSTEM_ADMIN.
- پنل مدیر: ایجاد Facility/Department/Bed و کاربر، اعطای نقش در Facility/Department، غیرفعال کردن حساب. Roleهای عادی حتماً Facility دارند و نمی‌توانند بدون Scope ایجاد شوند.
- بانک نشست‌ها از جدول موجود sec.UserSession استفاده می‌کند: Cookie فقط `hemo.session` امضاشده دارد، اعتبار و RevokedAt در هر درخواست به SQL مراجعه می‌شود. خروج و غیرفعالسازی کاربر نشست‌ها را باطل می‌کند.
- برای عملیات mutating از Cookie/CSRF سرور استفاده می‌شود. Audit فقط شناسه نقش/عملیات/مرکز را ثبت می‌کند، نه داده بیمار و رمز.
- صفحه منو فقط برای UX است؛ تمام APIهای حساس باید خودشان permission و scope را بررسی کنند.
- Security gaps before production: MFA, password-change-required at first login, lockout persistence, central DataProtection keys, penetration testing, HTTPS proxy hardening. User creation currently requires admin-supplied strong initial password; do not transmit it outside an approved secure channel.
- Test scenarios: Admin global, ADMISSION با بخش محدود و بخش دیگر، مرکز دیگر، session revocation و illegal grants.
- Clinical endpoints must check `Encounter.FacilityId` AND live `EncounterLocation.DepartmentId` to prevent IDOR.
