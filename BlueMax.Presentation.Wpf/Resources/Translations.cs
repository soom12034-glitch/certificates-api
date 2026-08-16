using System;
using System.Collections.Generic;
using System.Reflection;

namespace BlueMax.Presentation.Wpf.Resources;

public static class Translations
{
    public static class Arabic
    {
        // Main Window & Navigation
        public const string Home = "الرئيسية";
        public const string Certificates = "الشهادات";
        public const string Maintenance = "الصيانة";
        public const string Clients = "العملاء";
        public const string Inventory = "المخزون";
        public const string DeviceHistory = "سجل الأجهزة";
        public const string StickerDesigner = "مصمم الملصق";
        public const string ReceiptStickerDesigner = "مصمم استيكر الاستلام";
        public const string Settings = "الإعدادات";
        public const string Rentals = "الإيجارات";
        public const string Search = "بحث";
        public const string Help = "مساعدة";
        public const string Notifications = "الإشعارات";
        public const string Login = "تسجيل الدخول";
        public const string Logout = "تسجيل الخروج";
        public const string Language = "اللغة";
        public const string Theme = "المظهر";
        public const string Back = "رجوع";
        public const string PrintButton = "طباعة";
        public const string StickerPreview = "معاينة الملصق";
        public const string StickerSettings = "إعدادات الملصق";
        public const string HelpSystem = "نظام المساعدة";
        public const string ActivationTitle = "تفعيل البرنامج (محدث)";
        public const string ProductActivation = "تفعيل المنتج";
        public const string HardwareFingerprint = "بصمة الجهاز (أرسل هذا الرمز للمسؤول):";
        public const string ActivationCode = "رمز التفعيل:";
        public const string Activate = "تفعيل";
        public const string HardwareIdHint = "أرسل بصمة الجهاز أعلاه إلى المورّد لإنشاء رمز التفعيل";
        // Certificates View Fields
        public const string ClientData = "بيانات العميل";
        public const string ClientNameField = "اسم العميل *";
        public const string DelegateName = "اسم المفوض";
        public const string DeviceData = "بيانات الجهاز";
        public const string DeviceType = "نوع الجهاز";
        public const string Brand = "العلامة التجارية";
        public const string Model = "الموديل";
        public const string MainSerial = "السيريال الرئيسي";
        public const string FirstSerial = "السيريال الأول";
        public const string SecondSerial = "السيريال الثاني";
        public const string Accuracy = "الدقة";
        public const string DeviationRate = "معدل الانحراف";
        public const string CertificateData = "بيانات الشهادة";
        public const string IssueDate = "تاريخ الإصدار";
        public const string TemplateType = "نوع القالب";
        public const string SaveCertificate = "حفظ الشهادة";
        public const string PrintCertificateWord = "طباعة الشهادة (Word)";
        public const string PrintCertificatePdf = "طباعة الشهادة (PDF)";
        public const string PreviewCertificate = "معاينة الشهادة";
        public const string PrintStickerCert = "طباعة ملصق";
        
        // Settings View Fields
        public const string GeneralReports = "عام وتقارير";
        public const string ReportSettings = "إعدادات التقرير";
        public const string PrintBackground = "طباعة الخلفية";
        public const string TopMargin = "هامش علوي (مم)";
        public const string BottomMargin = "هامش سفلي (مم)";
        public const string RightMargin = "هامش أيمن (مم)";
        public const string LeftMargin = "هامش أيسر (مم)";
        public const string CompanyShort = "اختصار المنشأة";
        public const string CompanyNameField = "اسم المنشأة";
        public const string CompanyPhoneField = "هاتف المنشأة";
        public const string CompanyCommercialRecord = "السجل التجاري";
        public const string CompanyTaxNumber = "الرقم الضريبي";
        public const string CompanyNationalAddress = "العنوان الوطني";
        public const string ContractRepresentativeName = "اسم من ينوب عن المنشأة";
        public const string ContractRepresentativeId = "رقم هوية من ينوب عن المنشأة";
        public const string ContractRepresentativePhone = "جوال من ينوب عن المنشأة";
        public const string CertNumberPrefix = "بادئة رقم الشهادة";
        public const string AppIcon = "أيقونة البرنامج";
        public const string UploadIcon = "رفع أيقونة";
        public const string LibreOfficePath = "مسار LibreOffice";
        public const string Select = "تحديد";
        public const string UploadHeader = "رفع رأس";
        public const string UploadFooter = "رفع تذييل";
        public const string CompanyLogo = "شعار المنشأة";
        public const string UploadLogo = "رفع الشعار";
        public const string ShowLogoInHeader = "إظهار الشعار في ترويسة الوثائق";
        public const string SaveReportSettings = "حفظ إعدادات التقرير";
        public const string WebVerification = "التحقق عبر الويب";
        public const string BaseUrl = "رابط الأساس";
        public const string SaveButton = "حفظ";
        public const string DeleteButton = "حذف";
        public const string Database = "قاعدة البيانات";
        public const string CurrentProvider = "الموفر الحالي";
        public const string ConnectionString = "سلسلة الاتصال";
        public const string SaveDbSettings = "حفظ إعداد قاعدة البيانات";
        public const string PrintersTab = "الطابعات";
        public const string A4PrinterSettings = "إعدادات طابعة A4";
        public const string PrinterName = "اسم الطابعة";
        public const string Properties = "خصائص";
        public const string Preferences = "تفضيلات";
        public const string StickerPrinterSettings = "إعدادات طابعة الملصق";
        public const string PrinterModelProtocol = "موديل/بروتوكول الطابعة";
        public const string ConnectionType = "نوع الاتصال";
        public const string Host = "المضيف";
        public const string Port = "المنفذ";
        public const string LabelWidth = "عرض الملصق (مم)";
        public const string LabelHeight = "ارتفاع الملصق (مم)";
        public const string Darkness = "الحدة";
        public const string Speed = "السرعة (IPS)";
        public const string DotsPerMm = "نقاط/مم";
        public const string ThermalSafeMode = "وضع حراري آمن";
        public const string ThermalCoolingDelay = "تأخير التبريد (ثانية)";
        public const string SavePrinterSettings = "حفظ إعدادات الطابعة";
        public const string AccessControlTab = "صلاحيات الدخول";
        
        // Clients View Fields
        public const string Name = "الاسم";
        public const string ClientNotes = "ملاحظات";
        public const string Clear = "تفريغ";
        public const string Report = "تقرير";
        public const string DeviceMaintenanceHistory = "سجل الأجهزة والصيانة";
        public const string DateField = "التاريخ";
        public const string DeviceField = "الجهاز";
        public const string Cost = "التكلفة";
        public const string CalibrationCertificates = "شهادات المعايرة";
        public const string Issue = "الإصدار";
        public const string Expiry = "الانتهاء";
        public const string Refresh = "تحديث";
        public const string CompanyClientName = "اسم المنشأة / العميل";
        public const string CalibrationAlertTooltip = "تنبيه: يوجد أجهزة انتهت أو شارفت معايرتها على الانتهاء";
        public const string ClientsSearchSmartTooltip = "بحث ذكي بالاسم أو الهاتف";
        public const string RefreshCustomersListTooltip = "تحديث قائمة العملاء";
        
        // Rentals View Fields (Additional)
        public const string ExpiredRental = "إيجار منتهي";
        public const string CreateNewRental = "إنشاء إيجار جديد";
        public const string NewRentalForm = "إيجار جديد";
        public const string Company = "المنشأة";
        public const string TaxNumberField = "الرقم الضريبي";
        public const string IdNumber = "رقم الهوية/الإقامة";
        public const string Serial2Optional = "الرقم التسلسلي 2 (اختياري)";
        public const string StartDateField = "تاريخ البدء";
        public const string EndDateField = "تاريخ الانتهاء";
        public const string RentalType = "نوع الإيجار";
        public const string PriceField = "السعر";
        public const string PaidAmount = "المبلغ المدفوع";
        public const string RemainingAmount = "المبلغ المتبقي";
        public const string StatusField = "الحالة";
        public const string NewButton = "جديد";
        public const string ActionsSection = "الإجراءات";
        public const string PrintRentalReceipt = "طباعة إقرار استلام جهاز إيجار";
        public const string CompleteRental = "إنهاء الإيجار";
        public const string PrintReceipt = "طباعة إيصال";
        public const string SerialField = "السيريال";
        public const string CurrentlyRentedDevices = "الأجهزة المستأجرة حالياً";
        public const string RentalReport = "تقرير الإيجارات";
        public const string RentalStatistics = "إحصائيات الإيجارات";
        public const string TotalRentals = "إجمالي الإيجارات";
        public const string CurrentlyActive = "نشط حالياً";
        public const string ExpiredStatus = "منتهية";
        public const string TotalIncome = "إجمالي الدخل";

