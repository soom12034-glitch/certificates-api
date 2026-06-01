using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using BlueMax.Presentation.Wpf.ViewModels;
using BlueMax.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueMax.Presentation.Wpf.Views;

public partial class RentalReceiptWindow : Window
{
    public RentalReceiptWindow(RentalReceiptData data)
    {
        InitializeComponent();
        var vm = new RentalReceiptViewModel(data);
        DataContext = vm;
        Loaded += (_, __) => vm.RefreshPreviewCommand.Execute(null);
    }

    sealed class RentalReceiptViewModel : ViewModelBase
    {
        const string ReceiptPrinterNameKey = "RentalReceiptPrinterName";
        const string ReceiptOffsetYKey = "RentalReceiptOffsetY";
        const string ReceiptOffsetXKey = "RentalReceiptOffsetX";
        const string ReceiptTopMarginKey = "RentalReceiptTopMargin";
        const string ReceiptBottomMarginKey = "RentalReceiptBottomMargin";
        const string ReceiptIsLetterheadKey = "RentalReceiptIsLetterhead";
        const string ReceiptFrameColorKey = "RentalReceiptFrameColor";

        readonly RentalReceiptData _data;
        readonly ObservableCollection<string> _installedPrinters = new();
        string _selectedPrinterName = "";
        string _selectedFrameColor = "رمادي داكن";
        IDocumentPaginatorSource? _previewDocument;
        double _contentOffsetY = 0.0;
        double _contentOffsetX = 0.0;
        double _topMargin = 90.0;
        double _bottomMargin = 80.0;
        bool _isLetterhead = false;
        string? _generatedPdfPath;

        public RentalReceiptViewModel(RentalReceiptData data)
        {
            _data = data;
            LoadInstalledPrinters();
            LoadSavedPrinterName();
            LoadLayoutSettings();

            RefreshPreviewCommand = new RelayCommand(_ => RefreshPreview());
            ChoosePrinterCommand = new RelayCommand(_ => ChoosePrinter());
            PrintCommand = new RelayCommand(_ => Print());
        }

        public ObservableCollection<string> InstalledPrinters => _installedPrinters;

        public string SelectedPrinterName
        {
            get => _selectedPrinterName;
            set
            {
                if (!SetProperty(ref _selectedPrinterName, value ?? ""))
                    return;
                SaveAppSetting(ReceiptPrinterNameKey, _selectedPrinterName);
            }
        }

        public IDocumentPaginatorSource? PreviewDocument
        {
            get => _previewDocument;
            private set => SetProperty(ref _previewDocument, value);
        }

        public bool IsLetterhead
        {
            get => _isLetterhead;
            set
            {
                if (!SetProperty(ref _isLetterhead, value))
                    return;
                SaveAppSetting(ReceiptIsLetterheadKey, _isLetterhead.ToString());
                RefreshPreview();
            }
        }

