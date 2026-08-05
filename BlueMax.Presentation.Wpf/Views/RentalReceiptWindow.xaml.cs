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
using BlueMax.Infrastructure;

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
        readonly BlueMax.Infrastructure.ReportDesignerSettings _companySettings;
        readonly ObservableCollection<string> _installedPrinters = new();
        string _selectedPrinterName = "";
        string _selectedFrameColor = "رمادي داكن";
        IDocumentPaginatorSource? _previewDocument;
        double _contentOffsetY = 0.0;
        double _contentOffsetX = 0.0;
        double _topMargin = 90.0;
        double _bottomMargin = 80.0;
        bool _isLetterhead = false;

        public RentalReceiptViewModel(RentalReceiptData data)
        {
            _data = data;
            _companySettings = new ReportDesignerSettingsStore().Load();
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

        void RefreshPreview()
        {
            PreviewDocument = BuildDocument();
        }

        FixedDocument BuildDocument()
        {
            const double dpi = 96.0;
            const double a4Width = 8.27 * dpi;
            const double a4Height = 11.69 * dpi;
            const double pageMarginLeftRight = 40;
            double headerSpace = IsLetterhead ? TopMargin : 40;
            double footerSpace = BottomMargin;
            const double tableWidth = 600;
            const double labelColumnWidth = 140;

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(a4Width, a4Height);

            var page = new FixedPage
            {
                Width = a4Width,
                Height = a4Height,
                FlowDirection = FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            var bottomGuideline = new System.Windows.Shapes.Line
            {
                X1 = 0, Y1 = a4Height - footerSpace, X2 = a4Width, Y2 = a4Height - footerSpace,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 4 })
            };
            page.Children.Add(bottomGuideline);

            var transformContainer = new Grid
            {
                Width = a4Width,
                Height = a4Height
            };
            var transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(ContentOffsetX, ContentOffsetY));
            transformContainer.RenderTransform = transform;

            var viewbox = new Viewbox
            {
                Width = a4Width - (pageMarginLeftRight * 2),
                Height = a4Height - footerSpace - headerSpace,
                Margin = new Thickness(pageMarginLeftRight, headerSpace, pageMarginLeftRight, footerSpace),
                StretchDirection = StretchDirection.DownOnly,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Top
            };

            var outerBorder = new Border
            {
                BorderBrush = GetFrameBrush(),
                BorderThickness = new Thickness(SelectedFrameColor == "بدون برواز" ? 0 : 2),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24, 24, 24, 24),
                Background = Brushes.White
            };

            var root = new StackPanel
            {
                Width = a4Width - (pageMarginLeftRight * 2) - 48
            };

            outerBorder.Child = root;
            viewbox.Child = outerBorder;

            if (!IsLetterhead && !string.IsNullOrWhiteSpace(_companySettings.CompanyName))
            {
                var companyHeader = new TextBlock
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    FontFamily = new FontFamily("Cairo"),
                    FontSize = 26,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Text = _companySettings.CompanyName,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                root.Children.Add(companyHeader);

                if (!string.IsNullOrWhiteSpace(_companySettings.CompanyHeader))
                {
                    var companySubHeader = new TextBlock
                    {
                        FlowDirection = FlowDirection.RightToLeft,
                        FontFamily = new FontFamily("Tahoma"),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        Text = _companySettings.CompanyHeader,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20)
                    };
                    root.Children.Add(companySubHeader);
                }
            }

            var metaStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 10)
            };
            metaStack.Children.Add(new TextBlock
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Text = $"التاريخ: {DateTime.Now:yyyy-MM-dd}",
                Margin = new Thickness(0, 0, 0, 4)
            });
            metaStack.Children.Add(new TextBlock
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Text = $"رقم الإيصال: {Safe(_data.ReceiptNumber)}"
            });
            root.Children.Add(metaStack);

            var title = new TextBlock
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Cairo"),
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Text = "إيصال استلام جهاز إيجار",
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 30)
            };
            root.Children.Add(title);

            var customerBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Width = tableWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var customerGrid = new Grid();
            customerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelColumnWidth) });
            customerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddRow(customerGrid, 0, "اسم العميل", Safe(_data.CustomerName), true);
            AddRow(customerGrid, 1, "الشركة", Safe(_data.Company));
            AddRow(customerGrid, 2, "رقم الهاتف", Safe(_data.Phone), false, true);

            customerBorder.Child = customerGrid;
            root.Children.Add(customerBorder);

            var deviceBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Width = tableWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var deviceGrid = new Grid();
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelColumnWidth) });
            deviceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddRow(deviceGrid, 0, "نوع الجهاز", Safe(_data.DeviceType), true);
            AddRow(deviceGrid, 1, "الماركة", Safe(_data.Brand));
            AddRow(deviceGrid, 2, "الموديل", Safe(_data.Model));
            AddRow(deviceGrid, 3, "السيريال", Safe(_data.Serial));
            AddRow(deviceGrid, 4, "السيريال الثاني", Safe(_data.Serial2), false, true);

            deviceBorder.Child = deviceGrid;
            root.Children.Add(deviceBorder);

            var rentalBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Width = tableWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var rentalGrid = new Grid();
            rentalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelColumnWidth) });
            rentalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddRow(rentalGrid, 0, "نوع الإيجار", Safe(_data.RentalType), true);
            AddRow(rentalGrid, 1, "تاريخ البدء", _data.StartDate == default ? "-" : _data.StartDate.ToString("yyyy-MM-dd"));
            AddRow(rentalGrid, 2, "تاريخ الانتهاء", _data.EndDate == default ? "-" : _data.EndDate.ToString("yyyy-MM-dd"));
            AddRow(rentalGrid, 3, "السعر", _data.Price.ToString("0.##"));
            AddRow(rentalGrid, 4, "المبلغ المدفوع", _data.PaidAmount.ToString("0.##"));
            AddRow(rentalGrid, 5, "المبلغ المتبقي", _data.RemainingAmount.ToString("0.##"));
            AddRow(rentalGrid, 6, "الحالة", Safe(_data.Status), false, true);

            rentalBorder.Child = rentalGrid;
            root.Children.Add(rentalBorder);

            if (!string.IsNullOrWhiteSpace(_data.Notes))
            {
                var notesBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Width = tableWidth,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Background = Brushes.White,
                    Padding = new Thickness(12)
                };
                notesBorder.Child = new TextBlock
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    FontFamily = new FontFamily("Tahoma"),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                    Text = $"ملاحظات: {_data.Notes}",
                    TextWrapping = TextWrapping.Wrap
                };
                root.Children.Add(notesBorder);
            }

            transformContainer.Children.Add(viewbox);
            page.Children.Add(transformContainer);

            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            doc.Pages.Add(content);

            return doc;
        }

        static void AddRow(Grid grid, int rowIndex, string label, string value, bool isFirst = false, bool isLast = false)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var borderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            var labelBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            var labelCell = new Border
            {
                Background = labelBackground,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 1, isLast ? 0 : 1),
                Padding = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(isFirst ? 8 : 0, 0, 0, isLast ? 8 : 0)
            };
            labelCell.Child = new TextBlock
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetRow(labelCell, rowIndex);
            Grid.SetColumn(labelCell, 0);
            grid.Children.Add(labelCell);

            var valueCell = new Border
            {
                Background = Brushes.White,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 0, isLast ? 0 : 1),
                Padding = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(0, isFirst ? 8 : 0, isLast ? 8 : 0, 0)
            };
            valueCell.Child = new TextBlock
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Text = value,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetRow(valueCell, rowIndex);
            Grid.SetColumn(valueCell, 1);
            grid.Children.Add(valueCell);
        }

        static string Safe(string? value)
        {
            var v = (value ?? "").Trim();
            return string.IsNullOrWhiteSpace(v) ? "-" : v;
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

        Brush GetFrameBrush()
        {
            return SelectedFrameColor switch
            {
                "أسود" => Brushes.Black,
                "أزرق كحلي" => new SolidColorBrush(Color.FromRgb(0, 51, 102)),
                "أزرق فاتح" => new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                "بدون برواز" => Brushes.Transparent,
                _ => new SolidColorBrush(Color.FromRgb(80, 80, 80))
            };
        }

        static void SaveAppSetting(string key, string value)
        {
            Services.AppSettingHelper.Save(key, value);
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