        // Rentals View - Contracts, Receipts & Alerts
        public const string DailyPrice = "السعر اليومي";
        public const string MonthlyPrice = "السعر الشهري";
        public const string DeviceValue = "قيمة الجهاز (ريال سعودي)";
        public const string RentalContract = "عقد الإيجار";
        public const string PrintRentalContract = "طباعة عقد الإيجار";
        public const string ReceiveReceiptTitle = "إقرار استلام جهاز";
        public const string ReturnReceiptTitle = "إقرار إرجاع جهاز";
        public const string ContractNumberLabel = "رقم العقد";
        public const string RentalPeriod = "مدة الإيجار";
        public const string FirstParty = "الطرف الأول (المؤجر)";
        public const string SecondParty = "الطرف الثاني (المستأجر)";
        public const string RepresentativeOf = "من ينوب عن الطرف الأول";
        public const string RentalDaysCountLabel = "عدد الأيام";
        public const string PaymentSummary = "ملخص الدفع";
        public const string ContractTermsTitle = "الشروط والبنود";
        public const string RentalContractTerms = "أولاً: سريان العقد - يسري هذا العقد ويعتبر نافذاً وملزماً للطرفين من تاريخ تحريره، ويعتبر توقيع الطرفين عليه إقراراً منهما بجميع بنوده وموافقتهما عليها التزاماً كاملاً.\nثانياً: الطرفان - الطرف الأول هو المؤجر المذكور أعلاه ويمثله في التوقيع على هذا العقد الممثل المذكور، والطرف الثاني هو المستأجر المذكور أعلاه.\nثالثاً: ملكية الجهاز - يقر الطرفان بأن الجهاز المؤجر يظل ملكاً للمؤجر ملكية خالصة طوال مدة الإيجار وبعدها، ولا يترتب على هذا العقد أي نقل أو تحويل لملكية الجهاز إلى المستأجر أو إلى أي طرف آخر.\nرابعاً: إقرار الاستلام - يقر المستأجر ويشهد على نفسه بأنه استلم الجهاز المذكور أعلاه بتاريخ بدء العقد صالحاً للعمل بحالة ممتازة وخالياً من العيوب، وقد قام بفحصه بنفسه والتأكد من سلامته ومطابقته لمواصفاته والأرقام التسلسلية المدونة في هذا العقد، ويتحمل المسؤولية الكاملة عنه منذ لحظة الاستلام.\nخامساً: الالتزام بالرد - يتعهد المستأجر بإعادة الجهاز إلى المؤجر عند انتهاء مدة الإيجار وبالحالة ذاتها التي استلمه بها (صالحاً للعمل بحالة ممتازة) مع جميع ملحقاته وفي المكان المتفق عليه، دون أي مماطلة أو تأخير.\nسادساً: مسؤولية الجهاز - يتحمل المستأجر المسؤولية الكاملة عن الجهاز من تاريخ الاستلام حتى تاريخ الإرجاع الفعلي، بما في ذلك التلف أو الفقدان أو السرقة أو الحريق أو الضياع أو أي ضرر مهما كان سببه، ويلتزم بتعويض المؤجر تعويضاً كاملاً عن كل ذلك.\nسابعاً: قيمة الجهاز - يقدر الطرفان قيمة الجهاز المذكور أعلاه بمبلغ ({0}) ريال سعودي، وتعتبر هذه القيمة أساساً ملزماً لاحتساب التعويض في حال فقدان الجهاز أو إتلافه أو عدم إعادته، ويكون للمؤجر الحق في المطالبة بها كاملة.\nثامناً: عدم التأجير من الباطن - لا يجوز للمستأجر تأجير الجهاز من الباطن أو التنازل عن هذا العقد أو نقل حيازة الجهاز إلى أي طرف آخر إلا بموافقة كتابية مسبقة من المؤجر، وتعتبر أي مخالفة لذلك مبرراً لفسخ العقد مع تحميل المستأجر جميع التبعات.\nتاسعاً: الاستخدام والموقع - يلتزم المستأجر باستخدام الجهاز وفق تعليمات التشغيل وللغرض المخصص له فقط، وعدم نقله خارج موقع الاستخدام المتفق عليه أو إخراجه من أراضي المملكة إلا بموافقة كتابية من المؤجر.\nعاشراً: عدم العبث - يلتزم المستأجر بعدم إزالة أو إتلاف الأرقام التسلسلية أو اللوحات أو الملصقات أو أي علامات دالة على ملكية المؤجر، وعدم فتح الجهاز أو تعديله أو إصلاحه إلا بواسطة المؤجر أو من يفوّضه كتابياً.\nحادي عشر: الصيانة - يتكفل المؤجر بالصيانة العامة والدورية للجهاز خلال مدة الإيجار، ويتحمل المستأجر تكاليف الصيانة الناتجة عن سوء الاستخدام أو الإهمال أو الحوادث الواقعة منه.\nثاني عشر: غرامة التأخير - في حال تأخر المستأجر عن إعادة الجهاز في الموعد المحدد دون موافقة كتابية مسبقة من المؤجر، يستحق المؤجر أجرة عن فترة التأخير تعادل المبلغ اليومي المتفق عليه عن كل يوم تأخير مع عدم الإخلال بحقه في إنهاء العقد والمطالبة بالتعويضات.\nثالث عشر: عدم سقوط الحقوق - لا يعتبر قبول المؤجر لأي دفعات متأخرة أو جزئية إبراءً للمستأجر من التزاماته ولا سقوطاً لأي حق من حقوق المؤجر، ولا يؤثر الإعفاء من شرط معين على سريان بقية الشروط.\nرابع عشر: التضامن - إذا كان المستأجر أكثر من شخص فإنهم يلتزمون بالتضامن والتكافل فيما بينهم بتنفيذ جميع التزاماتهم وفق هذا العقد.\nخامس عشر: حل النزاعات - يخضع هذا العقد وتفسيره وتنفيذه لأنظمة المملكة العربية السعودية وعلى وجه الخصوص نظام المعاملات المدنية، وتكون المحاكم المختصة في المملكة هي الجهة الوحيدة المختصة بالفصل في أي نزاع ينشأ عن هذا العقد.\nسادس عشر: الإقرار والاطلاع - يقر الطرفان بأنهما قرآ جميع بنود هذا العقد واطلعا عليها وفهماها وأقروا بصحة جميع البيانات الواردة فيه، ويحرر هذا العقد من نسختين بيد كل طرف نسخة.";
        public const string ContractSignatureLessor = "توقيع المؤجر";
        public const string ContractSignatureRenter = "توقيع المستأجر";
        public const string ReceiveConfirmationText = "أقر أنا المستلم بأنني استلمت الجهاز المذكور أعلاه بحالة جيدة ومطابق لمواصفاته، وأتحمل مسؤولية حفظه واستخدامه خلال مدة الإيجار.";
        public const string ReturnConfirmationText = "أقر أنا المستلم بأنني أرجعت الجهاز المذكور أعلاه إلى المؤجر بنفس الحالة التي استلمته بها، وتم فحصه والتأكد من سلامته.";
        public const string RentalEndDateBeforeStart = "تاريخ الانتهاء لا يمكن أن يكون قبل تاريخ البدء";
        public const string MissingRequiredFields = "يرجى تعبئة الحقول المطلوبة";
        public const string ValidationTitle = "تحقق من الإدخال";
        public const string RentalSavedSuccess = "تم حفظ الإيجار بنجاح";
        public const string RentalUpdatedSuccess = "تم تحديث الإيجار بنجاح";
        public const string Alert = "تنبيه";
        public const string SelectRentalFirst = "يرجى اختيار إيجار أولاً";
        public const string GeneratingPdf = "جارٍ إنشاء PDF...";
        public const string ExpiringSoonRentals = "إيجارات تنتهي قريباً";
        public const string ExpiringSoonRentalsAlert = "تنبيه: هناك {0} إيجار سينتهي خلال {1} أيام";
        public const string HandoverReceiptButton = "إيصال استلام/إرجاع";

        
        // Maintenance View Fields
        public const string MaintenanceOrders = "أوامر الصيانة";
        public const string MaintenanceOrder = "أمر صيانة";
        public const string SerialNumber1 = "الرقم التسلسلي 1";
        public const string Accessories = "الملحقات";
        public const string Complaint = "الشكوى";
        public const string TechnicalReport = "التقرير الفني";
        public const string PartsCost = "تكلفة القطع";
        public const string LaborCost = "تكلفة العمل";
        public const string Total = "الإجمالي";
        public const string ReceiptDate = "تاريخ الاستلام";
        public const string UpdateDate = "تاريخ التحديث";
        public const string AddDeviceForSameCustomer = "جهاز إضافي للعميل";
        public const string AddDeviceTooltip = "نسخ بيانات العميل ورقم الإيصال لسهولة إضافة جهاز جديد";
        public const string ShowPrice = "عرض سعر";
        public const string ShowPriceTooltip = "عرض سعر صيانة";
        public const string IndividualReceipt = "إيصال فردى";
        public const string MaintenanceReport = "تقرير صيانة";
        public const string MaintenanceReportTooltip = "تقرير صيانة";
        public const string PreviewTooltip = "معاينة بيانات الجهاز والتقرير الفني";
        public const string ReceiptSticker = "إيصال استلام";
        public const string ReceiptStickerTooltip = "طباعة إيصال استلام الجهاز";
        public const string MaintenanceRequests = "طلبات الصيانة";
        public const string ReceiptNumber = "رقم الإيصال";
        public const string ReceiptStickerTab = "استيكر الاستلام";
        public const string PrintReceiptSticker = "طباعة استيكر استلام الصيانة";
        public const string ReceiptStickerNote = "هذه الشاشة مستقلة تمامًا عن استيكر شهادات المعايرة.";
        public const string StickerSize = "المقاس: عرض 41 مم × ارتفاع 21 مم";
        public const string ReceiptStickerSettings = "نافذة إعدادات وطباعة استيكر الاستلام";
        public const string PrintNow = "طباعة الآن";
        public const string ReceiptStickerExtraNote = "ملاحظة: يمكن اختيار طابعة منفصلة وضبط الإعدادات من النافذة (تكبير/إزاحة)، ولن يؤثر ذلك على أي إعدادات لطابعة استيكر الشهادات.";
        
        // Maintenance Messages
        public const string CloneDeviceSuccessTitle = "جهاز جديد لنفس العميل";
        public const string CloneDeviceSuccessMessage = "تم استنساخ البيانات! قم بتغيير (الجهاز/الموديل/السيريال) حسب الحاجة، ثم اضغط على زر [حفظ] ليتم إضافته كجهاز جديد لنفس الإيصال.";
        public const string MissingTemplateMessage = "يرجى تحديد قالب Word لهذا المستند من شاشة إدارة القوالب أولاً";
        public const string InvalidCostMessage = "لا يمكن حفظ أمر الصيانة: قيمة التكلفة يجب ألا تكون سالبة.";
        public const string SaveSuccessTitle = "تم الحفظ";
        public const string SaveSuccessMessage = "تم حفظ أمر الصيانة بنجاح.";
        public const string SaveFailedTitle = "فشل الحفظ";
        public const string SaveFailedMessage = "تعذر حفظ أمر الصيانة: {0}";
        public const string MissingClientNameMessage = "اسم العميل مطلوب قبل الحفظ.";
        public const string MissingDeviceTypeMessage = "نوع الجهاز مطلوب قبل الحفظ.";
        public const string PressEditFirstMessage = "اضغط زر [تعديل] أولاً لتعديل الأمر المحدد.";
        public const string DeleteConfirmTitle = "تأكيد الحذف";
        public const string DeleteConfirmMessage = "هل أنت متأكد من حذف هذا الأمر؟\nرقم الاستلام: {0}\nالعميل: {1}\nسيتم حذف الجهاز والشهادة المرتبطة نهائيًا ولا يمكن التراجع.";
        public const string DeletedSuccessTitle = "تم الحذف";
        public const string DeletedSuccessMessage = "تم حذف أمر الصيانة بنجاح.";
        public const string PrintRequiresSaveTitle = "الحفظ مطلوب أولاً";
        public const string PrintRequiresSaveMessage = "لا يمكن الطباعة بدون حفظ البيانات. يجب إدخال اسم العميل ونوع الجهاز وحفظ الأمر أولاً.";
        public const string SaveBeforePrintPrompt = "البيانات الحالية غير محفوظة.\nهل تريد حفظها الآن ثم الطباعة؟";

        // Maintenance Reports / Statistics / Financials Tab
        public const string MaintenanceReportsTab = "التقارير والإحصائيات والمالية";
        public const string ReportsFrom = "من تاريخ";
        public const string ReportsTo = "إلى تاريخ";
        public const string TotalOrders = "إجمالي الأوامر";
        public const string OrdersInProgress = "قيد التنفيذ";
        public const string CompletedDelivered = "مكتملة / مسلّمة";
        public const string TotalPartsCost = "إجمالي تكلفة القطع";
        public const string TotalLaborCost = "إجمالي تكلفة العمل";
        public const string StatusBreakdown = "التوزيع حسب الحالة";
        public const string MonthlyBreakdown = "التوزيع الشهري";
        public const string ExportPdf = "تصدير PDF";
        public const string ExportExcel = "تصدير Excel";
        public const string MonthColumn = "الشهر";
        public const string OrdersCount = "عدد الأوامر";
        public const string TotalCostColumn = "إجمالي التكلفة";
        public const string NoReportsData = "لا توجد بيانات في النطاق المحدد";
        public const string ReportsExportFailed = "فشل تصدير التقرير";
        public const string ReportsTitle = "تقرير الصيانة والإحصائيات والمالية";
        public const string ReportsGeneratedOn = "تاريخ إصدار التقرير";
        public const string ReportNumberLabel = "رقم التقرير";
        public const string SignatureMaintenanceResponsible = "توقيع مسئول الصيانة";
        public const string SignatureManager = "توقيع المدير";
        public const string NameLabel = "الاسم";
        public const string SignatureLabel = "التوقيع";
        public const string PageLabel = "الصفحة";
        public const string ReportDate = "تاريخ التقرير";
        public const string ClientNameLabel = "اسم العميل";
        public const string MaintenanceOrdersCount = "عدد أوامر الصيانة";
        public const string CertificatesCount = "عدد الشهادات";
        public const string TotalMaintenanceCost = "إجمالي تكلفة الصيانة";
        public const string CertificatesHistory = "سجل الشهادات";
        public const string SignatureCustomer = "توقيع العميل";
        public const string SignatureDepartmentManager = "توقيع مدير القسم";
        public const string DateColumn = "التاريخ";
        
        // Inventory View Fields
        public const string AddEditSparePart = "إضافة / تعديل قطعة غيار";
        public const string PartCodeSKU = "كود القطعة (SKU)";
        public const string ItemNameField = "اسم الصنف *";
        public const string CategoryField = "التصنيف *";
        public const string BrandField = "العلامة التجارية";
        public const string CurrentQuantity = "الكمية الحالية";
        public const string MinimumThreshold = "الحد الأدنى";
        public const string CostPrice = "سعر التكلفة";
        public const string SellingPrice = "سعر البيع";
        public const string PurchaseDate = "تاريخ الشراء";
        public const string Location = "الموقع";
        public const string LowStockAlert = "تحذير: يوجد عدد (";
        public const string LowStockAlertEnd = ") قطع وصلت لحد النواقص ويرجى طلبها فوراً!";
        public const string ShowLowStockOnly = "عرض النواقص فقط";
        public const string SearchRefresh = "بحث / تحديث";
        public const string SearchTooltip = "البحث بالكود، الاسم، أو البراند";
        public const string PrintReport = "طباعة تقرير";
        public const string Code = "الكود";
        public const string NameField = "الاسم";
        public const string BrandColumn = "البراند";
        
        // Device History View Fields
        public const string SerialSearch = "بحث السيريال";
        public const string DeviceHistoryField = "سجل الجهاز";
        public const string HistoryType = "النوع";
        public const string HistoryTitle = "العنوان";
        
