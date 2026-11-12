# Hemo | دفتر تصمیم‌ها، پرسش‌ها و ریسک‌ها
**Last review:** 2026-11-12 · هر تصمیم باید با `ADR` یا نتیجه جلسه ثبت شود. «پیشنهادی» به معنای «تأییدشده» نیست.

## تصمیم‌های تثبیت‌شده
| ID | تصمیم | دلیل | شاهد |
|---|---|---|---|
| DEC-001 | Hemo کاملاً مستقل از Foundation است | استقلال و سادگی توسعه | PR #1 هر دو ریپو |
| DEC-002 | Backend: .NET 10 Modular Monolith چهارلایه، Frontend: Angular 22 Standalone، SQL Server | یک برنامه و داده مشترک | ADR-0001 |
| DEC-003 | هویت از جداول `sec.*` مرجع Hemo و Cookie BFF بدون Token در LocalStorage | عدم ساخت دو سیستم نقش موازی | ADR-0002 |
| DEC-004 | بخش بالینی باید Vertical Slice همراه Frontend + Backend + DB اجرا شود | جلوگیری از صفحه نمایشی بدون جریان واقعی | Roadmap |
| DEC-005 | Baseline SQL فقط برای DB خالی؛ Schema migration بعدی نسخه‌بندی شود | محافظت داده | DATABASE_CONTRACT |
| DEC-006 | 62 صفحه پروتوتایپ «منبع UI/نیازمندی» هستند، نه کد Production | Mock و Alertها رفتار واقعی نیستند | سند مرجع صفحات |

## تصمیم‌های باز با اولویت
| ID | اولویت | پرسش | گیت |
|---|---|---|---|
| OPEN-01 | P0 | سیاست تأیید هویت، پذیرش بیمار مجهول‌الهویه، ادغام رکورد تکراری، ساخت بارکد؟ | قبل ST-04 |
| OPEN-02 | P0 | «SUPERVISOR» سرپرست بانک خون است یا سوپروایزر بالینی؟ آیا دو نقش جدا می‌خواهیم؟ | قبل ST-03/ST-07 |
| OPEN-03 | P0 | حد مجاز دسترسی Facility/Department، پرستار کشیک/پزشک معالج و escalation؟ | قبل ST-03 |
| OPEN-04 | P0 | SOP Crossmatch، گروه خون، استثناهای اضطراری، تأیید دوم و صدور ناسازگار؟ | قبل ST-06 |
| OPEN-05 | P0 | پروتکل پایش تزریق، توقف/Resume، شدت واکنش و مسیر اعلام سریع؟ | قبل ST-08/ST-09 |
| OPEN-06 | P0 | ذخیره/امضای قانونی، Audit/Retention و سیاست نگهداری PHI و Backup؟ | قبل هر استقرار واقعی |
| OPEN-07 | P1 | قرارداد واقعی HIS/HL7، شناسه مرکز، Retry و Conflict Resolution؟ | قبل ST-12 |
| OPEN-08 | P1 | گردش مرجوعی، قرنطینه، انتقال بین بیمارستانی، دما و قابلیت Restock؟ | قبل ST-07 |
| OPEN-09 | P1 | چه کسی تنظیمات استاندارد/آستانه دما و هشدار را تصویب و ویرایش می‌کند؟ | قبل ST-10 |
| OPEN-10 | P1 | ثبت آمار رسمی، فرمت خروجی سازمان انتقال خون و زمان ارسال آن؟ | قبل ST-12 |
| OPEN-11 | P1 | سیاست تغییر رمز اجباری، قفل ورود، MFA، ابطال نشست و Reset Password چیست؟ | قبل ST-03 |
| OPEN-12 | P1 | آیا ریپوهای عمومی فعلی باید Private شوند؟ | قبل افزودن هر محتوای محرمانه |

## دفتر ریسک
| ID | شدت | ریسک | پیشگیری / تست کنترل |
|---|---|---|---|
| R-01 | بحرانی | دیده‌شدن پرونده مرکز دیگر با تغییر ID | Resource-based authorization + SQL Integration tests با دو Facility |
| R-02 | بحرانی | ثبت/صدور/رزرو همزمان یک BloodUnit دو بار | Transaction + Unique filtered index + Concurrency stress |
| R-03 | بحرانی | برداشت قوانین پزشکی از UI نمایشی | SOP مصوب و امضای متخصص پیش از پیاده‌سازی/Go-Live |
| R-04 | بحرانی | نشت PHI، Password یا HIS Payload از Log/Repo | Redaction، Secret store، audit data-access، امنیت export |
| R-05 | بالا | تفاوت مدل EF Core و 88 جدول SQL | Contract tests و Migration drift check پیش از ST-04 |
| R-06 | بالا | عدم امکان revoke نشست‌ها و lockout فعلی | ST-03 Session ledger + Rate limiting + lockout + review |
| R-07 | بالا | عدم وجود تست E2E مرورگر برای ورود واقعی و بیمار | Playwright با داده مصنوعی + تست مرورگر + SQL |
| R-08 | بالا | نسخه‌های HTML تکراری و وضعیت‌های متفاوت | Canonical feature map قبل انتقال UI |
| R-09 | بالا | از دست‌رفتن traceability بین دو ریپو | Feature ID مشترک و Update Journal در هر PR |
| R-10 | بالا | خرابی داده هنگام اجرای دوباره SQL baseline | DB-empty preflight و جلوگیری از اجرای خودکار |
| R-11 | متوسط | گزارش‌ها با داده‌های صفحات مختلف ناسازگار شوند | Read model از جداول مرجع و reconciliation tests |
| R-12 | متوسط | تاریخ جلالی/UTC باعث اشتباه در گزارش‌ها شود | UTC storage + Jalali UI + boundary/date tests |

## معیار رفع یک مورد
1. Owner/تصمیم‌گیر مشخص شود (فنی، امنیت یا کارشناس بالینی).
2. گزینه‌ها و اثر بر API/Database/Frontend نوشته شود.
3. تصمیم نهایی با تاریخ و شماره ADR ثبت شود.
4. Test acceptance و PR مرتبط به این جدول لینک شوند.
5. پس از تأیید شواهد، وضعیت به Resolved تغییر کند. بدون مدرک حل‌شده علامت نخورد.
