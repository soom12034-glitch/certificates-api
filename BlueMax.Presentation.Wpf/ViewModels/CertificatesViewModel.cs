using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using BlueMax.Domain;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.Services;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlueMax.Presentation.Wpf.ViewModels;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BlueMax.Presentation.Wpf.ViewModels;

public enum QuickActionMode
{
    Search,
    PrintSticker,
    ExpiredDevices
}

public sealed class CertificatesViewModel : ViewModelBase
{
    const double PixelToMm = 0.264583;
    const int SearchDelayMs = 200;
    const int AutoFillDelayMs = 250;
    const int DeviceDefaultsDelayMs = 200;
    static string CloudApiBase =>
        System.Configuration.ConfigurationManager.AppSettings["CloudApiBase"] ?? "https://vnumera.cashierpro-cloud.com";
    static string CloudApiKey =>
        System.Configuration.ConfigurationManager.AppSettings["CloudApiKey"] ?? "fb3a9c12d7e54a8f309b2c6de1457f90c3ab8d6e1f2c4b5a7689e0f1d2c3b4a5";
    readonly Dictionary<string, CalibrationTemplate> _templates;
    readonly Dictionary<string, List<TemplateHubTagItem>> _templateHubTagsByDocumentType;
    CancellationTokenSource? _clientOptionsCts;
    CancellationTokenSource? _clientAutoFillCts;
    CancellationTokenSource? _deviceDefaultsCts;
    Task? _ensureDbCreatedTask;
    string _layoutTemplatePath = "";
    string _layoutTemplatePdfPath = "";
    string _statusMessage = "";
    double _layoutCanvasWidth = 210 / PixelToMm;
    double _layoutCanvasHeight = 297 / PixelToMm;
    bool _snapToGridEnabled = true;
    double _gridSizePx = 24;
    string _selectedTemplateName = "";
    string _clientName = "";
    string _phone = "";
    string _deviceType = "";
    string _brand = "";
    string _model = "";
    string _serialText = "";
    string _serialText2 = "";
    string _specValue = "";
    string _accuracy = "";
    bool _autoLevelCompensatorCheck;
    string _delegateName = "";
    string _certificateNumber = "";
    string _searchText = "";
    string _printMode = "Full Print";
    double _printOffsetXmm;
    double _printOffsetYmm;
    bool _printFlipX;
    bool _printFlipY;
    DateTime _issueDate = DateTime.Today;
    DateTime _expiryDate = DateTime.Today.AddMonths(6);
    CertificateRow? _selectedCertificate;
    int? _currentCertificateId;
    bool _isEditMode;
    string _modeText = "وضع: جديد";
    int _loadScope;
    bool _showExpiredOnly;
    bool _isInitialized;
    bool _isBusy;
    string _busyMessage = "";
    bool _isAutoUploadEnabled = true;
    string _lastExportedPdfPath = "";
    string _lastVerificationUrl = "";
    TemplateHubDocumentType? _selectedTemplateHubDocumentType;
    string _selectedTemplateHubPath = "";
    bool _isGpsDeviceType;
    bool _isTotalStationDeviceType;
    bool _isAutoLevelDeviceType;
    string _selectedGpsSerial1Kind = "Base";
    string _selectedGpsSerial2Kind = "Rover";
    string _gpsSerial1Text = "";
    string _gpsSerial2Text = "";
    int _applyTemplateVersion;
    int _templateOptionsVersion;
    public ObservableCollection<string> AutoLevelSpecOptions { get; }

    public CertificatesViewModel()
    {
        LayoutBoxes = new ObservableCollection<LayoutBoxItem>();
        Certificates = new ObservableCollection<CertificateRow>();
        CalibrationRows = new ObservableCollection<CalibrationRow>();
        TemplateOptions = new ObservableCollection<string>();
        DeviceTypes = new ObservableCollection<string> { "Total Station", "Auto Level", "GPS" };
        BrandOptions = new ObservableCollection<string> { "Leica", "Trimble", "Sokkia", "Topcon", "South", "CHC", "Kolida", "Stonex", "Ruide", "Sanding", "Hi-Target", "Foif", "Gowin", "CST/berger", "Nikon", "Pentax", "Spectra", "Geomax", "مختلف الاجهزة الصينيه" };
        PrintModes = new ObservableCollection<string> { "Full Print", "Data Only" };
        AccuracyOptions = new ObservableCollection<string> { "1\"", "2\"", "3\"", "5\"", "7\"" };
        AutoLevelSpecOptions = new ObservableCollection<string>();
        for (int i = 1; i <= 100; i++)
        {
            var value = (i / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            AutoLevelSpecOptions.Add(value);
        }

        GpsSerialKinds = new ObservableCollection<string> { "Base", "Rover" };
        ClientOptions = new ObservableCollection<string>();
        _templates = BuildCalibrationTemplates();
        FieldItems = new ObservableCollection<FieldItem>
        {
            new FieldItem("client_name", "Client Name"),
            new FieldItem("date", "Date"),
            new FieldItem("expiry_date", "Expiry Date"),
            new FieldItem("cert_no", "Certificate No."),
            new FieldItem("work_no", "Work Order No."),
            new FieldItem("report_type", "Report Type"),
            new FieldItem("phone", "Phone"),
            new FieldItem("device_type", "Device Type"),
            new FieldItem("brand", "Brand"),
            new FieldItem("model", "Model"),
            new FieldItem("serial", "Serial"),
            new FieldItem("serial2", "Serial 2"),
            new FieldItem("table_start", "Table Start")
        };
        foreach (var f in FieldItems)
            f.PropertyChanged += FieldItemOnPropertyChanged;

        TemplateHubDocumentTypes = new ObservableCollection<TemplateHubDocumentType>
        {
            new TemplateHubDocumentType("cert_total_station", "Certificate (Total Station)"),
            new TemplateHubDocumentType("cert_gps", "Certificate (GPS)"),
            new TemplateHubDocumentType("cert_auto_level", "Certificate (Auto Level)"),
            new TemplateHubDocumentType("invoice", "إيصال استلام فردى"),
            new TemplateHubDocumentType("price_quotation", "Price Quotation"),
            new TemplateHubDocumentType("maintenance_report", "Maintenance Report"),
            new TemplateHubDocumentType("rental_receipt", "إقرار استلام جهاز إيجار")
        };
        TemplateHubTags = new ObservableCollection<TemplateHubTagItem>();
        _templateHubTagsByDocumentType = BuildTemplateHubTagsDictionary();
        CopyTemplateHubTagCommand = new RelayCommand(p => CopyTemplateHubTag(p as string));
        UploadWordTemplateHubCommand = new RelayCommand(_ => UploadWordTemplateHub());
        SaveTemplateHubConfigurationCommand = new RelayCommand(_ => SaveTemplateHubConfiguration());
        SelectedTemplateHubDocumentType = TemplateHubDocumentTypes.FirstOrDefault();
        UploadTemplateCommand = new RelayCommand(_ => UploadTemplate());
        PreviewTemplateCommand = new AsyncRelayCommand(_ => PreviewTemplateAsync());
        SaveTemplateCommand = new RelayCommand(_ => SaveTemplate());
        NewTemplateCommand = new RelayCommand(_ => NewTemplate());
        AddLayoutBoxCommand = new RelayCommand(AddLayoutBox);
        SaveReportSettingsCommand = new RelayCommand(_ => SaveReportSettings());
        ResetLayoutCommand = new RelayCommand(_ => ResetLayout());
        ArrangeLayoutBoxesCommand = new RelayCommand(_ => ArrangeLayoutBoxes());
        RemoveLayoutBoxCommand = new RelayCommand(RemoveLayoutBox);
        ChooseBackgroundImageCommand = new RelayCommand(_ => ChooseBackgroundImage());
        NewCertificateCommand = new RelayCommand(_ => NewCertificate());
        SaveCertificateCommand = new AsyncRelayCommand(async _ => await SaveCertificateAsync());
        DeleteCertificateCommand = new AsyncRelayCommand(async _ => await DeleteCertificateAsync(), _ => _isEditMode && SelectedCertificate != null);
        RefreshCertificatesCommand = new AsyncRelayCommand(async _ => await RefreshCertificatesAsync());
        GeneratePdfCommand = new AsyncRelayCommand(async _ => await GeneratePdfAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        UploadCertificateCommand = new AsyncRelayCommand(async _ => await UploadCertificateAsync());
        CopyVerificationUrlCommand = new RelayCommand(_ => CopyVerificationUrl(), _ => !string.IsNullOrWhiteSpace(LastVerificationUrl));
        OpenVerificationUrlCommand = new RelayCommand(_ => OpenVerificationUrl(), _ => !string.IsNullOrWhiteSpace(LastVerificationUrl));
        PrintStickerCommand = new AsyncRelayCommand(async _ => await ExportStickerZplAsync());
        PreviewStickerZplCommand = new AsyncRelayCommand(async _ => await PreviewStickerZplAsync());
        PrintA4Command = new AsyncRelayCommand(async _ => await PrintA4Async());
        EditOldCertificateCommand = new RelayCommand(_ => EnterEditModeFromSelection(), _ => SelectedCertificate != null);
        OpenSelectedCertificateFileCommand = new AsyncRelayCommand(async _ => await OpenSelectedCertificateFileAsync(), _ => SelectedCertificate != null);

        if (DeviceTypes.Count > 0)
            DeviceType = DeviceTypes[0];
        SelectedGpsSerial1Kind = "Base";
        SelectedGpsSerial2Kind = "Rover";
        UpdateModeText();

        CalibrationRows.CollectionChanged += CalibrationRows_CollectionChanged;

    }

    async Task UploadCertificateAsync()
    {
        if (IsBusy)
        {
            StatusMessage = "Another operation is in progress.";
            return;
        }
        if (!await SaveCertificateInternalAsync())
            return;
        if (!_currentCertificateId.HasValue)
            return;

        IsBusy = true; BusyMessage = "Uploading PDF...";
        try
        {
            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
            var certNo = (CertificateNumber ?? "").Trim();
            var pdfPath = System.IO.Path.Combine(outputDir, $"Cert_{certNo}.pdf");
            if (!File.Exists(pdfPath))
            {
                using var db = CreateDbContext();
                await EnsureDbCreatedOnceAsync();
                var certificateId = _currentCertificateId.Value;
                var templateKey = MapCertificateTemplateKey(DeviceType);
                var engine = new WordTemplateEngine("", outputDir);
                var docService = new CertificateDocumentService(db, engine);
                var templatePath = await EnsureTemplatePathAsync(templateKey);
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    StatusMessage = "يرجى تحديد قالب Word أولاً من إدارة القوالب.";
                    return;
                }
                var docxPath = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(certificateId, templatePath);
                if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
                {
                    StatusMessage = "فشل إنشاء ملف Word.";
                    return;
                }
                Directory.CreateDirectory(outputDir);
                var questPdfPath = System.IO.Path.ChangeExtension(docxPath, ".pdf");
                if (System.IO.File.Exists(questPdfPath))
                {
                    pdfPath = questPdfPath;
                }
                _lastExportedPdfPath = pdfPath;
            }

            await UploadPdfToServerAsync(pdfPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر رفع الشهادة: {ex.Message}";
        }
        finally
        {
            IsBusy = false; BusyMessage = "";
        }
    }

    async Task UploadPdfToServerAsync(string pdfPath)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("X-Api-Key", CloudApiKey);

            using var content = new MultipartFormDataContent();
            var bytes = await File.ReadAllBytesAsync(pdfPath);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", Path.GetFileName(pdfPath));

            var fingerprint = (CertificateNumber ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(fingerprint))
                content.Add(new StringContent(fingerprint), "fingerprint");

            var meta = new Dictionary<string, string>
            {
                ["certificateNo"] = CertificateNumber ?? "",
                ["clientName"] = ClientName ?? "",
                ["deviceType"] = DeviceType ?? "",
                ["brand"] = Brand ?? "",
                ["model"] = Model ?? "",
                ["serial"] = SerialText ?? "",
                ["issueDate"] = IssueDate == default ? "" : IssueDate.ToString("yyyy-MM-dd"),
                ["expiryDate"] = ExpiryDate == default ? "" : ExpiryDate.ToString("yyyy-MM-dd")
            };
            content.Add(new StringContent(JsonSerializer.Serialize(meta)), "meta");

            var url = $"{CloudApiBase}/v1/certificates";
            var resp = await client.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                StatusMessage = $"فشل الرفع: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}";
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var verifyUrl = doc.RootElement.TryGetProperty("verifyUrl", out var v) ? v.GetString() : null;
                if (!string.IsNullOrWhiteSpace(verifyUrl))
                {
                    LastVerificationUrl = verifyUrl!;
                    StatusMessage = $"تم رفع الشهادة: {verifyUrl}";
                    try { SaveVerifyUrlLocal(CertificateNumber ?? string.Empty, verifyUrl!); } catch { }
                }
                else
                    StatusMessage = "تم رفع الشهادة بنجاح.";
            }
            catch
            {
                StatusMessage = "تم رفع الشهادة بنجاح.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر رفع الشهادة: {ex.Message}";
        }
    }