        // Sticker Designer View Fields
        public const string PageTab = "الصفحة";
        public const string WidthMm = "العرض (mm)";
        public const string PreviewScale = "معامل المعاينة";
        public const string HeightMm = "الارتفاع (mm)";
        public const string GridTab = "الشبكة";
        public const string ShowPreviewGrid = "إظهار شبكة المعاينة";
        public const string SnapToGrid = "محاذاة للسلم (Snap-To-Grid)";
        public const string EnableMouseEditing = "تمكين التحرير بالماوس";
        public const string GridStepMm = "خطوة الشبكة (mm)";
        public const string HeaderTab = "الترويسة";
        public const string UseLogo = "استخدام الشعار";
        public const string HeaderTitle = "عنوان الترويسة";
        public const string StickerCompanyName = "اسم المنشأة";
        public const string StickerAddress = "العنوان";
        public const string StickerPhone = "الهاتف";
        public const string HeaderFontSize = "حجم خط الترويسة";
        public const string ItemTab = "العنصر";
        public const string XPosition = "موقع X";
        public const string YPosition = "موقع Y";
        public const string ItemFontSize = "حجم خط العناصر";
        public const string ContentTab = "المحتوى";
        public const string DeviceTypeName = "نوع/اسم الجهاز (Device)";
        public const string StickerBrand = "الماركة (Brand)";
        public const string SerialNumberField = "الرقم التسلسلي (S/N)";
        public const string StickerCalibrationDate = "تاريخ المعايرة (Cal Date)";
        public const string StickerExpiryDate = "تاريخ الانتهاء (Exp Date)";
        public const string QrAndActions = "QR والإجراءات";
        public const string ShowQrInPreview = "إظهار QR في المعاينة";
        public const string SaveLayout = "حفظ التخطيط";
        public const string ResetLayout = "إعادة تعيين التخطيط";
        public const string TestPrint = "اختبار الطباعة";
        public const string QrLabel = "QR";
        public const string GenerateAndSaveQr = "توليد وحفظ QR";
        public const string DetectStickerPrinter = "كشف طابعة الاستيكر";
        public const string DetectedPrinters = "الطابعات المكتشفة";
        public const string ApplySelectedPrinter = "تطبيق الطابعة المحددة";
        public const string PrinterDriver = "الدرايفر";
        public const string PrinterPort = "المنفذ";
        public const string PrinterDpi = "الدقة (DPI)";
        public const string PrinterProtocolHeader = "البروتوكول";
        public const string ThermalPrinterType = "حرارية";
        public const string StandardPrinterType = "عادية";
        public const string NoPrinterSelected = "اختر طابعة أولاً.";
        public const string PrinterDetectedMessage = "تم تحديد الطابعة: {0} ({1}, {2} DPI)";
        public const string ThermalHint = "الطابعات الحرارية مميزة باللون الأزرق";
        // Sticker Designer Extra
        public const string SaveAsTemplate = "حفظ كقالب";
        public const string ContentHeaderCompanyName = "اسم المنشأة:";
        public const string ContentHeaderAddress = "العنوان:";
        public const string ContentHeaderPhone = "رقم الهاتف:";
        public const string LogoSettings = "إعدادات اللوجو";
        public const string UseLogoText = "استخدام اللوجو";
        public const string PickLogo = "اختيار صورة اللوجو";
        public const string AddItems = "إضافة عناصر";
        public const string AddTextItem = "إضافة نص";
        public const string AddLineItem = "إضافة خط";
        public const string DeleteSelectedItem = "حذف المحدد";
        public const string SelectedItemProperties = "خصائص العنصر المحدد";
        public const string ItemNameLabel = "اسم العنصر:";
        public const string PositionX = "الموقع X:";
        public const string PositionY = "الموقع Y:";
        public const string WidthLabel = "العرض:";
        public const string HeightLabel = "الارتفاع:";
        public const string PositionXMm = "الموقع X (مم):";
        public const string PositionYMm = "الموقع Y (مم):";
        public const string WidthMmLabel = "العرض (مم):";
        public const string HeightMmLabel = "الارتفاع (مم):";
        public const string RotationLabel = "الدوران:";
        public const string FontSizeLabel = "حجم الخط:";
        public const string FontColorLabel = "لون الخط:";
        public const string FontFamilyLabel = "نوع الخط:";
        public const string BoldLabel = "عريض";
        public const string ItalicLabel = "مائل";
        public const string UnderlineLabel = "تحته خط";
        public const string AlignmentLabel = "المحاذاة:";
        public const string QrSizeMm = "حجم QR (مم):";
        public const string ActionsTab = "الإجراءات";
        public const string TextLabel = "النص:";
        public const string BindToVariableLabel = "ربط بمتغير:";
        public const string ZIndexLabel = "الترتيب (Z-Index):";
        public const string ItemVisibility = "ظهور العنصر";
        public const string BodyFontSize = "حجم خط النصوص";
        public const string AutoLayout = "تخطيط تلقائي";
        public const string DeviceDataSettings = "بيانات الجهاز";
        public const string FontSettings = "إعدادات الخط";
        public const string ModelLabel = "الموديل:";
        public const string CertificateNumberField = "رقم الشهادة:";
        public const string CustomTemplatesLabel = "القوالب المخصصة";
        public const string TestPrintSent = "تم إرسال ملصق الاختبار بنجاح.";
        public const string TestPrintFailed = "فشل اختبار الطباعة: {0}";
        public const string NoPrintersDetected = "لم يتم اكتشاف أي طابعة.";
        public const string PrinterDetectionFailed = "فشل كشف الطابعات: {0}";
        public const string LayoutSaved = "تم حفظ التخطيط.";
        public const string LayoutResetDone = "تمت إعادة ضبط التخطيط.";
        public const string PreviewFailed = "فشل فتح المعاينة: {0}";
        public const string EnterTemplateName = "أدخل اسم القالب:";
        public const string TemplateSavedMsg = "تم حفظ القالب: {0}";
        public const string TemplateAppliedMsg = "تم تطبيق القالب: {0}";
        public const string TemplateNameRequired = "يرجى إدخال اسم القالب.";
        public const string SaveTemplateTitle = "حفظ كقالب";
        public const string QrItemNotFound = "عنصر QR غير موجود في تخطيط الملصق.";
        public const string QrGenerateFailed = "فشل توليد صورة رمز QR.";
        public const string QrCodeSavedMsg = "تم حفظ رمز QR في: {0}";
        public const string QrSaveError = "خطأ في توليد أو حفظ رمز QR: {0}";

        // Settings Access/License
        public const string CurrentStatus = "الوضع الحالي";
        public const string LoginAs = "الدخول كـ";
        public const string LoginAsAdmin = "تسجيل دخول المدير";
        public const string LogoutAdmin = "تسجيل خروج المدير";
        public const string LogoutUser = "تسجيل خروج المستخدم";
        public const string UserManagement = "إدارة المستخدمين";
        public const string UserLabel = "المستخدم";
        public const string AddUserButton = "إضافة";
        public const string DeleteUserButton = "حذف";
        public const string ChangePasswordButton = "تغيير كلمة المرور";
        public const string SavePermissions = "حفظ الصلاحيات";
        public const string AdminPin = "الرقم السري للمدير";
        public const string AdminPinNote = "الرقم السري الافتراضي: 123456 (يمكن تغييره لاحقًا).";
        public const string ChangeAdminPin = "تغيير الرقم السري";
        public const string LicenseTab = "الترخيص";
        public const string LicenseInfo = "معلومات الترخيص";
        public const string LicenseStatusLabel = "حالة الترخيص:";
        public const string LicenseExpiryLabel = "تاريخ الانتهاء:";
        public const string HardwareIdLabel = "بصمة الجهاز:";
        public const string CopyButton = "نسخ";
        public const string LicenseUpdate = "تحديث الترخيص";
        public const string EnterNewKey = "إذا كان لديك مفتاح تفعيل جديد، أدخله هنا:";
        public const string ActivateLicense = "تفعيل الترخيص";
        
        // Home View
        public const string Dashboard = "لوحة التحكم";
        public const string SystemOverview = "نظرة عامة على أداء النظام";
        public const string TotalCertificates = "إجمالي الشهادات";
        public const string ActiveRentals = "الإيجارات النشطة";
        public const string ExpiringSoon = "تنتهي قريباً";
        public const string TotalClients = "إجمالي العملاء";
        public const string RecentActivity = "النشاط الأخير";
        public const string QuickActions = "إجراءات سريعة";
        public const string NewCertificate = "شهادة جديدة";
        public const string NewRental = "إيجار جديد";
        public const string NewClient = "عميل جديد";
        public const string ViewReports = "عرض التقارير";
        public const string CertificatesLog = "سجل الشهادات";
        public const string TemplateSettings = "إعدادات القوالب";
        public const string LowStock = "مخزون منخفض";
        public const string LastCertificates = "آخر الشهادات";
        public const string LastMaintenances = "آخر الصيانات";
        
        // Home View
        public const string SearchCertificate = "بحث شهادة";
        public const string ExpiredDevices = "الأجهزة المنتهية";
        public const string Statistics = "الإحصائيات";
        public const string MaintenanceRecords = "سجلات الصيانة";
        
        // Maintenance Receipt Window
        public const string DeviceReceipt = "إيصال استلام الجهاز";
        public const string RentalDeviceReceipt = "إيصال استلام جهاز إيجار";
        public const string Printer = "الطابعة";
        public const string SelectPrinter = "تحديد الطابعة";
        public const string RefreshPreview = "تحديث المعاينة";
        public const string PrintReceiptWindow = "طباعة";
        public const string CloseReceipt = "إغلاق";
        public const string UseLetterhead = "استخدام ورق مروس";
        public const string VerticalOffset = "تحريك عمودي:";
        public const string HorizontalOffset = "تحريك أفقي:";
        public const string HeaderMargin = "مساحة الترويسة:";
        public const string FooterMargin = "مساحة التذييل:";
        public const string FrameColor = "لون البرواز:";
        public const string ZoomIn = "تكبير";
        public const string ZoomOut = "تصغير";
        public const string FitWidth = "ملء العرض";
        public const string FitPage = "صفحة كاملة";
        
        // Maintenance Preview Window
        public const string MaintenanceOrderPreview = "معاينة أمر الصيانة";
        public const string ClientDataPreview = "بيانات العميل";
        public const string ClientNamePreview = "اسم العميل";
        public const string PhonePreview = "الهاتف";
        public const string DeviceDataPreview = "بيانات الجهاز";
        public const string DeviceTypePreview = "نوع الجهاز";
        public const string ModelPreview = "الموديل";
        public const string SerialNumberPreview = "الرقم التسلسلي";
        public const string AccessoriesPreview = "الملحقات";
        public const string StatusAndDates = "الحالة والتواريخ";
        public const string StatusFieldPreview = "الحالة";
        public const string ReceivedDatePreview = "تاريخ الاستلام";
        public const string UpdatedDatePreview = "تاريخ التحديث";
        public const string TotalPreview = "الإجمالي";
        public const string ComplaintPreview = "الشكوى";
        public const string TechnicalReportPreview = "التقرير الفني";
        
        // Maintenance Receipt Sticker Window
        public const string MaintenanceSticker = "استيكر استلام الصيانة";
        public const string MaintenanceStickerSettings = "إعدادات استيكر الاستلام";
        public const string MaintenanceStickerData = "بيانات الاستيكر";
        public const string DeviceSerial = "سيريال الجهاز";
        public const string RealDimensions = "الأبعاد الحقيقية (mm)";
        public const string StickerWidthMm = "العرض (مم)";
        public const string StickerHeightMm = "الارتفاع (مم)";
        public const string DefaultSize = "الافتراضي: 41 × 21 مم";
        public const string StickerPrinter = "الطابعة الخاصة باستيكر الاستلام";
        public const string SelectPrinterButton = "اختيار طابعة";
        public const string ScaleContent = "تكبير المحتوى";
        public const string HorizontalOffsetSticker = "إزاحة أفقية";
        public const string VerticalOffsetValue = "إزاحة رأسية";
        public const string RealPreview = "معاينة حقيقية";
        public const string PrintStickerButton = "طباعة الاستيكر";
        
        // Client Report Window
        public const string ClientAccountStatement = "كشف حساب عميل";
        
        // Sticker Preview Window
        public const string StickerPreviewWindow = "معاينة الملصق (Sticker Preview)";
        
        // Certificates View
        public const string CertificateNumber = "رقم الشهادة";
        public const string DeviceName = "اسم الجهاز";
        public const string SerialNumber = "الرقم التسلسلي";
        public const string CalibrationDate = "تاريخ المعايرة";
        public const string ValidUntil = "صالح حتى";
        public const string ExpiryDate = "تاريخ الانتهاء";
        public const string Status = "الحالة";
        public const string Active = "نشط";
        public const string Expired = "منتهي";
        public const string AddCertificate = "إضافة شهادة";
        public const string EditCertificate = "تعديل الشهادة";
        public const string DeleteCertificate = "حذف الشهادة";
        public const string PrintCertificate = "طباعة الشهادة";
        public const string ExportPDF = "تصدير PDF";

        // Cloud Upload
        public const string UploadToCloud = "رفع للسحابة";
        public const string AutoUpload = "رفع تلقائي";
        public const string DownloadCloudBackup = "تحميل نسخة سحابية";
        public const string UploadCloudBackup = "رفع نسخة سحابية";
        public const string CloudConnectionStatus = "حالة الاتصال السحابي:";
        public const string OfflineStatus = "غير متصل";
        public const string CloudBackupList = "قائمة النسخ الاحتياطية السحابية:";
        public const string CloudBackupUnavailable = "النسخ الاحتياطي السحابي غير متاح حالياً";
        public const string VerificationLink = "رابط التحقق";
        public const string CopyVerificationLink = "نسخ رابط التحقق";
        public const string OpenVerificationLink = "فتح رابط التحقق";

