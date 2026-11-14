# Hemo | مرکز رسمی مستندات پروژه
> نسخه مرجع مستندات: **1.1** — 2026-11-14  
> مالک مستندات فنی: مخزن **hemo-backend**، پوشه **docs/**  
> Frontend و Backend دو ریپو مجزا هستند، ولی **یک محصول و یک نقشه راه** دارند.

## از کجا شروع کنم؟
1. [وضعیت واقعی امروز](management/CURRENT_STATUS_FA.md) — چه چیزی ساخته، تست، و ادغام شده و چه چیزی هنوز آماده نیست.
2. [نقشه راه کل پروژه](management/ROADMAP_FA.md) — مراحل 00 تا 13، خروجی هر مرحله و معیار خروج.
3. [دستور کار دقیق گام بعد](management/NEXT_ITERATION_FA.md) — اولین PRها و ریزکارهای ترتیب‌دار.
4. [روش اجرای مراحل و ادغام](management/EXECUTION_PLAYBOOK_FA.md) — قرارداد کار بین دو ریپو، CI و Definition of Done.
5. [جدول پوشش همه صفحات](management/FEATURE_TRACEABILITY_FA.md) — 62 صفحه و فاز مالک هر صفحه.
6. [ریسک‌ها و تصمیم‌های باز](management/DECISIONS_RISKS_FA.md) — ابهامات فنی/بالینی، تصمیم‌گیر و گیت پذیرش.
7. [قرارداد دیتابیس و مهاجرت‌ها](architecture/DATABASE_CONTRACT_FA.md) — 88 جدول، مرز DDL و EF Core، اصول تغییر Schema.
8. [مرز ماژول‌ها و امنیت داده](architecture/MODULE_MAP_FA.md) — مالکیت داده و API و مسیر ارتباط نقش‌ها.
9. [تست و معیارهای پذیرش](quality/QUALITY_AND_TEST_MATRIX_FA.md) — تست‌های لازم هر جریان و سناریوهای پرریسک.
10. [تاریخچه و انتقال کار بین گفتگوها](management/PROJECT_JOURNAL_FA.md) — تصمیم‌ها، Mergeها، نتیجه CI و دستور ادامه کار.
11. [شواهد تکمیل فنی ST-04](management/ST04_COMPLETION_EVIDENCE_FA.md) — PRهای ادغام‌شده، تست SQL و Chromium و گیت‌های بالینی باقی‌مانده.

## اسناد مرجع اولیه
- [تحلیل همه ۶۲ صفحه و ۱۵ گردش کاری (نسخه آرشیوی 2026-10-30)](reference/HEMO_WORKFLOWS_AND_DATA_MODEL_FA.md): شاهد استخراج‌شده از **hemo-raw**؛ *رفتار پروتوتایپ را با رفتار عملیاتی اشتباه نگیریم*. بخشی از توضیحات وضعیت CI/اجرای SQL در این نسخه تاریخی قدیمی است؛ وضعیت جاری در **CURRENT_STATUS_FA.md** ثبت می‌شود.
- [اسکریپت فیزیکی SQL Server 2022](../database/reference/Hemo_Database_SQLServer_2022.sql): Baseline اولیه، مخصوص دیتابیس خالی، نه Migration ارتقا.
- [ADR-0001 — معماری مستقل](ADR-0001-architecture.md) و [ADR-0002 — هویت و BFF](ADR-0002-authentication.md).
- [قبولی مرحله ۱](STAGE1-ACCEPTANCE.md) و [مرحله ۲A](STAGE2A-ACCEPTANCE.md).

## ریپوها و قواعد مالکیت
| ریپو | مسئولیت | مرجع |
|---|---|---|
| [hemo-raw](https://github.com/sholehamed/hemo-raw) | منبع اولیه HTML/CSS/JS، نه کد تولیدی | فقط خواندن و تطبیق؛ بدون انتقال Mock به محصول |
| [hemo-backend](https://github.com/sholehamed/hemo-backend) | Domain، Application، Infrastructure، API، SQL reference، معماری و **نقشه راه واحد** | همین docs/README.md |
| [hemo-frontend](https://github.com/sholehamed/hemo-frontend) | Angular، UI، route guards، API client، تست UI | docs/PROJECT_HANDOFF_FA.md به همین مرکز لینک می‌دهد |

**پروتکل تحویل:** هر تغییر عملکردی باید در همان PR به وضعیت Roadmap، جدول صفحات، تست‌ها و Journal مرتبط اشاره کند؛ Backend و Frontend یک شناسه مرحله/قابلیت مشترک دارند؛ تمام ادعاها باید بر مبنای CI/تست قابل مشاهده باشند.

## چه چیزهایی در این نسخه «کامل» نیست؟
- احراز هویت پایه با SQL Server در CI تست شده، ولی MFA، قفل حساب، ابطال متمرکز همه Sessionها، ممیزی امنیتی و تفکیک دسترسی داده بالینی بر اساس Facility/Department **تکمیل نشده‌اند**.
- ساخت کامل SQL روی SQL Server موقت CI موفق بوده، اما **تأیید بالینی، استقرار واقعی، کارایی زیر بار، پشتیبان‌گیری و تمرین بازیابی** انجام نشده است.
- هنوز هیچ جریان بالینی مثل ثبت بیمار، درخواست خون، کراس‌مچ و تزریق به API واقعی متصل نشده است.
- ریپوهای Backend و Frontend در بررسی تاریخ 2026-11-12 **Public** بودند؛ هیچ داده واقعی بیمار، Secret یا اطلاعات اتصال در Git ذخیره نشود.
