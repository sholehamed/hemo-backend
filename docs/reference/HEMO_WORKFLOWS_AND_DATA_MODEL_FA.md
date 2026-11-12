# سند جامع تحلیل فرایندها و مدل داده سامانه Hemo
**نسخه:** 1.0 | **تاریخ بررسی:** ۱۴۰۵/۰۸/۰۸ (2026-10-30) | **منبع:** `sholehamed/hemo-raw`, شاخه `main`, کامیت `5ebfc2e1101491d1162853a449ce947c7cc01941`
**هدف:** استخراج معماری داده و گردش عملیات کل ۶۲ صفحه HTML در قالب یک سامانه با سطح دسترسی مبتنی بر نقش و پایگاه‌داده واحد SQL Server. این سند مبنای فنی اولیه است، نه دستورالعمل پزشکی تأییدشده.

## ۱. روش بررسی و حدود اطمینان
کد منبع تمام ۶۲ فایل HTML به‌همراه `assets/menu.js` بررسی شد؛ عنوان‌ها، منوها، فرم‌ها، توابع JavaScript، آرایه‌ها، مسیرهای صفحات، پیام‌های ثبت و وضعیت‌ها استخراج شدند. مرور عملی مرورگری، تست End-to-End، و تأیید فرایندها توسط مسئولان بالینی در این مرحله انجام نشده است.
- **مشاهده‌شده:** صفحه، کنترل و کدی که مستقیماً در سورس وجود دارد. **شبیه‌سازی‌شده:** Alert/Toast یا آرایه داخل فایل که پایدار یا بین نقش‌ها مشترک نیست. **پیشنهادی:** قواعد دامنه، جداول و وضعیت‌های مناسب محصول واقعی.
- عبارت‌های SQL Server، HIS و ثبت موفق در بعضی پیام‌های UI، وجود اتصال حقیقی به دیتابیس/سامانه بیرونی را اثبات نمی‌کنند.
- نسخه‌های تکراری باید پس از مقایسه رابط و سناریوها به یک صفحه مرجع نگاشت شوند؛ این فایل‌ها به‌عنوان زیرسیستم مستقل شمرده نشده‌اند.

## ۲. ساختار واحد سامانه و حوزه‌ها
| حوزه | تعداد HTML | نقش اولیه | کانون داده |
|---|---:|---|---|
| پذیرش و هویت بیمار (Admission) | 8 | `ADMISSION` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| پرستاری (Nurse) | 9 | `NURSE` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| پزشک (Doctor) | 8 | `DOCTOR` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| کارشناس بانک خون (Blood Bank) | 7 | `BLOOD_TRANSFUSION_EXPERT` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| سوپروایزر و لجستیک خون (Supervisor) | 13 | `SUPERVISOR` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| تجهیزات پزشکی (Equipment) | 5 | `BIOMED_ENGINEER` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| هموویژیلانس، کیفیت و آموزش (HV) | 8 | `HV_MANAGER` | داده مشترک بیمار، کیسه، رویداد و سازمان |
| ورود و صفحات جایگزین | 4 | `COMMON` | مشترک/ورود |
**جمع: ۶۲ صفحه**؛ ماژول‌های فوق نمای UI هستند و مالکیت دیتابیس به حوزه‌های موضوعی مستقل تعلق دارد؛ جداول نباید برای هر صفحه جداگانه تکثیر شوند.

## ۳. نقش‌ها، محدوده اطلاعات و مسئولیت
| کد نقش | مسئولیت | کنترل دسترسی/ابهام |
|---|---|---|
| `ADMISSION` | پذیرش، شناسه/مچ‌بند، سابقه، ترخیص، HIS | ایجاد و اصلاح رکورد بیمار به تفکیک مرکز؛ نه تأیید Crossmatch |
| `NURSE` | درخواست، بیماران بخش، علائم، تزریق، واکنش اولیه | تنها بر اساس بخش/نوبت/بیمار مجاز؛ نه تأیید سازگاری |
| `DOCTOR` | سفارش و تأیید، مشاهده پرونده، تشخیص واکنش | رعایت پزشک معالج/کشیک و دامنه مرکز |
| `BLOOD_TRANSFUSION_EXPERT` | آزمایش و ذخیره نتایج، صدور، دما، موجودی | الزامات جداسازی انجام‌دهنده از تأییدکننده در موارد حساس |
| `SUPERVISOR` | تأیید بانک، لجستیک، مرجوعی، امحا، QC و انقضا | عنوان نقش در پروتوتایپ گاهی «سوپروایزر بالینی» و گاهی «سرپرست بانک خون» است؛ باید تفکیک نهایی شود |
| `BIOMED_ENGINEER` | شناسنامه تجهیزات، PM، کالیبراسیون، دما و خرابی | به اطلاعات حساس بیمار فقط در حد لازم |
| `HV_MANAGER` | واکنش، RCA، کمیته، ممیزی، آموزش، گزارش و تنظیمات | گزارش و نظارت بالینی؛ ویرایش درمان/آزمایش فقط با مجوز خاص |
| `SYSTEM_ADMIN` | مدیریت کاربران، نقش و زیرساخت | افزوده در معماری؛ در انتخاب نقش پروتوتایپ وجود ندارد |
کاربر می‌تواند هم‌زمان چند نقش داشته باشد. نسبت نقش به مرکز و بخش در `sec.UserRole` نگهداری می‌شود. دسترسی باید هم در API و هم در محدودسازی Query کنترل شود؛ پنهان‌کردن منو کافی نیست.