        // Certificates Tabs / Template Settings
        public const string TemplateDocTypeStep = "1) اختيار نوع المستند";
        public const string TemplateChooseStep = "2) اختيار وحفظ القالب";
        public const string DocumentType = "نوع المستند";
        public const string CurrentPath = "المسار الحالي";
        public const string ChooseWordTemplate = "اختيار ملف Word (.docx)";
        public const string SaveTemplateSetting = "حفظ الإعداد";
        public const string TemplateTagsDictionary = "قاموس الوسوم (اختياري)";
        public const string DescriptionLabel = "الوصف";
        public const string TagLabel = "الوسم";
        
        // Clients View
        public const string ClientName = "اسم العميل";
        public const string PhoneNumber = "رقم الهاتف";
        public const string Email = "البريد الإلكتروني";
        public const string Address = "العنوان";
        public const string TaxNumber = "الرقم الضريبي";
        public const string IDNumber = "رقم الهوية";
        public const string AddClient = "إضافة عميل";
        public const string EditClient = "تعديل العميل";
        public const string DeleteClient = "حذف العميل";
        
        // Inventory View
        public const string ItemName = "اسم الصنف";
        public const string Quantity = "الكمية";
        public const string Unit = "الوحدة";
        public const string Price = "السعر";
        public const string Category = "الفئة";
        public const string AddItem = "إضافة صنف";
        public const string EditItem = "تعديل الصنف";
        public const string DeleteItem = "حذف الصنف";
        public const string StockIn = "إدخال مخزون";
        public const string StockOut = "إخراج مخزون";
        
        // Settings View
        public const string General = "عام";
        public const string Printers = "الطابعات";
        public const string AccessControl = "التحكم في الوصول";
        public const string License = "الترخيص";
        public const string Backup = "النسخ الاحتياطي";
        public const string CloudBackup = "النسخ الاحتياطي السحابي";
        public const string CompanyName = "اسم المنشأة";
        public const string CompanyAddress = "عنوان المنشأة";
        public const string CompanyPhone = "هاتف المنشأة";
        public const string CompanyEmail = "بريد المنشأة الإلكتروني";
        public const string Logo = "الشعار";
        public const string DefaultPrinter = "الطابعة الافتراضية";
        public const string AdminPIN = "رمز المدير";
        public const string DefaultPIN = "الرمز الافتراضي";
        public const string ChangePIN = "تغيير الرمز";
        public const string Users = "المستخدمون";
        public const string AddUser = "إضافة مستخدم";
        public const string Username = "اسم المستخدم";
        public const string Password = "كلمة المرور";
        public const string Permissions = "الصلاحيات";
        public const string LicenseKey = "مفتاح الترخيص";
        public const string DeviceFingerprint = "بصمة الجهاز";
        public const string Copy = "نسخ";
        public const string BackupPath = "مسار النسخ الاحتياطي";
        public const string Change = "تغيير";
        public const string CreateBackup = "إنشاء نسخة احتياطية";
        public const string RestoreBackup = "استعادة نسخة احتياطية";
        public const string DeleteBackup = "حذف النسخة الاحتياطية";
        public const string LocalBackups = "النسخ الاحتياطية المحلية";
        
        // Backup Settings
        public const string BackupSettings = "إعدادات النسخ الاحتياطي";
        public const string DefaultBackupPath = "المسار الافتراضي: مجلد Backups بجانب ملف البرنامج";
        public const string BackupEncryption = "تشفير النسخ الاحتياطي";
        public const string EnableBackupEncryption = "تفعيل تشفير النسخ الاحتياطية (AES-256)";
        public const string EncryptionPassword = "كلمة مرور التشفير";
        public const string GeneratePassword = "إنشاء";
        public const string EncryptionPasswordNote = "ملاحظة: كلمة المرور مطلوبة لفك تشفير النسخ الاحتياطية عند الاستعادة";
        public const string AutoBackup = "النسخ الاحتياطي التلقائي";
        public const string EnableAutoBackup = "تفعيل النسخ الاحتياطي التلقائي";
        public const string BackupSchedule = "جدولة النسخ";
        public const string Daily = "يومي";
        public const string Weekly = "أسبوعي";
        public const string Monthly = "شهري";
        public const string BackupTime = "وقت النسخ";
        public const string MaxBackupCount = "الحد الأقصى للنسخ";
        public const string SaveBackupSettings = "حفظ الإعدادات";
        public const string LocalBackup = "النسخ الاحتياطي المحلي";
        public const string BackupList = "قائمة النسخ الاحتياطية:";
        
        // Backup Messages
        public const string BackupSettingsSaved = "تم حفظ إعدادات النسخ الاحتياطي.";
        public const string BackupSettingsSaveFailed = "تعذر حفظ إعدادات النسخ الاحتياطي.";
        public const string BackupPasswordGenerated = "تم إنشاء كلمة مرور جديدة للنسخ الاحتياطي.";
        public const string DatabaseFileNotFound = "لم يتم العثور على ملف قاعدة البيانات.";
        public const string EncryptedBackupCreated = "تم إنشاء النسخة الاحتياطية المشفرة بنجاح:";
        public const string BackupCreated = "تم إنشاء النسخة الاحتياطية بنجاح:";
        public const string BackupCreationError = "خطأ في إنشاء النسخة الاحتياطية:";
        public const string PasswordRequiredForEncrypted = "كلمة المرور مطلوبة للنسخ الاحتياطية المشفرة.";
        public const string BackupInvalidOrWrongPassword = "النسخة الاحتياطية غير صالحة أو كلمة المرور غير صحيحة.";
        public const string BackupInvalid = "النسخة الاحتياطية غير صالحة.";
        public const string BackupRestoredSuccessfully = "تم استعادة النسخة الاحتياطية بنجاح. يرجى إعادة تشغيل البرنامج.";
        public const string BackupRestoreError = "خطأ في استعادة النسخة الاحتياطية:";
        public const string BackupPathSelected = "تم تحديد مسار النسخ الاحتياطي:";
        public const string BackupPathError = "خطأ في تحديد المسار:";
        public const string SelectBackupFile = "اختر ملف النسخة الاحتياطية";
        public const string BackupPasswordTitle = "كلمة مرور النسخة الاحتياطية";
        public const string EnterBackupPassword = "أدخل كلمة مرور النسخة الاحتياطية المشفرة:";
        public const string ConfirmRestoreBackup = "هل أنت متأكد من استعادة هذه النسخة الاحتياطية؟ سيتم استبدال قاعدة البيانات الحالية.";
        public const string ConfirmRestoreTitle = "تأكيد الاستعادة";
        public const string SelectAnyFileInBackupFolder = "اختر أي ملف في مجلد النسخ الاحتياطي المطلوب";
        public const string Date = "التاريخ";
        public const string Size = "الحجم";
        public const string Actions = "الإجراءات";
        public const string Save = "حفظ";
        public const string Cancel = "إلغاء";
        public const string Delete = "حذف";
        public const string Edit = "تعديل";
        public const string Close = "إغلاق";
        
        // Rentals View
        public const string RentalNumber = "رقم الإيجار";
        public const string Client = "العميل";
        public const string Device = "الجهاز";
        public const string StartDate = "تاريخ البدء";
        public const string EndDate = "تاريخ الانتهاء";
        public const string DailyRate = "السعر اليومي";
        public const string TotalAmount = "المبلغ الإجمالي";
        public const string Deposit = "الإيداع";
        public const string Notes = "ملاحظات";
        public const string AddRental = "إضافة إيجار";
        public const string EditRental = "تعديل الإيجار";
        public const string DeleteRental = "حذف الإيجار";
        public const string ReturnDevice = "إرجاع الجهاز";
        public const string PrintContract = "طباعة العقد";
        
        // Device History View
        public const string History = "السجل";
        public const string DeviceDetails = "تفاصيل الجهاز";
        public const string CalibrationHistory = "سجل المعايرة";
        public const string RentalHistory = "سجل الإيجار";
        public const string MaintenanceHistory = "سجل الصيانة";
        public const string ViewDetails = "عرض التفاصيل";
        
        // Sticker Designer View
        public const string StickerTemplate = "قالب الملصق";
        public const string Preview = "معاينة";
        public const string Reprint = "إعادة طباعة";
        public const string PrintSticker = "طباعة الملصق";
        public const string Customize = "تخصيص";
        public const string Text = "النص";
        public const string Barcode = "الباركود";
        public const string QRCode = "رمز QR";
        
        // Help Window
        public const string HelpTitle = "المساعدة";
        public const string SearchHelp = "بحث في المساعدة";
        public const string GettingStarted = "البدء";
        public const string UserGuide = "دليل المستخدم";
        public const string FAQ = "الأسئلة الشائعة";
        public const string ContactSupport = "اتصل بالدعم";
        public const string HelpPrintGuide = "طباعة الدليل";
        public const string HelpExportPdf = "تصدير PDF";
        public const string Introduction = "مقدمة";
        public const string Navigation = "التنقل";
        public const string CertificatesHelp = "الشهادات";
        public const string MaintenanceHelp = "الصيانة";
        public const string ClientsHelp = "العملاء";
        public const string InventoryHelp = "المخزون";
        public const string SettingsHelp = "الإعدادات";
        public const string RentalsHelp = "الإيجارات";
        
        // Login Window
        public const string LoginTitle = "تسجيل الدخول";
        public const string Manager = "المدير";
        public const string Enter = "دخول";
        public const string InvalidCredentials = "بيانات الدخول غير صحيحة";
        
        // Messages
        public const string Success = "نجاح";
        public const string Error = "خطأ";
        public const string Warning = "تنبيه";
        public const string Information = "معلومات";
        public const string ConfirmDelete = "هل أنت متأكد من الحذف؟";
        public const string ItemDeleted = "تم حذف العنصر بنجاح";
        public const string ItemSaved = "تم حفظ العنصر بنجاح";
        public const string ItemUpdated = "تم تحديث العنصر بنجاح";
        public const string NoNotifications = "لا توجد إشعارات جديدة";
        public const string ExpiredRentalsAlert = "تنبيه: هناك {0} إيجار منتهي";
        public const string ExpiringCertificatesAlert = "تنبيه: هناك {0} شهادة ستنتهي خلال 7 أيام";
        public const string LanguageInDevelopment = "اختيار اللغة - قيد التطوير";
        public const string ErrorOpeningHelp = "خطأ في فتح نافذة المساعدة";

        // Certificates: validation messages (MessageBox)
        public const string MsgCannotOpenPreview = "تعذر فتح المعاينة";
        public const string MsgCannotOpenFile = "تعذر فتح الملف";
        public const string MsgMissingData = "بيانات ناقصة";
        public const string MsgClientNameRequired = "يرجى إدخال اسم العميل أولاً.";
        public const string MsgModelRequired = "يرجى إدخال الموديل أولاً.";
        public const string MsgSerialRequired = "يرجى إدخال السيريال أولاً.";
        public const string MsgAutoLevelDeviationRequired = "يرجى إدخال قيمة معيار الانحراف (Auto Level) أولاً.";
        public const string MsgTemplateMissingForType = "يرجى تحديد قالب Word لهذا النوع من شاشة إدارة القوالب أولاً";
        public const string MsgTemplateMissing = "قالب غير موجود";
        public const string MsgSaveMethod = "طريقة الحفظ";
        public const string MsgArchiveSavePrompt = "تم تحميل هذه الشهادة من الأرشيف.\nهل تريد تعديل الشهادة الحالية (استبدالها) أم حفظها كشهادة جديدة؟\n\nنعم = تعديل الشهادة الحالية\nلا = حفظ كشهادة جديدة\nإلغاء = إلغاء العملية";

        // Certificates: archive context menu
        public const string EditRename = "تعديل / إعادة تسمية";
        public const string Open = "فتح";

