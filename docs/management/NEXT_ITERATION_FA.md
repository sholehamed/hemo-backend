# Hemo | دستورکار بعد از ST-04
**آخرین بازبینی:** 2026-11-14. ST-02B و ST-03 در Main ادغام شده‌اند. ST-04 در PR #6 هر دو ریپو Merge و Backend CI / SQL Integration / Frontend CI / Chromium Browser E2E سبز شد. SOP هویت و الزامات امنیت انتشار هنوز بازند.

## ST-04 — بررسی نهایی و موارد حل‌نشده
- [x] ۶ Entity پذیرش به SQL Server Baseline نگاشت شدند.
- [x] API اسکوب‌دار ثبت/ویرایش/فهرست/جزئیات، صدور مچ‌بند و بررسی تطبیق هویت.
- [x] سناریو SQL یکپارچه: حفظ بعد از ثبت، scope بین بخش/مرکز، Barcode، Verified/Mismatch، RowVersion، تکرار کد ملی و اشغال تخت.
- [x] Angular چهار صفحه پذیرش با HttpClient واقعی و تاریخ UI شمسی؛ Build/Smoke سبز.
- [x] **Browser E2E واقعی** با Backend و SQL و داده ساختگی: Login → Admission → Reload → Verify و جست‌وجو؛ Chromium 1/1 موفق. تست‌های دسترسی غیرمجاز SQL جداگانه سبزند.
- [ ] **تأیید بالینی SOP:** سیاست دو شناسه، تشخیص بیمار مجهول‌الهویه، duplicate-person reconciliation، مچ‌بند قابل‌اسکن و قواعد مغایرت.
- [ ] بازبینی security برای اطلاعات محرمانه بیمار؛ تکمیل MFA/lockout/force-password-change و audit جزئی‌تر پیش از بهره‌برداری.
- [ ] بررسی چاپ بارکد فیزیکی/اسکنر و آزمون دسترس‌پذیری؛ متن بارکد فعلی جایگزین استاندارد چاپ بیمارستان نیست.
- [x] Backend Main CI پس از Merge سبز؛ Frontend PR CI/Browser سبز و Main CI بررسی‌شده. این وضعیت باید در هر تغییر جدید دوباره بررسی شود.

## ST-05 — طراحی و پیاده‌سازی درخواست خون
1. تحلیل همه فرم‌های nurse-request، nurse-tasks، doctor-orders/approval و bloodbank-requests؛ یک Request مشترک بین نقش‌ها تعریف شود.
2. طراحی دقیق State Machine با تأیید متخصص بیمارستان و تفکیک Draft/Submitted/DoctorReview/Approved/Rejected، Cancellation و Emergency.
3. نگاشت SQL clinical.BloodRequest / Item / Approval / ClinicalTask به EF و قراردادهای DTO/API.
4. Server-side permission و resource scope روی Encounter/Facility/Department و تقویت audit/TraceId.
5. Angular فرم درخواست، جدول کارتابل پرستار، تأیید پزشک، درخواست‌های بانک خون در Branchهای مستقل Backend/Frontend.
6. CI SQL + Integration تست multi-role، concurrency/idempotency، denied scope و Browser E2E.
7. فقط بعد از سبزشدن تست‌های آخرین Commit PR و مرور تغییرات Merge شود.

**اصل ثابت:** Hemo مستقل از Foundation باقی می‌ماند و هر Feature با شناسه مشترک و تست/مستند Backend+Frontend تحویل می‌شود.