## ۴. شناسنامه تمامی ۶۲ صفحه و اتصال آن‌ها به داده
### پذیرش و هویت بیمار (Admission)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `admission-dashboard.html` | داشبورد وضعیت پذیرش، میانبر پذیرش سریع، استعلام و وضعیت شناسه‌ها | Encounter, Patient, IdentityVerification | نمایشی/ایستا |
| `admission-new-patient.html` | ایجاد مشخصات، بیمار مجهول‌الهویه، پیش‌نمایش مچ‌بند؛ استعلام و ثبت نهایی نمایشی | Patient, PatientIdentifier, Encounter, EncounterLocation, Wristband | ثبت ساختگی |
| `admission-identity-check.html` | جست‌وجو با شناسه/بارکد، تطبیق دو شناسه و تأیید یا اعلام مغایرت | Wristband, IdentityVerification, ClinicalTask | ثبت ساختگی |
| `admission-patient-list.html` | فیلتر بستری‌ها، رهگیری وضعیت، چاپ مچ‌بند و خروجی | Encounter, EncounterLocation, Wristband, IdentityVerification | اطلاعات ثابت |
| `admission-blood-history.html` | استعلام سوابق تزریق، واکنش و هشدارها، همگام‌سازی نمایشی | HistoricalTransfusion, AdverseEvent, Message | استعلام ساختگی |
| `admission-discharge-clearance.html` | انتخاب بیمار، چک‌لیست هویت و مدارک تزریق/واکنش، پیش‌نویس و تأیید ترخیص | DischargeReview, DischargeReviewItem, Encounter | پیش‌نویس/ترخیص ساختگی |
| `admission-his-sync.html` | صف تبادل HIS، لاگ HL7/API، Retry، Test و همگام‌سازی دستی | Endpoint, Message, MessageAttempt, SyncJob | شبیه‌سازی اتصال |
| `admission-stats.html` | آمار پذیرش، پوشش مچ‌بند، گروه خون و پرونده‌های ترخیص | ReportRun و Queryهای تحلیلی | داشبورد نمایشی |

### پرستاری (Nurse)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `nurse-dashboard.html` | وظایف امروز، درخواست‌های اخیر و دسترسی به ثبت علائم | ClinicalTask, BloodRequest, VitalObservation | داشبورد نمایشی |
| `nurse-patients.html` | پرونده بیماران بر اساس بخش و وضعیت فعال/بحرانی/پایان‌یافته | Patient, Encounter, EncounterLocation | فیلتر روی داده نمایشی |
| `nurse-request.html` | ویزارد درخواست انواع فرآورده با فیلد شرطی، سابقه بیمار و اعتبارسنجی | BloodRequest, BloodRequestItem, RequestClinicalData | فرم تعاملی با بیمار ساختگی |
| `nurse-tasks.html` | فیلتر کارتابل، ثبت علائم در مودال و شروع تزریق | ClinicalTask, Transfusion, VitalObservation | بخشاً نمایشی |
| `nurse-tasks - Copy.html` | نسخه جایگزین کارتابل با وضعیت‌های متفاوت | همان ClinicalTask | نسخه تکراری؛ نیاز به تعیین مرجع |
| `nurse-vitals.html` | انتخاب بیمار/نوبت پایش، ثبت فشار/نبض/دما/SpO2/تنفس و هشدار لحظه‌ای | Transfusion, VitalObservation, AlertThreshold | فرم با منطق سمت مرورگر |
| `nurse-transfusion-form.html` | فرم RBC/FFP، مشخصات واحد و بیمار، جدول زمان‌بندی پایش، تأیید و پایان تزریق | Transfusion, TransfusionChecklist, VitalObservation, TransfusionSignoff | منطق UI؛ ذخیره سراسری تأیید نشده |
| `nurse-reaction.html` | ثبت واکنش با علائم، شدت، توقف/ازسرگیری، اقدامات و نمونه‌های آزمایشگاهی | AdverseEvent, EventSymptom, TransfusionAction, EventInvestigation | اعتبارسنجی سمت مرورگر |
| `nurse-stats.html` | آمار درخواست‌ها، توزیع فرآورده‌ها، وضعیت‌ها و روند ماهانه | BloodRequest, BloodRequestItem, ReportRun | گزارش نمایشی |

### پزشک (Doctor)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `doctor-dashboard.html` | نیازمند تأیید، واکنش‌ها، بیماران تحت تزریق و شاخص‌ها | BloodRequest, RequestApproval, AdverseEvent | داشبورد نمایشی |
| `doctor-approval.html` | کارتابل فرم‌های ناقص، پرکردن فیلدهای شرطی و ثبت تکمیل | RequestApproval, RequestClinicalData, ClinicalTask | تغییر وضعیت DOM/Toast |
| `doctor-orders.html` | فرم‌های چندگانه سفارش فرآورده، PAD و درخواست‌های خاص | BloodRequest, BloodRequestItem, RequestClinicalData | در برخی سناریوها صرفاً alert |
| `doctor-patients.html` | فهرست بیمار، فیلتر و جزئیات پرونده بالینی | Encounter, Transfusion, ClinicalNote | نمونه نمایشی |
| `doctor-transfusion-record.html` | مشاهده سابقه تزریق، تغییر تب و ثبت یادداشت/برنامه درمان | Transfusion, VitalObservation, ClinicalNote | داده UI/شبیه‌سازی |
| `doctor-monitoring.html` | پایش بالینی، علائم، دارو/مداخله، پیش‌نویس و ارجاع اضطراری | Transfusion, VitalObservation, TransfusionAction, ClinicalTask | ثبت و ارجاع ساختگی |
| `doctor-reactions.html` | تشخیص/شدت/علیت/پیامد و تصمیم پزشک درباره واکنش گزارش‌شده | PhysicianEventAssessment, AdverseEvent | ثبت ساختگی |
| `doctor-quarterly.html` | روند فصلی، KPI، مقایسه فراورده، خلاصه/امضا و ارسال گزارش | ReportDefinition, ReportRun, ReportSubmission | نمودار با داده نمونه |