        // Certificates: status bar messages
        public const string StSelectWordTemplateFirst = "يرجى تحديد قالب Word أولاً من إدارة القوالب.";
        public const string StFailedToCreateWordFile = "فشل إنشاء ملف Word.";
        public const string StCloudNotConfigured = "الرفع للسحابة غير مُفعّل: يرجى ضبط CloudApiBase و CloudApiKey في ملف الإعدادات.";
        public const string StHelpPrintFailed = "تعذر الطباعة: {0}";
        public const string StHelpExportFailed = "تعذر التصدير: {0}";
        public const string StUploadFailed = "تعذر رفع الشهادة: {0}";
        public const string StUploadHttpFailed = "فشل الرفع: {0} {1}. {2}";
        public const string StUploadedWithUrl = "تم رفع الشهادة: {0}";
        public const string StUploadedOk = "تم رفع الشهادة بنجاح.";
        public const string StUploadedPrefix = "تم رفع الشهادة";
        public const string StSavedButNoWord = "تم حفظ الشهادة، لكن تعذر الرفع: يرجى تحديد قالب Word أولاً من إدارة القوالب.";
        public const string StSavedButNoWordFile = "تم حفظ الشهادة، لكن تعذر إنشاء ملف Word للرفع.";
        public const string StSavedButNoPdf = "تم حفظ الشهادة، لكن تعذر إنشاء ملف PDF للرفع.";
        public const string StUpdatedUploaded = "تم تحديث الملف المرفوع بنجاح: {0}";
        public const string StAutoUploaded = "تم رفع الشهادة تلقائياً بنجاح: {0}";
        public const string StUpdatedUploadedPlain = "تم تحديث الملف المرفوع بنجاح.";
        public const string StAutoUploadedPlain = "تم رفع الشهادة تلقائياً بنجاح.";
        public const string StCreatedPdf = "تم إنشاء PDF: {0}";
        public const string StPdfExportFailed = "تعذر تصدير PDF: {0}";
        public const string StPreparingPreview = "جارٍ تجهيز المعاينة...";
        public const string StSaving = "جارٍ الحفظ...";
        public const string StUploadingCloud = "جارٍ رفع الشهادة للسحابة...";
        public const string StDeleting = "جارٍ الحذف...";
        public const string StPdfPreviewFailedLibreOffice = "تعذر معاينة PDF: يرجى تثبيت LibreOffice أو ضبط LIBREOFFICE_PROGRAM.";
        public const string StBgFromTextFailed = "تعذر توليد خلفية من الملف النصي.";
        public const string StBgImageSelected = "تم اختيار صورة خلفية للقالب.";
        public const string StUploadTemplateFirst = "قم برفع قالب أولاً ثم اضغط معاينة.";
        public const string StDocxPreviewCreated = "تم إنشاء معاينة DOCX.";
        public const string StPreviewDocxOnly = "المعاينة تعتمد على قوالب Word (DOCX) فقط.";
        public const string StPreviewErrorDetails = "حدث خطأ أثناء إنشاء المعاينة (تفاصيل في last-pdf-error.txt): {0}";
        public const string StPreparingPdfPreview = "جاري تجهيز معاينة PDF...";
        public const string StTemplateLoaded = "تم تحميل القالب: {0}";
        public const string StTemplateLoadFailed = "تعذر تحميل القالب المحدد.";
        public const string StTemplateSaveCancelled = "ألغيت حفظ القالب.";
        public const string StTemplateSaved = "تم حفظ القالب.";
        public const string StTemplateSaveFailed = "تعذر حفظ القالب.";
        public const string StTemplateCreated = "تم إنشاء وحفظ قالب جديد.";
        public const string StTemplateCreateFailed = "تعذر إنشاء القالب.";
        public const string StFilePrepareFailed = "تعذر تجهيز الملف: {0}";
        public const string StWordTemplateMissing = "قالب Word غير موجود. يرجى إعادة رفع القالب من إدارة القوالب.";
        public const string StSendingSticker = "جارٍ إرسال الملصق...";
        public const string StStickerSentWindows = "تم إرسال الملصق عبر تعريف Windows.";
        public const string StStickerSentLayout = "تم إرسال ملصق {0} (Layout) للطابعة.";
        public const string StStickerSendFailed = "فشل إرسال الملصق: {0}";
        public const string StPreparingStickerPreview = "جارٍ إنشاء معاينة الملصق...";
        public const string StStickerPreviewOpened = "تم فتح معاينة الاستيكر وفق نموذج التصميم الحقيقي.";
        public const string StStickerPreviewFailed = "فشل المعاينة: {0}";
        public const string StFieldsAutoDistributed = "تم توزيع الحقول تلقائيًا.";
        public const string StFieldRemoved = "تمت إزالة الحقل من التصميم.";
        public const string LoadingData = "جارٍ التحميل...";
        public const string NoDataAvailable = "لا توجد بيانات";
        public const string NoSearchResults = "لا توجد نتائج مطابقة";
        public const string StLayoutReset = "تمت إعادة ضبط التخطيط.";
        public const string StVerifyLinkCopied = "تم نسخ رابط التحقق.";
        public const string StCancelled = "تم إلغاء العملية.";
        public const string StCertificateSaved = "تم حفظ الشهادة.";
        public const string StCertificateNumberDuplicate = "رقم الشهادة مكرر! هذا الرقم مستخدم لشهادة أخرى. تم إلغاء الحفظ.";
        public const string StSaveFailed = "تعذر الحفظ: {0}";

        // Certificates: mode labels
        public const string ModeNew = "وضع: جديد";
        public const string ModeEdit = "وضع: تعديل";

        // Certificates: layout field names
        public const string FieldClientName = "اسم العميل";
        public const string FieldDate = "التاريخ";
        public const string FieldExpiryDate = "تاريخ الانتهاء";
        public const string FieldCertNo = "رقم الشهادة";
        public const string FieldWorkNo = "أمر العمل";
        public const string FieldReportType = "نوع التقرير";
        public const string FieldPhone = "الهاتف";
        public const string FieldDeviceType = "نوع الجهاز";
        public const string FieldBrand = "الماركة";
        public const string FieldModel = "الموديل";
        public const string FieldSerial = "السيريال";
        public const string FieldTableStart = "بداية الجدول";
        public const string FieldGeneric = "حقل";

        // Certificates: template hub document types
        public const string DocTypeInvoice = "إيصال استلام فردى";
        public const string DocTypeRentalReceipt = "إقرار استلام جهاز إيجار";
        public const string BrandMiscChineseDevices = "مختلف الاجهزة الصينيه";
    }

    public static class English
    {
        // Main Window & Navigation
        public const string Home = "Home";
        public const string Certificates = "Certificates";
        public const string Maintenance = "Maintenance";
        public const string Clients = "Clients";
        public const string Inventory = "Inventory";
        public const string DeviceHistory = "Device History";
        public const string StickerDesigner = "Sticker Designer";
        public const string ReceiptStickerDesigner = "Receipt Sticker Designer";
        public const string Settings = "Settings";
        public const string Rentals = "Rentals";
        public const string Search = "Search";
        public const string Help = "Help";
        public const string Notifications = "Notifications";
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string Language = "Language";
        public const string Theme = "Theme";
        public const string Back = "Back";
        public const string PrintButton = "Print";
        public const string StickerPreview = "Sticker Preview";
        public const string StickerSettings = "Sticker Settings";
        public const string HelpSystem = "Help System";
        public const string ActivationTitle = "Program Activation (Updated)";
        public const string ProductActivation = "Product Activation";
        public const string HardwareFingerprint = "Hardware Fingerprint (Send this code to admin):";
        public const string ActivationCode = "Activation Code:";
        public const string Activate = "Activate";
        public const string HardwareIdHint = "Send this fingerprint to your admin to generate the activation code";
        
        
        // Certificates View Fields
        public const string ClientData = "Client Data";
        public const string ClientNameField = "Client Name *";
        public const string DelegateName = "Delegate Name";
        public const string DeviceData = "Device Data";
        public const string DeviceType = "Device Type";
        public const string Brand = "Brand";
        public const string Model = "Model";
        public const string MainSerial = "Main Serial";
        public const string FirstSerial = "First Serial";
        public const string SecondSerial = "Second Serial";
        public const string Accuracy = "Accuracy";
        public const string DeviationRate = "Deviation Rate";
        public const string CertificateData = "Certificate Data";
        public const string IssueDate = "Issue Date";
        public const string TemplateType = "Template Type";
        public const string SaveCertificate = "Save Certificate";
        public const string PrintCertificateWord = "Print Certificate (Word)";
        public const string PrintCertificatePdf = "Print Certificate (PDF)";
        public const string PreviewCertificate = "Preview Certificate";
        public const string PrintStickerCert = "Print Sticker";
        
        // Settings View Fields
        public const string GeneralReports = "General & Reports";
        public const string ReportSettings = "Report Settings";
        public const string PrintBackground = "Print Background";
        public const string TopMargin = "Top Margin (mm)";
        public const string BottomMargin = "Bottom Margin (mm)";
        public const string RightMargin = "Right Margin (mm)";
        public const string LeftMargin = "Left Margin (mm)";
        public const string CompanyShort = "Establishment Short";
        public const string CompanyNameField = "Establishment Name";
        public const string CompanyPhoneField = "Establishment Phone";
        public const string CompanyCommercialRecord = "Commercial Registration";
        public const string CompanyTaxNumber = "Tax Number";
        public const string CompanyNationalAddress = "National Address";
        public const string ContractRepresentativeName = "Establishment Representative Name";
        public const string ContractRepresentativeId = "Representative ID Number";
        public const string ContractRepresentativePhone = "Representative Mobile";
        public const string CertNumberPrefix = "Certificate Number Prefix";
        public const string AppIcon = "App Icon";
        public const string UploadIcon = "Upload Icon";
        public const string LibreOfficePath = "LibreOffice Path";
        public const string Select = "Select";
        public const string UploadHeader = "Upload Header";
        public const string UploadFooter = "Upload Footer";
        public const string CompanyLogo = "Establishment Logo";
        public const string UploadLogo = "Upload Logo";
        public const string ShowLogoInHeader = "Show logo in document header";
        public const string SaveReportSettings = "Save Report Settings";
        public const string WebVerification = "Web Verification";
        public const string BaseUrl = "Base URL";
        public const string SaveButton = "Save";
        public const string DeleteButton = "Delete";
        public const string Database = "Database";
        public const string CurrentProvider = "Current Provider";
        public const string ConnectionString = "Connection String";
        public const string SaveDbSettings = "Save Database Settings";
        public const string PrintersTab = "Printers";
        public const string A4PrinterSettings = "A4 Printer Settings";
        public const string PrinterName = "Printer Name";
        public const string Properties = "Properties";
        public const string Preferences = "Preferences";
        public const string StickerPrinterSettings = "Sticker Printer Settings";
        public const string PrinterModelProtocol = "Printer Model/Protocol";
        public const string ConnectionType = "Connection Type";
        public const string Host = "Host";
        public const string Port = "Port";
        public const string LabelWidth = "Label Width (mm)";
        public const string LabelHeight = "Label Height (mm)";
        public const string Darkness = "Darkness";
        public const string Speed = "Speed (IPS)";
        public const string DotsPerMm = "Dots/mm";
        public const string ThermalSafeMode = "Thermal Safe Mode";
        public const string ThermalCoolingDelay = "Cooling Delay (sec)";
        public const string SavePrinterSettings = "Save Printer Settings";
        public const string AccessControlTab = "Access Control";
        
        // Clients View Fields
        public const string Name = "Name";
        public const string ClientNotes = "Notes";
        public const string Clear = "Clear";
        public const string Report = "Report";
        public const string DeviceMaintenanceHistory = "Device & Maintenance History";
        public const string DateField = "Date";
        public const string DeviceField = "Device";
        public const string Cost = "Cost";
        public const string CalibrationCertificates = "Calibration Certificates";
        public const string Issue = "Issue";
        public const string Expiry = "Expiry";
        public const string Refresh = "Refresh";
        public const string CompanyClientName = "Establishment/Client Name";
        public const string CalibrationAlertTooltip = "Alert: Devices have expired or are about to expire";
        public const string ClientsSearchSmartTooltip = "Smart search by name or phone";
        public const string RefreshCustomersListTooltip = "Refresh customers list";
        
        // Rentals View Fields (Additional)
        public const string ExpiredRental = "Expired Rental";
        public const string CreateNewRental = "Create New Rental";
        public const string NewRentalForm = "New Rental";
        public const string Company = "Establishment";
        public const string TaxNumberField = "Tax Number";
        public const string IdNumber = "ID/Residence Number";
        public const string Serial2Optional = "Serial 2 (Optional)";
        public const string StartDateField = "Start Date";
        public const string EndDateField = "End Date";
        public const string RentalType = "Rental Type";
        public const string PriceField = "Price";
        public const string PaidAmount = "Paid Amount";
        public const string RemainingAmount = "Remaining Amount";
        public const string StatusField = "Status";
        public const string NewButton = "New";
        public const string ActionsSection = "Actions";
        public const string PrintRentalReceipt = "Print Rental Receipt";
        public const string CompleteRental = "Complete Rental";
        public const string PrintReceipt = "Print Receipt";
        public const string SerialField = "Serial";
        public const string CurrentlyRentedDevices = "Currently Rented Devices";
        public const string RentalReport = "Rental Report";
        public const string RentalStatistics = "Rental Statistics";
        public const string TotalRentals = "Total Rentals";
        public const string CurrentlyActive = "Currently Active";
        public const string ExpiredStatus = "Expired";
        public const string TotalIncome = "Total Income";