    async Task UploadCurrentCertificateAfterSaveAsync()
    {
        if (!_currentCertificateId.HasValue)
            return;

        var certNo = (CertificateNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(certNo))
            return;

        var hadPreviousUrl = false;
        try
        {
            var mapPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output", "verify_urls.json");
            if (System.IO.File.Exists(mapPath))
            {
                var json = System.IO.File.ReadAllText(mapPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                hadPreviousUrl = dict != null && dict.TryGetValue(certNo, out var url) && !string.IsNullOrWhiteSpace(url);
            }
        }
        catch
        {
        }

        var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
        using var db = CreateDbContext();
        await EnsureDbCreatedOnceAsync();
        var certificateId = _currentCertificateId.Value;
        var templateKey = MapCertificateTemplateKey(DeviceType);
        var engine = new WordTemplateEngine("", outputDir);
        var docService = new CertificateDocumentService(db, engine);
        var templatePath = await EnsureTemplatePathAsync(templateKey);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            StatusMessage = "تم حفظ الشهادة، لكن تعذر الرفع: يرجى تحديد قالب Word أولاً من إدارة القوالب.";
            return;
        }

        var docxPath = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(certificateId, templatePath);
        if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
        {
            StatusMessage = "تم حفظ الشهادة، لكن تعذر إنشاء ملف Word للرفع.";
            return;
        }

        var pdfPath = System.IO.Path.ChangeExtension(docxPath, ".pdf");
        if (!File.Exists(pdfPath))
        {
            StatusMessage = "تم حفظ الشهادة، لكن تعذر إنشاء ملف PDF للرفع.";
            return;
        }

        _lastExportedPdfPath = pdfPath;
        await UploadPdfToServerAsync(pdfPath);

        if (StatusMessage.StartsWith("تم رفع الشهادة", StringComparison.Ordinal))
        {
            StatusMessage = hadPreviousUrl
                ? $"تم تحديث الملف المرفوع بنجاح: {LastVerificationUrl}"
                : $"تم رفع الشهادة تلقائياً بنجاح: {LastVerificationUrl}";
        }
        else if (StatusMessage == "تم رفع الشهادة بنجاح.")
        {
            StatusMessage = hadPreviousUrl
                ? "تم تحديث الملف المرفوع بنجاح."
                : "تم رفع الشهادة تلقائياً بنجاح.";
        }
    }

    async Task EnsureVerifyUrlBeforeDocumentAsync()
    {
        try
        {
            var certNo = (CertificateNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(certNo)) return;
            var mapPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output", "verify_urls.json");
            var hasUrl = false;
            if (System.IO.File.Exists(mapPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(mapPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue(certNo, out var url) && !string.IsNullOrWhiteSpace(url))
                        hasUrl = true;
                }
                catch { }
            }

            if (!hasUrl)
            {
                // Generate PDF (if not present) and upload to get verifyUrl
                var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
                var path = System.IO.Path.Combine(outputDir, $"Cert_{certNo}.pdf");
                if (!System.IO.File.Exists(path))
                {
                    if (!_currentCertificateId.HasValue) return;
                    using var db = CreateDbContext();
                    await EnsureDbCreatedOnceAsync();
                    var certificateId = _currentCertificateId.Value;
                    var templateKey = MapCertificateTemplateKey(DeviceType);
                    var engine = new WordTemplateEngine("", outputDir);
                    var docService = new CertificateDocumentService(db, engine);
                    var templatePath = await EnsureTemplatePathAsync(templateKey);
                    if (string.IsNullOrWhiteSpace(templatePath)) return;
                    var docxPath = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(certificateId, templatePath);
                    var questPdf = System.IO.Path.ChangeExtension(docxPath, ".pdf");
                    if (System.IO.File.Exists(questPdf))
                        path = questPdf;
                }
                await UploadPdfToServerAsync(path);
            }
        }
        catch { }
    }

    async Task ExportPdfAsync()
    {
        if (IsBusy)
        {
            StatusMessage = "Another operation is in progress.";
            return;
        }
        if (!await SaveCertificateInternalAsync())
            return;
        if (!_currentCertificateId.HasValue)
            return;
        if (IsAutoUploadEnabled)
            await EnsureVerifyUrlBeforeDocumentAsync();

        IsBusy = true; BusyMessage = "Exporting PDF...";
        try
        {
            using var db = CreateDbContext();
            await EnsureDbCreatedOnceAsync();
            var certificateId = _currentCertificateId.Value;
            var templateKey = MapCertificateTemplateKey(DeviceType);
            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
            var engine = new WordTemplateEngine("", outputDir);
            var docService = new CertificateDocumentService(db, engine);
            var templatePath = await EnsureTemplatePathAsync(templateKey);

            if (string.IsNullOrWhiteSpace(templatePath))
            {
                StatusMessage = "يرجى تحديد قالب Word أولاً من إدارة القوالب.";
                return;
            }

            var docxPath = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(certificateId, templatePath);
            if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
            {
                StatusMessage = "فشل إنشاء ملف Word.";
                return;
            }

            Directory.CreateDirectory(outputDir);
            var certNo = (CertificateNumber ?? "").Trim();
            string Sanitize(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
                var arr = s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
                var cleaned = new string(arr).Trim('-');
                return string.IsNullOrWhiteSpace(cleaned) ? "UNNAMED" : cleaned;
            }
            var safeNo = Sanitize(certNo);
            var pdfPath = System.IO.Path.Combine(outputDir, $"Cert_{safeNo}.pdf");

            // Use QuestPDF output produced by CertificateDocumentService
            var questPdfPath = System.IO.Path.ChangeExtension(docxPath, ".pdf");
            if (System.IO.File.Exists(questPdfPath))
            {
                try
                {
                    System.IO.File.Copy(questPdfPath, pdfPath, overwrite: true);
                }
                catch { pdfPath = questPdfPath; }
            }
            else
            {
                // Fallback: if QuestPDF output missing, just use the intended path
                pdfPath = questPdfPath;
            }

            StatusMessage = $"تم إنشاء PDF: {pdfPath}";
            try { Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true }); } catch { }
            _lastExportedPdfPath = pdfPath;
            if (IsAutoUploadEnabled)
            {
                BusyMessage = "Uploading PDF...";
                await UploadPdfToServerAsync(pdfPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر تصدير PDF: {ex.Message}";
        }
        finally
        {
            IsBusy = false; BusyMessage = "";
        }
    }



    async Task OpenSelectedCertificateFileAsync()
    {
        var row = SelectedCertificate;
        if (row == null)
            return;

        var certNo = (row.CertificateNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(certNo))
            return;

        var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
        var pdfPath = System.IO.Path.Combine(outputDir, $"Cert_{certNo}.pdf");
        var docxPath = System.IO.Path.Combine(outputDir, $"Cert_{certNo}.docx");
        var openPath = System.IO.File.Exists(docxPath) ? docxPath : (System.IO.File.Exists(pdfPath) ? pdfPath : "");
        if (string.IsNullOrWhiteSpace(openPath))
        {
            IsBusy = true; BusyMessage = "جارٍ تجهيز المعاينة...";
            try
            {
                await EnsureDbCreatedOnceAsync();
                var templateKey = MapCertificateTemplateKey(row.DeviceType);
                var templatePath = await LoadTemplatePathAsync(templateKey);
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    MessageBox.Show(
                        "يرجى تحديد قالب Word لهذا النوع من شاشة إدارة القوالب أولاً",
                        "قالب غير موجود",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                using var db = CreateDbContext();
                var engine = new WordTemplateEngine("", outputDir);
                var docService = new CertificateDocumentService(db, engine);
                var result = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(row.Id, templatePath);
                openPath = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "تعذر فتح المعاينة", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                IsBusy = false; BusyMessage = "";
            }
        }

        if (string.IsNullOrWhiteSpace(openPath))
            return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = openPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "تعذر فتح الملف", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public ObservableCollection<LayoutBoxItem> LayoutBoxes { get; }
    public ObservableCollection<CertificateRow> Certificates { get; }
    public ObservableCollection<CalibrationRow> CalibrationRows { get; }
    public ObservableCollection<string> TemplateOptions { get; }
    public ObservableCollection<string> DeviceTypes { get; }
    public ObservableCollection<string> BrandOptions { get; }
    public ObservableCollection<string> PrintModes { get; }
    public ObservableCollection<string> AccuracyOptions { get; }
    public ObservableCollection<string> GpsSerialKinds { get; }
    public ObservableCollection<string> ClientOptions { get; }
    public ObservableCollection<FieldItem> FieldItems { get; }

    public ObservableCollection<TemplateHubDocumentType> TemplateHubDocumentTypes { get; }
    public ObservableCollection<TemplateHubTagItem> TemplateHubTags { get; }

    public TemplateHubDocumentType? SelectedTemplateHubDocumentType
    {
        get => _selectedTemplateHubDocumentType;
        set
        {
            if (!SetProperty(ref _selectedTemplateHubDocumentType, value))
                return;
            RefreshTemplateHubTags();
            if (_isInitialized)
                _ = LoadSelectedTemplateHubPathAsync();
            else
                SelectedTemplateHubPath = "";
        }
    }

    public string SelectedTemplateHubPath
    {
        get => _selectedTemplateHubPath;
        set => SetProperty(ref _selectedTemplateHubPath, value);
    }

    public RelayCommand CopyTemplateHubTagCommand { get; }
    public RelayCommand UploadWordTemplateHubCommand { get; }
    public RelayCommand SaveTemplateHubConfigurationCommand { get; }
    public RelayCommand EditOldCertificateCommand { get; }

    public string ModeText
    {
        get => _modeText;
        private set => SetProperty(ref _modeText, value);
    }

    public bool IsGpsDeviceType
    {
        get => _isGpsDeviceType;
        private set => SetProperty(ref _isGpsDeviceType, value);
    }

    public bool IsTotalStationDeviceType
    {
        get => _isTotalStationDeviceType;
        private set => SetProperty(ref _isTotalStationDeviceType, value);
    }

    public bool IsAutoLevelDeviceType
    {
        get => _isAutoLevelDeviceType;
        private set => SetProperty(ref _isAutoLevelDeviceType, value);
    }

    public string SelectedGpsSerial1Kind
    {
        get => _selectedGpsSerial1Kind;
        set => SetProperty(ref _selectedGpsSerial1Kind, value);
    }

    public string SelectedGpsSerial2Kind
    {
        get => _selectedGpsSerial2Kind;
        set => SetProperty(ref _selectedGpsSerial2Kind, value);
    }

    public string GpsSerial1Text
    {
        get => _gpsSerial1Text;
        set
        {
            var sanitized = StripArabic(value);
            if (!SetProperty(ref _gpsSerial1Text, sanitized))
                return;
            MarkDraftChanged();
        }
    }

    public string GpsSerial2Text
    {
        get => _gpsSerial2Text;
        set
        {
            var sanitized = StripArabic(value);
            if (!SetProperty(ref _gpsSerial2Text, sanitized))
                return;
            MarkDraftChanged();
        }
    }

    public double LayoutCanvasWidth
    {
        get => _layoutCanvasWidth;
        set => SetProperty(ref _layoutCanvasWidth, value);
    }

    public double LayoutCanvasHeight
    {
        get => _layoutCanvasHeight;
        set => SetProperty(ref _layoutCanvasHeight, value);
    }

    public bool SnapToGridEnabled
    {
        get => _snapToGridEnabled;
        set => SetProperty(ref _snapToGridEnabled, value);
    }

    public double GridSizePx
    {
        get => _gridSizePx;
        set => SetProperty(ref _gridSizePx, Math.Max(4, Math.Min(64, value)));
    }

    public string LayoutTemplatePath
    {
        get => _layoutTemplatePath;
        set => SetProperty(ref _layoutTemplatePath, value);
    }

    public string LayoutTemplatePdfPath
    {
        get => _layoutTemplatePdfPath;
        set => SetProperty(ref _layoutTemplatePdfPath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (!SetProperty(ref _isBusy, value))
                return;
            GeneratePdfCommand?.RaiseCanExecuteChanged();
            ExportPdfCommand?.RaiseCanExecuteChanged();
            UploadCertificateCommand?.RaiseCanExecuteChanged();
            PrintStickerCommand?.RaiseCanExecuteChanged();
            PreviewStickerZplCommand?.RaiseCanExecuteChanged();
            PrintA4Command?.RaiseCanExecuteChanged();
        }
    }
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    public bool IsAutoUploadEnabled
    {
        get => _isAutoUploadEnabled;
        set => SetProperty(ref _isAutoUploadEnabled, value);
    }

    public string LastVerificationUrl
    {
        get => _lastVerificationUrl;
        set
        {
            if (!SetProperty(ref _lastVerificationUrl, value)) return;
            CopyVerificationUrlCommand?.RaiseCanExecuteChanged();
            OpenVerificationUrlCommand?.RaiseCanExecuteChanged();
        }
    }

    public string ClientName
    {
        get => _clientName;
        set
        {
            if (!SetProperty(ref _clientName, value))
                return;
            var text = (value ?? "").Trim();
            _ = DebouncedLoadClientOptionsAsync(text);
            _ = DebouncedAutoFillClientAsync(text);
        }
    }

    public bool IsClientOptionsOpen
    {
        get => _isClientOptionsOpen;
        set => SetProperty(ref _isClientOptionsOpen, value);
    }
    bool _isClientOptionsOpen;

    public string SelectedClientOption
    {
        get => string.Empty; 
        set
        {
            if (value != null)
            {
                ClientName = value;
                IsClientOptionsOpen = false;
            }
        }
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, StripArabic(value));
    }

    public string DeviceType
    {
        get => _deviceType;
        set
        {
            var sanitized = StripArabic(value);
            if (!SetProperty(ref _deviceType, sanitized))
                return;
            MarkDraftChanged();
            ApplyCalibrationTemplate(sanitized);
            RefreshTemplateOptions();
            UpdateDeviceTypeUiState(sanitized);
            _ = DebouncedAutoFillDeviceDefaultsAsync((ClientName ?? "").Trim(), (sanitized ?? "").Trim());
        }
    }

    void UpdateDeviceTypeUiState(string? deviceType)
    {
        var isGps = string.Equals(deviceType, "GPS", StringComparison.OrdinalIgnoreCase);
        var isTotalStation = string.Equals(deviceType, "Total Station", StringComparison.OrdinalIgnoreCase);
        var isAutoLevel = string.Equals(deviceType, "Auto Level", StringComparison.OrdinalIgnoreCase);

        IsGpsDeviceType = isGps;
        IsTotalStationDeviceType = isTotalStation;
        IsAutoLevelDeviceType = isAutoLevel;

        if (!isGps)
            SerialText2 = "";
        if (!isGps)
        {
            GpsSerial1Text = "";
            GpsSerial2Text = "";
            SelectedGpsSerial1Kind = "Base";
            SelectedGpsSerial2Kind = "Rover";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(GpsSerial1Text))
                GpsSerial1Text = SerialText ?? "";
            if (string.IsNullOrWhiteSpace(GpsSerial2Text))
                GpsSerial2Text = SerialText2 ?? "";
        }

        if (isTotalStation)
        {
            if (string.IsNullOrWhiteSpace(Accuracy))
                Accuracy = "3\"";
            SpecValue = "";
        }
        else if (isAutoLevel)
        {
            Accuracy = "";
            if (string.IsNullOrWhiteSpace(SpecValue) && AutoLevelSpecOptions.Count > 0)
                SpecValue = AutoLevelSpecOptions[0];
        }
        else if (isGps)
        {
            Accuracy = "";
            SpecValue = "";
        }
        else
        {
            Accuracy = "";
            SpecValue = "";
        }

        AutoLevelCompensatorCheck = false;
    }