### کارشناس بانک خون (Blood Bank)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `bloodbank-dashboard.html` | نمای موجودی، صف درخواست، Crossmatch سریع و وضعیت دما | BloodUnit, BloodRequest, Crossmatch, TemperatureReading | نمایشی |
| `bloodbank-requests.html` | صف درخواست با فیلتر، ثبت نتیجه Crossmatch، صدور واحد و رد درخواست | BloodRequest, Crossmatch, UnitIssue, InventoryMovement | آرایه مستقل در JS |
| `bloodbank-crossmatch.html` | نتایج ABO/Rh و غربالگری/آنتی‌بادی، کنترل مغایرت و تست سازگاری، تاریخچه | Crossmatch, CrossmatchTest, BloodUnit, BloodGroup | تعامل واقعی در مرورگر؛ بدون DB |
| `bloodbank-inventory.html` | ثبت/ویرایش/دریافت و جست‌وجوی موجودی، انقضا و CSV | BloodUnit, StockReceipt, StockReceiptLine, InventoryMovement | آرایه درون صفحه |
| `bloodbank-thawing.html` | صف ذوب، ثبت روش و دما، شروع/پایان و تحویل فرآورده | ThawProcess, BloodUnit, BloodRequestItem | رفتار نمایشی |
| `bloodbank-temperature.html` | وضعیت تجهیزات، فرم ثبت نوبت، رخداد و فیلتر تاریخچه دما | TemperatureReading, TemperatureExcursion, Equipment | ثبت محلی/نمایشی |
| `bloodbank-reports.html` | گزارش خروجی، واکنش، موجودی و دمای اخیر | ReportRun و Viewهای تحلیلی | صفحه عمدتاً ایستا |

### سوپروایزر و لجستیک خون (Supervisor)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `supervisor-dashboard.html` | پنل مرکزی: تأیید درخواست، آفرزیس، سفارش از سازمان، رسید، بحران، مصرف، نزدیک‌انقضا | RequestApproval, StockOrder, StockReceipt, InterfacilityTransfer, UnitReservation | چندین مودال عملیاتی؛ ثبت اغلب نمایشی |
| `supervisor-crossmatch.html` | صف Crossmatch فعال، فیلتر، شروع، بازبینی، تحویل و جزئیات | Crossmatch, CrossmatchTest, UnitIssue, Dispatch | داده و وضعیت مستقل در صفحه |
| `supervisor-crossmatch1.html` | نمای دیگری برای مدیریت Crossmatch | همان Crossmatch | نسخه تکراری |
| `supervisor-inventory.html` | موجودی تفکیکی RBC، FFP، پلاکت، Cryo و تاریخچه تراکنش | BloodUnit, InventoryMovement, ProductType | داشبورد ایستا |
| `supervisor-receive.html` | حواله تحویل، بررسی تطبیق، تحویل گیرنده، بازگشت و Audit Trail | UnitIssue, Dispatch, DispatchItem, UnitReturn | داده نمونه تعاملی |
| `supervisor-dispatch.html` | ایجاد ارسال، شرایط حمل/دما، انتقال و تحویل به بخش | Dispatch, DispatchItem, UnitIssue | آرایه JS با وضعیت داخلی |
| `supervisor-return.html` | ثبت مرجوعی، وضعیت کیسه، تصمیم بازگشت به موجودی یا امحا، تأیید نهایی | UnitReturn, UnitWaste, InventoryMovement | داده داخلی صفحه |
| `supervisor-expiry.html` | FEFO، تخصیص فوریتی، قرنطینه و ثبت اسقاط | BloodUnit, UnitReservation, UnitWaste, InventoryMovement | تعامل UI؛ وضعیت‌های حساس نیازمند Backend |
| `supervisor-qc.html` | QC معرف/تجهیز، انحراف، اصلاح، امضای شیفت | DailyQualityControl, QualityControlResult, CorrectiveAction | ثبت نمایشی |
| `supervisor-temperature.html` | کنترل و تأیید زنجیره سرما، مداخله و امضای شیفت | TemperatureReading, TemperatureExcursion, DutyShift | بازبینی ساختگی |
| `supervisor-waste.html` | فرم رسمی هدررفت، علل و آمار اسقاط | UnitWaste, UnitReturn | فرم نمونه |
| `supervisor-stats.html` | بیلان مصرف، توزیع بخش، تحلیل مصرف و خروجی ماهانه | InventoryMovement, Transfusion, ReportRun | تحلیل نمایشی |
| `supervisor-committee.html` | گزارش کمیته و MSBOS، مصوبات و واکنش‌ها | CommitteeMeeting, MeetingDecision, AdverseEvent | گزارش نمایشی |