        // Rentals View - Contracts, Receipts & Alerts
        public const string DailyPrice = "Daily Price";
        public const string MonthlyPrice = "Monthly Price";
        public const string DeviceValue = "Device Value (SAR)";
        public const string RentalContract = "Rental Contract";
        public const string PrintRentalContract = "Print Rental Contract";
        public const string ReceiveReceiptTitle = "Device Receive Acknowledgment";
        public const string ReturnReceiptTitle = "Device Return Acknowledgment";
        public const string ContractNumberLabel = "Contract Number";
        public const string RentalPeriod = "Rental Period";
        public const string FirstParty = "First Party (Lessor)";
        public const string SecondParty = "Second Party (Lessee)";
        public const string RepresentativeOf = "Representative of the First Party";
        public const string RentalDaysCountLabel = "Days Count";
        public const string PaymentSummary = "Payment Summary";
        public const string ContractTermsTitle = "Terms and Conditions";
        public const string RentalContractTerms = "First: Contract Validity - This contract is effective and binding on both parties from its date of execution, and their signatures constitute full acknowledgment and acceptance of all its terms.\nSecond: The Parties - The First Party is the lessor mentioned above, represented in signing this contract by the representative stated above; the Second Party is the lessee mentioned above.\nThird: Device Ownership - Both parties acknowledge that the rented device remains the exclusive property of the lessor throughout and after the rental term, and this contract does not transfer or convey ownership of the device to the lessee or any third party.\nFourth: Receipt Acknowledgment - The lessee acknowledges and confirms that he/she received the aforementioned device on the contract start date in working condition, in excellent condition and free of defects, having personally inspected it and verified its condition and conformity with its specifications and the serial numbers stated in this contract, and bears full responsibility for it from the moment of receipt.\nFifth: Obligation to Return - The lessee undertakes to return the device to the lessor upon expiry of the rental term in the same condition in which it was received (working and in excellent condition) together with all its accessories and at the agreed location, without any delay.\nSixth: Device Liability - The lessee bears full responsibility for the device from the date of receipt until its actual return, including damage, loss, theft, fire, destruction or any harm regardless of its cause, and shall fully compensate the lessor for all of the foregoing.\nSeventh: Device Value - The parties estimate the value of the aforementioned device at ({0}) Saudi Riyals, and this value constitutes a binding basis for calculating compensation in the event of loss, damage or non-return of the device, which the lessor is entitled to claim in full.\nEighth: No Subleasing - The lessee may not sublease the device, assign this contract or transfer possession of the device to any third party without the prior written consent of the lessor, and any violation thereof is grounds for terminating the contract with all consequences borne by the lessee.\nNinth: Use and Location - The lessee shall use the device in accordance with the operating instructions and solely for its intended purpose, and shall not move it outside the agreed place of use or take it out of the Kingdom without the lessor's written consent.\nTenth: No Tampering - The lessee shall not remove or damage the serial numbers, plates, stickers or any marks indicating the lessor's ownership, and shall not open, modify or repair the device except by the lessor or by a person authorized in writing by the lessor.\nEleventh: Maintenance - The lessor shall carry out general and periodic maintenance of the device during the rental term, and the lessee shall bear the costs of maintenance resulting from misuse, negligence or accidents caused by the lessee.\nTwelfth: Late Return Penalty - If the lessee delays returning the device beyond the agreed date without the lessor's prior written consent, the lessor is entitled to a fee for the delay period equal to the agreed daily rate for each day of delay, without prejudice to the lessor's right to terminate the contract and claim damages.\nThirteenth: No Waiver - The lessor's acceptance of late or partial payments shall not constitute a release of the lessee from obligations nor a waiver of any of the lessor's rights, and waiver of one term shall not affect the enforceability of the remaining terms.\nFourteenth: Joint Liability - If the lessee consists of more than one person, they shall be jointly and severally liable for performing all their obligations under this contract.\nFifteenth: Dispute Resolution - This contract, its interpretation and performance are governed by the laws of the Kingdom of Saudi Arabia, in particular the Civil Transactions Law, and the competent courts in the Kingdom shall have exclusive jurisdiction over any dispute arising out of this contract.\nSixteenth: Acknowledgment - Both parties confirm that they have read, understood and acknowledged all the terms and the accuracy of all data contained in this contract, which is executed in two copies, one for each party.";
        public const string ContractSignatureLessor = "Lessor Signature";
        public const string ContractSignatureRenter = "Renter Signature";
        public const string ReceiveConfirmationText = "I, the receiver, confirm that I have received the above-mentioned device in good condition matching its specifications, and I am responsible for its safekeeping and use during the rental period.";
        public const string ReturnConfirmationText = "I, the receiver, confirm that I have returned the above-mentioned device to the lessor in the same condition in which I received it, and it was inspected and confirmed to be in good condition.";
        public const string RentalEndDateBeforeStart = "End date cannot be before start date";
        public const string MissingRequiredFields = "Please fill in the required fields";
        public const string ValidationTitle = "Input Validation";
        public const string RentalSavedSuccess = "Rental saved successfully";
        public const string RentalUpdatedSuccess = "Rental updated successfully";
        public const string Alert = "Alert";
        public const string SelectRentalFirst = "Please select a rental first";
        public const string GeneratingPdf = "Generating PDF...";
        public const string ExpiringSoonRentals = "Expiring Soon";
        public const string ExpiringSoonRentalsAlert = "Alert: {0} rentals expire within {1} days";
        public const string HandoverReceiptButton = "Receive/Return Receipt";

        
        // Maintenance View Fields
        public const string MaintenanceOrders = "Maintenance Orders";
        public const string MaintenanceOrder = "Maintenance Order";
        public const string SerialNumber1 = "Serial Number 1";
        public const string Accessories = "Accessories";
        public const string Complaint = "Complaint";
        public const string TechnicalReport = "Technical Report";
        public const string PartsCost = "Parts Cost";
        public const string LaborCost = "Labor Cost";
        public const string Total = "Total";
        public const string ReceiptDate = "Receipt Date";
        public const string UpdateDate = "Update Date";
        public const string AddDeviceForSameCustomer = "Add Device for Same Customer";
        public const string AddDeviceTooltip = "Copy customer data and receipt number for easy device addition";
        public const string ShowPrice = "Show Price";
        public const string ShowPriceTooltip = "Show maintenance price";
        public const string IndividualReceipt = "Individual Receipt";
        public const string MaintenanceReport = "Maintenance Report";
        public const string MaintenanceReportTooltip = "Maintenance report";
        public const string PreviewTooltip = "Preview device data and technical report";
        public const string ReceiptSticker = "Receipt Sticker";
        public const string ReceiptStickerTooltip = "Print device receipt sticker";
        public const string MaintenanceRequests = "Maintenance Requests";
        public const string ReceiptNumber = "Receipt Number";
        public const string ReceiptStickerTab = "Receipt Sticker";
        public const string PrintReceiptSticker = "Print Maintenance Receipt Sticker";
        public const string ReceiptStickerNote = "This screen is completely independent from calibration certificate stickers.";
        public const string StickerSize = "Size: Width 41mm × Height 21mm";
        public const string ReceiptStickerSettings = "Receipt Sticker Settings and Print Window";
        public const string PrintNow = "Print Now";
        public const string ReceiptStickerExtraNote = "Note: You can choose a separate printer and adjust settings (scale/offset) in this window; this will not affect any settings of the certificate sticker printer.";
        
        // Maintenance Messages
        public const string CloneDeviceSuccessTitle = "New Device for Same Customer";
        public const string CloneDeviceSuccessMessage = "Data has been copied! Change (device/model/serial) as needed, then press [Save] to add it as a new device on the same receipt.";
        public const string MissingTemplateMessage = "Please specify a Word template for this document from the template management screen first";
        public const string InvalidCostMessage = "Cannot save the work order: cost values must not be negative.";
        public const string SaveSuccessTitle = "Saved";
        public const string SaveSuccessMessage = "The work order was saved successfully.";
        public const string SaveFailedTitle = "Save failed";
        public const string SaveFailedMessage = "Failed to save the work order: {0}";
        public const string MissingClientNameMessage = "Customer name is required before saving.";
        public const string MissingDeviceTypeMessage = "Device type is required before saving.";
        public const string PressEditFirstMessage = "Press [Edit] first to modify the selected order.";
        public const string DeleteConfirmTitle = "Confirm deletion";
        public const string DeleteConfirmMessage = "Are you sure you want to delete this order?\nReceipt No.: {0}\nCustomer: {1}\nThe device and its linked certificate will be permanently deleted. This cannot be undone.";
        public const string DeletedSuccessTitle = "Deleted";
        public const string DeletedSuccessMessage = "The work order was deleted successfully.";
        public const string PrintRequiresSaveTitle = "Save required first";
        public const string PrintRequiresSaveMessage = "You cannot print without saving the data. Enter the customer name and device type, then save the order first.";
        public const string SaveBeforePrintPrompt = "The current data is not saved yet.\nDo you want to save it now and then print?";

        // Maintenance Reports / Statistics / Financials Tab
        public const string MaintenanceReportsTab = "Reports, Statistics & Financials";
        public const string ReportsFrom = "From Date";
        public const string ReportsTo = "To Date";
        public const string TotalOrders = "Total Orders";
        public const string OrdersInProgress = "In Progress";
        public const string CompletedDelivered = "Completed / Delivered";
        public const string TotalPartsCost = "Total Parts Cost";
        public const string TotalLaborCost = "Total Labor Cost";
        public const string StatusBreakdown = "Breakdown by Status";
        public const string MonthlyBreakdown = "Monthly Breakdown";
        public const string ExportPdf = "Export PDF";
        public const string ExportExcel = "Export Excel";
        public const string MonthColumn = "Month";
        public const string OrdersCount = "Order Count";
        public const string TotalCostColumn = "Total Cost";
        public const string NoReportsData = "No data in the selected range";
        public const string ReportsExportFailed = "Failed to export the report";
        public const string ReportsTitle = "Maintenance Statistics & Financial Report";
        public const string ReportsGeneratedOn = "Report Generated On";
        public const string ReportNumberLabel = "Report Number";
        public const string SignatureMaintenanceResponsible = "Maintenance Supervisor Signature";
        public const string SignatureManager = "Manager Signature";
        public const string NameLabel = "Name";
        public const string SignatureLabel = "Signature";
        public const string PageLabel = "Page";
        public const string ReportDate = "Report Date";
        public const string ClientNameLabel = "Client Name";
        public const string MaintenanceOrdersCount = "Maintenance Orders Count";
        public const string CertificatesCount = "Certificates Count";
        public const string TotalMaintenanceCost = "Total Maintenance Cost";
        public const string CertificatesHistory = "Certificates History";
        public const string SignatureCustomer = "Customer Signature";
        public const string SignatureDepartmentManager = "Department Manager Signature";
        public const string DateColumn = "Date";
        
        // Inventory View Fields
        public const string AddEditSparePart = "Add / Edit Spare Part";
        public const string PartCodeSKU = "Part Code (SKU)";
        public const string ItemNameField = "Item Name *";
        public const string CategoryField = "Category *";
        public const string BrandField = "Brand";
        public const string CurrentQuantity = "Current Quantity";
        public const string MinimumThreshold = "Minimum Threshold";
        public const string CostPrice = "Cost Price";
        public const string SellingPrice = "Selling Price";
        public const string PurchaseDate = "Purchase Date";
        public const string Location = "Location";
        public const string LowStockAlert = "Warning: (";
        public const string LowStockAlertEnd = ") items have reached minimum threshold, please order immediately!";
        public const string ShowLowStockOnly = "Show Low Stock Only";
        public const string SearchRefresh = "Search / Refresh";
        public const string SearchTooltip = "Search by code, name, or brand";
        public const string PrintReport = "Print Report";
        public const string Code = "Code";
        public const string NameField = "Name";
        public const string BrandColumn = "Brand";
        
        // Device History View Fields
        public const string SerialSearch = "Serial Search";
        public const string DeviceHistoryField = "Device History";
        public const string HistoryType = "Type";
        public const string HistoryTitle = "Title";
        
