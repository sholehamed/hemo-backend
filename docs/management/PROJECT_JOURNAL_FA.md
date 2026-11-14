# Hemo | دفتر ثبت پیشرفت و تحویل بین جلسات

## وضعیت فعلی (2026-11-14)
**آخرین مرحله Merge شده:** ST-03. ST-02B PR #4 و ST-03 PR #5 در دو ریپو Merge شده‌اند. **ST-04 PR #6 باز است:** SQL integration 1/1 و Backend 23/23 سبز؛ Frontend Production Build/Smoke سبز؛ E2E واقعی مرورگر و تأیید بالینی باقی‌اند.  
**ریپو اصلی برنامه:** `hemo-backend/docs/README.md`.  
**گفتگو/جلسه تازه باید از:** [CURRENT_STATUS_FA](CURRENT_STATUS_FA.md) و [NEXT_ITERATION_FA](NEXT_ITERATION_FA.md) و [ROADMAP_FA](ROADMAP_FA.md) شروع شود.

## وقایع ثبت‌شده
| تاریخ | اتفاق | Backend | Frontend | نتیجه |
|---|---|---|---|---|
| 2026-10-30 | تحلیل 62 صفحه پروتوتایپ و مدل DB | سند مرجع و SQL baseline اولیه | فهرست UI | تحلیل source؛ بدون browser validation |
| 2026-11-12 | ST-01 bootstrap مستقل | [PR #1](https://github.com/sholehamed/hemo-backend/pull/1) | [PR #1](https://github.com/sholehamed/hemo-frontend/pull/1) | Merge و CI سبز |
| 2026-11-12 | ST-02A BFF/Identity/CSRF | [PR #2](https://github.com/sholehamed/hemo-backend/pull/2) | [PR #2](https://github.com/sholehamed/hemo-frontend/pull/2) | Merge و CI سبز؛ SQL auth E2E سبز |
| 2026-11-12 | تجمیع اسناد، نقشه راه، ماتریس صفحه و چک‌لیست | این PR مستندسازی | PR ارجاع Frontend | فقط مستندات، بدون تغییر قابلیت بالینی |

| 2026-11-14 | ST-02B database contract | [PR #4](https://github.com/sholehamed/hemo-backend/pull/4) | [PR #4](https://github.com/sholehamed/hemo-frontend/pull/4) | Merge، CI و SQL Server سبز |
| 2026-11-14 | ST-03 organization access | [PR #5](https://github.com/sholehamed/hemo-backend/pull/5) | [PR #5](https://github.com/sholehamed/hemo-frontend/pull/5) | Merge؛ SQL تست cross-facility / cross-department / revocation سبز |
| 2026-11-14 | ST-04 admission & identity | [PR #6](https://github.com/sholehamed/hemo-backend/pull/6) | [PR #6](https://github.com/sholehamed/hemo-frontend/pull/6) | در حال بررسی؛ 23 Backend + 1 SQL integration + frontend build/smoke سبز؛ Browser E2E لازم است |

## قالب افزودن رکورد جدید (در انتهای این فایل)
```md
### YYYY-MM-DD — HEMO-STxx-FEATURE
- Scope and reason:
- Backend PR / commit / DB migration:
- Frontend PR / commit / affected raw pages:
- Tests executed (CI run URL + exact pass/fail):
- Open risks / blockers:
- ADR and updated documentation:
- Merge state + next agreed task:
```

## قواعد نگهداری
- تا Build/Integration و مرور امنیتی مرحله‌ای موفق نباشد برچسب DONE صادر نشود.
- هر مورد تعویق‌یافته به [Risk](DECISIONS_RISKS_FA.md) یا [Next iteration](NEXT_ITERATION_FA.md) منتقل شود، نه اینکه از گزارش حذف شود.
- همه زمان‌های وقایع و وعده‌های تحویل بر اساس رویداد واقعی GitHub نوشته شوند؛ بدون ادعای کار در پس‌زمینه.