    static string StripArabic(string? value)
    {
        return value ?? "";
    }

    public string Brand
    {
        get => _brand;
        set => SetProperty(ref _brand, StripArabic(value));
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, StripArabic(value));
    }

    public string SerialText
    {
        get => _serialText;
        set
        {
            var sanitized = StripArabic(value);
            if (!SetProperty(ref _serialText, sanitized))
                return;
            MarkDraftChanged();
        }
    }

    public string SerialText2
    {
        get => _serialText2;
        set
        {
            var sanitized = StripArabic(value);
            if (!SetProperty(ref _serialText2, sanitized))
                return;
            MarkDraftChanged();
        }
    }

    public string SpecValue
    {
        get => _specValue;
        set => SetProperty(ref _specValue, StripArabic(value));
    }
    public string Accuracy
    {
        get => _accuracy;
        set => SetProperty(ref _accuracy, StripArabic(value));
    }
    public bool AutoLevelCompensatorCheck
    {
        get => _autoLevelCompensatorCheck;
        set => SetProperty(ref _autoLevelCompensatorCheck, value);
    }

    public string DelegateName
    {
        get => _delegateName;
        set => SetProperty(ref _delegateName, value);
    }

    public string CertificateNumber
    {
        get => _certificateNumber;
        set => SetProperty(ref _certificateNumber, StripArabic(value));
    }

    public DateTime IssueDate
    {
        get => _issueDate;
        set
        {
            if (!SetProperty(ref _issueDate, value))
                return;
            ExpiryDate = _issueDate.AddMonths(6);
        }
    }

    public DateTime ExpiryDate
    {
        get => _expiryDate;
        set => SetProperty(ref _expiryDate, value);
    }

    public string SelectedTemplateName
    {
        get => _selectedTemplateName;
        set
        {
            if (!SetProperty(ref _selectedTemplateName, value))
                return;
            LoadSelectedTemplate();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, StripArabic(value));
    }

    public string PrintMode
    {
        get => _printMode;
        set => SetProperty(ref _printMode, value);
    }

    public double PrintOffsetXmm
    {
        get => _printOffsetXmm;
        set => SetProperty(ref _printOffsetXmm, value);
    }

    public double PrintOffsetYmm
    {
        get => _printOffsetYmm;
        set => SetProperty(ref _printOffsetYmm, value);
    }

    public bool PrintFlipX
    {
        get => _printFlipX;
        set => SetProperty(ref _printFlipX, value);
    }

    public bool PrintFlipY
    {
        get => _printFlipY;
        set => SetProperty(ref _printFlipY, value);
    }

    public CertificateRow? SelectedCertificate
    {
        get => _selectedCertificate;
        set
        {
            var changed = SetProperty(ref _selectedCertificate, value);
            LoadFromSelection(value);
            if (changed)
            {
                EditOldCertificateCommand?.RaiseCanExecuteChanged();
                DeleteCertificateCommand?.RaiseCanExecuteChanged();
                OpenSelectedCertificateFileCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand UploadTemplateCommand { get; }
    public RelayCommand AddLayoutBoxCommand { get; }
    public RelayCommand SaveReportSettingsCommand { get; }
    public RelayCommand ResetLayoutCommand { get; }
    public RelayCommand NewCertificateCommand { get; }
    public RelayCommand RemoveLayoutBoxCommand { get; }
    public AsyncRelayCommand SaveCertificateCommand { get; }
    public AsyncRelayCommand DeleteCertificateCommand { get; }
    public AsyncRelayCommand RefreshCertificatesCommand { get; }
    public AsyncRelayCommand GeneratePdfCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }
    public AsyncRelayCommand UploadCertificateCommand { get; }
    public RelayCommand CopyVerificationUrlCommand { get; }
    public RelayCommand OpenVerificationUrlCommand { get; }
    public AsyncRelayCommand PrintStickerCommand { get; }
    public AsyncRelayCommand PreviewStickerZplCommand { get; }
    public AsyncRelayCommand PrintA4Command { get; }
    public RelayCommand ArrangeLayoutBoxesCommand { get; }
    public AsyncRelayCommand PreviewTemplateCommand { get; }
    public RelayCommand SaveTemplateCommand { get; }
    public RelayCommand NewTemplateCommand { get; }
    public RelayCommand ChooseBackgroundImageCommand { get; }
    public AsyncRelayCommand OpenSelectedCertificateFileCommand { get; }

    public void ApplyQuickAction(QuickActionMode mode)
    {
        _showExpiredOnly = mode == QuickActionMode.ExpiredDevices;
        if (mode == QuickActionMode.Search)
            StatusMessage = "Use the archive panel to search certificates.";
        if (mode == QuickActionMode.PrintSticker)
            StatusMessage = "Select a certificate and choose Print Sticker.";
        if (mode == QuickActionMode.ExpiredDevices)
            StatusMessage = "Showing expired devices only.";
        _ = RefreshCertificatesAsync();
    }

    void NewCertificate()
    {
        _isEditMode = false;
        UpdateModeText();
        _currentCertificateId = null;
        ClientName = "";
        Phone = "";
        Brand = "";
        Model = "";
        SerialText = "";
        SerialText2 = "";
        GpsSerial1Text = "";
        GpsSerial2Text = "";
        SelectedGpsSerial1Kind = "Base";
        SelectedGpsSerial2Kind = "Rover";
        SpecValue = "";
        Accuracy = "";
        AutoLevelCompensatorCheck = false;
        DelegateName = "";
        CertificateNumber = "";
        IssueDate = DateTime.Today;
        ExpiryDate = IssueDate.AddMonths(6);
        SelectedCertificate = null;
        ApplyCalibrationTemplate(DeviceType);
        DeleteCertificateCommand.RaiseCanExecuteChanged();
        GeneratePdfCommand.RaiseCanExecuteChanged();
        PrintStickerCommand.RaiseCanExecuteChanged();
        PreviewStickerZplCommand.RaiseCanExecuteChanged();
        PrintA4Command.RaiseCanExecuteChanged();
    }

    void UpdateModeText()
    {
        ModeText = _isEditMode ? "وضع: تعديل" : "وضع: جديد";
        DeleteCertificateCommand?.RaiseCanExecuteChanged();
    }

    void BeginLoad() => _loadScope++;
    void EndLoad()
    {
        if (_loadScope > 0)
            _loadScope--;
    }

    void MarkDraftChanged()
    {
        if (_loadScope > 0)
            return;
        if (_isEditMode)
            return;
        _currentCertificateId = null;
        CertificateNumber = "";
        UpdateModeText();
        DeleteCertificateCommand?.RaiseCanExecuteChanged();
        GeneratePdfCommand?.RaiseCanExecuteChanged();
        PrintStickerCommand?.RaiseCanExecuteChanged();
        PreviewStickerZplCommand?.RaiseCanExecuteChanged();
        PrintA4Command?.RaiseCanExecuteChanged();
    }

    void ResetDeviceFieldsForNewRecord()
    {
        Brand = "";
        Model = "";
        SerialText = "";
        SerialText2 = "";
        GpsSerial1Text = "";
        GpsSerial2Text = "";
        SelectedGpsSerial1Kind = "Base";
        SelectedGpsSerial2Kind = "Rover";
        SpecValue = "";
        Accuracy = "";
        AutoLevelCompensatorCheck = false;
        IssueDate = DateTime.Today;
        ExpiryDate = IssueDate.AddMonths(6);
        CertificateNumber = "";
        _currentCertificateId = null;
        _isEditMode = false;
        UpdateModeText();
    }

    void EnterEditModeFromSelection()
    {
        if (SelectedCertificate == null)
            return;
        LoadFullCertificateForEdit(SelectedCertificate);
    }

    (string BaseSerial, string RoverSerial) MapGpsSerials()
    {
        var baseSerial = "";
        var roverSerial = "";

        // Helper to assign based on kind, with fallback to empty slot
        void Assign(string kind, string serial)
        {
            var s = (serial ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s))
                return;

            // Explicitly check for Rover
            if (string.Equals(kind, "Rover", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(roverSerial))
                    roverSerial = s;
                else if (string.IsNullOrWhiteSpace(baseSerial))
                    baseSerial = s; // Fallback to Base if Rover is full
            }
            // Explicitly check for Base
            else if (string.Equals(kind, "Base", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(baseSerial))
                    baseSerial = s;
                else if (string.IsNullOrWhiteSpace(roverSerial))
                    roverSerial = s; // Fallback to Rover if Base is full
            }
            // Default/Fallback behavior for unknown kind
            else
            {
                if (string.IsNullOrWhiteSpace(baseSerial))
                    baseSerial = s;
                else if (string.IsNullOrWhiteSpace(roverSerial))
                    roverSerial = s;
            }
        }

        Assign(SelectedGpsSerial1Kind, GpsSerial1Text);
        Assign(SelectedGpsSerial2Kind, GpsSerial2Text);

        if (string.IsNullOrWhiteSpace(baseSerial))
            Assign("Base", SerialText);
        if (string.IsNullOrWhiteSpace(roverSerial))
            Assign("Rover", SerialText2);

        // Final fallback: if Base is empty but we have data in the first box, use it
        if (string.IsNullOrWhiteSpace(baseSerial) && !string.IsNullOrWhiteSpace(GpsSerial1Text) && string.IsNullOrWhiteSpace(roverSerial))
             baseSerial = GpsSerial1Text.Trim();
             
        // Ensure we don't return nulls
        return (baseSerial ?? "", roverSerial ?? "");
    }

    async Task<bool> SaveCertificateInternalAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientName))
        {
            StatusMessage = "Client name is required.";
            MessageBox.Show("يرجى إدخال اسم العميل أولاً.", "بيانات ناقصة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(Model))
        {
            StatusMessage = "Model is required.";
            MessageBox.Show("يرجى إدخال الموديل أولاً.", "بيانات ناقصة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (string.Equals(DeviceType, "GPS", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(GpsSerial1Text) && string.IsNullOrWhiteSpace(GpsSerial2Text))
            {
                StatusMessage = "Serial number is required.";
                MessageBox.Show("Please enter GPS serial (Base or Rover).", "Missing data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(SerialText))
            {
                StatusMessage = "Serial number is required.";
                MessageBox.Show("يرجى إدخال السيريال أولاً.", "بيانات ناقصة", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        if (string.Equals(DeviceType, "Auto Level", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(SpecValue))
        {
            StatusMessage = "Auto Level deviation rate is required.";
            MessageBox.Show("يرجى إدخال قيمة معيار الانحراف (Auto Level) أولاً.", "بيانات ناقصة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        IsBusy = true; BusyMessage = "جارٍ الحفظ...";
        try
        {
            using var db = CreateDbContext();
            await db.Database.EnsureCreatedAsync();

            var typedName = (ClientName ?? "").Trim();
            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(typedName))
            {
                customer = await db.Customers.FirstOrDefaultAsync(c => c.Name == typedName);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        CreatedAt = DateTime.Now,
                        Name = typedName,
                        Phone = Phone ?? ""
                    };
                    db.Customers.Add(customer);
                    await db.SaveChangesAsync();
                    if (!ClientOptions.Contains(typedName))
                        ClientOptions.Add(typedName);
                }
            }

            var isLoadedFromArchive = _currentCertificateId.HasValue &&
                                      SelectedCertificate != null &&
                                      SelectedCertificate.Id == _currentCertificateId.Value;

            var forceNewFromExisting = false;
            if (isLoadedFromArchive)
            {
                var result = MessageBox.Show(
                    "تم تحميل هذه الشهادة من الأرشيف.\nهل تريد تعديل الشهادة الحالية (استبدالها) أم حفظها كشهادة جديدة؟\n\nنعم = تعديل الشهادة الحالية\nلا = حفظ كشهادة جديدة\nإلغاء = إلغاء العملية",
                    "طريقة الحفظ",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    StatusMessage = "تم إلغاء العملية.";
                    return false;
                }
                forceNewFromExisting = (result == MessageBoxResult.No);
            }
            else if (_currentCertificateId.HasValue && SelectedCertificate == null)
            {
                // This means it was loaded directly from Maintenance Sync, not from the Archive grid.
                // In this case, we MUST update the existing one (WO-xxxx) directly to finalize its values and change its number.
                forceNewFromExisting = false;
            }

            Certificate certificate;
            if (_currentCertificateId.HasValue && !forceNewFromExisting)
            {
                certificate = await db.Certificates.FirstOrDefaultAsync(c => c.Id == _currentCertificateId.Value) ?? new Certificate();
                if (certificate.Id == 0)
                {
                    certificate.CreatedAt = DateTime.Now;
                    certificate.RowVersion = Guid.NewGuid().ToByteArray();
                    db.Certificates.Add(certificate);
                }
            }
            else
            {
                var isGps = string.Equals(DeviceType, "GPS", StringComparison.OrdinalIgnoreCase);
                var mapped = isGps ? MapGpsSerials() : (BaseSerial: "", RoverSerial: "");
                var baseSerial = isGps ? mapped.BaseSerial : (SerialText ?? "");
                var roverSerial = isGps ? mapped.RoverSerial : "";
                var clientName = customer?.Name ?? ClientName ?? "";
                var phone = string.IsNullOrWhiteSpace(Phone) ? (customer?.Phone ?? "") : Phone ?? "";
                var brand = Brand ?? "";
                var model = Model ?? "";
                var specValue = string.Equals(DeviceType, "Total Station", StringComparison.OrdinalIgnoreCase)
                    ? (Accuracy ?? "")
                    : (SpecValue ?? "");
                var delegateName = DelegateName ?? "";

                if (forceNewFromExisting)
                {
                    certificate = new Certificate();
                }
                else
                {
                    var recentCutoff = DateTime.Now.AddMinutes(-10);
                    certificate = await db.Certificates.FirstOrDefaultAsync(c =>
                        c.CreatedAt >= recentCutoff &&
                        (c.ClientName ?? "") == clientName &&
                        (c.Phone ?? "") == phone &&
                        (c.DeviceType ?? "") == (DeviceType ?? "") &&
                        (c.Brand ?? "") == brand &&
                        (c.Model ?? "") == model &&
                        (c.SerialText ?? "") == baseSerial &&
                        (c.SerialText2 ?? "") == roverSerial &&
                        (c.SpecValue ?? "") == specValue &&
                        (c.DelegateName ?? "") == delegateName &&
                        c.IssueDate == IssueDate &&
                        c.ExpiryDate == ExpiryDate
                    ) ?? new Certificate();
                }

                if (certificate.Id == 0)
                {
                    certificate.CreatedAt = DateTime.Now;
                    certificate.RowVersion = Guid.NewGuid().ToByteArray();
                    db.Certificates.Add(certificate);
                }
            }

            certificate.ClientName = customer?.Name ?? ClientName ?? string.Empty;
            certificate.Phone = string.IsNullOrWhiteSpace(Phone) ? (customer?.Phone ?? "") : Phone;
            certificate.DeviceType = DeviceType;
            certificate.Brand = Brand ?? string.Empty;
            certificate.Model = Model ?? string.Empty;
            if (string.Equals(DeviceType, "GPS", StringComparison.OrdinalIgnoreCase))
            {
                var mapped = MapGpsSerials();
                SerialText = mapped.BaseSerial;
                SerialText2 = mapped.RoverSerial;
                certificate.SerialText = mapped.BaseSerial;
                certificate.SerialText2 = mapped.RoverSerial;
            }
            else
            {
                certificate.SerialText = SerialText ?? string.Empty;
                certificate.SerialText2 = "";
            }
            if (string.Equals(DeviceType, "Total Station", StringComparison.OrdinalIgnoreCase))
                certificate.SpecValue = Accuracy ?? string.Empty;
            else if (string.Equals(DeviceType, "Auto Level", StringComparison.OrdinalIgnoreCase))
                certificate.SpecValue = SpecValue ?? "";
            else
                certificate.SpecValue = SpecValue ?? "";
            certificate.IssueDate = IssueDate;
            certificate.ExpiryDate = ExpiryDate;
            certificate.DelegateName = DelegateName ?? string.Empty;
            certificate.PayloadEnc = BuildCalibrationPayload();
            try
            {
                var tplStore = new ReportTemplateStore();
                var templateName = string.IsNullOrWhiteSpace(SelectedTemplateName) ? DeviceType : SelectedTemplateName;
                var templateId = tplStore.FindTemplateId(DeviceType, templateName);
                certificate.TemplateId = templateId.HasValue ? (int?)templateId.Value : null;
            }
            catch
            {
                certificate.TemplateId = null;
            }

            await db.SaveChangesAsync();

            // Generate proper certificate number if it's null/whitespace or has temporary marker
            if (string.IsNullOrWhiteSpace(certificate.CertificateNumber) || certificate.CertificateNumber.StartsWith("TEMP-WO-"))
            {
                certificate.CertificateNumber = BuildCertificateNumber(certificate);
                await db.SaveChangesAsync();
            }

            _currentCertificateId = certificate.Id;
            _isEditMode = true;
            UpdateModeText();
            CertificateNumber = certificate.CertificateNumber;
            DeleteCertificateCommand.RaiseCanExecuteChanged();
            GeneratePdfCommand.RaiseCanExecuteChanged();
            PrintStickerCommand.RaiseCanExecuteChanged();
            PreviewStickerZplCommand.RaiseCanExecuteChanged();
            PrintA4Command.RaiseCanExecuteChanged();
            await RefreshCertificatesAsync();
            StatusMessage = "تم حفظ الشهادة.";

            try
            {
                var store = new CalibrationDefaultsStore();
                var rows = CalibrationRows.Select(r => new CalibrationDefaultsRow { Item = r.Item, MeasuredValue = r.MeasuredValue.HasValue ? (decimal?)r.MeasuredValue.Value : null }).ToList();
                store.Save(DeviceType, rows);
            }
            catch
            {
            }
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر الحفظ: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false; BusyMessage = "";
        }
    }

    static string GetA4PrinterName()
    {
        try
        {
            return (ConfigurationManager.AppSettings["A4PrinterName"] ?? "").Trim();
        }
        catch
        {
            return "";
        }
    }

    static bool IsPrinterInstalled(string name)
    {
        try
        {
            foreach (string p in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(p, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
        }
        return false;
    }

    async Task SaveCertificateAsync()
    {
        if (!await SaveCertificateInternalAsync())
            return;

        if (IsAutoUploadEnabled)
        {
            BusyMessage = "جارٍ رفع الشهادة للسحابة...";
            await UploadCurrentCertificateAfterSaveAsync();
        }
    }

    void CalibrationRows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void Hook(INotifyPropertyChanged? npc)
        {
            if (npc == null) return;
            npc.PropertyChanged += CalibrationRow_PropertyChanged;
        }
        void Unhook(INotifyPropertyChanged? npc)
        {
            if (npc == null) return;
            npc.PropertyChanged -= CalibrationRow_PropertyChanged;
        }
        if (e.OldItems != null)
            foreach (var item in e.OldItems.Cast<INotifyPropertyChanged>())
                Unhook(item);
        if (e.NewItems != null)
            foreach (var item in e.NewItems.Cast<INotifyPropertyChanged>())
                Hook(item);
    }

    void CalibrationRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CalibrationRow.MeasuredValue))
            return;
        try
        {
            var store = new CalibrationDefaultsStore();
            var rows = CalibrationRows.Select(r => new CalibrationDefaultsRow { Item = r.Item, MeasuredValue = r.MeasuredValue.HasValue ? (decimal?)r.MeasuredValue.Value : null }).ToList();
            store.Save(DeviceType, rows);
        }
        catch
        {
        }
    }

    async Task DeleteCertificateAsync()
    {
        if (SelectedCertificate == null)
            return;
        IsBusy = true; BusyMessage = "جارٍ الحذف...";
        using var db = CreateDbContext();
        var entity = await db.Certificates.FirstOrDefaultAsync(c => c.Id == SelectedCertificate.Id);
        if (entity != null)
        {
            db.Certificates.Remove(entity);
            await db.SaveChangesAsync();
        }
        NewCertificate();
        await RefreshCertificatesAsync();
        StatusMessage = "Certificate deleted.";
        IsBusy = false; BusyMessage = "";
    }

    async Task RefreshCertificatesAsync()
    {
        await Task.Yield();
        await EnsureDbCreatedOnceAsync();

        var search = (SearchText ?? "").Trim();
        var showExpired = _showExpiredOnly;
        var items = await Task.Run(() =>
        {
            using var db = CreateDbContext();
            var query = db.Certificates.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.ClientName.Contains(search) || c.Phone.Contains(search) || c.SerialText.Contains(search));
            if (showExpired)
                query = query.Where(c => c.ExpiryDate < DateTime.Today);
            return query
                .OrderByDescending(c => c.IssueDate)
                .Take(50)
                .Select(c => new CertificateRow
                {
                    Id = c.Id,
                    CertificateNumber = c.CertificateNumber,
                    ClientName = c.ClientName,
                    Phone = c.Phone,
                    DeviceType = c.DeviceType,
                    Brand = c.Brand,
                    Model = c.Model,
                    SerialText = c.SerialText,
                    SerialText2 = c.SerialText2,
                    SpecValue = c.SpecValue,
                    IssueDate = c.IssueDate,
                    ExpiryDate = c.ExpiryDate,
                    DelegateName = c.DelegateName,
                    PayloadEnc = c.PayloadEnc
                })
                .ToList();
        });

        Certificates.Clear();
        foreach (var item in items)
            Certificates.Add(item);
    }

    void LoadFromSelection(CertificateRow? row)
    {
        if (row == null)
            return;
        LoadFullCertificateForEdit(row);
    }

    void LoadFullCertificateForEdit(CertificateRow row)
    {
        BeginLoad();
        try
        {
            _isEditMode = true;
            UpdateModeText();
            _currentCertificateId = row.Id;
            ClientName = row.ClientName;
            Phone = row.Phone;
            DeviceType = row.DeviceType;
            Brand = row.Brand;
            Model = row.Model;
            SerialText = row.SerialText;
            SerialText2 = row.SerialText2;
            if (string.Equals(row.DeviceType, "Total Station", StringComparison.OrdinalIgnoreCase))
            {
                Accuracy = row.SpecValue;
                SpecValue = "";
            }
            else
            {
                Accuracy = "";
                SpecValue = row.SpecValue;
            }
            if (string.Equals(row.DeviceType, "GPS", StringComparison.OrdinalIgnoreCase))
            {
                GpsSerial1Text = row.SerialText;
                SelectedGpsSerial1Kind = "Base";
                GpsSerial2Text = row.SerialText2;
                SelectedGpsSerial2Kind = "Rover";
            }
            else
            {
                GpsSerial1Text = "";
                GpsSerial2Text = "";
                SelectedGpsSerial1Kind = "Base";
                SelectedGpsSerial2Kind = "Rover";
            }
            AutoLevelCompensatorCheck = false;
            IssueDate = row.IssueDate;
            ExpiryDate = row.ExpiryDate;
            DelegateName = row.DelegateName;
            CertificateNumber = row.CertificateNumber;
            LoadCalibrationPayload(row.PayloadEnc);
            try { LastVerificationUrl = LoadVerifyUrlLocal(CertificateNumber ?? ""); } catch { }
        }
        finally
        {
            EndLoad();
        }
        DeleteCertificateCommand?.RaiseCanExecuteChanged();
        GeneratePdfCommand?.RaiseCanExecuteChanged();
        PrintStickerCommand?.RaiseCanExecuteChanged();
        PreviewStickerZplCommand?.RaiseCanExecuteChanged();
        PrintA4Command?.RaiseCanExecuteChanged();
    }

    void UploadTemplate()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "RDLC/FastReport/PDF/Word|*.rdlc;*.frx;*.pdf;*.docx|Text Files|*.txt;*.rtf|Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*"
        };
        if (dialog.ShowDialog() != true)
            return;
        var path = dialog.FileName;
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".pdf")
        {
            var outDir = System.IO.Path.Combine(AppContext.BaseDirectory, "TemplatePreview");
            System.IO.Directory.CreateDirectory(outDir);
            var png = BlueMax.Infrastructure.PdfConverter.TryConvertPdfToPng(path, 300, 90);
            LayoutTemplatePdfPath = path;
            LayoutTemplatePath = png ?? "";
            if (string.IsNullOrWhiteSpace(LayoutTemplatePath))
            {
                StatusMessage = "تعذر معاينة PDF: يرجى تثبيت LibreOffice أو ضبط LIBREOFFICE_PROGRAM.";
                return;
            }
        }
        else if (ext == ".docx")
        {
            LayoutTemplatePdfPath = path;
            LayoutTemplatePath = "";
        }
        else if (ext == ".rdlc" || ext == ".frx")
        {
            LayoutTemplatePdfPath = "";
            LayoutTemplatePath = path;
        }
        else
        {
            LayoutTemplatePdfPath = "";
            LayoutTemplatePath = path;
        }
        StatusMessage = "Template uploaded.";
    }

    void ChooseBackgroundImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Templates and Backgrounds|*.rdlc;*.frx;*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.txt;*.rtf"
        };
        if (dialog.ShowDialog() != true)
            return;
        var path = dialog.FileName;
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".pdf")
        {
            LayoutTemplatePdfPath = path;
            LayoutTemplatePath = "";
        }
        else if (ext == ".txt" || ext == ".rtf")
        {
            var outDir = System.IO.Path.Combine(AppContext.BaseDirectory, "TemplatePreview");
            System.IO.Directory.CreateDirectory(outDir);
            var pngPath = System.IO.Path.Combine(outDir, $"text-bg-{DateTime.Now:yyyyMMddHHmmss}.png");
            try
            {
                RenderTextOrRtfToPng(path, LayoutCanvasWidth, LayoutCanvasHeight, pngPath);
                LayoutTemplatePdfPath = "";
                LayoutTemplatePath = File.Exists(pngPath) ? pngPath : "";
            }
            catch
            {
                LayoutTemplatePdfPath = "";
                LayoutTemplatePath = "";
                StatusMessage = "تعذر توليد خلفية من الملف النصي.";
                return;
            }
        }
        else if (ext == ".rdlc" || ext == ".frx")
        {
            LayoutTemplatePdfPath = "";
            LayoutTemplatePath = path;
        }
        else
        {
            LayoutTemplatePdfPath = "";
            LayoutTemplatePath = path;
        }
        StatusMessage = "تم اختيار صورة خلفية للقالب.";
    }

    public static void RenderTextOrRtfToPng(string path, double width, double height, string outputPng)
    {
        var rtb = new RichTextBox
        {
            Width = width,
            Height = height,
            BorderThickness = new Thickness(0),
            Background = Brushes.White,
            Foreground = Brushes.Black
        };
        using var fs = File.OpenRead(path);
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
        if (ext == ".rtf")
            range.Load(fs, DataFormats.Rtf);
        else
            range.Text = File.ReadAllText(path);

        rtb.Measure(new Size(width, height));
        rtb.Arrange(new Rect(0, 0, width, height));
        var bmp = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(rtb);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var outFs = File.Create(outputPng);
        encoder.Save(outFs);
    }

    async Task PreviewTemplateAsync()
    {
        var path = (LayoutTemplatePdfPath ?? "") != "" ? LayoutTemplatePdfPath : (LayoutTemplatePath ?? "");
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "قم برفع قالب أولاً ثم اضغط معاينة.";
            return;
        }
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        try
        {
            if (ext is ".docx")
            {
            if (_currentCertificateId.HasValue)
            {
                using var db = CreateDbContext();
                await EnsureDbCreatedOnceAsync();
                var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
                var engine = new WordTemplateEngine(System.IO.Path.GetDirectoryName(path) ?? "", outputDir);
                var docService = new CertificateDocumentService(db, engine);
                var result = await docService.GenerateCertificateDocxAndPdfByIdAsync(_currentCertificateId.Value, System.IO.Path.GetFileName(path));
                var openPath = result;
                Process.Start(new ProcessStartInfo { FileName = openPath, UseShellExecute = true });
                StatusMessage = "تم إنشاء معاينة DOCX.";
            }
            else
            {
                var data = new Dictionary<string, object>
                {
                    ["client_name"] = ClientName,
                    ["phone"] = Phone ?? "",
                    ["device_type"] = DeviceType,
                    ["brand"] = Brand ?? "",
                    ["model"] = Model ?? "",
                    ["serial"] = SerialText ?? "",
                    ["spec"] = SpecValue ?? "",
                    ["cert_no"] = string.IsNullOrWhiteSpace(CertificateNumber) ? "PREVIEW" : CertificateNumber,
                    ["exp"] = ExpiryDate.ToString("dd-MM-yyyy"),
                    ["issue_date"] = IssueDate.ToString("dd-MM-yyyy"),
                    ["delegate_name"] = DelegateName
                };
                var engine = new WordTemplateEngine(System.IO.Path.GetDirectoryName(path) ?? "", System.IO.Path.Combine(AppContext.BaseDirectory, "TemplatePreview"));
                var docxName = System.IO.Path.GetFileName(path);
                var outputDocx = await Task.Run(() => engine.GenerateDocument(docxName, data));
                Process.Start(new ProcessStartInfo { FileName = outputDocx, UseShellExecute = true });
                StatusMessage = "تم إنشاء معاينة DOCX.";
            }
            }
            else
            {
                StatusMessage = "المعاينة تعتمد على قوالب Word (DOCX) فقط.";
            }
        }
        catch (Exception ex)
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "last-pdf-error.txt");
            try
            {
                System.IO.File.WriteAllText(logPath, ex.ToString());
            }
            catch
            {
            }
            StatusMessage = $"حدث خطأ أثناء إنشاء المعاينة (تفاصيل في last-pdf-error.txt): {ex.Message}";
        }
    }

    void RefreshTemplateOptions()
    {
        var version = ++_templateOptionsVersion;
        var deviceType = DeviceType ?? "";
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var store = new ReportTemplateStore();
                return store.ListTemplates(deviceType);
            }
            catch
            {
                return new List<string>();
            }
        }).ContinueWith(async t =>
        {
            if (version != _templateOptionsVersion)
                return;
            if (!t.IsCompletedSuccessfully)
                return;
            var names = await t ?? new List<string>();
            try
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    TemplateOptions.Clear();
                    foreach (var n in names)
                        TemplateOptions.Add(n);
                    if (TemplateOptions.Count > 0 && string.IsNullOrWhiteSpace(SelectedTemplateName))
                        SelectedTemplateName = TemplateOptions[0];
                });
            }
            catch
            {
            }
        });
    }

    void LoadSelectedTemplate()
    {
        if (string.IsNullOrWhiteSpace(SelectedTemplateName))
            return;
        try
        {
            var store = new ReportTemplateStore();
            var tpl = store.Load(DeviceType, SelectedTemplateName);
            var path = tpl?.LayoutTemplatePath ?? "";
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".pdf")
            {
                LayoutTemplatePdfPath = path;
                LayoutTemplatePath = "";
                StatusMessage = "جاري تجهيز معاينة PDF...";
            }
        else if (ext == ".docx")
        {
            LayoutTemplatePdfPath = path;
            LayoutTemplatePath = "";
        }
            else
            {
                LayoutTemplatePdfPath = "";
                LayoutTemplatePath = path;
            }
            PrintMode = "Full Print";
            LayoutBoxes.Clear();
            foreach (var item in tpl?.LayoutItems ?? new List<ReportLayoutItem>())
            {
                LayoutBoxes.Add(new LayoutBoxItem
                {
                    Key = item.Key ?? "",
                    Title = item.Title ?? item.Key ?? "",
                    X = item.Xmm / PixelToMm,
                    Y = item.Ymm / PixelToMm
                });
            }
            StatusMessage = $"تم تحميل القالب: {SelectedTemplateName}";
        }
        catch
        {
            StatusMessage = "تعذر تحميل القالب المحدد.";
        }
    }

    void SaveTemplate()
    {
        try
        {
            var store = new ReportTemplateStore();
            var name = SelectedTemplateName;
            if (string.IsNullOrWhiteSpace(name))
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Layout Templates|*.xml",
                    FileName = $"{DeviceType}.xml",
                    InitialDirectory = store.GetDeviceDir(DeviceType)
                };
                if (sfd.ShowDialog() != true)
                {
                    StatusMessage = "ألغيت حفظ القالب.";
                    return;
                }
                name = System.IO.Path.GetFileNameWithoutExtension(sfd.FileName);
            }

            var rawPath = string.IsNullOrWhiteSpace(LayoutTemplatePdfPath) ? (LayoutTemplatePath ?? "") : LayoutTemplatePdfPath;
            var layoutPath = rawPath;
            try
            {
                if (!string.IsNullOrWhiteSpace(rawPath) && System.IO.File.Exists(rawPath))
                {
                    var deviceDir = store.GetDeviceDir(DeviceType);
                    var ext = System.IO.Path.GetExtension(rawPath);
                    if (string.IsNullOrWhiteSpace(ext))
                        ext = ".png";
                    var destPath = System.IO.Path.Combine(deviceDir, name + ext);
                    System.IO.Directory.CreateDirectory(deviceDir);
                    System.IO.File.Copy(rawPath, destPath, overwrite: true);
                    layoutPath = destPath;
                    if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        var previewPng = LayoutTemplatePath ?? "";
                        if (!string.IsNullOrWhiteSpace(previewPng) && System.IO.File.Exists(previewPng))
                        {
                            var destPng = System.IO.Path.Combine(deviceDir, name + ".png");
                            System.IO.File.Copy(previewPng, destPng, overwrite: true);
                        }
                    }
                }
            }
            catch
            {
                layoutPath = rawPath;
            }

            var tpl = new ReportTemplateXml
            {
                LayoutTemplatePath = layoutPath ?? "",
                PrintWithBackground = false,
                LayoutItems = LayoutBoxes.Select(box => new ReportLayoutItem
                {
                    Key = box.Key,
                    Title = box.Title,
                    Xmm = box.X * PixelToMm,
                    Ymm = box.Y * PixelToMm
                }).ToList()
            };

            store.Save(DeviceType, name, tpl);
            if (!TemplateOptions.Contains(name))
                TemplateOptions.Add(name);
            SelectedTemplateName = name;
            StatusMessage = "تم حفظ القالب.";
        }
        catch
        {
            StatusMessage = "تعذر حفظ القالب.";
        }
    }

    void NewTemplate()
    {
        try
        {
            var store = new ReportTemplateStore();
            var sfd = new SaveFileDialog
            {
                Filter = "Layout Templates|*.xml",
                FileName = $"{DeviceType}.xml",
                InitialDirectory = store.GetDeviceDir(DeviceType)
            };
            if (sfd.ShowDialog() != true)
                return;
            var name = System.IO.Path.GetFileNameWithoutExtension(sfd.FileName);
            var rawPath = string.IsNullOrWhiteSpace(LayoutTemplatePdfPath) ? (LayoutTemplatePath ?? "") : LayoutTemplatePdfPath;
            var layoutPath = rawPath;
            try
            {
                if (!string.IsNullOrWhiteSpace(rawPath) && System.IO.File.Exists(rawPath))
                {
                    var deviceDir = store.GetDeviceDir(DeviceType);
                    var ext = System.IO.Path.GetExtension(rawPath);
                    if (string.IsNullOrWhiteSpace(ext))
                        ext = ".png";
                    var destPath = System.IO.Path.Combine(deviceDir, name + ext);
                    System.IO.Directory.CreateDirectory(deviceDir);
                    System.IO.File.Copy(rawPath, destPath, overwrite: true);
                    layoutPath = destPath;
                    if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        var previewPng = LayoutTemplatePath ?? "";
                        if (!string.IsNullOrWhiteSpace(previewPng) && System.IO.File.Exists(previewPng))
                        {
                            var destPng = System.IO.Path.Combine(deviceDir, name + ".png");
                            System.IO.File.Copy(previewPng, destPng, overwrite: true);
                        }
                    }
                }
            }
            catch
            {
                layoutPath = rawPath;
            }
            var tpl = new ReportTemplateXml
            {
                LayoutTemplatePath = layoutPath ?? "",
                PrintWithBackground = PrintMode == "Full Print",
                LayoutItems = LayoutBoxes.Select(box => new ReportLayoutItem
                {
                    Key = box.Key,
                    Title = box.Title,
                    Xmm = box.X * PixelToMm,
                    Ymm = box.Y * PixelToMm
                }).ToList()
            };
            store.Save(DeviceType, name, tpl);
            RefreshTemplateOptions();
            SelectedTemplateName = name;
            StatusMessage = "تم إنشاء وحفظ قالب جديد.";
        }
        catch
        {
            StatusMessage = "تعذر إنشاء القالب.";
        }
    }

    void SaveReportSettings()
    {
        var store = new ReportDesignerSettingsStore();
        var settings = store.Load();
        settings.LayoutTemplatePath = string.IsNullOrWhiteSpace(LayoutTemplatePdfPath) ? LayoutTemplatePath : LayoutTemplatePdfPath;
        settings.PrintWithBackground = string.Equals(PrintMode, "Full Print", StringComparison.OrdinalIgnoreCase);
        settings.OffsetXmm = PrintOffsetXmm;
        settings.OffsetYmm = PrintOffsetYmm;
        settings.FlipX = PrintFlipX;
        settings.FlipY = PrintFlipY;
        settings.LayoutItems = LayoutBoxes
            .Where(box => IsFieldVisible(box.Key))
            .Select(box => new ReportLayoutItem
        {
            Key = box.Key,
            Title = box.Title,
            Xmm = box.X * PixelToMm,
            Ymm = box.Y * PixelToMm
        }).ToList();
        store.Save(settings);
        StatusMessage = "Layout saved.";
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        ReportDesignerSettings settings;
        try
        {
            settings = await Task.Run(() => new ReportDesignerSettingsStore().Load());
        }
        catch
        {
            settings = new ReportDesignerSettings();
        }
        ApplyReportSettings(settings);
        _ = LoadSelectedTemplateHubPathAsync();
        _ = EnsureDbCreatedOnceAsync();
        _ = RefreshCertificatesAsync();
        RefreshTemplateOptions();
    }

    void ApplyReportSettings(ReportDesignerSettings settings)
    {
        var templatePath = settings.LayoutTemplatePath ?? "";
        LayoutTemplatePdfPath = "";
        LayoutTemplatePath = templatePath;
        PrintMode = settings.PrintWithBackground ? "Full Print" : "Data Only";
        PrintOffsetXmm = settings.OffsetXmm;
        PrintOffsetYmm = settings.OffsetYmm;
        PrintFlipX = settings.FlipX;
        PrintFlipY = settings.FlipY;
        LayoutBoxes.Clear();
        foreach (var item in settings.LayoutItems)
        {
            LayoutBoxes.Add(new LayoutBoxItem
            {
                Key = item.Key ?? string.Empty,
                Title = string.IsNullOrWhiteSpace(item.Title) ? item.Key ?? string.Empty : item.Title,
                X = item.Xmm / PixelToMm,
                Y = item.Ymm / PixelToMm
            });
        }
    }

    ReportDesignerSettings BuildReportSettingsForPrint(bool onlyVisibleFields)
    {
        var store = new ReportDesignerSettingsStore();
        var settings = store.Load();

        var liveTemplatePath = string.IsNullOrWhiteSpace(LayoutTemplatePdfPath) ? (LayoutTemplatePath ?? "") : LayoutTemplatePdfPath;
        settings.LayoutTemplatePath = liveTemplatePath;
        settings.PrintWithBackground = string.Equals(PrintMode, "Full Print", StringComparison.OrdinalIgnoreCase);
        settings.OffsetXmm = PrintOffsetXmm;
        settings.OffsetYmm = PrintOffsetYmm;
        settings.FlipX = PrintFlipX;
        settings.FlipY = PrintFlipY;
        try
        {
            if (!string.IsNullOrWhiteSpace(SelectedTemplateName))
            {
                var tplStore = new ReportTemplateStore();
                var tpl = tplStore.Load(DeviceType, SelectedTemplateName);
                if (tpl != null && (tpl.LayoutItems?.Count > 0 || !string.IsNullOrWhiteSpace(tpl.LayoutTemplatePath)))
                {
                    var path = tpl?.LayoutTemplatePath ?? "";
                    settings.LayoutTemplatePath = path;
                    if (!string.IsNullOrWhiteSpace(settings.LayoutTemplatePath))
                        settings.PrintWithBackground = true;
                    settings.LayoutItems = (tpl?.LayoutItems ?? new List<ReportLayoutItem>())
                        .Where(i => !onlyVisibleFields || IsFieldVisible(i.Key ?? string.Empty))
                        .ToList();
                    return settings;
                }
            }
        }
        catch
        {
        }
        settings.LayoutTemplatePath = string.IsNullOrWhiteSpace(LayoutTemplatePdfPath) ? LayoutTemplatePath : LayoutTemplatePdfPath;
        settings.LayoutItems = LayoutBoxes
            .Where(box => !onlyVisibleFields || IsFieldVisible(box.Key))
            .Select(box => new ReportLayoutItem
            {
                Key = box.Key,
                Title = box.Title,
                Xmm = box.X * PixelToMm,
                Ymm = box.Y * PixelToMm
            }).ToList();
        return settings;
    }

    async Task GeneratePdfAsync()
    {
        if (IsBusy)
        {
            StatusMessage = "Another operation is in progress.";
            return;
        }
        if (!await SaveCertificateInternalAsync())
            return;
        if (!_currentCertificateId.HasValue)
            return;

        if (IsAutoUploadEnabled)
            await EnsureVerifyUrlBeforeDocumentAsync();

        IsBusy = true; BusyMessage = "Generating Word preview...";
        try
        {
            using var db = CreateDbContext();
            await EnsureDbCreatedOnceAsync();
            var certificateId = _currentCertificateId.Value;
            var templateKey = MapCertificateTemplateKey(DeviceType);
            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
            var engine = new WordTemplateEngine("", outputDir);
            var docService = new CertificateDocumentService(db, engine);
            var templatePath = await EnsureTemplatePathAsync(templateKey);
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                StatusMessage = "Please select a Word template from the template manager first.";
                return;
            }

            var result = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(certificateId, templatePath);
            var outputPath = result;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                StatusMessage = "Failed to generate Word document.";
                return;
            }

            StatusMessage = $"Word document generated: {outputPath}";
            Process.Start(new ProcessStartInfo { FileName = outputPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to generate Word preview: {ex.Message}";
        }
        finally
        {
            IsBusy = false; BusyMessage = "";
        }
    }

    async Task PrintA4Async()
    {
        if (IsBusy)
        {
            StatusMessage = "Another operation is in progress.";
            return;
        }
        if (!await SaveCertificateInternalAsync())
            return;
        if (!_currentCertificateId.HasValue)
            return;
        if (IsAutoUploadEnabled)
            await EnsureVerifyUrlBeforeDocumentAsync();
        IsBusy = true; BusyMessage = "Preparing document for printing...";
        try
        {
            using var db = CreateDbContext();
            await EnsureDbCreatedOnceAsync();
            var certificateId = _currentCertificateId.Value;
            var templateKey = MapCertificateTemplateKey(DeviceType);
            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
            var engine = new WordTemplateEngine("", outputDir);
            var docService = new CertificateDocumentService(db, engine);
            var templatePath = await EnsureTemplatePathAsync(templateKey);
            
            string? docxPath;
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                StatusMessage = "يرجى تحديد قالب Word أولاً من إدارة القوالب.";
                return;
            }
            else
            {
                var result = await docService.GenerateCertificateDocxAndPdfByIdFromPathAsync(certificateId, templatePath);
                docxPath = result;
                if (string.IsNullOrWhiteSpace(docxPath))
                {
                    StatusMessage = "فشل إنشاء ملف Word.";
                    return;
                }
            }
            
            try
            {
                var a4Printer = GetA4PrinterName();
                var useSpecific = !string.IsNullOrWhiteSpace(a4Printer) && IsPrinterInstalled(a4Printer);
                var printInfo = new ProcessStartInfo
                {
                    FileName = docxPath,
                    UseShellExecute = true,
                    Verb = useSpecific ? "printto" : "print",
                    Arguments = useSpecific ? $"\"{a4Printer}\"" : ""
                };
                Process.Start(printInfo);
                StatusMessage = useSpecific
                    ? $"Document sent to printer: {a4Printer}"
                    : "Document sent to the default printer.";
            }
            catch (Exception)
            {
                try
                {
                    // Fallback: just open in Word and let the user print manually
                    Process.Start(new ProcessStartInfo { FileName = docxPath, UseShellExecute = true });
                    StatusMessage = "Opened in Word. Please print from Word.";
                }
                catch (Exception ex2)
                {
                    StatusMessage = $"Failed to print or open in Word: {ex2.Message}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر تجهيز الملف: {ex.Message}";
        }
        finally
        {
            IsBusy = false; BusyMessage = "";
        }
    }

    static string MapCertificateTemplateKey(string deviceType)
    {
        var dt = (deviceType ?? "").Trim();
        if (string.Equals(dt, "GPS", StringComparison.OrdinalIgnoreCase))
            return "cert_gps";
        if (string.Equals(dt, "Auto Level", StringComparison.OrdinalIgnoreCase))
            return "cert_auto_level";
        return "cert_total_station";
    }

    async Task<string> LoadTemplatePathAsync(string docKey)
    {
        if (string.IsNullOrWhiteSpace(docKey))
            return "";
        try
        {
            var raw = await Task.Run(() => new DocumentPathMappingStore().GetPath(docKey)) ?? "";
            var path = raw.Trim();
            if (string.IsNullOrWhiteSpace(path))
                return "";
            if (Path.IsPathRooted(path) && File.Exists(path))
                return path;
            var fallback = Path.Combine(AppContext.BaseDirectory, path);
            if (File.Exists(fallback))
                return fallback;
            return "";
        }
        catch
        {
            return "";
        }
    }

    async Task<string> EnsureTemplatePathAsync(string docKey)
    {
        var path = await LoadTemplatePathAsync(docKey);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            return path;
        if (!string.IsNullOrWhiteSpace(path))
            StatusMessage = "قالب Word غير موجود. يرجى إعادة رفع القالب من إدارة القوالب.";
        var ofd = new OpenFileDialog
        {
            Filter = "Word Template (*.docx)|*.docx",
            Multiselect = false
        };
        if (ofd.ShowDialog() != true)
            return "";
        var selected = (ofd.FileName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(selected) || !File.Exists(selected))
            return "";
        new DocumentPathMappingStore().SetPath(docKey, selected);
        return selected;
    }

    async Task ExportStickerZplAsync()
    {
        if (!await SaveCertificateInternalAsync())
            return;
        if (!_currentCertificateId.HasValue)
            return;
        if (IsAutoUploadEnabled)
            await EnsureVerifyUrlBeforeDocumentAsync();

        IsBusy = true; BusyMessage = "جارٍ إرسال الملصق...";
        var reportSettings = await Task.Run(() => new ReportDesignerSettingsStore().Load());
        var companyName = reportSettings.CompanyName ?? "";
        var companyHeader = reportSettings.CompanyHeader ?? "";
        var companyPhone = reportSettings.CompanyPhone ?? "";
        var companyAddress = reportSettings.CompanyAddress ?? "";

        var settings = await Task.Run(() => new PrinterSettingsStore().Load());
        var protocol = settings.Protocol ?? "ZPL";

        try
        {
            var tempStickerDesignerViewModel = new StickerDesignerViewModel();
            tempStickerDesignerViewModel.CompanyName = companyName;
            tempStickerDesignerViewModel.CompanyHeader = companyHeader;
            tempStickerDesignerViewModel.CompanyAddress = companyAddress;
            tempStickerDesignerViewModel.CompanyPhone = companyPhone;
            tempStickerDesignerViewModel.Brand = Brand ?? "";
            tempStickerDesignerViewModel.Model = Model ?? "";
            tempStickerDesignerViewModel.Serial = SerialText ?? "";
            tempStickerDesignerViewModel.CertificateNumber = CertificateNumber ?? "";
            tempStickerDesignerViewModel.CalDate = IssueDate;
            tempStickerDesignerViewModel.ExpDate = ExpiryDate;
            tempStickerDesignerViewModel.UpdateQrContent();

            var service = new StickerPrintService();
            await service.PrintStickerAsync(tempStickerDesignerViewModel, settings);
            StatusMessage = string.Equals(protocol, "Windows", StringComparison.OrdinalIgnoreCase)
                ? "تم إرسال الملصق عبر تعريف Windows."
                : $"تم إرسال ملصق {protocol} (Layout) للطابعة.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"فشل إرسال الملصق: {ex.Message}";
        }
        IsBusy = false; BusyMessage = "";
    }

    async Task PreviewStickerZplAsync()
    {
        if (!_currentCertificateId.HasValue)
        {
            StatusMessage = "Select a certificate before preview.";
            return;
        }
        if (IsAutoUploadEnabled)
            await EnsureVerifyUrlBeforeDocumentAsync();
        IsBusy = true; BusyMessage = "جارٍ إنشاء معاينة الملصق...";
        var reportSettings = await Task.Run(() => new ReportDesignerSettingsStore().Load());
        var companyName = reportSettings.CompanyName ?? "";
        var companyHeader = reportSettings.CompanyHeader ?? "";
        var companyPhone = reportSettings.CompanyPhone ?? "";
        var companyAddress = reportSettings.CompanyAddress ?? "";

        var payload = new CertificateBarcodeInfo
        {
            CompanyName = companyName,
            DeviceBrand = Brand,
            DeviceModel = Model,
            DeviceSerialNumber = SerialText,
            CertificateNumber = CertificateNumber,
            IssueDate = IssueDate,
            ExpiryDate = ExpiryDate
        }.ToPayload();

        var settings = await Task.Run(() => new PrinterSettingsStore().Load());
        var protocol = settings.Protocol ?? "ZPL";

        try
        {
            {
                var vm = new StickerDesignerViewModel();
                // Data is already loaded in VM constructor via LoadLayout()
                
                vm.CompanyName = companyName;
                vm.CompanyHeader = companyHeader;
                vm.CompanyAddress = companyAddress;
                vm.CompanyPhone = companyPhone;
                vm.Brand = Brand ?? "";
                vm.Model = Model ?? "";
                vm.Serial = SerialText ?? "";
                vm.CertificateNumber = CertificateNumber ?? "";
                vm.CalDate = IssueDate;
                vm.ExpDate = ExpiryDate;

                // Ø¥Ø¶Ø§ÙØ© Ø³Ø¬Ù„Ø§Øª Ø§Ù„ØªØµØ­ÙŠØ­ Ù‡Ù†Ø§
                Debug.WriteLine($"[CertificatesViewModel] Passing to StickerDesignerViewModel:");
                Debug.WriteLine($"  CompanyName: {vm.CompanyName}");
                Debug.WriteLine($"  CompanyHeader: {vm.CompanyHeader}");
                Debug.WriteLine($"  CompanyAddress: {vm.CompanyAddress}");
                Debug.WriteLine($"  CompanyPhone: {vm.CompanyPhone}");
                Debug.WriteLine($"  Brand: {vm.Brand}");
                Debug.WriteLine($"  Model: {vm.Model}");
                Debug.WriteLine($"  Serial: {vm.Serial}");
                Debug.WriteLine($"  CertificateNumber: {vm.CertificateNumber}");
                Debug.WriteLine($"  CalDate: {vm.CalDate}");
                Debug.WriteLine($"  ExpDate: {vm.ExpDate}");
                
                // Final force update to ensure all items are updated correctly
                vm.UpdateItemText("company", vm.CompanyName);
                vm.UpdateItemText("header", vm.CompanyHeader);
                vm.UpdateItemText("address", vm.CompanyAddress);
                vm.UpdateItemText("phone", vm.CompanyPhone);
                vm.UpdateItemText("brand_value", vm.Brand);
                vm.UpdateItemText("model_value", vm.Model);
                vm.UpdateItemText("serial_value", vm.Serial);
                vm.UpdateItemText("cert_value", vm.CertificateNumber);
                vm.UpdateItemText("cal_value", vm.CalDate.ToString("dd-MM-yyyy"));
                vm.UpdateItemText("exp_value", vm.ExpDate.ToString("dd-MM-yyyy"));
                
                vm.ApplyVariableBindings();
                vm.UpdateQrContent();
                
                // Ensure Header and Company items are visible in preview
                vm.UpdateItemVisibility("header", true);
                vm.UpdateItemVisibility("company", !string.IsNullOrEmpty(vm.CompanyName));

                var previewWindow = new BlueMax.Presentation.Wpf.Views.StickerPreviewWindow(vm)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                previewWindow.ShowDialog();
                StatusMessage = "تم فتح معاينة الاستيكر وفق نموذج التصميم الحقيقي.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"فشل المعاينة: {ex.Message}";
        }
        IsBusy = false; BusyMessage = "";
    }

    void ArrangeLayoutBoxes()
    {
        if (LayoutBoxes.Count == 0)
            return;
        var leftX = 80d;
        var rightX = LayoutCanvasWidth / 2 + 80d;
        var startY = 120d;
        var stepY = 120d;
        var y = startY;
        for (int i = 0; i < LayoutBoxes.Count; i++)
        {
            var box = LayoutBoxes[i];
            if (i % 2 == 0)
            {
                box.X = leftX;
                box.Y = y;
            }
            else
            {
                box.X = rightX;
                box.Y = y;
                y += stepY;
            }
        }
        StatusMessage = "تم توزيع الحقول تلقائيًا.";
    }
    void AddLayoutBox(object? parameter)
    {
        var key = parameter?.ToString() ?? "";
        if (LayoutBoxes.Any(b => string.Equals(b.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            var f = FieldItems.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (f != null) f.InCanvas = true;
            return;
        }
        var title = key switch
        {
            "client_name" => "اسم العميل",
            "date" => "التاريخ",
            "expiry_date" => "تاريخ الانتهاء",
            "cert_no" => "رقم الشهادة",
            "work_no" => "أمر العمل",
            "report_type" => "نوع التقرير",
            "phone" => "الهاتف",
            "device_type" => "نوع الجهاز",
            "brand" => "الماركة",
            "model" => "الموديل",
            "serial" => "السيريال",
            "table_start" => "بداية الجدول",
            _ => "حقل"
        };

        LayoutBoxes.Add(new LayoutBoxItem
        {
            Key = key,
            Title = title,
            X = 40 + (LayoutBoxes.Count * 10),
            Y = 40 + (LayoutBoxes.Count * 10)
        });
        var fi = FieldItems.FirstOrDefault(x => x.Key == key);
        if (fi != null)
        {
            fi.InCanvas = true;
            fi.IsVisible = true;
        }
    }

    void RemoveLayoutBox(object? parameter)
    {
        if (parameter is not LayoutBoxItem item)
            return;
        var key = item.Key ?? "";
        LayoutBoxes.Remove(item);
        var fi = FieldItems.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (fi != null)
        {
            fi.InCanvas = false;
        }
        StatusMessage = "تمت إزالة الحقل من التصميم.";
    }

    void ResetLayout()
    {
        LayoutBoxes.Clear();
        foreach (var f in FieldItems)
            f.InCanvas = false;
        StatusMessage = "تمت إعادة ضبط التخطيط.";
    }

    public class CertificateBarcodeInfo
    {
        public string CompanyName { get; set; } = "";
        public string DeviceBrand { get; set; } = "";
        public string DeviceModel { get; set; } = "";
        public string DeviceSerialNumber { get; set; } = "";
        public string CertificateNumber { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public string ToPayload()
        {
            return $"{DeviceBrand},{DeviceModel},{DeviceSerialNumber},{IssueDate:dd-MM-yyyy},{ExpiryDate:dd-MM-yyyy}";
        }
    }

    void FieldItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FieldItem fi)
            return;
        if (e.PropertyName == nameof(FieldItem.IsVisible))
        {
            if (!fi.IsVisible)
            {
                var toRemove = LayoutBoxes.Where(b => string.Equals(b.Key, fi.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var r in toRemove)
                    LayoutBoxes.Remove(r);
                fi.InCanvas = false;
            }
        }
    }

    bool IsFieldVisible(string key)
    {
        var fi = FieldItems.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        return fi?.IsVisible ?? true;
    }

    void ApplyCalibrationTemplate(string deviceType)
    {
        var version = ++_applyTemplateVersion;
        var dt = deviceType ?? "";
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (!_templates.TryGetValue(dt, out var template))
                    template = _templates.Values.FirstOrDefault() ?? new CalibrationTemplate(0.5, new List<CalibrationTemplateRow>());

                var store = new CalibrationDefaultsStore();
                var lastRows = store.LoadRows(dt);
                var lastMap = lastRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Item) && r.MeasuredValue.HasValue)
                    .ToDictionary(r => r.Item!, r => (object)r.MeasuredValue!.Value);
                var rnd = new Random();
                var rows = new List<CalibrationRow>();
                foreach (var row in template.Rows)
                {
                    var cal = new CalibrationRow(row.Item, row.StandardValue, template.Tolerance);
                    if (row.StandardValue.HasValue)
                    {
                        if (lastMap.TryGetValue(row.Item, out var saved))
                        {
                            if (saved is decimal decimalValue)
                                cal.MeasuredValue = (double)decimalValue;
                        }
                        else
                        {
                            var jitter = (rnd.NextDouble() - 0.5) * template.Tolerance * 0.2;
                            cal.MeasuredValue = row.StandardValue.Value + jitter;
                        }
                    }
                    rows.Add(cal);
                }
                return rows;
            }
            catch
            {
                return new List<CalibrationRow>();
            }
        }).ContinueWith(async t =>
        {
            if (version != _applyTemplateVersion)
                return;
            if (!t.IsCompletedSuccessfully)
                return;
            var rows = await t ?? new List<CalibrationRow>();
            try
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    CalibrationRows.Clear();
                    foreach (var r in rows)
                        CalibrationRows.Add(r);
                });
            }
            catch
            {
            }
        });
    }

    Dictionary<string, CalibrationTemplate> BuildCalibrationTemplates()
    {
        return new Dictionary<string, CalibrationTemplate>
        {
            ["Total Station"] = new CalibrationTemplate(0.5, new List<CalibrationTemplateRow>
            {
                new("Horizontal Angle", 0),
                new("Vertical Angle", 0),
                new("Distance Check", 30),
                new("Compensator", 0)
            }),
            ["Auto Level"] = new CalibrationTemplate(0.3, new List<CalibrationTemplateRow>
            {
                new("Line of Sight", 0),
                new("Bubble Level", 0),
                new("Horizontal Circle", 0)
            }),
            ["GPS"] = new CalibrationTemplate(1.0, new List<CalibrationTemplateRow>
            {
                new("Baseline", 50),
                new("Height", 0),
                new("RTK Fix", 0)
            })
        };
    }

    string BuildCalibrationPayload()
    {
        var payload = new CalibrationPayload
        {
            DeviceType = DeviceType,
            Rows = CalibrationRows.Select(r => new CalibrationPayloadRow
            {
                Item = r.Item,
                StandardValue = r.StandardValue,
                MeasuredValue = r.MeasuredValue,
                Error = r.Error,
                Result = r.Result
            }).ToList()
        };
        return JsonSerializer.Serialize(payload);
    }

    void LoadCalibrationPayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            ApplyCalibrationTemplate(DeviceType);
            return;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<CalibrationPayload>(payloadJson);
            if (payload?.Rows == null || payload.Rows.Count == 0)
            {
                ApplyCalibrationTemplate(DeviceType);
                return;
            }

            CalibrationRows.Clear();
            var templateTolerance = _templates.TryGetValue(DeviceType, out var template) ? template.Tolerance : 0.5;
            foreach (var row in payload.Rows)
            {
                var cal = new CalibrationRow(row.Item, row.StandardValue, templateTolerance)
                {
                    MeasuredValue = row.MeasuredValue
                };
                CalibrationRows.Add(cal);
            }
        }
        catch
        {
            ApplyCalibrationTemplate(DeviceType);
        }
    }

    static AppDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext();
    }

    async Task LoadClientOptionsAsync(string? filter = null)
    {
        try
        {
            using var db = CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            var query = db.Customers.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim();
                query = query.Where(c => c.Name.Contains(f));
            }
            var names = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .Select(c => c.Name)
                .ToListAsync();
            ClientOptions.Clear();
            foreach (var n in names.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)))
                ClientOptions.Add(n);
        }
        catch
        {
        }
    }

    async Task DebouncedLoadClientOptionsAsync(string filter)
    {
        try
        {
            _clientOptionsCts?.Cancel();
            _clientOptionsCts?.Dispose();
            _clientOptionsCts = new CancellationTokenSource();
            var ct = _clientOptionsCts.Token;

            var normalized = (filter ?? "").Trim();
            if (normalized.Length < 2)
            {
                ClientOptions.Clear();
                IsClientOptionsOpen = false;
                return;
            }

            await Task.Delay(SearchDelayMs, ct).ConfigureAwait(false);
            await EnsureDbCreatedOnceAsync().ConfigureAwait(false);

            var names = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var db = CreateDbContext();
                var f = normalized;
                return db.Customers.AsNoTracking()
                    .Where(c => c.Name.Contains(f))
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => c.Name)
                    .Take(50)
                    .ToList();
            }, ct).ConfigureAwait(false);

            if (ct.IsCancellationRequested)
                return;
            if (!string.Equals((ClientName ?? "").Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                return;

            var newItems = names.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            
            // Only show if we have items
            IsClientOptionsOpen = newItems.Count > 0;

            // Smarter update to avoid UI flicker (sync list instead of clear/add)
            for (int i = ClientOptions.Count - 1; i >= 0; i--)
            {
                if (!newItems.Contains(ClientOptions[i]))
                    ClientOptions.RemoveAt(i);
            }

            for (int i = 0; i < newItems.Count; i++)
            {
                var item = newItems[i];
                var oldIndex = ClientOptions.IndexOf(item);

                if (oldIndex < 0)
                    ClientOptions.Insert(i, item);
                else if (oldIndex != i)
                    ClientOptions.Move(oldIndex, i);
            }
        }
        catch
        {
        }
    }

    async Task DebouncedAutoFillClientAsync(string clientName)
    {
        try
        {
            if (_loadScope > 0 || _isEditMode)
                return;

            _clientAutoFillCts?.Cancel();
            _clientAutoFillCts?.Dispose();
            _clientAutoFillCts = new CancellationTokenSource();
            var ct = _clientAutoFillCts.Token;

            var name = (clientName ?? "").Trim();
            if (name.Length < 2)
                return;

            await Task.Delay(AutoFillDelayMs, ct);
            await EnsureDbCreatedOnceAsync();

            var last = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var db = CreateDbContext();
                return db.Certificates.AsNoTracking()
                    .Where(c => c.ClientName == name)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new { c.Phone, c.DelegateName })
                    .FirstOrDefault();
            }, ct);

            if (ct.IsCancellationRequested || last == null)
                return;

            BeginLoad();
            try
            {
                if (string.IsNullOrWhiteSpace(Phone))
                    Phone = last.Phone ?? "";
                if (string.IsNullOrWhiteSpace(DelegateName))
                    DelegateName = last.DelegateName ?? "";
            }
            finally
            {
                EndLoad();
            }
        }
        catch
        {
        }
    }

    async Task DebouncedAutoFillDeviceDefaultsAsync(string clientName, string deviceType)
    {
        try
        {
            if (_loadScope > 0 || _isEditMode)
                return;

            _deviceDefaultsCts?.Cancel();
            _deviceDefaultsCts?.Dispose();
            _deviceDefaultsCts = new CancellationTokenSource();
            var ct = _deviceDefaultsCts.Token;

            var name = (clientName ?? "").Trim();
            var dt = (deviceType ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(dt))
                return;

            await Task.Delay(SearchDelayMs, ct);
            await EnsureDbCreatedOnceAsync();

            var last = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var db = CreateDbContext();
                return db.Certificates.AsNoTracking()
                    .Where(c => c.ClientName == name && c.DeviceType == dt)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new { c.Brand, c.Model, c.SpecValue })
                    .FirstOrDefault();
            }, ct);

            if (ct.IsCancellationRequested || last == null)
                return;

            BeginLoad();
            try
            {
                if (string.IsNullOrWhiteSpace(Brand))
                    Brand = last.Brand ?? "";
                if (string.IsNullOrWhiteSpace(Model))
                    Model = last.Model ?? "";

                if (string.Equals(dt, "Total Station", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(Accuracy))
                        Accuracy = last.SpecValue ?? "";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(SpecValue))
                        SpecValue = last.SpecValue ?? "";
                }
            }
            finally
            {
                EndLoad();
            }
        }
        catch
        {
        }
    }

    Task EnsureDbCreatedOnceAsync()
    {
        if (_ensureDbCreatedTask != null)
            return _ensureDbCreatedTask;
        _ensureDbCreatedTask = Task.Run(() =>
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
        });
        return _ensureDbCreatedTask;
    }

    void RefreshTemplateHubTags()
    {
        TemplateHubTags.Clear();
        var key = SelectedTemplateHubDocumentType?.Key ?? "";
        if (string.IsNullOrWhiteSpace(key))
            return;
        if (_templateHubTagsByDocumentType.TryGetValue(key, out var tags))
        {
            foreach (var t in tags)
                TemplateHubTags.Add(t);
        }
        
        // Debug logging
        try
        {
            var logDir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Logs");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "template_tags.log");
            var logLines = new List<string>
            {
                $"=== Template Tags Refresh ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===",
                $"Selected Key: {key}",
                $"Tags Count: {tags?.Count ?? 0}",
                "---"
            };
            if (tags != null)
            {
                foreach (var t in tags)
                {
                    logLines.Add($"{t.Description} = {t.Tag}");
                }
            }
            System.IO.File.AppendAllLines(logPath, logLines);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    static Dictionary<string, List<TemplateHubTagItem>> BuildTemplateHubTagsDictionary()
    {
        var certificateTags = new List<TemplateHubTagItem>
        {
            new("Client Name", "{{ client_name }}"),
            new("Phone", "{{ phone }}"),
            new("Device Type", "{{ device_type }}"),
            new("Brand", "{{ brand }}"),
            new("Model", "{{ model }}"),
            new("Serial", "{{ sn }}"),
            new("Serial (alias)", "{{ serial }}"),
            new("Serial 2", "{{ sn2 }}"),
            new("Serial 2 (alias)", "{{ serial2 }}"),
            new("Spec Value", "{{ spec_value }}"),
            new("Certificate No", "{{ cert_no }}"),
            new("Issue Date", "{{ issue_date }}"),
            new("Date", "{{ date }}"),
            new("Expiry Date", "{{ expiry_date }}"),
            new("Valid Until", "{{ valid_until }}"),
            new("Delegate Name", "{{ delegate_name }}"),
            new("QR / Barcode Image", "{{ qr_code_img }}"),
            new("Company Logo Image", "{{ company_logo_img }}")
        };

        var workOrderTags = new List<TemplateHubTagItem>
        {
            new("Customer Name", "{{ customer_name }}"),
            new("Phone", "{{ phone }}"),
            new("Device Type", "{{ device_type }}"),
            new("Model", "{{ model }}"),
            new("Serial", "{{ serial }}"),
            new("Accessories", "{{ accessories }}"),
            new("Complaint", "{{ complaint }}"),
            new("Technical Report", "{{ technical_report }}"),
            new("Status", "{{ status }}"),
            new("Parts Cost", "{{ parts_cost }}"),
            new("Labor Cost", "{{ labor_cost }}"),
            new("Total Cost", "{{ total_cost }}"),
            new("Received Date", "{{ received_date }}"),
            new("Updated Date", "{{ updated_date }}"),
            new("Work Order No", "{{ work_order_no }}"),
            new("Report Type", "{{ report_type }}"),
            new("QR / Barcode Image", "{{ qr_code_img }}"),
            new("Company Logo Image", "{{ company_logo_img }}")
        };

        var rentalReceiptTags = new List<TemplateHubTagItem>
        {
            new("Customer Name", "{{ customer_name }}"),
            new("Client Name", "{{ client_name }}"),
            new("Company", "{{ company }}"),
            new("Phone", "{{ phone }}"),
            new("Tax Number", "{{ tax_number }}"),
            new("ID Number", "{{ id_number }}"),
            new("Device Type", "{{ device_type }}"),
            new("Brand", "{{ brand }}"),
            new("Model", "{{ model }}"),
            new("Serial", "{{ serial }}"),
            new("Serial Number", "{{ serial_number }}"),
            new("Serial 2", "{{ serial2 }}"),
            new("Serial Number 2", "{{ serial_number_2 }}"),
            new("Start Date", "{{ start_date }}"),
            new("End Date", "{{ end_date }}"),
            new("Rental Type", "{{ rental_type }}"),
            new("Price", "{{ price }}"),
            new("Paid Amount", "{{ paid_amount }}"),
            new("Remaining Amount", "{{ remaining_amount }}"),
            new("Status", "{{ status }}"),
            new("Notes", "{{ notes }}"),
            new("Rental Number", "{{ rental_number }}"),
            new("Receipt Number", "{{ receipt_number }}"),
            new("Rental No", "{{ rental_no }}"),
            new("Issue Date", "{{ issue_date }}"),
            new("Created Date", "{{ created_date }}"),
            new("Date", "{{ date }}"),
            new("QR / Barcode Image", "{{ qr_code_img }}"),
            new("Company Logo Image", "{{ company_logo_img }}")
        };

        return new Dictionary<string, List<TemplateHubTagItem>>(StringComparer.OrdinalIgnoreCase)
        {
            ["cert_total_station"] = certificateTags,
            ["cert_gps"] = certificateTags,
            ["cert_auto_level"] = certificateTags,
            ["price_quotation"] = workOrderTags,
            ["invoice"] = workOrderTags,
            ["maintenance_report"] = workOrderTags,
            ["rental_receipt"] = rentalReceiptTags
        };
    }

    void CopyTemplateHubTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
        try
        {
            Clipboard.SetText(tag);
            StatusMessage = $"Copied: {tag}";
        }
        catch
        {
            StatusMessage = "Copy failed.";
        }
    }

    void UploadWordTemplateHub()
    {
        var docType = SelectedTemplateHubDocumentType;
        if (docType == null)
        {
            StatusMessage = "Select a Document Type first.";
            return;
        }

        var ofd = new OpenFileDialog
        {
            Filter = "Word Template (*.docx)|*.docx",
            Multiselect = false
        };
        if (ofd.ShowDialog() != true)
            return;

        SelectedTemplateHubPath = ofd.FileName;
        StatusMessage = $"Template selected for {docType.Name}: {Path.GetFileName(ofd.FileName)}";
    }

    void SaveTemplateHubConfiguration()
    {
        var docType = SelectedTemplateHubDocumentType;
        if (docType == null)
        {
            StatusMessage = "Select a Document Type first.";
            return;
        }

        var path = (SelectedTemplateHubPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = "Upload a Word template first.";
            return;
        }
        new DocumentPathMappingStore().SetPath(docType.Key, path);
        SelectedTemplateHubPath = path;
        StatusMessage = $"Configuration saved for {docType.Name}.";
    }

    async Task LoadSelectedTemplateHubPathAsync()
    {
        try
        {
            var key = SelectedTemplateHubDocumentType?.Key ?? "";
            if (string.IsNullOrWhiteSpace(key))
            {
                SelectedTemplateHubPath = "";
                return;
            }

            var path = await LoadTemplatePathAsync(key);
            SelectedTemplateHubPath = path ?? "";
        }
        catch
        {
        }
    }

    static string BuildCertificateNumber(Certificate certificate)
    {
        var date = certificate.IssueDate == default ? DateTime.Today : certificate.IssueDate;
        var yymm = date.ToString("yyMM");
        return $"HAT-{yymm}-{certificate.Id:0000}";
    }

    static string GetVerifyUrlMapPath()
    {
        var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
        System.IO.Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir, "verify_urls.json");
    }

    static void SaveVerifyUrlLocal(string certificateNumber, string url)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber) || string.IsNullOrWhiteSpace(url)) return;
        var path = GetVerifyUrlMapPath();
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (existing != null)
                    dict = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch { }
        dict[certificateNumber] = url;
        try
        {
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(dict));
        }
        catch { }
    }

    static string LoadVerifyUrlLocal(string certificateNumber)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber)) return "";
        var path = GetVerifyUrlMapPath();
        try
        {
            if (!System.IO.File.Exists(path)) return "";
            var json = System.IO.File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null && dict.TryGetValue(certificateNumber, out var url))
                return url ?? "";
        }
        catch { }
        return "";
    }

    void CopyVerificationUrl()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(LastVerificationUrl))
                Clipboard.SetText(LastVerificationUrl);
            StatusMessage = "تم نسخ رابط التحقق.";
        }
        catch { }
    }

    void OpenVerificationUrl()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(LastVerificationUrl))
                Process.Start(new ProcessStartInfo { FileName = LastVerificationUrl, UseShellExecute = true });
        }
        catch { }
    }
}