### تجهیزات پزشکی (Equipment)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `equipment-dashboard.html` | نمایش آنلاین زنجیره سرما، هشدار و سرویس‌های لازم | Equipment, TemperatureReading, TemperatureExcursion | داشبورد نمایشی |
| `equipment-inventory-list.html` | شناسنامه و اموال دستگاه، ثبت، آرشیو و فیلتر تجهیزات | Equipment, EquipmentCategory | آرایه JS در صفحه |
| `equipment-temperature-log.html` | ثبت نوبت دما، حداقل/حداکثر و کنترل زنجیره سرد | TemperatureReading, AlertThreshold | ثبت با پیام ساختگی |
| `equipment-maintenance-calibration.html` | PM، کالیبراسیون، نتایج، گواهی و برچسب بعدی | MaintenancePlan, MaintenanceRecord | فرم با ذخیره ساختگی |
| `equipment-incident-report.html` | خرابی دستگاه، اولویت، ثبت/پیگیری تیکت و خروجی | EquipmentIncident, EquipmentIncidentAction | ثبت نمایشی |

### هموویژیلانس، کیفیت و آموزش (HV)
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `hemovigilance-dashboard.html` | کارتابل واکنش، آمار، کمیته، ممیزی، آموزش، گزارش | AdverseEvent, RcaCase, ClinicalAudit, TrainingSession | داشبورد ایستا |
| `hemovigilance-reactions.html` | فهرست و فرم رسمی واکنش نامطلوب، گزارش و فیلتر | AdverseEvent, PhysicianEventAssessment | ثبت با پیام نمایشی |
| `hemovigilance-rca.html` | تحلیل علت ریشه‌ای وقایع و ثبت اقدامات اصلاحی CAPA | RcaCase, RcaCause, CorrectiveAction | ثبت نمایشی |
| `hemovigilance-committee.html` | جلسات و صورت‌جلسه کمیته و پیگیری مصوبات | CommitteeMeeting, MeetingAttendee, MeetingDecision | ثبت نمایشی |
| `hemovigilance-audit.html` | ممیزی بخش، فرم سؤال پویا و نتایج ارزیابی | ClinicalAudit, AuditQuestion, AuditAnswer | امتیاز/ثبت در UI |
| `hemovigilance-training.html` | تقویم دوره، حضور، آزمون، مستندات و پوشش آموزشی | TrainingCourse, TrainingSession, TrainingEnrollment, TrainingExamResult, DocumentLibrary | مدیریت نمایشی |
| `hemovigilance-reports.html` | تولید، آرشیو و ارسال خروجی استاندارد | ReportDefinition, ReportRun, ReportSubmission | تولید و پیش‌نمایش ساختگی |
| `hemovigilance-settings.html` | تنظیم مرکز، حد هشدار، HIS/SMS، کاربران و نگهداری | Facility, Setting, AlertThreshold, Endpoint, AccountUser | ذخیره با alert |

### ورود و صفحات جایگزین
| صفحه | آنچه در کد دیده می‌شود | جداول/حوزه موردنیاز | وضعیت فعلی |
|---|---|---|---|
| `login.html` | ورود نمایشی با انتخاب نقش، ذخیره userRole/userName در localStorage و هدایت به داشبورد | AccountUser, Role, UserRole, UserSession | احراز هویت واقعی ندارد |
| `crossmatch-updated.html` | نسخه جایگزین bloodbank-crossmatch با عملکرد بسیار مشابه | Crossmatch, CrossmatchTest | نسخه تکراری؛ مقایسه و انتخاب |
| `artifact (20).html` | نسخه کپی/ابتدایی داشبورد مسئول هموویژیلانس | همان داشبورد HV | تکراری |
| `artifact (23).html` | فرم درخواست کوچک‌تر با تأیید و مرحله موفقیت | BloodRequest, BloodRequestItem | نمونه جایگزین |

