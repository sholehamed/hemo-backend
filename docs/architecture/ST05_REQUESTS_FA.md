# ST-05 | درخواست واحد خون با گردش مشترک
**وضعیت:** در حال پیاده‌سازی فنی؛ سیاست‌های پزشکی و UAT مرکز تأیید نشده‌اند.

- nurse/doctor دارای Role و Scope واقعی می‌تواند بعد از **Verified** شدن هویت، برای Encounter بستری همان بخش درخواست فرآورده ثبت کند.
- RequestNo مشترک و Item و ClinicalData و ClinicalTask پزشک در یک تراکنش Serializable ساخته می‌شوند.
- وضعیت آغازین `AwaitingDoctor` است. فقط پزشک دارای نقش و Scope همان بخش و متفاوت از درخواست‌دهنده می‌تواند `Approved/Rejected/Returned` ثبت کند؛ تصمیم و actor در RequestApproval و audit ثبت می‌شود.
- `Approved` به مفهوم تأیید درخواست است، **نه تأیید سازگاری یا مجوز صدور واحد**. هیچ استنتاج پزشکی از گروه خونی انجام نشده است.
- `RowVersion` برای جلوگیری از بازبینی هم‌زمان استفاده می‌شود. API فقط روی Encounter.FacilityId و EncounterLocation فعال فیلتر می‌کند؛ IDOR بین مراکز/بخش‌ها ممنوع است.
- API: `GET /api/v1/facilities/{f}/departments/{d}/blood-requests/{products|,/{id}}`, `POST /...`, `POST /{id}/review`.
- تمام تاریخ‌های ذخیره UTC هستند. UI شمسی فقط قالب نمایش است.
- قبل از استفاده بالینی: شرایط اورژانس، رد درخواست، محصول و مقدار، هویت، امضای پزشکی و SOP اختصاصی مرکز باید تأیید شوند.