        public double ContentOffsetY
        {
            get => _contentOffsetY;
            set
            {
                if (!SetProperty(ref _contentOffsetY, Clamp(value, -80, 80)))
                    return;
                SaveAppSetting(ReceiptOffsetYKey, _contentOffsetY.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public double TopMargin
        {
            get => _topMargin;
            set
            {
                if (!SetProperty(ref _topMargin, Clamp(value, 0, 200)))
                    return;
                SaveAppSetting(ReceiptTopMarginKey, _topMargin.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public double BottomMargin
        {
            get => _bottomMargin;
            set
            {
                if (!SetProperty(ref _bottomMargin, Clamp(value, 0, 200)))
                    return;
                SaveAppSetting(ReceiptBottomMarginKey, _bottomMargin.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public RelayCommand RefreshPreviewCommand { get; }
        public RelayCommand ChoosePrinterCommand { get; }
        public RelayCommand PrintCommand { get; }

        public ObservableCollection<string> FrameColors { get; } = new ObservableCollection<string> { "رمادي داكن", "أسود", "أزرق كحلي", "أزرق فاتح", "بدون برواز" };

        public string SelectedFrameColor
        {
            get => _selectedFrameColor;
            set
            {
                if (!SetProperty(ref _selectedFrameColor, value))
                    return;
                SaveAppSetting(ReceiptFrameColorKey, value ?? "");
                RefreshPreview();
            }
        }

        public double ContentOffsetX
        {
            get => _contentOffsetX;
            set
            {
                if (!SetProperty(ref _contentOffsetX, Clamp(value, -80, 80)))
                    return;
                SaveAppSetting(ReceiptOffsetXKey, _contentOffsetX.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        void LoadInstalledPrinters()
        {
            _installedPrinters.Clear();
            try
            {
                foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                    _installedPrinters.Add(name);
            }
            catch
            {
            }
        }

        void LoadSavedPrinterName()
        {
            try
            {
                var saved = ConfigurationManager.AppSettings[ReceiptPrinterNameKey] ?? "";
                if (!string.IsNullOrWhiteSpace(saved) && _installedPrinters.Contains(saved))
                    SelectedPrinterName = saved;
            }
            catch
            {
            }
        }

        void LoadLayoutSettings()
        {
            try
            {
                var offsetRaw = ConfigurationManager.AppSettings[ReceiptOffsetYKey];
                if (double.TryParse(offsetRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var offset))
                    _contentOffsetY = Clamp(offset, -80, 80);
                var offsetXRaw = ConfigurationManager.AppSettings[ReceiptOffsetXKey];
                if (double.TryParse(offsetXRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var offsetX))
                    _contentOffsetX = Clamp(offsetX, -80, 80);
                var topMarginRaw = ConfigurationManager.AppSettings[ReceiptTopMarginKey];
                if (double.TryParse(topMarginRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var topMargin))
                    _topMargin = Clamp(topMargin, 0, 200);
                var bottomMarginRaw = ConfigurationManager.AppSettings[ReceiptBottomMarginKey];
                if (double.TryParse(bottomMarginRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bottomMargin))
                    _bottomMargin = Clamp(bottomMargin, 0, 200);
                var isLetterheadRaw = ConfigurationManager.AppSettings[ReceiptIsLetterheadKey];
                if (bool.TryParse(isLetterheadRaw, out var isLetterhead))
                    _isLetterhead = isLetterhead;
                var colorRaw = ConfigurationManager.AppSettings[ReceiptFrameColorKey];
                if (!string.IsNullOrWhiteSpace(colorRaw) && FrameColors.Contains(colorRaw))
                    _selectedFrameColor = colorRaw;
            }
            catch
            {
            }
        }

        async void RefreshPreview()
        {
            try
            {
                var templatePath = await LoadTemplatePathAsync("rental_receipt");
                
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    MessageBox.Show(
                        "يرجى تحديد قالب Word لإيصال الإيجار (rental_receipt) من شاشة إدارة القوالب أولاً",
                        "Missing template",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var outputDir = Path.Combine(AppContext.BaseDirectory, "Reports_Output");
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                // Build token data directly from _data (no database needed)
                var data = new Dictionary<string, object>
                {
                    ["RentalNumber"] = _data.ReceiptNumber,
                    ["rental_number"] = _data.ReceiptNumber,
                    ["ReceiptNumber"] = _data.ReceiptNumber,
                    ["receipt_number"] = _data.ReceiptNumber,

                    ["CustomerName"] = _data.CustomerName,
                    ["customer_name"] = _data.CustomerName,
                    ["ClientName"] = _data.CustomerName,
                    ["client_name"] = _data.CustomerName,

                    ["Company"] = _data.Company ?? "",
                    ["company"] = _data.Company ?? "",

                    ["Phone"] = _data.Phone ?? "",
                    ["phone"] = _data.Phone ?? "",

                    ["DeviceType"] = _data.DeviceType,
                    ["device_type"] = _data.DeviceType,

                    ["Brand"] = _data.Brand ?? "",
                    ["brand"] = _data.Brand ?? "",

                    ["Model"] = _data.Model ?? "",
                    ["model"] = _data.Model ?? "",

                    ["Serial"] = _data.Serial,
                    ["serial"] = _data.Serial,
                    ["SerialNumber"] = _data.Serial,
                    ["serial_number"] = _data.Serial,

                    ["Serial2"] = _data.Serial2 ?? "",
                    ["serial2"] = _data.Serial2 ?? "",
                    ["SerialNumber2"] = _data.Serial2 ?? "",
                    ["serial_number_2"] = _data.Serial2 ?? "",

                    ["StartDate"] = _data.StartDate.ToString("yyyy-MM-dd"),
                    ["start_date"] = _data.StartDate.ToString("yyyy-MM-dd"),

                    ["EndDate"] = _data.EndDate.ToString("yyyy-MM-dd"),
                    ["end_date"] = _data.EndDate.ToString("yyyy-MM-dd"),

                    ["RentalType"] = _data.RentalType,
                    ["rental_type"] = _data.RentalType,

                    ["Price"] = _data.Price.ToString("0.##"),
                    ["price"] = _data.Price.ToString("0.##"),

                    ["PaidAmount"] = _data.PaidAmount.ToString("0.##"),
                    ["paid_amount"] = _data.PaidAmount.ToString("0.##"),

                    ["RemainingAmount"] = _data.RemainingAmount.ToString("0.##"),
                    ["remaining_amount"] = _data.RemainingAmount.ToString("0.##"),

                    ["Status"] = "Active",
                    ["status"] = "Active",

                    ["Notes"] = _data.Notes ?? "",
                    ["notes"] = _data.Notes ?? "",

                    ["IssueDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
                    ["issue_date"] = DateTime.Now.ToString("yyyy-MM-dd"),
                    ["created_date"] = DateTime.Now.ToString("yyyy-MM-dd"),

                    ["company_logo_img"] = "",
                    ["qr_code_img"] = ""
                };

                var engine = new WordTemplateEngine("", outputDir);
                var docxPath = await Task.Run(() => engine.GenerateDocumentFromPath(templatePath, data));
                
                _generatedPdfPath = docxPath;

                if (!string.IsNullOrWhiteSpace(docxPath) && File.Exists(docxPath))
                {
                    LoadPdfIntoViewer(docxPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء المعاينة: {ex.Message}\n\nInner: {ex.InnerException?.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        async Task<string> LoadTemplatePathAsync(string docKey)
        {
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

        void LoadPdfIntoViewer(string pdfPath)
        {
            // Simply show message with PDF path like certificates do
            MessageBox.Show($"تم إنشاء ملف PDF بنجاح في:\n{pdfPath}\n\nيمكنك فتح الملف لطباعته.", "تم الإنشاء", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void ChoosePrinter()
        {
            try
            {
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true && dialog.PrintQueue != null)
                    SelectedPrinterName = dialog.PrintQueue.FullName;
            }
            catch
            {
            }
        }

        void Print()
        {
            try
            {
                if (PreviewDocument == null)
                    RefreshPreview();
                if (PreviewDocument == null)
                    return;

                var dialog = new PrintDialog();
                try
                {
                    if (!string.IsNullOrWhiteSpace(SelectedPrinterName))
                    {
                        var server = new LocalPrintServer();
                        dialog.PrintQueue = server.GetPrintQueue(SelectedPrinterName);
                    }
                }
                catch
                {
                }

                if (dialog.ShowDialog() != true)
                    return;

                try
                {
                    if (dialog.PrintQueue != null)
                        SelectedPrinterName = dialog.PrintQueue.FullName;
                }
                catch
                {
                }

                var paginator = ((IDocumentPaginatorSource)PreviewDocument).DocumentPaginator;
                dialog.PrintDocument(paginator, "Rental Receipt");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Print failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        static string ResolveTemplatePath(string rawPath)
        {
            var path = (rawPath ?? "").Trim();
            if (string.IsNullOrWhiteSpace(path))
                return "";
            if (Path.IsPathRooted(path) && File.Exists(path))
                return path;
            var fallback = Path.Combine(AppContext.BaseDirectory, path);
            if (File.Exists(fallback))
                return fallback;
            return "";
        }

        static void SaveAppSetting(string key, string value)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] == null)
                    config.AppSettings.Settings.Add(key, value ?? "");
                else
                    config.AppSettings.Settings[key].Value = value ?? "";
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
            }
        }

        static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