## ۵. گردش‌های کاری انتها‌به‌انتها (مدل پیشنهادی)
### ۵.1 ورود و کنترل دامنه دسترسی
کاربر → اعتبارسنجی هویت → استخراج UserRole در سطح Facility/Department → دریافت Permissionها → ورود به داشبورد نقش → ثبت Session و Audit؛ منبع نقش نباید localStorage باشد.
**رد داده:** `AccountUser, Role, Permission, RolePermission, UserRole, UserSession, AuditEvent`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.2 پذیرش، پرونده و تطبیق هویت
جست‌وجوی بیمار با شناسه‌ها → ایجاد یا بازیابی Patient → ایجاد Encounter مستقل از Patient → تخصیص بخش/تخت با تاریخچه → چاپ Wristband → دو شناسه و تأیید/مغایرت → اعلام Task در صورت مغایرت.
**رد داده:** `Patient, PatientIdentifier, Encounter, EncounterLocation, Wristband, IdentityVerification, ClinicalTask`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.3 استعلام سوابق قبلی و اتصال به HIS
جست‌وجوی سوابق محلی → استعلام سامانه خارجی در صورت اتصال → ثبت CorrelationId و وضعیت هر Message → ثبت سوابق واردشده بدون جعل رابطه تزریق داخلی → نمایش هشدار برای بیمار.
**رد داده:** `HistoricalTransfusion, Endpoint, Message, MessageAttempt, SyncJob`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.4 ثبت و بررسی درخواست فرآورده
پرستار یا پزشک درخواست Draft ایجاد می‌کند → اقلام RequestItem و سابقه/اندیکاسیون تکمیل می‌شود → Submitted → در صورت نیاز AwaitingDoctor → Approval/Reject/Returned → AwaitingBank. تعداد واحد باید مثبت و نوع محصول معتبر باشد.
**رد داده:** `BloodRequest, BloodRequestItem, RequestClinicalData, RequestApproval, ClinicalTask`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.5 بانک خون و آزمون سازگاری
بانک خون درخواست Approved را می‌بیند → موجودی Available و تاریخ انقضا بررسی می‌شود → واحد مناسب رزرو می‌شود → آزمایش ABO/Rh، غربالگری و CrossmatchTest ثبت می‌گردد → نتیجه Compatible/Incompatible/Discrepancy → فقط با تأیید مجاز به صدور می‌رسد. قواعد پزشکی/استثناهای اورژانس نیازمند SOP مصوب هستند.
**رد داده:** `BloodUnit, UnitReservation, Crossmatch, CrossmatchTest, UnitIssue`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.6 تأمین موجودی از سازمان و رسیدگیری
سرپرست StockOrder به تأمین‌کننده ثبت می‌کند → StockReceipt و ReceiptLine با بررسی تعداد، برچسب، دما و مغایرت → تأیید دریافت → ایجاد BloodUnit با شناسه یکتا → InventoryMovement → Available/Quarantined. ثبت فیزیکی هر کیسه از یک ردیف دقیق مستقل استفاده می‌کند.
**رد داده:** `StockOrder, StockOrderLine, StockReceipt, StockReceiptLine, BloodUnit, InventoryMovement`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.7 مدیریت موجودی و زنجیره سرما
پایش FEFO و هشدار نزدیک انقضا؛ ثبت Reading از کاربر/سنسور؛ وقوع خروج از محدوده → TemperatureExcursion و تکلیف قرنطینه واحدهای مرتبط → اصلاح و تأیید سوپروایزر.
**رد داده:** `BloodUnit, TemperatureReading, TemperatureExcursion, AlertThreshold, Equipment, InventoryMovement`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.8 ذوب، ارسال و تحویل
برای محصول نیازمند ذوب → ThawProcess و نتیجه؛ صدور UnitIssue پس از کنترل مجاز بودن → Dispatch/DispatchItem و ثبت دما/ناقل → Delivered و ثبت گیرنده → رسید بخش. میان Dispatch و Issue باید هر کیسه فقط یک حواله فعال داشته باشد.
**رد داده:** `ThawProcess, UnitIssue, Dispatch, DispatchItem, InventoryMovement`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.9 تزریق و علائم حیاتی
تأیید تطبیق بیمار/کیسه و چک‌لیست قبل از تزریق → Transfusion Planned → InProgress → Observation در نوبت‌های زمانی → Completed/Stopped → امضای پرستار/پزشک → بستن Task؛ مشخصات کیسه باید از BloodUnit فراخوانی شود، نه تایپ دوباره.
**رد داده:** `Transfusion, TransfusionChecklist, VitalObservation, TransfusionAction, TransfusionSignoff`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.10 واکنش نامطلوب و ارجاع بالینی
گزارش پرستار به Transfusion موجود → ثبت علائم و شدت و اقدام فوری → DoctorReview و تشخیص پزشک → Investigation/Confirmed → تصمیم/گزارش مسئول HV → RCA در صورت نیاز → CAPA و پیگیری.
**رد داده:** `AdverseEvent, EventSymptom, PhysicianEventAssessment, EventInvestigation, RcaCase, CorrectiveAction`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.11 مرجوعی، قرنطینه، هدررفت و انتقال بین مراکز
مرجوعی وابسته به Issue و Unit → ثبت شرایط و دما → تصمیم Restock/Quarantine/Waste به‌صورت اتمیک → Movement؛ برای انتقال اضطراری بین مراکز Transfer و TransferItem و رسید مقصد ثبت می‌شود.
**رد داده:** `UnitReturn, UnitWaste, BloodUnit, InterfacilityTransfer, TransferItem, InventoryMovement`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.12 QC، تجهیزات و کالیبراسیون
ثبت شناسنامه Equipment → دوره PM/Calibration → ثبت نتایج و گواهی → کنترل کیفی روزانه معرف/تجهیز و امضای مسئول → ثبت حادثه/خرابی → اقدام اصلاحی و تأیید.
**رد داده:** `Equipment, MaintenancePlan, MaintenanceRecord, EquipmentIncident, EquipmentIncidentAction, DailyQualityControl, QualityControlResult`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.13 ممیزی، آموزش و کمیته
ممیزی و پرسش‌های استاندارد → پاسخ/امتیاز/نقص → CAPA؛ دوره و جلسه آموزشی → شرکت‌کننده، حضور و آزمون؛ جلسه BTC → صورت‌جلسه، حاضران و مصوبات قابل پیگیری.
**رد داده:** `ClinicalAudit, AuditQuestion, AuditAnswer, CorrectiveAction, TrainingCourse, TrainingSession, TrainingEnrollment, TrainingExamResult, CommitteeMeeting, MeetingDecision`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.14 ترخیص و بستن پرونده
کنترل هویت، مستندات تزریق، وضعیت واکنش و اسناد → DischargeReview Draft/Blocked/Cleared → به‌روزرسانی Encounter پس از مجوز → پیام خروجی HIS و ثبت نتیجه همگام‌سازی.
**رد داده:** `DischargeReview, DischargeReviewItem, Encounter, Message`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

### ۵.15 گزارش‌های چندنقشی و گزارش ارسالی
داشبوردها از Query/Read Model مشترک → انتخاب بازه و نقش → ReportRun با فیلتر قابل رهگیری → فایل و آرشیو → ReportSubmission و رسید مرجع؛ آمار بر اساس رخداد واقعی محاسبه می‌شود.
**رد داده:** `ReportDefinition, ReportRun, ReportSubmission, AuditEvent`. ترتیب‌ها و کنترل‌های مربوط به ایمنی بیمار باید در محیط واقعی و با پروتکل مصوب نهایی شوند.

