# Hemo | دستورالعمل ثابت اجرای توسعه و جلوگیری از قاطی شدن مراحل

## قوانین غیرقابل‌تغییر بدون ADR
1. **یک مرجع مرکزی:** نقشه راه و وضعیت پروژه در hemo-backend/docs. Frontend فقط مستندات UI/قرارداد مصرف و لینک به مرجع دارد.
2. **دو ریپو، یک Feature ID:** نمونه شناسه `HEMO-ST04-PATIENT-CREATE`؛ PRها، تست‌ها، DTOها و جدول صفحات با همین شناسه ارجاع داده شوند.
3. **توسعه Vertical Slice:** هر Feature شامل DB (اگر لازم است)، دامنه، API، امنیت، UI و تست است. بدون API واقعی دکمه «ثبت موفق» نشان داده نشود.
4. **Main همیشه سبز:** هیچ PR بدون CI موفق و مرور تغییرات Merge نشود؛ بعد از Merge نیز اجرای Main بررسی شود.
5. **DDL حساس:** اسکریپت 88 جدولی مرجع نسخه شروع است، **نه Migration قابل تکرار**. هیچ EnsureCreated یا SQL DDL مخفی در Startup اجرا نشود.
6. **داده بیمار:** هیچ بیمار واقعی یا رمز و Secrets در ریپو Public، خروجی log یا Fixture ثابت قرار نگیرد.
7. **SOP:** هیچ الگوریتم بالینی و استثنا/تأیید بالینی بدون شواهد و مصوبه به‌عنوان «درست» تلقی نشود.
8. **جداسازی پروژه:** به Foundation وابستگی ایجاد نشود؛ قطعات reusable بعد از بلوغ ارزیابی می‌شوند.

## چرخه هر قابلیت از ابتدا تا انتها
1. **بررسی قبل از کد:** نام فاز، Page IDs، actor، داده ورودی/خروجی، وضعیت و تصمیم باز را در Issue/PR مشخص کن.
2. **Design contract:** مدل DB (و migration)، DTO/endpoint/OpenAPI، Permission/Scope، وضعیت‌ها و error contract را بنویس.
3. **ایجاد Branch:** مستقل در هر ریپو، نمونه `feat/st04-admission-create`، پایه Main آخرین Commit تأییدشده.
4. **Backend:** Domain entities، EF config، Application use case، Validation، Authorization و Audit؛ همه تراکنش‌های حیاتی Atomic.
5. **Frontend:** انتقال UI از hemo-raw به Angular Standalone با components مشترک؛ type-safe client، loading/error/empty states، Jalali در نمایش/ورودی.
6. **آزمون:** unit، API integration+SQL، negative authorization، concurrency و browser E2E برای مسیر حساس؛ پوشش نسخه‌های تکراری در ماتریس.
7. **مستند:** اصلاح ROADMAP/CURRENT_STATUS/FEATURE_TRACEABILITY و ADR در صورت تغییر تصمیم؛ اسکرین‌شات/نمونه فقط بدون PHI.
8. **PR Review:** توضیح تست، commit SHA، schema migration، impacts و rollback؛ تفکیک «واقعاً تست شده» از «پیاده شده ولی تست نشده».
9. **Merge:** فقط بعد از موفقیت CI آخرین SHA و تأیید review؛ اگر Backend/Frontend وابستگی دارند، ادغام با feature flag یا API backward compatibility.
10. **Verify main:** اجرای main CI در هر دو ریپو؛ ثبت نتیجه نهایی در Journal و تعیین گام بعد؛ بدون وعده انجام خودکار در آینده.

## Definition of Ready
- [ ] صفحه/جریان و نقش‌ها شناسایی شده‌اند.
- [ ] Data owner و API contract نوشته شده.
- [ ] مجوز و Facility/Department scope معین است.
- [ ] قوانین باز بالینی برای پیاده‌سازی این Feature تأیید شده‌اند.
- [ ] Rollback/migration approach واضح است.

## Definition of Done
- [ ] DTO و API و UI متصل به DB/سرویس مشترک‌اند؛ داده آرایه Mock مبنای عملیات نیست.
- [ ] تست happy path و شکست مجوز/Concurrency/داده نامعتبر سبز است.
- [ ] تغییرات SQL Migration کنترل‌شده و در SQL CI اجرا می‌شوند.
- [ ] Audit/TraceId و فیلترینگ PHI اعمال شده.
- [ ] Manual/Browser smoke برای UX اصلی ثبت شده یا عدم انجام شفاف و Blocked است.
- [ ] صفحات جدول traceability با Feature ID و نتیجه تست مشخص شده‌اند.
- [ ] README/ADR/Journal/Status به‌روز است.
- [ ] آخرین PR SHA تست سبز دارد و Merge شده؛ Main CI سبز است.

## قالب ثابت گزارش به کاربر بعد از هر مرحله
**مرحله/شناسه**، **تغییر Backend**، **تغییر Frontend**، **تغییر SQL**، **تست اجراشده با نتیجه**، **PR و Merge SHA**، **موارد باز و ریسک‌ها**، **گام بعد**. اگر کاری انجام نشده، با عبارت «انجام نشده» نوشته شود.

## روند جابجایی گفتگو یا شروع جلسه تازه
1. فقط `docs/README.md` و `management/CURRENT_STATUS_FA.md` را باز کن.
2. `management/NEXT_ITERATION_FA.md` را بخوان و آخرین Merge/CI را از GitHub تأیید کن.
3. قبل از کار، Decision/Risk و آخرین Feature Traceability را بخوان.
4. مرحله فعال را فقط پس از تکمیل Definition of Ready ادامه بده.