        // Sticker Designer View Fields
        public const string PageTab = "Page";
        public const string WidthMm = "Width (mm)";
        public const string PreviewScale = "Preview Scale";
        public const string HeightMm = "Height (mm)";
        public const string GridTab = "Grid";
        public const string ShowPreviewGrid = "Show Preview Grid";
        public const string SnapToGrid = "Snap-To-Grid";
        public const string EnableMouseEditing = "Enable Mouse Editing";
        public const string GridStepMm = "Grid Step (mm)";
        public const string HeaderTab = "Header";
        public const string UseLogo = "Use Logo";
        public const string HeaderTitle = "Header Title";
        public const string StickerCompanyName = "Establishment Name";
        public const string StickerAddress = "Address";
        public const string StickerPhone = "Phone";
        public const string HeaderFontSize = "Header Font Size";
        public const string ItemTab = "Item";
        public const string XPosition = "X Position";
        public const string YPosition = "Y Position";
        public const string ItemFontSize = "Item Font Size";
        public const string ContentTab = "Content";
        public const string DeviceTypeName = "Device Type/Name";
        public const string StickerBrand = "Brand";
        public const string SerialNumberField = "Serial Number (S/N)";
        public const string StickerCalibrationDate = "Calibration Date";
        public const string StickerExpiryDate = "Expiry Date";
        public const string QrAndActions = "QR & Actions";
        public const string ShowQrInPreview = "Show QR in Preview";
        public const string SaveLayout = "Save Layout";
        public const string ResetLayout = "Reset Layout";
        public const string TestPrint = "Test Print";
        public const string QrLabel = "QR";
        public const string GenerateAndSaveQr = "Generate & Save QR";
        public const string DetectStickerPrinter = "Detect Sticker Printer";
        public const string DetectedPrinters = "Detected Printers";
        public const string ApplySelectedPrinter = "Apply Selected Printer";
        public const string PrinterDriver = "Driver";
        public const string PrinterPort = "Port";
        public const string PrinterDpi = "Resolution (DPI)";
        public const string PrinterProtocolHeader = "Protocol";
        public const string ThermalPrinterType = "Thermal";
        public const string StandardPrinterType = "Standard";
        public const string NoPrinterSelected = "Select a printer first.";
        public const string PrinterDetectedMessage = "Printer selected: {0} ({1}, {2} DPI)";
        public const string ThermalHint = "Thermal printers are highlighted in blue";
        // Sticker Designer Extra
        public const string SaveAsTemplate = "Save as Template";
        public const string ContentHeaderCompanyName = "Establishment Name:";
        public const string ContentHeaderAddress = "Address:";
        public const string ContentHeaderPhone = "Phone:";
        public const string LogoSettings = "Logo Settings";
        public const string UseLogoText = "Use Logo";
        public const string PickLogo = "Pick Logo Image";
        public const string AddItems = "Add Items";
        public const string AddTextItem = "Add Text";
        public const string AddLineItem = "Add Line";
        public const string DeleteSelectedItem = "Delete Selected";
        public const string SelectedItemProperties = "Selected Item Properties";
        public const string ItemNameLabel = "Item Name:";
        public const string PositionX = "Position X:";
        public const string PositionY = "Position Y:";
        public const string WidthLabel = "Width:";
        public const string HeightLabel = "Height:";
        public const string PositionXMm = "Position X (mm):";
        public const string PositionYMm = "Position Y (mm):";
        public const string WidthMmLabel = "Width (mm):";
        public const string HeightMmLabel = "Height (mm):";
        public const string RotationLabel = "Rotation:";
        public const string FontSizeLabel = "Font Size:";
        public const string FontColorLabel = "Font Color:";
        public const string FontFamilyLabel = "Font Family:";
        public const string BoldLabel = "Bold";
        public const string ItalicLabel = "Italic";
        public const string UnderlineLabel = "Underline";
        public const string AlignmentLabel = "Alignment:";
        public const string QrSizeMm = "QR Size (mm):";
        public const string ActionsTab = "Actions";
        public const string TextLabel = "Text:";
        public const string BindToVariableLabel = "Bind to Variable:";
        public const string ZIndexLabel = "Z-Index:";
        public const string ItemVisibility = "Item Visibility";
        public const string BodyFontSize = "Body Font Size";
        public const string AutoLayout = "Auto Layout";
        public const string DeviceDataSettings = "Device Data";
        public const string FontSettings = "Font Settings";
        public const string ModelLabel = "Model:";
        public const string CertificateNumberField = "Certificate No.:";
        public const string CustomTemplatesLabel = "Custom Templates";
        public const string TestPrintSent = "Test print sent successfully.";
        public const string TestPrintFailed = "Test print failed: {0}";
        public const string NoPrintersDetected = "No printers detected.";
        public const string PrinterDetectionFailed = "Printer detection failed: {0}";
        public const string LayoutSaved = "Layout saved.";
        public const string LayoutResetDone = "Layout reset.";
        public const string PreviewFailed = "Failed to open preview: {0}";
        public const string EnterTemplateName = "Enter template name:";
        public const string TemplateSavedMsg = "Template saved: {0}";
        public const string TemplateAppliedMsg = "Template applied: {0}";
        public const string TemplateNameRequired = "Please enter a template name.";
        public const string SaveTemplateTitle = "Save as Template";
        public const string QrItemNotFound = "QR item not found in sticker layout.";
        public const string QrGenerateFailed = "Failed to generate QR code image.";
        public const string QrCodeSavedMsg = "QR Code saved to {0}";
        public const string QrSaveError = "Error generating or saving QR code: {0}";

        // Settings Access/License
        public const string CurrentStatus = "Current Status";
        public const string LoginAs = "Logged in as";
        public const string LoginAsAdmin = "Admin Login";
        public const string LogoutAdmin = "Admin Logout";
        public const string LogoutUser = "User Logout";
        public const string UserManagement = "User Management";
        public const string UserLabel = "User";
        public const string AddUserButton = "Add";
        public const string DeleteUserButton = "Delete";
        public const string ChangePasswordButton = "Change Password";
        public const string SavePermissions = "Save Permissions";
        public const string AdminPin = "Admin PIN";
        public const string AdminPinNote = "Default PIN: 123456 (can be changed).";
        public const string ChangeAdminPin = "Change PIN";
        public const string LicenseTab = "License";
        public const string LicenseInfo = "License Info";
        public const string LicenseStatusLabel = "License Status:";
        public const string LicenseExpiryLabel = "Expiry Date:";
        public const string HardwareIdLabel = "Hardware ID:";
        public const string CopyButton = "Copy";
        public const string LicenseUpdate = "License Update";
        public const string EnterNewKey = "If you have a new activation key, enter it here:";
        public const string ActivateLicense = "Activate License";
        
        // Home View
        public const string Dashboard = "Dashboard";
        public const string SystemOverview = "System performance overview";
        public const string TotalCertificates = "Total Certificates";
        public const string ActiveRentals = "Active Rentals";
        public const string ExpiringSoon = "Expiring Soon";
        public const string TotalClients = "Total Clients";
        public const string RecentActivity = "Recent Activity";
        public const string QuickActions = "Quick Actions";
        public const string NewCertificate = "New Certificate";
        public const string NewRental = "New Rental";
        public const string NewClient = "New Client";
        public const string ViewReports = "View Reports";
        public const string LowStock = "Low Stock";
        public const string LastCertificates = "Latest Certificates";
        public const string LastMaintenances = "Latest Maintenances";
        
        // Home View
        public const string SearchCertificate = "Search Certificate";
        public const string ExpiredDevices = "Expired Devices";
        public const string Statistics = "Statistics";
        public const string MaintenanceRecords = "Maintenance Records";
        
        // Maintenance Receipt Window
        public const string DeviceReceipt = "Device Receipt";
        public const string RentalDeviceReceipt = "Rental Device Receipt";
        public const string Printer = "Printer";
        public const string SelectPrinter = "Select Printer";
        public const string RefreshPreview = "Refresh Preview";
        public const string PrintReceiptWindow = "Print";
        public const string CloseReceipt = "Close";
        public const string UseLetterhead = "Use Letterhead";
        public const string VerticalOffset = "Vertical Offset:";
        public const string HorizontalOffset = "Horizontal Offset:";
        public const string HeaderMargin = "Header Margin:";
        public const string FooterMargin = "Footer Margin:";
        public const string FrameColor = "Frame Color:";
        public const string ZoomIn = "Zoom In";
        public const string ZoomOut = "Zoom Out";
        public const string FitWidth = "Fit to Width";
        public const string FitPage = "Fit Page";
        
        // Maintenance Preview Window
        public const string MaintenanceOrderPreview = "Maintenance Order Preview";
        public const string ClientDataPreview = "Client Data";
        public const string ClientNamePreview = "Client Name";
        public const string PhonePreview = "Phone";
        public const string DeviceDataPreview = "Device Data";
        public const string DeviceTypePreview = "Device Type";
        public const string ModelPreview = "Model";
        public const string SerialNumberPreview = "Serial Number";
        public const string AccessoriesPreview = "Accessories";
        public const string ReceivedDatePreview = "Received Date";
        public const string UpdatedDatePreview = "Updated Date";
        public const string TotalPreview = "Total";
        public const string ComplaintPreview = "Complaint";
        public const string TechnicalReportPreview = "Technical Report";
        
        // Maintenance Receipt Sticker Window
        public const string MaintenanceSticker = "Maintenance Receipt Sticker";
        public const string MaintenanceStickerSettings = "Sticker Settings";
        public const string MaintenanceStickerData = "Sticker Data";
        public const string DeviceSerial = "Device Serial";
        public const string RealDimensions = "Real Dimensions (mm)";
        public const string StickerWidthMm = "Width (mm)";
        public const string StickerHeightMm = "Height (mm)";
        public const string DefaultSize = "Default: 41 × 21 mm";
        public const string StickerPrinter = "Sticker Printer";
        public const string SelectPrinterButton = "Select Printer";
        public const string ScaleContent = "Scale Content";
        public const string HorizontalOffsetValue = "Horizontal Offset";
        public const string VerticalOffsetValue = "Vertical Offset";
        public const string RealPreview = "Real Preview";
        public const string PrintStickerButton = "Print Sticker";
        
        // Client Report Window
        public const string ClientAccountStatement = "Client Account Statement";
        
        // Sticker Preview Window
        public const string StickerPreviewWindow = "Sticker Preview";
        
        // Certificates View
        public const string CertificateNumber = "Certificate Number";
        public const string DeviceName = "Device Name";
        public const string SerialNumber = "Serial Number";
        public const string CalibrationDate = "Calibration Date";
        public const string ValidUntil = "Valid until";
        public const string ExpiryDate = "Expiry Date";
        public const string Status = "Status";
        public const string Active = "Active";
        public const string Expired = "Expired";
        public const string AddCertificate = "Add Certificate";
        public const string EditCertificate = "Edit Certificate";
        public const string DeleteCertificate = "Delete Certificate";
        public const string PrintCertificate = "Print Certificate";
        public const string ExportPDF = "Export PDF";

        // Certificates Tabs / Template Settings
        public const string TemplateDocTypeStep = "1) Select Document Type";
        public const string TemplateChooseStep = "2) Choose and Save Template";
        public const string DocumentType = "Document Type";
        public const string CurrentPath = "Current Path";
        public const string ChooseWordTemplate = "Choose Word File (.docx)";
        public const string SaveTemplateSetting = "Save Setting";
        public const string TemplateTagsDictionary = "Template Tags Dictionary (Optional)";
        public const string DescriptionLabel = "Description";
        public const string TagLabel = "Tag";
        
        // Maintenance View
        public const string MaintenanceRequest = "Maintenance Request";
        public const string RequestDate = "Request Date";
        public const string Description = "Description";
        public const string Priority = "Priority";
        public const string High = "High";
        public const string Medium = "Medium";
        public const string Low = "Low";
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string AddMaintenance = "Add Maintenance";
        public const string EditMaintenance = "Edit Maintenance";
        
        // Clients View
        public const string ClientName = "Client Name";
        public const string PhoneNumber = "Phone Number";
        public const string Email = "Email";
        public const string Address = "Address";
        public const string TaxNumber = "Tax Number";
        public const string IDNumber = "ID Number";
        public const string AddClient = "Add Client";
        public const string EditClient = "Edit Client";
        public const string DeleteClient = "Delete Client";
        
        // Inventory View
        public const string ItemName = "Item Name";
        public const string Quantity = "Quantity";
        public const string Unit = "Unit";
        public const string Price = "Price";
        public const string Category = "Category";
        public const string AddItem = "Add Item";
        public const string EditItem = "Edit Item";
        public const string DeleteItem = "Delete Item";
        public const string StockIn = "Stock In";
        public const string StockOut = "Stock Out";
        
        // Settings View
        public const string General = "General";
        public const string Printers = "Printers";
        public const string AccessControl = "Access Control";
        public const string License = "License";
        public const string Backup = "Backup";
        public const string CloudBackup = "Cloud Backup";
        public const string UploadToCloud = "Upload to Cloud";
        public const string AutoUpload = "Auto Upload";
        public const string VerificationLink = "Verification Link";
        public const string CopyVerificationLink = "Copy Verification Link";
        public const string OpenVerificationLink = "Open Verification Link";
        public const string DownloadCloudBackup = "Download Cloud Backup";
        public const string UploadCloudBackup = "Upload Cloud Backup";
        public const string CloudConnectionStatus = "Cloud Connection Status:";
        public const string OfflineStatus = "Offline";
        public const string CloudBackupList = "Cloud Backup List:";
        public const string CloudBackupUnavailable = "Cloud backup is currently unavailable";
        public const string CompanyName = "Establishment Name";
        public const string CompanyAddress = "Establishment Address";
        public const string CompanyPhone = "Establishment Phone";
        public const string CompanyEmail = "Establishment Email";
        public const string Logo = "Logo";
        public const string DefaultPrinter = "Default Printer";
        public const string AdminPIN = "Admin PIN";
        public const string DefaultPIN = "Default PIN";
        public const string ChangePIN = "Change PIN";
        public const string Users = "Users";
        public const string AddUser = "Add User";
        public const string Username = "Username";
        public const string Password = "Password";
        public const string Permissions = "Permissions";
        public const string LicenseKey = "License Key";
        public const string DeviceFingerprint = "Device Fingerprint";
        public const string Copy = "Copy";
        public const string BackupPath = "Backup Path";
        public const string Change = "Change";
        public const string CreateBackup = "Create Backup";
        public const string RestoreBackup = "Restore Backup";
        public const string DeleteBackup = "Delete Backup";
        public const string LocalBackups = "Local Backups";
        