## ۶. ماشین وضعیت‌ها و قوانین انتقال
| موجودیت | گردش و قاعده انتقال پیشنهادی |
|---|---|
| `BloodRequest` | Draft → Submitted → AwaitingDoctor/ AwaitingBank → Approved → Crossmatch → Ready → PartiallyIssued/Issued → Completed؛ Reject/Cancel مسیر نهایی کنترل‌شده است. |
| `BloodUnit` | PendingReceipt → Available → Reserved/Crossmatch → Issued/InTransit → Transfused؛ مسیر جایگزین Returned → Available/Quarantined/Wasted؛ Expired و Wasted ترمینال‌اند. |
| `Crossmatch` | Draft → PendingReview → Final؛ CompatibilityResult مستقل: Pending / Compatible / Incompatible / Discrepancy / NotApplicable. |
| `UnitReservation` | Active → Released/Consumed؛ در هر لحظه فقط یک رزرو Active به ازای هر کیسه. |
| `Dispatch` | Pending → Preparing → InTransit → Delivered؛ Return/Cancel باید با تراکنش موجودی همزمان شود. |
| `Transfusion` | Planned → InProgress → Completed یا Stopped؛ عملیات Resume با Action ثبت شود و پایان/بازگشایی بدون سیاست مجاز نباشد. |
| `AdverseEvent` | Reported → DoctorReview → Investigation → Confirmed → Closed؛ Cancelled برای گزارش خطا و با Audit. |
| `TemperatureExcursion` | Open → InProgress → Resolved → Closed؛ امضای نظارتی و تأثیر روی موجودی لازم است. |
| `UnitReturn` | Pending → Approved/Rejected همراه با تصمیم Restock/Quarantine/Waste. |
| `ClinicalAudit` | Draft → Submitted → Approved → Closed. |
**مهم:** CHECKهای SQL فقط مقادیر مجاز را کنترل می‌کنند؛ مجازبودن عبور از یک وضعیت به وضعیت دیگر با هماهنگی چند رکورد باید در Service/Transaction و ثبت رخداد تضمین شود.

## ۷. مدل منطقی ارتباط‌های مهم
- Patient (1) ← Encounter (N)؛ Encounter (1) ← BloodRequest (N)؛ BloodRequest (1) ← BloodRequestItem (N).
- BloodRequest (1) ← RequestApproval (N) و RequestClinicalData (0..1)؛ هر قلم از ProductType (N:1) استفاده می‌کند.
- BloodUnit (1) ← Crossmatch (N)؛ Crossmatch (1) ← CrossmatchTest (N)؛ BloodUnit (1) ← UnitReservation (N تاریخچه؛ حداکثر یک Active).
- BloodUnit (1) ← UnitIssue (N تاریخچه)؛ UnitIssue (1) ← DispatchItem (0..1)؛ Dispatch (1) ← DispatchItem (N).
- Transfusion به Encounter، BloodRequestItem و BloodUnit متصل است و VitalObservation، Checklist، Signoff و Action زیرمجموعه آن هستند.
- AdverseEvent به Transfusion وابسته است؛ PhysicianEventAssessment و EventInvestigation به آن وابسته‌اند؛ RcaCase به Event یا حادثه دستگاه متصل است.
- Equipment (1) ← TemperatureReading, TemperatureExcursion, MaintenancePlan, MaintenanceRecord, EquipmentIncident (N).
- Facility (1) ← Department (N) ← Bed (N)، و UserRole محدوده مرکز و بخش را نگهداری می‌کند.
- CommitteeMeeting ← MeetingDecision / MeetingAttendee؛ ClinicalAudit ← AuditAnswer؛ TrainingCourse ← TrainingSession ← TrainingEnrollment ← TrainingExamResult.
- Endpoint ← Message ← MessageAttempt؛ ReportDefinition ← ReportRun ← ReportSubmission؛ AuditEvent تراکنش‌های حساس را ثبت می‌کند.

