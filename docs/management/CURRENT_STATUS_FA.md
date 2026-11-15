# Hemo — وضعیت واقعی و محل ادامه کار
**به‌روزرسانی:** 2026-11-15 · **وضعیت فنی:** ST-01 تا ST-09 (برش‌های اصلی) در هر دو ریپو Merge شده‌اند، با **گیت بالینی/امنیتی انتشار هنوز باز**.

## وضعیت اجرا
| مرحله | Backend | Frontend | توضیح و محدودیت |
|---|---|---|---|
| ST-00 — تحلیل پروتوتایپ | مستند | 62 صفحه/15 جریان بررسی‌شده | تحلیل منبع، نه تصدیق نیازمندی درمانی |
| ST-01 — راه‌اندازی | [PR #1](https://github.com/sholehamed/hemo-backend/pull/1) | [PR #1](https://github.com/sholehamed/hemo-frontend/pull/1) | .NET 10، Angular 22، CI |
| ST-02A — هویت و BFF | [PR #2](https://github.com/sholehamed/hemo-backend/pull/2) | [PR #2](https://github.com/sholehamed/hemo-frontend/pull/2) | Cookie HttpOnly، CSRF، نشست |
| ST-02B — SQL قرارداد داده | [PR #4](https://github.com/sholehamed/hemo-backend/pull/4) | [PR #4](https://github.com/sholehamed/hemo-frontend/pull/4) | Fresh-only baseline؛ upgrade migration مستقل باز |
| ST-03 — سازمان و مجوز | [PR #5](https://github.com/sholehamed/hemo-backend/pull/5) | [PR #5](https://github.com/sholehamed/hemo-frontend/pull/5) | Facility/Department scoped؛ MFA/lockout هنوز باز |
| ST-04 — پذیرش و هویت | [PR #6](https://github.com/sholehamed/hemo-backend/pull/6) | [PR #6](https://github.com/sholehamed/hemo-frontend/pull/6) + refactor [#8](https://github.com/sholehamed/hemo-frontend/pull/8) | Wristband/Identity Verification؛ چاپ اسکنر و SOP باز |
| ST-05 — درخواست و تأیید | [PR #9](https://github.com/sholehamed/hemo-backend/pull/9) | [PR #9](https://github.com/sholehamed/hemo-frontend/pull/9) | درخواست واحد و تأیید مستقل پزشک؛ emergency SOP باز |
| ST-06 — موجودی و سازگاری | [PR #10](https://github.com/sholehamed/hemo-backend/pull/10) | [PR #10](https://github.com/sholehamed/hemo-frontend/pull/10) | رزرو و Crossmatch مبتنی بر شواهد و تأیید مستقل؛ قوانین پزشکی خودکار نیست |
| ST-07 — صدور و تحویل | [PR #11](https://github.com/sholehamed/hemo-backend/pull/11) | [PR #11](https://github.com/sholehamed/hemo-frontend/pull/11) | صدور/حمل/دریافت/قرنطینه/امحا؛ **thaw، انتقال و restock هنوز باز** |
| ST-08 — تزریق و پایش | [PR #12](https://github.com/sholehamed/hemo-backend/pull/12) | [PR #12](https://github.com/sholehamed/hemo-frontend/pull/12) | آماده‌سازی، precheck، baseline/during vitals، start/stop/complete، independent signoff؛ SOP و قوانین بالینی باز |
| ST-09 — واکنش و RCA | [PR #13](https://github.com/sholehamed/hemo-backend/pull/13) | [PR #13](https://github.com/sholehamed/hemo-frontend/pull/13) | گزارش پرستار، physician assessment، HV investigation، RCA/CAPA و تأیید مستقل؛ notification واقعی/گزارش ملی باز |
| ST-10 تا ST-13 | برنامه‌ریزی | برنامه‌ریزی | تجهیزات، QC/آموزش، HIS/گزارش، hardening/release |

## شواهد کیفیت آخرین مرحله
- ST-08 Backend: [PR #12](https://github.com/sholehamed/hemo-backend/pull/12) — CI و SQL Server Integration سبز؛ سناریوی کامل صدور تا ثبت علائم، خاتمه و امضای مستقل با داده ساختگی آزمایش شد.
- ST-08 Frontend: [PR #12](https://github.com/sholehamed/hemo-frontend/pull/12) — Angular Build/Smoke و Chromium E2E سبز پیش از Merge.
- ST-09 Backend: [PR #13](https://github.com/sholehamed/hemo-backend/pull/13) — CI و SQL Server Integration سبز؛ ابتدا آزمون fixture به rate limit ورود برخورد کرد، با پیکربندی آزمایشی 24 (پیش‌فرض production **8** بدون تغییر) اصلاح شد.
- ST-09 Frontend: [PR #13](https://github.com/sholehamed/hemo-frontend/pull/13) — Angular CI سبز، SQL seed و **8/8 Playwright** روی Chromium سبز. Rate-limit آزمون و locator مبهم در E2E اصلاح شدند.
- **سبز بودن CI به معنی مجوز تزریق بیمار واقعی نیست:** قواعد پایش، تأیید بالینی، داده حساس، گزارش ملی، on-call escalation، تست بار/امنیت و UAT همچنان نیازمند تصویب و آزمون جداگانه‌اند.

## دستورکار پس از ST-09
1. [NEXT_ITERATION_FA](NEXT_ITERATION_FA.md) — شروع ST-10 تجهیزات و زنجیره سرد، مشروط به SOP دما/هشدار.
2. تکمیل موارد باقیمانده ST-07 (Thaw، انتقال، Restock طبق SOP) و hardening پیش از هر استقرار.
3. نگهداری سند تصمیم‌ها و traceability؛ هر مرحله باید PRهای جدا و CI سبز قبل از Merge داشته باشد.

**هشدار:** Hemo هنوز برای استفاده درمانی واقعی، داده واقعی بیمار یا استقرار عمومی تأیید نشده است.