        // Backup Settings
        public const string BackupSettings = "Backup Settings";
        public const string DefaultBackupPath = "Default path: Backups folder next to program file";
        public const string BackupEncryption = "Backup Encryption";
        public const string EnableBackupEncryption = "Enable backup encryption (AES-256)";
        public const string EncryptionPassword = "Encryption Password";
        public const string GeneratePassword = "Generate";
        public const string EncryptionPasswordNote = "Note: Password is required to decrypt backups when restoring";
        public const string AutoBackup = "Automatic Backup";
        public const string EnableAutoBackup = "Enable automatic backup";
        public const string BackupSchedule = "Backup Schedule";
        public const string Daily = "Daily";
        public const string Weekly = "Weekly";
        public const string Monthly = "Monthly";
        public const string BackupTime = "Backup Time";
        public const string MaxBackupCount = "Max Backup Count";
        public const string SaveBackupSettings = "Save Settings";
        public const string LocalBackup = "Local Backup";
        public const string BackupList = "Backup List:";
        
        // Backup Messages
        public const string BackupSettingsSaved = "Backup settings saved successfully.";
        public const string BackupSettingsSaveFailed = "Failed to save backup settings.";
        public const string BackupPasswordGenerated = "New backup password generated.";
        public const string DatabaseFileNotFound = "Database file not found.";
        public const string EncryptedBackupCreated = "Encrypted backup created successfully:";
        public const string BackupCreated = "Backup created successfully:";
        public const string BackupCreationError = "Error creating backup:";
        public const string PasswordRequiredForEncrypted = "Password required for encrypted backup.";
        public const string BackupInvalidOrWrongPassword = "Backup is invalid or password is incorrect.";
        public const string BackupInvalid = "Backup is invalid.";
        public const string BackupRestoredSuccessfully = "Backup restored successfully. Please restart the application.";
        public const string BackupRestoreError = "Error restoring backup:";
        public const string BackupPathSelected = "Backup path selected:";
        public const string BackupPathError = "Error selecting path:";
        public const string SelectBackupFile = "Select backup file";
        public const string BackupPasswordTitle = "Backup password";
        public const string EnterBackupPassword = "Enter encrypted backup password:";
        public const string ConfirmRestoreBackup = "Are you sure you want to restore this backup? The current database will be replaced.";
        public const string ConfirmRestoreTitle = "Confirm Restore";
        public const string SelectAnyFileInBackupFolder = "Select any file in the desired backup folder";
        public const string Date = "Date";
        public const string Size = "Size";
        public const string Actions = "Actions";
        public const string Save = "Save";
        public const string Cancel = "Cancel";
        public const string Delete = "Delete";
        public const string Edit = "Edit";
        public const string Close = "Close";
        
        // Rentals View
        public const string RentalNumber = "Rental Number";
        public const string Client = "Client";
        public const string Device = "Device";
        public const string StartDate = "Start Date";
        public const string EndDate = "End Date";
        public const string DailyRate = "Daily Rate";
        public const string TotalAmount = "Total Amount";
        public const string Deposit = "Deposit";
        public const string Notes = "Notes";
        public const string AddRental = "Add Rental";
        public const string EditRental = "Edit Rental";
        public const string DeleteRental = "Delete Rental";
        public const string ReturnDevice = "Return Device";
        public const string PrintContract = "Print Contract";
        
        // Device History View
        public const string History = "History";
        public const string DeviceDetails = "Device Details";
        public const string CalibrationHistory = "Calibration History";
        public const string RentalHistory = "Rental History";
        public const string MaintenanceHistory = "Maintenance History";
        public const string ViewDetails = "View Details";
        
        // Sticker Designer View
        public const string StickerTemplate = "Sticker Template";
        public const string Preview = "Preview";
        public const string Reprint = "Reprint";
        public const string PrintSticker = "Print Sticker";
        public const string Customize = "Customize";
        public const string Text = "Text";
        public const string Barcode = "Barcode";
        public const string QRCode = "QR Code";
        
        // Help Window
        public const string HelpTitle = "Help";
        public const string SearchHelp = "Search Help";
        public const string GettingStarted = "Getting Started";
        public const string UserGuide = "User Guide";
        public const string FAQ = "FAQ";
        public const string ContactSupport = "Contact Support";
        public const string HelpPrintGuide = "Print Guide";
        public const string HelpExportPdf = "Export PDF";
        public const string Introduction = "Introduction";
        public const string Navigation = "Navigation";
        public const string CertificatesHelp = "Certificates";
        public const string MaintenanceHelp = "Maintenance";
        public const string ClientsHelp = "Clients";
        public const string InventoryHelp = "Inventory";
        public const string SettingsHelp = "Settings";
        public const string RentalsHelp = "Rentals";
        
        // Login Window
        public const string LoginTitle = "Login";
        public const string Manager = "Manager";
        public const string Enter = "Enter";
        public const string InvalidCredentials = "Invalid credentials";
        
        // Messages
        public const string Success = "Success";
        public const string Error = "Error";
        public const string Warning = "Warning";
        public const string Information = "Information";
        public const string ConfirmDelete = "Are you sure you want to delete?";
        public const string ItemDeleted = "Item deleted successfully";
        public const string ItemSaved = "Item saved successfully";
        public const string ItemUpdated = "Item updated successfully";
        public const string NoNotifications = "No new notifications";
        public const string ExpiredRentalsAlert = "Alert: {0} expired rentals";
        public const string ExpiringCertificatesAlert = "Alert: {0} certificates expiring within 7 days";
        public const string LanguageInDevelopment = "Language selection - Under development";
        public const string ErrorOpeningHelp = "Error opening help window";

        // Certificates: validation messages (MessageBox)
        public const string MsgCannotOpenPreview = "Cannot open preview";
        public const string MsgCannotOpenFile = "Cannot open file";
        public const string MsgMissingData = "Missing data";
        public const string MsgClientNameRequired = "Please enter the client name first.";
        public const string MsgModelRequired = "Please enter the model first.";
        public const string MsgSerialRequired = "Please enter the serial first.";
        public const string MsgAutoLevelDeviationRequired = "Please enter the Auto Level deviation rate first.";
        public const string MsgTemplateMissingForType = "Please select a Word template for this type from Template Management first";
        public const string MsgTemplateMissing = "Template not found";
        public const string MsgSaveMethod = "Save method";
        public const string MsgArchiveSavePrompt = "This certificate was loaded from the archive.\nDo you want to edit the current certificate (replace it) or save it as a new one?\n\nYes = edit current certificate\nNo = save as new certificate\nCancel = cancel operation";

        // Certificates: archive context menu
        public const string EditRename = "Edit / Rename";
        public const string Open = "Open";

        // Certificates: status bar messages
        public const string StSelectWordTemplateFirst = "Please select a Word template first from Template Management.";
        public const string StFailedToCreateWordFile = "Failed to create Word file.";
        public const string StCloudNotConfigured = "Cloud upload is disabled: configure CloudApiBase and CloudApiKey in the config file.";
        public const string StHelpPrintFailed = "Could not print: {0}";
        public const string StHelpExportFailed = "Could not export: {0}";
        public const string StUploadFailed = "Could not upload certificate: {0}";
        public const string StUploadHttpFailed = "Upload failed: {0} {1}. {2}";
        public const string StUploadedWithUrl = "Certificate uploaded: {0}";
        public const string StUploadedOk = "Certificate uploaded successfully.";
        public const string StUploadedPrefix = "Certificate uploaded";
        public const string StSavedButNoWord = "Certificate saved, but upload failed: select a Word template first from Template Management.";
        public const string StSavedButNoWordFile = "Certificate saved, but could not create Word file for upload.";
        public const string StSavedButNoPdf = "Certificate saved, but could not create PDF file for upload.";
        public const string StUpdatedUploaded = "Uploaded file updated successfully: {0}";
        public const string StAutoUploaded = "Certificate auto-uploaded successfully: {0}";
        public const string StUpdatedUploadedPlain = "Uploaded file updated successfully.";
        public const string StAutoUploadedPlain = "Certificate auto-uploaded successfully.";
        public const string StCreatedPdf = "PDF created: {0}";
        public const string StPdfExportFailed = "Could not export PDF: {0}";
        public const string StPreparingPreview = "Preparing preview...";
        public const string StSaving = "Saving...";
        public const string StUploadingCloud = "Uploading certificate to cloud...";
        public const string StDeleting = "Deleting...";
        public const string StPdfPreviewFailedLibreOffice = "PDF preview failed: install LibreOffice or set LIBREOFFICE_PROGRAM.";
        public const string StBgFromTextFailed = "Could not generate background from text file.";
        public const string StBgImageSelected = "Background image selected for the template.";
        public const string StUploadTemplateFirst = "Upload a template first, then preview.";
        public const string StDocxPreviewCreated = "DOCX preview created.";
        public const string StPreviewDocxOnly = "Preview supports Word (DOCX) templates only.";
        public const string StPreviewErrorDetails = "Error while creating preview (details in last-pdf-error.txt): {0}";
        public const string StPreparingPdfPreview = "Preparing PDF preview...";
        public const string StTemplateLoaded = "Template loaded: {0}";
        public const string StTemplateLoadFailed = "Could not load the selected template.";
        public const string StTemplateSaveCancelled = "Template save cancelled.";
        public const string StTemplateSaved = "Template saved.";
        public const string StTemplateSaveFailed = "Could not save template.";
        public const string StTemplateCreated = "New template created and saved.";
        public const string StTemplateCreateFailed = "Could not create template.";
        public const string StFilePrepareFailed = "Could not prepare file: {0}";
        public const string StWordTemplateMissing = "Word template is missing. Re-upload it from Template Management.";
        public const string StSendingSticker = "Sending sticker...";
        public const string StStickerSentWindows = "Sticker sent via Windows driver.";
        public const string StStickerSentLayout = "Sticker {0} (Layout) sent to printer.";
        public const string StStickerSendFailed = "Failed to send sticker: {0}";
        public const string StPreparingStickerPreview = "Creating sticker preview...";
        public const string StStickerPreviewOpened = "Sticker preview opened using the real design.";
        public const string StStickerPreviewFailed = "Preview failed: {0}";
        public const string StFieldsAutoDistributed = "Fields distributed automatically.";
        public const string StFieldRemoved = "Field removed from design.";
        public const string LoadingData = "Loading...";
        public const string NoDataAvailable = "No data available";
        public const string NoSearchResults = "No matching results";
        public const string StLayoutReset = "Layout reset.";
        public const string StVerifyLinkCopied = "Verification link copied.";
        public const string StCancelled = "Operation cancelled.";
        public const string StCertificateSaved = "Certificate saved.";
        public const string StCertificateNumberDuplicate = "Certificate number is a duplicate! This number is already used by another certificate. Save cancelled.";
        public const string StSaveFailed = "Could not save: {0}";

        // Certificates: mode labels
        public const string ModeNew = "Mode: New";
        public const string ModeEdit = "Mode: Edit";

        // Certificates: layout field names
        public const string FieldClientName = "Client Name";
        public const string FieldDate = "Date";
        public const string FieldExpiryDate = "Expiry Date";
        public const string FieldCertNo = "Certificate No.";
        public const string FieldWorkNo = "Work Order";
        public const string FieldReportType = "Report Type";
        public const string FieldPhone = "Phone";
        public const string FieldDeviceType = "Device Type";
        public const string FieldBrand = "Brand";
        public const string FieldModel = "Model";
        public const string FieldSerial = "Serial";
        public const string FieldTableStart = "Table Start";
        public const string FieldGeneric = "Field";

        // Certificates: template hub document types
        public const string DocTypeInvoice = "Individual Receipt";
        public const string DocTypeRentalReceipt = "Rental Device Receipt";
        public const string BrandMiscChineseDevices = "Various Chinese devices";
    }

    private static readonly Dictionary<string, string> ArabicTable = LoadTable(typeof(Arabic));
    private static readonly Dictionary<string, string> EnglishTable = LoadTable(typeof(English));

    public static IReadOnlyCollection<string> Keys => ArabicTable.Keys;

    private static Dictionary<string, string> LoadTable(Type type)
    {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.IsLiteral && field.FieldType == typeof(string))
                table[field.Name] = (string)field.GetValue(null)!;
        }
        return table;
    }

    public static string Get(string key)
    {
        var table = Services.LanguageService.Instance.IsRTL ? ArabicTable : EnglishTable;
        return table.TryGetValue(key, out var value) ? value : key;
    }
}