## ۸. طراحی فیزیکی SQL Server
خروجی SQL، یک دیتابیس `Hemo` با ۱۱ Schema و ۸۸ جدول، ۲۰۳ رابطه FK و ۲۵۹ ایندکس (Unique/Nonclustered) ایجاد می‌کند. کلیدهای اصلی BIGINT IDENTITY هستند؛ برای رکوردهای قابل ویرایش از RowVersion استفاده می‌شود. حذف آبشاری برای اطلاعات بالینی فعال نشده است.
### ۸.۱. فهرست کامل جداول هر Schema
**`org` (4 جدول):** `Facility`, `Department`, `Bed`, `DutyShift`.
**`sec` (6 جدول):** `AccountUser`, `Role`, `Permission`, `RolePermission`, `UserRole`, `UserSession`.
**`patient` (9 جدول):** `Patient`, `PatientIdentifier`, `Encounter`, `EncounterLocation`, `Wristband`, `IdentityVerification`, `HistoricalTransfusion`, `DischargeReview`, `DischargeReviewItem`.
**`clinical` (11 جدول):** `BloodRequest`, `RequestClinicalData`, `BloodRequestItem`, `RequestApproval`, `ClinicalTask`, `ClinicalNote`, `Transfusion`, `TransfusionChecklist`, `VitalObservation`, `TransfusionAction`, `TransfusionSignoff`.
**`blood` (22 جدول):** `ProductType`, `BloodGroup`, `Supplier`, `StockOrder`, `StockOrderLine`, `StockReceipt`, `BloodUnit`, `StockReceiptLine`, `UnitReservation`, `Crossmatch`, `CrossmatchTest`, `UnitIssue`, `Dispatch`, `DispatchItem`, `UnitReturn`, `UnitWaste`, `ThawProcess`, `InterfacilityTransfer`, `TransferItem`, `InventoryMovement`, `DailyQualityControl`, `QualityControlResult`.
**`equip` (8 جدول):** `EquipmentCategory`, `Equipment`, `MaintenancePlan`, `MaintenanceRecord`, `TemperatureReading`, `TemperatureExcursion`, `EquipmentIncident`, `EquipmentIncidentAction`.
**`hv` (18 جدول):** `AdverseEvent`, `EventSymptom`, `PhysicianEventAssessment`, `EventInvestigation`, `RcaCase`, `RcaCause`, `CorrectiveAction`, `CommitteeMeeting`, `MeetingAttendee`, `MeetingDecision`, `AuditQuestion`, `ClinicalAudit`, `AuditAnswer`, `TrainingCourse`, `TrainingSession`, `TrainingEnrollment`, `TrainingExamResult`, `DocumentLibrary`.
**`integration` (4 جدول):** `Endpoint`, `Message`, `MessageAttempt`, `SyncJob`.
**`report` (3 جدول):** `ReportDefinition`, `ReportRun`, `ReportSubmission`.
**`cfg` (2 جدول):** `Setting`, `AlertThreshold`.
**`audit` (1 جدول):** `AuditEvent`.
### ۸.۲. کلیدها، ایندکس‌ها و محدودیت‌های اصلی
- تمام PKها در هر جدول `PRIMARY KEY CLUSTERED` هستند؛ ۲۰۳ FK واقعی با `WITH CHECK` پس از ایجاد جداول اضافه می‌شود.
- کلیدهای یکتا: UserName، Role/Permission Code، NationalId غیرتهی، Identifier محلی/سراسری، HisEncounterNo هر مرکز، Wristband Barcode، RequestNo، UnitCode، IssueNo، EventNo و کدهای اسناد.
- ایندکس یکتا و Filtered از دو مچ‌بند Active برای یک بستری، و از دو رزرو Active هم‌زمان برای یک واحد خون جلوگیری می‌کند.
- ایندکس FEFO برای موجودی Available و غیرقرنطینه، ایندکس صف درخواست‌ها و وظایف فعال، و ایندکس رخداد دمای حل‌نشده ایجاد می‌شود.
- CHECKها وضعیت‌های معتبر، مقادیر مثبت، زمان‌بندی‌ها، حدهای اندازه‌گیری و JSON معتبر را پوشش می‌دهند.
- تاریخ‌ها در دیتابیس میلادی و ساعت‌ها UTC با DATETIME2(3) هستند؛ نمایش تاریخ جلالی در UI انجام شود.
- رمز عبور صرفاً Hash، Refresh Token صرفاً Hash و اسرار اتصال فقط Secret Reference نگهداری می‌شوند؛ Payload HIS در مخزن امن بیرونی با کلید نگهداری ارجاع داده می‌شود.

## ۹. الزامات تراکنشی که صرفاً FK تضمین نمی‌کند
- Patient/Encounter/Department/Facility/BloodRequest/Crossmatch باید در یک محدوده سازمانی معتبر باشند؛ برای نقش‌های بخشی از مجوز سازمانی و محدودسازی داده استفاده شود.
- برای Reserve/Issue/Return/Transfer/Transfusion از Transaction و Lock مناسب (یا RowVersion + Retry) استفاده شود؛ عدم وجود دو عملیات مصرف روی یک کیسه صرفاً با وضعیت ستون قابل ضمانت نیست.
- صدور فرآورده بدون نتیجه مجاز Crossmatch/آزمایش‌ها و بدون سیاست استثنای تاییدشده ممنوع شود؛ قواعد سازگاری را بدون تصویب تخصصی حدس نزنید.
- هر Movement دقیقاً همراه با تغییر Status و CurrentFacilityId انجام شود و در AuditEvent ثبت گردد. برای تاریخچه واقعی، ویرایش مستقیم رویدادها ممنوع باشد.
- تطبیق Encounter و RequestItem و BloodUnit و Issue در StartTransfusion بررسی شود؛ وضعیت درخواست و موجودی بر اساس سرجمع اقلام هماهنگ گردد.
- عودت به موجودی تنها پس از ارزیابی شرایط/دما/زمان حمل مجاز است؛ تصمیم Restock/Quarantine/Waste باید در یک تراکنش ثبت شود.
- آلارم‌های دمایی باید بر اساس کالیبراسیون/آستانه هر دستگاه و آخرین بازه اطلاعات بررسی و به وظیفه یا حادثه متصل شوند.
- گزارش‌های بیمارمحور و فعالیت کارکنان نیازمند Data Access Logging، حداقل‌سازی اطلاعات، نسخه پشتیبان رمزنگاری‌شده و Retention Policy هستند.