public sealed class LayoutBoxItem : ViewModelBase
{
    string _key = "";
    string _title = "";
    double _x;
    double _y;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }
}

public sealed class FieldItem : ViewModelBase
{
    string _key = "";
    string _title = "";
    bool _isVisible = true;
    bool _inCanvas;

    public FieldItem(string key, string title)
    {
        _key = key;
        _title = title;
    }

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool InCanvas
    {
        get => _inCanvas;
        set => SetProperty(ref _inCanvas, value);
    }
}

public sealed class CertificateRow : ViewModelBase
{
    int _id;
    string _certificateNumber = "";
    string _clientName = "";
    string _phone = "";
    string _deviceType = "";
    string _brand = "";
    string _model = "";
    string _serialText = "";
    string _serialText2 = "";
    string _specValue = "";
    DateTime _issueDate;
    DateTime _expiryDate;
    string _delegateName = "";
    string _payloadEnc = "";

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string CertificateNumber
    {
        get => _certificateNumber;
        set => SetProperty(ref _certificateNumber, value);
    }

    public string ClientName
    {
        get => _clientName;
        set => SetProperty(ref _clientName, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string DeviceType
    {
        get => _deviceType;
        set => SetProperty(ref _deviceType, value);
    }

    public string Brand
    {
        get => _brand;
        set => SetProperty(ref _brand, value);
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public string SerialText
    {
        get => _serialText;
        set => SetProperty(ref _serialText, value);
    }

    public string SerialText2
    {
        get => _serialText2;
        set => SetProperty(ref _serialText2, value);
    }

    public string SpecValue
    {
        get => _specValue;
        set => SetProperty(ref _specValue, value);
    }

    public DateTime IssueDate
    {
        get => _issueDate;
        set => SetProperty(ref _issueDate, value);
    }

    public DateTime ExpiryDate
    {
        get => _expiryDate;
        set => SetProperty(ref _expiryDate, value);
    }

    public string DelegateName
    {
        get => _delegateName;
        set => SetProperty(ref _delegateName, value);
    }

    public string PayloadEnc
    {
        get => _payloadEnc;
        set => SetProperty(ref _payloadEnc, value);
    }

    public string IssueDateText => IssueDate == default ? "" : IssueDate.ToString("yyyy-MM-dd");
}

public sealed class TemplateHubDocumentType
{
    public TemplateHubDocumentType(string key, string name)
    {
        Key = key;
        Name = name;
    }

    public string Key { get; }
    public string Name { get; }
}

public sealed class TemplateHubTagItem
{
    public TemplateHubTagItem(string description, string tag)
    {
        Description = description;
        Tag = tag;
    }

    public string Description { get; }
    public string Tag { get; }
}

public sealed class CalibrationRow : ViewModelBase
{
    string _item;
    double? _standardValue;
    double? _measuredValue;
    double? _error;
    string _result = "Pass";
    double _tolerance;

    public CalibrationRow(string item, double? standardValue, double tolerance)
    {
        _item = item;
        _standardValue = standardValue;
        _tolerance = tolerance;
        Recalculate();
    }

    public string Item
    {
        get => _item;
        set
        {
            if (SetProperty(ref _item, value))
                Recalculate();
        }
    }

    public double? StandardValue
    {
        get => _standardValue;
        set
        {
            if (SetProperty(ref _standardValue, value))
                Recalculate();
        }
    }

    public double? MeasuredValue
    {
        get => _measuredValue;
        set
        {
            double? v = value;
            if (v.HasValue)
                v = Math.Round(v.Value, 2);
            if (SetProperty(ref _measuredValue, v))
                Recalculate();
        }
    }

    public double? Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    public string Result
    {
        get => _result;
        private set => SetProperty(ref _result, value);
    }

    void Recalculate()
    {
        if (_standardValue.HasValue && _measuredValue.HasValue)
        {
            var err = Math.Round(_measuredValue.Value - _standardValue.Value, 2);
            Error = err;
            Result = Math.Abs(err) > _tolerance ? "Fail" : "Pass";
        }
        else
        {
            Error = null;
            Result = "Pass";
        }
    }
}

public sealed class CalibrationTemplate
{
    public CalibrationTemplate(double tolerance, List<CalibrationTemplateRow> rows)
    {
        Tolerance = tolerance;
        Rows = rows;
    }

    public double Tolerance { get; }
    public List<CalibrationTemplateRow> Rows { get; }
}

public sealed class CalibrationTemplateRow
{
    public CalibrationTemplateRow(string item, double? standardValue)
    {
        Item = item;
        StandardValue = standardValue;
    }

    public string Item { get; }
    public double? StandardValue { get; }
}

public sealed class CalibrationPayload
{
    public string DeviceType { get; set; } = "";
    public List<CalibrationPayloadRow> Rows { get; set; } = new();
}

public sealed class CalibrationPayloadRow
{
    public string Item { get; set; } = "";
    public double? StandardValue { get; set; }
    public double? MeasuredValue { get; set; }
    public double? Error { get; set; }
    public string Result { get; set; } = "";
}
