using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class MaintenanceReceiptStickerWindow : Window
{
    public MaintenanceReceiptStickerWindow(MaintenanceViewModel.MaintenanceReceiptData data)
    {
        InitializeComponent();
        var vm = new ReceiptStickerViewModel(data);
        DataContext = vm;
        Loaded += (_, __) => vm.RefreshPreviewCommand.Execute(null);
    }

    sealed class ReceiptStickerViewModel : ViewModelBase
    {
        const string StickerPrinterNameKey = "MaintenanceReceiptStickerPrinterName";
        const string StickerScaleKey = "MaintenanceReceiptStickerScale";
        const string StickerOffsetXKey = "MaintenanceReceiptStickerOffsetX";
        const string StickerOffsetYKey = "MaintenanceReceiptStickerOffsetY";
        const string StickerWidthMmKey = "MaintenanceReceiptStickerWidthMm";
        const string StickerHeightMmKey = "MaintenanceReceiptStickerHeightMm";

        readonly MaintenanceViewModel.MaintenanceReceiptData _data;
        readonly ObservableCollection<string> _installedPrinters = new();
        string _selectedPrinterName = "";
        IDocumentPaginatorSource? _previewDocument;
        double _contentScale = 1.0;
        double _contentOffsetX;
        double _contentOffsetY;
        string _stickerCustomerName = "";
        string _stickerDeviceType = "";
        string _stickerModel = "";
        string _stickerSerialNumber = "";
        DateTime _stickerReceivedDate = DateTime.Today;
        double _stickerWidthMm = 41.0;
        double _stickerHeightMm = 21.0;

        public ReceiptStickerViewModel(MaintenanceViewModel.MaintenanceReceiptData data)
        {
            _data = data;
            _stickerCustomerName = data.CustomerName ?? "";
            
            if (data.Devices != null && data.Devices.Count > 0)
            {
                var firstDevice = data.Devices[0];
                _stickerDeviceType = firstDevice.DeviceType ?? "";
                _stickerModel = firstDevice.Model ?? "";
                _stickerSerialNumber = firstDevice.SerialNumber ?? "";
            }
            else
            {
                _stickerDeviceType = "";
                _stickerModel = "";
                _stickerSerialNumber = "";
            }
            
            _stickerReceivedDate = data.ReceivedDate == default ? DateTime.Today : data.ReceivedDate;
            LoadInstalledPrinters();
            LoadSavedPrinterName();
            LoadLayoutSettings();

            RefreshPreviewCommand = new RelayCommand(_ => RefreshPreview());
            ChoosePrinterCommand = new RelayCommand(_ => ChoosePrinter());
            PrintCommand = new RelayCommand(_ => Print());
        }

        public ObservableCollection<string> InstalledPrinters => _installedPrinters;

        public string StickerCustomerName
        {
            get => _stickerCustomerName;
            set
            {
                if (!SetProperty(ref _stickerCustomerName, value ?? ""))
                    return;
                RefreshPreview();
            }
        }

        public string StickerDeviceType
        {
            get => _stickerDeviceType;
            set
            {
                if (!SetProperty(ref _stickerDeviceType, value ?? ""))
                    return;
                RefreshPreview();
            }
        }

        public string StickerModel
        {
            get => _stickerModel;
            set
            {
                if (!SetProperty(ref _stickerModel, value ?? ""))
                    return;
                RefreshPreview();
            }
        }

        public string StickerSerialNumber
        {
            get => _stickerSerialNumber;
            set
            {
                if (!SetProperty(ref _stickerSerialNumber, value ?? ""))
                    return;
                RefreshPreview();
            }
        }

        public DateTime StickerReceivedDate
        {
            get => _stickerReceivedDate;
            set
            {
                if (!SetProperty(ref _stickerReceivedDate, value))
                    return;
                RefreshPreview();
            }
        }

        public double StickerWidthMm
        {
            get => _stickerWidthMm;
            set
            {
                if (!SetProperty(ref _stickerWidthMm, Clamp(value, 20, 100)))
                    return;
                SaveAppSetting(StickerWidthMmKey, _stickerWidthMm.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public double StickerHeightMm
        {
            get => _stickerHeightMm;
            set
            {
                if (!SetProperty(ref _stickerHeightMm, Clamp(value, 10, 60)))
                    return;
                SaveAppSetting(StickerHeightMmKey, _stickerHeightMm.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public string SelectedPrinterName
        {
            get => _selectedPrinterName;
            set
            {
                if (!SetProperty(ref _selectedPrinterName, value ?? ""))
                    return;
                SaveAppSetting(StickerPrinterNameKey, _selectedPrinterName);
            }
        }

        public IDocumentPaginatorSource? PreviewDocument
        {
            get => _previewDocument;
            private set => SetProperty(ref _previewDocument, value);
        }

        public double ContentScale
        {
            get => _contentScale;
            set
            {
                if (!SetProperty(ref _contentScale, Clamp(value, 0.8, 1.3)))
                    return;
                SaveAppSetting(StickerScaleKey, _contentScale.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public double ContentOffsetX
        {
            get => _contentOffsetX;
            set
            {
                if (!SetProperty(ref _contentOffsetX, Clamp(value, -30, 30)))
                    return;
                SaveAppSetting(StickerOffsetXKey, _contentOffsetX.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public double ContentOffsetY
        {
            get => _contentOffsetY;
            set
            {
                if (!SetProperty(ref _contentOffsetY, Clamp(value, -30, 30)))
                    return;
                SaveAppSetting(StickerOffsetYKey, _contentOffsetY.ToString(System.Globalization.CultureInfo.InvariantCulture));
                RefreshPreview();
            }
        }

        public RelayCommand RefreshPreviewCommand { get; }
        public RelayCommand ChoosePrinterCommand { get; }
        public RelayCommand PrintCommand { get; }

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
                var saved = ConfigurationManager.AppSettings[StickerPrinterNameKey] ?? "";
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
                var scaleRaw = ConfigurationManager.AppSettings[StickerScaleKey];
                if (double.TryParse(scaleRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var scale))
                    _contentScale = Clamp(scale, 0.8, 1.3);

                var offsetXRaw = ConfigurationManager.AppSettings[StickerOffsetXKey];
                if (double.TryParse(offsetXRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var offsetX))
                    _contentOffsetX = Clamp(offsetX, -30, 30);

                var offsetYRaw = ConfigurationManager.AppSettings[StickerOffsetYKey];
                if (double.TryParse(offsetYRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var offsetY))
                    _contentOffsetY = Clamp(offsetY, -30, 30);

                var widthRaw = ConfigurationManager.AppSettings[StickerWidthMmKey];
                if (double.TryParse(widthRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var widthMm))
                    _stickerWidthMm = Clamp(widthMm, 20, 100);

                var heightRaw = ConfigurationManager.AppSettings[StickerHeightMmKey];
                if (double.TryParse(heightRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var heightMm))
                    _stickerHeightMm = Clamp(heightMm, 10, 60);
            }
            catch
            {
            }
        }

        void RefreshPreview()
        {
            PreviewDocument = BuildDocument();
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
                try
                {
                    var widthDip = StickerWidthMm / 25.4 * 96.0;
                    var heightDip = StickerHeightMm / 25.4 * 96.0;
                    var ticket = dialog.PrintTicket;
                    ticket.PageMediaSize = new PageMediaSize(widthDip, heightDip);
                    ticket.PageOrientation = PageOrientation.Portrait;
                    dialog.PrintTicket = ticket;
                }
                catch
                {
                }
                dialog.PrintDocument(paginator, "Maintenance Receipt Sticker");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sticker print failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        FixedDocument BuildDocument()
        {
            const double dpi = 96.0;
            var widthMm = Clamp(StickerWidthMm, 20, 100);
            var heightMm = Clamp(StickerHeightMm, 10, 60);
            var pageWidth = widthMm / 25.4 * dpi;
            var pageHeight = heightMm / 25.4 * dpi;
            var titleFont = Clamp(pageHeight * 0.09, 6, 8);
            var bodyFont = Clamp(pageHeight * 0.088, 6, 8.5);
            var smallFont = Clamp(pageHeight * 0.08, 5.8, 7.8);

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);

            var page = new FixedPage
            {
                Width = pageWidth,
                Height = pageHeight,
                FlowDirection = FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            var border = new Border
            {
                Width = Math.Max(0, pageWidth - 2),
                Height = Math.Max(0, pageHeight - 2),
                Margin = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(3),
                Background = Brushes.White
            };

            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.RenderTransform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(ContentScale, ContentScale),
                    new TranslateTransform(ContentOffsetX, ContentOffsetY)
                }
            };

            var root = new Grid { FlowDirection = FlowDirection.RightToLeft };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                Padding = new Thickness(3, 1, 3, 1),
                CornerRadius = new CornerRadius(2, 2, 0, 0)
            };

            var headerGrid = new Grid { FlowDirection = FlowDirection.RightToLeft };
            headerGrid.Children.Add(new TextBlock
            {
                Text = $"رقم: {WrapLtr(Safe(_data.ReceiptNumber))}",
                FontFamily = new FontFamily("Tahoma"),
                FontWeight = FontWeights.Bold,
                FontSize = titleFont,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });
            header.Child = headerGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var details = new StackPanel
            {
                Margin = new Thickness(6, 3, 6, 3),
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            details.Children.Add(MakeCenteredLine("الاسم: ", Safe(StickerCustomerName), bodyFont));
            details.Children.Add(MakeCenteredLine("موديل الجهاز: ", Safe(StickerModel), bodyFont));
            details.Children.Add(MakeCenteredLine("السيريال: ", Safe(StickerSerialNumber), bodyFont));
            details.Children.Add(MakeCenteredLine("التاريخ: ", StickerReceivedDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), smallFont));

            Grid.SetRow(details, 1);
            root.Children.Add(details);

            border.Child = root;
            page.Children.Add(border);

            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            doc.Pages.Add(content);

            return doc;
        }

        static string Safe(string? value)
        {
            var v = (value ?? "").Trim();
            return string.IsNullOrWhiteSpace(v) ? "-" : v;
        }

        static string WrapLtr(string value)
        {
            return "\u2066" + (value ?? "") + "\u2069";
        }

        static string FormatValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";
            return IsAsciiHeavy(value) ? WrapLtr(value) : value;
        }

        static TextBlock MakeCenteredLine(string label, string value, double fontSize)
        {
            var safeValue = Safe(value);
            return new TextBlock
            {
                Text = label + (IsAsciiHeavy(safeValue) ? WrapLtr(safeValue) : safeValue),
                FontFamily = new FontFamily("Tahoma"),
                FontSize = fontSize,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 1)
            };
        }

        static bool IsAsciiHeavy(string value)
        {
            var total = 0;
            var ascii = 0;
            foreach (var ch in value)
            {
                if (char.IsWhiteSpace(ch))
                    continue;
                total++;
                if (ch <= 0x007F)
                    ascii++;
            }
            if (total == 0)
                return false;
            return ascii * 100 / total >= 60;
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