## ۱۰. شکاف‌های قطعی پروتوتایپ و تصمیم‌های باز
- انتخاب نقش در login.html، Authentication نیست؛ به ASP.NET Core Identity/پروتکل سازمانی و RBAC تبدیل شود.
- آرایه‌های REQUESTS، PATIENTS، inventory، dispatches و returnRecords میان صفحه‌ها منبع داده مشترک نیستند؛ باید API و بانک داده مشترک داشته باشند.
- گزارش‌های موفقیت SQL/HIS و ثبت ترخیص/کالیبراسیون/پزشک در چند صفحه صرفاً Alert هستند.
- `supervisor-crossmatch.html` / `supervisor-crossmatch1.html` و `bloodbank-crossmatch.html` / `crossmatch-updated.html` و `nurse-tasks - Copy.html` و دو artifact باید با هم مقایسه و نسخه مرجع انتخاب شوند.
- «SUPERVISOR» در صفحه ورود به سوپروایزر بالینی اشاره می‌کند ولی صفحات عمدتاً سرپرست بانک خون هستند؛ نیازمند تصمیم نقش و مسئولیت.
- فهرست نام دقیق همه نمونه‌های خون و حداقل تست‌های الزامی هر گروه محصول از SOP مرکز/استاندارد مصوب استخراج شود؛ در حال حاضر فقط فرم‌های UI شاهد هستند.
- ماهیت استعلام HIS، شناسه پرونده، سازوکار بیمار مجهول‌الهویه و الگوریتم همگام‌سازی نیازمند قرارداد API/HL7 واقعی است.
- شماره پرونده ممکن است بین مراکز تکرار شود؛ طرح کلید یکتا برای مرکز و HIS در جداول لحاظ شده است.
- چک‌لیست پیش از تزریق، شرط توقف و از سرگیری، امضا، احراز هویت لحظه تحویل و تأیید ثانویه باید توسط کمیته بالینی تأیید شود.
- پایداری داده‌های نمونه، سازگاری بین نقش‌ها و تمام تغییرات وضعیت باید در تست‌های E2E مبتنی بر دیتابیس کنترل شوند.

## ۱۱. ترتیب توسعه پیشنهادی
- **گام ۱: زیرساخت:** SQL Script + Migration Strategy + Auth/RBAC + Facility/Department + Patient/Encounter و Audit.
- **گام ۲: جریان عملیاتی اصلی:** Request/Approval → Stock/Reservation → Crossmatch → Issue/Dispatch → Transfusion → Reaction؛ تست پایان‌به‌پایان بین نقش‌ها.
- **گام ۳: واحد خون و تجهیزات:** StockOrder/Receipt/Transfer/Return/Waste/FEFO، QC، Reading/Alarm/PM/Calibration.
- **گام ۴: حاکمیت و یکپارچگی:** RCA/CAPA، ممیزی، کمیته، آموزش، گزارش، HIS/HL7، خروجی و انطباق.
- **گام ۵: مهاجرت UI:** انتقال صفحات به Angular Standalone و Signals، Component مشترک، Route Guard، فرم Reactive، تاریخ شمسی و طراحی RTL؛ عدم ایجاد API مجزا برای هر صفحه.

## ۱۲. آزمون‌های پذیرش حداقلی
- پرستار درخواست می‌سازد، پزشک تأیید می‌کند و بانک خون همان RequestNo را می‌بیند.
- یک واحد رزروشده برای Request A نباید هم‌زمان برای Request B رزرو/صادر شود.
- درخواست با Crossmatch ناسازگار، بدون مجوز استثنای مصوب نباید قابل صدور شود.
- فرآورده تحویل‌شده در سابقه UnitIssue، Dispatch، InventoryMovement و Transfusion به همان UnitCode متصل باشد.
- ثبت واکنش توسط پرستار فوراً در کارتابل پزشک و مسئول HV نمایش داده شود.
- بعد از مرجوعی، موجودی و وضعیت کیسه و گزارش ضایعات ناسازگار نشود.
- سازمان A نباید بتواند پرونده بیماران سازمان B را با تغییر URL/API مشاهده کند.
- حذف یا تغییر غیرمجاز تاریخچه واکنش/موجودی ثبت و مسدود شود؛ Retention و بازیابی آزمایش گردد.
- تاریخ شمسی UI با UTC ذخیره‌شده و خروجی گزارش همخوان باشد.
- پیام ناموفق HIS قابل Retry با CorrelationId ثابت و کنترل Idempotency باشد.

## ۱۳. راهنمای اجرای Script
۱) SQL Server 2019 یا 2022 با دسترسی CREATE DATABASE؛ ۲) اجرای فایل `Hemo_Database_SQLServer_2022.sql` در SSMS یا sqlcmd با پشتیبانی `GO`؛ ۳) بررسی تعداد جدول‌ها و FKها؛ ۴) اجرای Seed سازمان، بخش‌ها، کاربران دارای PasswordHash واقعی؛ ۵) اعمال Migrationهای نسخه‌های بعد و تست فرایندی قبل از استفاده با اطلاعات بیمار واقعی.
**تذکر:** اسکریپت برای دیتابیس خالی است، نه Migration قابل اجرای مجدد یا ارتقای سامانه موجود. اجرای آن روی دیتابیس غیرخالی یا در محیط عملیاتی بدون Backup توصیه نمی‌شود. جداول و Indexها به‌صورت استاتیک کنترل شده‌اند؛ اجرای واقعی در SQL Server در این محیط امکان‌پذیر نبوده است.

## ۱۴. منابع تحلیل
- مخزن خصوصی GitHub: `https://github.com/sholehamed/hemo-raw` (دسترسی از اتصال مجاز GitHub).
- بازبینی ساختار `main` به همراه `assets/menu.js` و فایل‌های HTML فوق؛ شرح رفتار، مشتق از سورس و نام توابع است، نه شاهد عملیات واقعی دیتابیس.
- SQL: `Hemo_Database_SQLServer_2022.sql`؛ مدل ماشینی موقت: `hemo_schema_model.json`.