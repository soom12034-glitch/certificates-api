using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using BlueMax.Presentation.Wpf.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BlueMax.Presentation.Wpf.ViewModels
{
    public class HelpViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<HelpCategory> HelpCategories { get; }
        private HelpContent? _selectedContent;
        public HelpContent? SelectedContent
        {
            get => _selectedContent;
            set
            {
                _selectedContent = value;
                OnPropertyChanged(nameof(SelectedContent));
            }
        }
        public string SearchText { get; set; } = "";

        public ICommand SelectCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand CloseCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private Window? _ownerWindow;

        public HelpViewModel()
        {
            HelpCategories = new ObservableCollection<HelpCategory>();
            LoadHelpContent();
            
            SelectCommand = new RelayCommand(param => 
            {
                if (param is HelpCategory category)
                    SelectCategory(category);
            });
            PrintCommand = new RelayCommand(_ => PrintHelp());
            ExportPdfCommand = new RelayCommand(_ => ExportToPdf());
            CloseCommand = new RelayCommand(_ => CloseWindow());
            
            // Select first category by default
            if (HelpCategories.Count > 0 && HelpCategories[0].Content != null)
                SelectedContent = HelpCategories[0].Content;
        }

        public void SetOwnerWindow(Window window)
        {
            _ownerWindow = window;
        }

        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void LoadHelpContent()
        {
            var lang = LanguageService.Instance.CurrentCulture.TwoLetterISOLanguageName;

            // البدء السريع
            var quickStart = new HelpCategory
            {
                Title = lang == "ar" ? "البدء السريع" : "Quick Start",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "البدء السريع" : "Quick Start",
                    Content = lang == "ar"
                        ? @"مرحباً بك في نظام إدارة الشهادات والإيجارات.

**ما هو البرنامج؟**
هذا برنامج متكامل لإدارة شهادات المعايرة، الإيجارات، أوامر العمل، والمخزون. يساعدك في طباعة الشهادات، إدارة الأجهزة المستأجرة، وتتبع الصيانة.

**خطوات البدء:**

1. **تسجيل الدخول**
   - أدخل اسم المستخدم وكلمة المرور
   - إذا كانت أول مرة، استخدم حساب المدير

2. **إنشاء أول شهادة**
   - اذهب إلى قسم الشهادات
   - اختر نوع الجهاز (Total Station, GPS, Auto Level)
   - املأ بيانات الجهاز (الماركة، الموديل، الرقم التسلسلي)
   - املأ بيانات العميل (الاسم، الهاتف)
   - اختر القالب المناسب
   - اضغط على طباعة

3. **طباعة المستند**
   - تأكد من إعداد الطابعة في الإعدادات
   - اضغط على طباعة مباشرة
   - المستند سيتم طباعته على الطابعة A4 المحددة"
                        : @"Welcome to the Certificates & Rentals management system.

**What is this app?**
It manages calibration certificates, rentals, work orders, and inventory. It helps print certificates, track rented devices, and follow maintenance.

**Getting started:**

1. **Login**
   - Enter username and password
   - For first time, use the admin account

2. **Create your first certificate**
   - Go to Certificates
   - Choose device type (Total Station, GPS, Auto Level)
   - Fill device data (brand, model, serial)
   - Fill client data (name, phone)
   - Pick a template
   - Click Print

3. **Print the document**
   - Ensure printer is set in Settings
   - Click Print
   - It prints on the selected A4 printer"
                }
            };

            // إدارة المستخدمين
            var userManagement = new HelpCategory
            {
                Title = lang == "ar" ? "إدارة المستخدمين" : "User Management",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "إدارة المستخدمين" : "User Management",
                    Content = lang == "ar"
                        ? @"**إنشاء مستخدم المدير**
عند أول تشغيل للبرنامج، سيتم إنشاء حساب المدير تلقائياً. تأكد من حمايته بكلمة مرور قوية.

**إضافة مستخدم جديد**
1. اذهب إلى الإعدادات
2. اختر إدارة المستخدمين
3. اضغط على إضافة مستخدم
4. املأ البيانات:
   - اسم المستخدم
   - كلمة المرور
   - الاسم الكامل
   - الدور (صلاحيات)
5. اضغط على حفظ

**إدارة الصلاحيات**
- صلاحيات القراءة: عرض البيانات فقط
- صلاحيات الكتابة: إضافة وتعديل البيانات
- صلاحيات الحذف: حذف البيانات
- صلاحيات المدير: كل الصلاحيات

**تعديل وحذف المستخدمين**
- لتعديل مستخدم: اختر المستخدم واضغط على تعديل
- لحذف مستخدم: اختر المستخدم واضغط على حذف (مع تأكيد)"
                        : @"**Create the Admin user**
On first run, an Admin account is created automatically. Protect it with a strong password.

**Add a new user**
1. Go to Settings
2. Choose User Management
3. Click Add User
4. Fill the fields:
   - Username
   - Password
   - Full name
   - Role (permissions)
5. Click Save

**Manage permissions**
- Read: view data only
- Write: add and edit data
- Delete: delete data
- Admin: all permissions

**Edit/Delete users**
- To edit: select a user and click Edit
- To delete: select a user and click Delete (with confirmation)"
                }
            };

            // القوالب
            var templates = new HelpCategory
            {
                Title = lang == "ar" ? "القوالب" : "Templates",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "القوالب" : "Templates",
                    Content = lang == "ar"
                        ? @"**أنواع القوالب**
- شهادات المعايرة (Total Station, GPS, Auto Level)
- إيصالات الاستلام الفردية
- إقرارات استلام أجهزة الإيجار
- عروض الأسعار (Price Quotation)
- تقارير الصيانة (Maintenance Report)
- الفواتير (Invoice)

**إنشاء قالب جديد**
1. اذهب إلى إعدادات القوالب
2. اختر نوع المستند
3. اضغط على اختيار ملف Word
4. حدد ملف .docx
5. اضغط على حفظ الإعداد

**استخدام الوسوم**
1. افتح قاموس الوسوم في إعدادات القوالب
2. اختر نوع المستند
3. انسخ الوسوم المطلوبة
4. الصقها في قالب Word

**الوسوم المتاحة:**
- {{ client_name }} - اسم العميل
- {{ phone }} - رقم الهاتف
- {{ device_type }} - نوع الجهاز
- {{ brand }} - الماركة
- {{ model }} - الموديل
- {{ serial }} - الرقم التسلسلي
- {{ tax_number }} - الرقم الضريبي (للإيجارات)
- {{ id_number }} - رقم الهوية/الإقامة (للإيجارات)

**تسمية القوالب**
استخدم نظام تسمية موحد:
- شهادة معايرة Total Station - 2024
- إقرار استلام إيجار GPS
- إيصال استلام فردى - قياسي"
                        : @"**Template types**
- Calibration Certificates (Total Station, GPS, Auto Level)
- Individual Receipts
- Rental Handover Forms
- Price Quotations
- Maintenance Reports
- Invoices

**Create a new template**
1. Open Template Settings
2. Choose document type
3. Click Choose Word File
4. Select a .docx file
5. Click Save Setting

**Using tags**
1. Open the Tags Dictionary in Template Settings
2. Select the document type
3. Copy the needed tags
4. Paste them into the Word template

**Available tags:**
- {{ client_name }} – client name
- {{ phone }} – phone number
- {{ device_type }} – device type
- {{ brand }} – brand
- {{ model }} – model
- {{ serial }} – serial number
- {{ tax_number }} – tax/VAT number (rentals)
- {{ id_number }} – ID/Residency (rentals)

**Naming templates**
Use a unified naming style, e.g.:
- Calibration Certificate – Total Station – 2024
- Rental Handover – GPS
- Individual Receipt – Standard"
                }
            };

            // الشهادات
            var certificates = new HelpCategory
            {
                Title = lang == "ar" ? "الشهادات" : "Certificates",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "قسم الشهادات" : "Certificates Section",
                    Content = lang == "ar"
                        ? @"**إنشاء شهادة جديدة**
1. املأ بيانات الجهاز:
   - نوع الجهاز
   - الماركة
   - الموديل
   - الرقم التسلسلي
   - القيمة المحددة

2. املأ بيانات العميل:
   - اسم العميل
   - رقم الهاتف
   - العنوان

3. املأ بيانات الشهادة:
   - رقم الشهادة
   - تاريخ الإصدار
   - تاريخ الانتهاء
   - اسم المندوب

4. اختر القالب
5. اضغط على معاينة ثم طباعة

**طباعة ملصق QR Code**
1. اختر الشهادة من السجل
2. اضغط على طباعة ملصق
3. تأكد من إعداد الطابعة ZPL
4. اضغط على طباعة

**إدارة السجل**
- عرض جميع الشهادات
- البحث والتصفية
- تعديل شهادة
- حذف شهادة"
                        : @"**Create a certificate**
1. Fill device data:
   - Device type
   - Brand
   - Model
   - Serial number
   - Specified value

2. Fill client data:
   - Client name
   - Phone number
   - Address

3. Fill certificate data:
   - Certificate number
   - Issue date
   - Expiry date
   - Representative name

4. Choose a template
5. Click Preview then Print

**Print the QR sticker**
1. Select the certificate in the log
2. Click Print Sticker
3. Ensure ZPL printer is set up
4. Click Print

**Manage the log**
- View all certificates
- Search and filter
- Edit a certificate
- Delete a certificate"
                }
            };

            // الإيجارات
            var rentals = new HelpCategory
            {
                Title = lang == "ar" ? "الإيجارات" : "Rentals",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "قسم الإيجارات" : "Rentals Section",
                    Content = lang == "ar"
                        ? @"**إنشاء إيجار جديد**
1. املأ بيانات العميل:
   - اسم العميل
   - المنشأة
   - الهاتف
   - الرقم الضريبي
   - رقم الهوية/الإقامة

2. املأ بيانات الجهاز:
   - نوع الجهاز
   - الماركة
   - الموديل
   - الرقم التسلسلي
   - الرقم التسلسلي 2 (للـ GPS)

3. املأ بيانات الإيجار:
   - تاريخ البدء
   - تاريخ الانتهاء
   - نوع الإيجار (يومي/شهري)
   - السعر
   - المبلغ المدفوع
   - المبلغ المتبقي
   - الحالة
   - ملاحظات

4. اضغط على حفظ ثم طباعة إقرار الاستلام

**إدارة السجل**
- عرض جميع الإيجارات
- البحث والتصفية
- تعديل إيجار
- حذف إيجار
- إنهاء الإيجار

**الأجهزة المستأجرة حالياً**
- عرض الأجهزة النشطة
- التحقق من تواريخ الانتهاء
- تنبيهات للإيجارات المنتهية"
                        : @"**Create a new rental**
1. Fill client data:
   - Client name
   - Company
   - Phone
   - Tax/VAT number
   - ID/Residency number

2. Fill device data:
   - Device type
   - Brand
   - Model
   - Serial
   - Serial 2 (for GPS)

3. Fill rental details:
   - Start date
   - End date
   - Rental type (Daily/Monthly)
   - Price
   - Paid amount
   - Remaining amount
   - Status
   - Notes

4. Click Save then Print Handover Form

**Manage the log**
- View all rentals
- Search and filter
- Edit a rental
- Delete a rental
- Complete rental

**Currently rented devices**
- View active devices
- Check end dates
- Alerts for expired rentals"
                }
            };

            // أوامر العمل
            var workOrders = new HelpCategory
            {
                Title = lang == "ar" ? "أوامر العمل" : "Work Orders",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "قسم أوامر العمل" : "Work Orders Section",
                    Content = lang == "ar"
                        ? @"**إنشاء أمر عمل**
1. املأ بيانات العميل
2. املأ بيانات الجهاز
3. وصف المشكلة
4. اكتب التقرير الفني
5. حساب التكاليف:
   - تكلفة القطع
   - تكلفة العمالة
   - التكلفة الإجمالية
6. اضغط على طباعة الإيصال والتقرير

**إدارة السجل**
- عرض جميع أوامر العمل
- البحث والتصفية
- تعديل أمر عمل
- حذف أمر عمل"
                        : @"**Create a work order**
1. Fill client data
2. Fill device data
3. Write the problem description
4. Write the technical report
5. Calculate costs:
   - Parts cost
   - Labor cost
   - Total cost
6. Click Print Receipt and Report

**Manage the log**
- View all work orders
- Search and filter
- Edit a work order
- Delete a work order"
                }
            };

            // العملاء
            var clients = new HelpCategory
            {
                Title = lang == "ar" ? "العملاء" : "Clients",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "قسم العملاء" : "Clients Section",
                    Content = lang == "ar"
                        ? @"**إضافة عميل**
1. املأ بيانات العميل
2. اضغط على حفظ

**إدارة العملاء**
- عرض جميع العملاء
- البحث عن عميل
- تعديل بيانات العميل
- حذف عميل"
                        : @"**Add a client**
1. Fill client data
2. Click Save

**Manage clients**
- View all clients
- Search for a client
- Edit client data
- Delete a client"
                }
            };

            // قطع الغيار
            var inventory = new HelpCategory
            {
                Title = lang == "ar" ? "قطع الغيار" : "Inventory",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "قسم قطع الغيار" : "Inventory Section",
                    Content = lang == "ar"
                        ? @"**إضافة قطعة غيار**
1. املأ بيانات القطعة:
   - الكود
   - الاسم
   - الكمية
   - سعر التكلفة
   - سعر البيع
   - الماركة
   - الموقع
2. اضغط على حفظ

**إدارة المخزون**
- عرض جميع القطع
- تحديث الكميات
- تنبيهات المخزون المنخفض"
                        : @"**Add a spare part**
1. Fill item details:
   - Code
   - Name
   - Quantity
   - Cost price
   - Selling price
   - Brand
   - Location
2. Click Save

**Manage inventory**
- View all parts
- Update quantities
- Low stock alerts"
                }
            };

            // النسخ الاحتياطي
            var backup = new HelpCategory
            {
                Title = lang == "ar" ? "النسخ الاحتياطي" : "Backup",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "النسخ الاحتياطي" : "Backup",
                    Content = lang == "ar"
                        ? @"**إنشاء نسخة احتياطية**
1. اختر موقع الحفظ
2. اضغط على إنشاء النسخة
3. انتظر اكتمال العملية
4. تأكد من النجاح

**استعادة النسخة**
1. اختر ملف النسخة
2. اضغط على استعادة
3. انتظر اكتمال العملية
4. تأكد من النجاح

**جدولة النسخ التلقائي**
1. حدد التكرار (يومي/أسبوعي/شهري)
2. حدد الوقت
3. حدد موقع الحفظ
4. اضغط على حفظ"
                        : @"**Create a backup**
1. Choose save location
2. Click Create Backup
3. Wait for completion
4. Confirm success

**Restore a backup**
1. Choose the backup file
2. Click Restore
3. Wait for completion
4. Confirm success

**Schedule automatic backups**
1. Select frequency (Daily/Weekly/Monthly)
2. Select time
3. Select save location
4. Click Save"
                }
            };

            // الإعدادات
            var settings = new HelpCategory
            {
                Title = lang == "ar" ? "الإعدادات" : "Settings",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "الإعدادات" : "Settings",
                    Content = lang == "ar"
                        ? @"**إعدادات الطابعات**
- إعداد طابعة A4 للشهادات والتقارير
- إعداد طابعة ZPL للملصقات
- اختبار الطباعة

**إعدادات المنشأة**
- اسم المنشأة
- الشعار
- العنوان
- رقم الهاتف
- البريد الإلكتروني

**إعدادات القوالب**
- إدارة القوالب
- إضافة/حذف القوالب
- تعديل مسارات القوالب

**إعدادات النظام**
- اللغة
- المنطقة الزمنية
- تنسيق التاريخ"
                        : @"**Printer settings**
- Configure A4 printer for certificates and reports
- Configure ZPL printer for stickers
- Test printing

**Company settings**
- Company name
- Logo
- Address
- Phone
- Email

**Template settings**
- Manage templates
- Add/Delete templates
- Edit template paths

**System settings**
- Language
- Timezone
- Date format"
                }
            };

            // الأسئلة الشائعة
            var faq = new HelpCategory
            {
                Title = lang == "ar" ? "الأسئلة الشائعة" : "FAQ",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "الأسئلة الشائعة" : "Frequently Asked Questions",
                    Content = lang == "ar"
                        ? @"**س: كيف أنشئ مستخدم جديد؟**
ج: من الإعدادات، اختر إدارة المستخدمين، ثم اضغط على إضافة مستخدم واملأ البيانات.

**س: كيف أضيف قالب جديد؟**
ج: من إعدادات القوالب، اختر نوع المستند، ثم اضغط على اختيار ملف Word.

**س: كيف أستخدم الوسوم في القوالب؟**
ج: افتح قاموس الوسوم في إعدادات القوالب، اختر نوع المستند، انسخ الوسوم والصقها في قالب Word.

**س: كيف أطبع شهادة؟**
ج: من قسم الشهادات، بعد إنشاء الشهادة، اضغط على طباعة.

**س: كيف أنشئ نسخة احتياطية؟**
ج: من الإعدادات، اختر النسخ الاحتياطي، ثم حدد موقع الحفظ واضغط على إنشاء.

**س: ماذا أفعل إذا نسيت كلمة المرور؟**
ج: اتصل بالدعم الفني لاستعادة كلمة المرور.

**س: كيف أتصل بالدعم الفني؟**
ج: راجع قسم الدعم الفني للحصول على معلومات الاتصال."
                        : @"**Q: How do I create a new user?**
A: Go to Settings > User Management, click Add User, then fill the fields.

**Q: How do I add a new template?**
A: In Template Settings, choose the document type, then click Choose Word File.

**Q: How do I use tags in templates?**
A: Open the Tags Dictionary in Template Settings, copy the tags you need, and paste them into the Word template.

**Q: How do I print a certificate?**
A: From Certificates, after creating the certificate, click Print.

**Q: How do I create a backup?**
A: In Settings > Backup, choose a path and click Create Backup.

**Q: I forgot the password, what do I do?**
A: Contact support to reset the password.

**Q: How do I contact support?**
A: See the Support section for contact information."
                }
            };

            // الدعم الفني
            var support = new HelpCategory
            {
                Title = lang == "ar" ? "الدعم الفني" : "Support",
                Content = new HelpContent
                {
                    Title = lang == "ar" ? "الدعم الفني" : "Support",
                    Content = lang == "ar"
                        ? @"**معلومات الاتصال**
- الهاتف: [رقم الهاتف]
- البريد الإلكتروني: [البريد الإلكتروني]
- الموقع الإلكتروني: [الموقع]

**ساعات العمل**
- من الأحد إلى الخميس
- من 9:00 صباحاً إلى 5:00 مساءً

**طرق الحصول على المساعدة**
- البريد الإلكتروني
- الهاتف
- الدردشة المباشرة (قريباً)"
                        : @"**Contact info**
- Phone: [phone number]
- Email: [email]
- Website: [website]

**Business hours**
- Sunday to Thursday
- 9:00 AM to 5:00 PM

**How to get help**
- Email
- Phone
- Live chat (coming soon)"
                }
            };

            HelpCategories.Add(quickStart);
            HelpCategories.Add(userManagement);
            HelpCategories.Add(templates);
            HelpCategories.Add(certificates);
            HelpCategories.Add(rentals);
            HelpCategories.Add(workOrders);
            HelpCategories.Add(clients);
            HelpCategories.Add(inventory);
            HelpCategories.Add(backup);
            HelpCategories.Add(settings);
            HelpCategories.Add(faq);
            HelpCategories.Add(support);
        }

        void SelectCategory(HelpCategory category)
        {
            if (category?.Content != null)
                SelectedContent = category.Content;
        }

        void PrintHelp()
        {
            try
            {
                var content = SelectedContent;
                if (content == null)
                    return;

                var pdfPath = BuildHelpPdf(content);
                if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                    return;

                var psi = new ProcessStartInfo(pdfPath) { UseShellExecute = true, Verb = "print" };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.Translations.Get("StHelpPrintFailed"), ex.Message), Resources.Translations.Get("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        void ExportToPdf()
        {
            try
            {
                var content = SelectedContent;
                if (content == null)
                    return;

                var sfd = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    FileName = "BlueMax-Help.pdf"
                };
                if (sfd.ShowDialog() != true)
                    return;

                var pdfPath = BuildHelpPdf(content, sfd.FileName);
                if (string.IsNullOrWhiteSpace(pdfPath))
                    return;

                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.Translations.Get("StHelpExportFailed"), ex.Message), Resources.Translations.Get("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        static string BuildHelpPdf(HelpContent content, string? outputPath = null)
        {
            outputPath ??= Path.Combine(Path.GetTempPath(), $"BlueMax_Help_{Guid.NewGuid():N}.pdf");

            QuestPDF.Settings.License = LicenseType.Community;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 842);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => c.AlignCenter().Text(content.Title ?? "").Bold().FontSize(18));
                    page.Content().Element(c => c.Column(col =>
                    {
                        foreach (var rawLine in (content.Content ?? "").Split('\n'))
                        {
                            var line = rawLine.TrimEnd('\r');
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("**", StringComparison.Ordinal) && trimmed.EndsWith("**", StringComparison.Ordinal))
                            {
                                col.Item().PaddingTop(6).Text(trimmed.Trim('*').Trim()).Bold().FontSize(13);
                            }
                            else if (string.IsNullOrWhiteSpace(line))
                            {
                                col.Item().PaddingTop(6);
                            }
                            else
                            {
                                col.Item().PaddingBottom(3).Text(line);
                            }
                        }
                    }));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        void CloseWindow()
        {
            _ownerWindow?.Close();
        }
    }

    public class HelpCategory
    {
        public string Title { get; set; } = "";
        public HelpContent? Content { get; set; }
    }

    public class HelpContent
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
