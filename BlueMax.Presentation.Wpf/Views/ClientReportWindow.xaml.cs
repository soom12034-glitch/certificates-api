using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using BlueMax.Domain;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class ClientReportWindow : Window
{
    public ClientReportWindow(CustomerItem customer, List<WorkOrder> workOrders, List<Certificate> certificates)
    {
        InitializeComponent();
        var vm = new ClientReportViewModel(customer, workOrders, certificates);
        DataContext = vm;
        Loaded += (_, __) => vm.RefreshPreviewCommand.Execute(null);
    }

    void DocumentViewer_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DocumentViewer viewer)
            return;

        var toolbar = viewer.Template.FindName("PART_Toolbar", viewer) as FrameworkElement
                      ?? viewer.Template.FindName("PART_ToolBar", viewer) as FrameworkElement;

        if (toolbar != null)
            toolbar.Visibility = Visibility.Collapsed;
    }

    sealed class ClientReportViewModel : ViewModelBase
    {
        const string PrinterNameKey = "ClientReportPrinterName";
        const string OffsetYKey = "ClientReportOffsetY";
        const string OffsetXKey = "ClientReportOffsetX";
        const string TopMarginKey = "ClientReportTopMargin";
        const string BottomMarginKey = "ClientReportBottomMargin";
        const string IsLetterheadKey = "ClientReportIsLetterhead";
        const string FrameColorKey = "ClientReportFrameColor";

        readonly CustomerItem _customer;
        readonly List<WorkOrder> _workOrders;
        readonly List<Certificate> _certificates;
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

        public ClientReportViewModel(CustomerItem customer, List<WorkOrder> workOrders, List<Certificate> certificates)
        {
            _customer = customer;
            _workOrders = workOrders;
            _certificates = certificates;
            _companySettings = new BlueMax.Infrastructure.ReportDesignerSettingsStore().Load();
            LoadInstalledPrinters();
            LoadSavedSettings();

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
                SaveAppSetting(PrinterNameKey, _selectedPrinterName);
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
                SaveAppSetting(IsLetterheadKey, _isLetterhead.ToString());
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
                SaveAppSetting(OffsetYKey, _contentOffsetY.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
                SaveAppSetting(TopMarginKey, _topMargin.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
                SaveAppSetting(BottomMarginKey, _bottomMargin.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
                SaveAppSetting(FrameColorKey, value ?? "");
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
                SaveAppSetting(OffsetXKey, _contentOffsetX.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
            catch { }
        }

        void LoadSavedSettings()
        {
            try
            {
                var savedPrinter = ConfigurationManager.AppSettings[PrinterNameKey] ?? "";
                if (!string.IsNullOrWhiteSpace(savedPrinter) && _installedPrinters.Contains(savedPrinter))
                    SelectedPrinterName = savedPrinter;

                var offsetRaw = ConfigurationManager.AppSettings[OffsetYKey];
                if (double.TryParse(offsetRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var offset))
                    _contentOffsetY = Clamp(offset, -80, 80);
                    
                var offsetXRaw = ConfigurationManager.AppSettings[OffsetXKey];
                if (double.TryParse(offsetXRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var offsetX))
                    _contentOffsetX = Clamp(offsetX, -80, 80);
                    
                var topMarginRaw = ConfigurationManager.AppSettings[TopMarginKey];
                if (double.TryParse(topMarginRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var topMargin))
                    _topMargin = Clamp(topMargin, 0, 200);
                    
                var bottomMarginRaw = ConfigurationManager.AppSettings[BottomMarginKey];
                if (double.TryParse(bottomMarginRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bottomMargin))
                    _bottomMargin = Clamp(bottomMargin, 0, 200);
                    
                var isLetterheadRaw = ConfigurationManager.AppSettings[IsLetterheadKey];
                if (bool.TryParse(isLetterheadRaw, out var isLetterhead))
                    _isLetterhead = isLetterhead;
                    
                var colorRaw = ConfigurationManager.AppSettings[FrameColorKey];
                if (!string.IsNullOrWhiteSpace(colorRaw) && FrameColors.Contains(colorRaw))
                    _selectedFrameColor = colorRaw;
            }
            catch { }
        }

        void RefreshPreview() => PreviewDocument = BuildDocument();

        void ChoosePrinter()
        {
            try
            {
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true && dialog.PrintQueue != null)
                    SelectedPrinterName = dialog.PrintQueue.FullName;
            }
            catch { }
        }

        void Print()
        {
            try
            {
                if (PreviewDocument == null) RefreshPreview();
                if (PreviewDocument == null) return;

                var dialog = new PrintDialog();
                try
                {
                    if (!string.IsNullOrWhiteSpace(SelectedPrinterName))
                    {
                        var server = new LocalPrintServer();
                        dialog.PrintQueue = server.GetPrintQueue(SelectedPrinterName);
                    }
                }
                catch { }

                if (dialog.ShowDialog() != true) return;
                
                try
                {
                    if (dialog.PrintQueue != null)
                        SelectedPrinterName = dialog.PrintQueue.FullName;
                }
                catch { }

                var paginator = ((IDocumentPaginatorSource)PreviewDocument).DocumentPaginator;
                dialog.PrintDocument(paginator, "Client Report");
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

        FixedDocument BuildDocument()
        {
            const double dpi = 96.0;
            const double a4Width = 8.27 * dpi;
            const double a4Height = 11.69 * dpi;
            const double pageMarginLeftRight = 40;
            double headerSpace = IsLetterhead ? TopMargin : 40; 
            double footerSpace = BottomMargin;

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(a4Width, a4Height);

            var page = new FixedPage
            {
                Width = a4Width,
                Height = a4Height,
                FlowDirection = FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            if (IsLetterhead)
            {
                var topGuideline = new System.Windows.Shapes.Line
                {
                    X1 = 0, Y1 = headerSpace, X2 = a4Width, Y2 = headerSpace,
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 4 })
                };
                page.Children.Add(topGuideline);
            }
            var bottomGuideline = new System.Windows.Shapes.Line
            {
                X1 = 0, Y1 = a4Height - footerSpace, X2 = a4Width, Y2 = a4Height - footerSpace,
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 0, 0)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 4 })
            };
            page.Children.Add(bottomGuideline);

            var transformContainer = new Grid { Width = a4Width, Height = a4Height };
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
                Padding = new Thickness(24),
                Background = Brushes.White
            };

            var root = new StackPanel { Width = a4Width - (pageMarginLeftRight * 2) - 48 };
            outerBorder.Child = root;
            viewbox.Child = outerBorder;

            if (!IsLetterhead && !string.IsNullOrWhiteSpace(_companySettings.CompanyName))
            {
                root.Children.Add(new TextBlock
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    FontFamily = new FontFamily("Cairo"),
                    FontSize = 26,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Text = _companySettings.CompanyName,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            // Title
            root.Children.Add(new TextBlock
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Cairo"),
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Text = "كشف حساب عميل",
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 30)
            });

            // Meta Info (Right aligned)
            var metaStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 30) };
            metaStack.Children.Add(new TextBlock { FontFamily = new FontFamily("Tahoma"), FontSize = 16, FontWeight = FontWeights.Bold, Text = $"اسم العميل: {_customer.Name}", Margin = new Thickness(0,0,0,5) });
            metaStack.Children.Add(new TextBlock { FontFamily = new FontFamily("Tahoma"), FontSize = 14, Text = $"رقم الهاتف: {_customer.Phone}", Margin = new Thickness(0,0,0,5) });
            metaStack.Children.Add(new TextBlock { FontFamily = new FontFamily("Tahoma"), FontSize = 14, Text = $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd}", Margin = new Thickness(0,0,0,5) });
            root.Children.Add(metaStack);

            // Maintenance Table
            root.Children.Add(new TextBlock { Text = "سجل الصيانة والأعطال", FontSize = 18, FontFamily = new FontFamily("Cairo"), FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,10) });
            var woGrid = new Grid();
            woGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            woGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            woGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            woGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            
            AddCell(woGrid, 0, 0, "التاريخ", true);
            AddCell(woGrid, 0, 1, "الجهاز", true);
            AddCell(woGrid, 0, 2, "العطل / الإصلاح", true);
            AddCell(woGrid, 0, 3, "التكلفة", true);
            woGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            int row = 1;
            decimal totalCost = 0;
            foreach(var w in _workOrders)
            {
                woGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddCell(woGrid, row, 0, w.ReceivedDate.ToString("yyyy-MM-dd"), false);
                AddCell(woGrid, row, 1, $"{w.DeviceType} {w.Model}", false);
                AddCell(woGrid, row, 2, w.Complaint, false);
                AddCell(woGrid, row, 3, w.TotalCost.ToString("0.00"), false);
                totalCost += w.TotalCost;
                row++;
            }
            woGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(woGrid, row, 0, "الإجمالي", true);
            AddCell(woGrid, row, 1, "", true);
            AddCell(woGrid, row, 2, "", true);
            AddCell(woGrid, row, 3, totalCost.ToString("0.00"), true);
            root.Children.Add(woGrid);

            // Certificates Table
            root.Children.Add(new TextBlock { Text = "Calibration Certificates History", FontSize = 18, FontFamily = new FontFamily("Cairo"), FontWeight = FontWeights.Bold, Margin = new Thickness(0,30,0,10) });
            var certGrid = new Grid();
            certGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            certGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            certGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            certGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddCell(certGrid, 0, 0, "رقم الشهادة", true);
            AddCell(certGrid, 0, 1, "الجهاز / السيريال", true);
            AddCell(certGrid, 0, 2, "تاريخ الإصدار", true);
            AddCell(certGrid, 0, 3, "تاريخ الانتهاء", true);
            certGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            row = 1;
            foreach(var c in _certificates)
            {
                certGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddCell(certGrid, row, 0, c.CertificateNumber, false);
                AddCell(certGrid, row, 1, $"{c.DeviceType} ({c.SerialText})", false);
                AddCell(certGrid, row, 2, c.IssueDate.ToString("yyyy-MM-dd"), false);
                AddCell(certGrid, row, 3, c.ExpiryDate.ToString("yyyy-MM-dd"), false);
                row++;
            }
            root.Children.Add(certGrid);

            transformContainer.Children.Add(viewbox);
            page.Children.Add(transformContainer);

            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            doc.Pages.Add(content);
            return doc;
        }

        static void AddCell(Grid grid, int row, int col, string text, bool isHeader)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200,200,200)),
                BorderThickness = new Thickness(col == 0 ? 1 : 0, row == 0 ? 1 : 0, 1, 1),
                Padding = new Thickness(6),
                Background = isHeader ? new SolidColorBrush(Color.FromRgb(245, 245, 245)) : Brushes.White
            };
            var tb = new TextBlock 
            { 
                Text = text ?? "", 
                FontSize = 13, 
                FontFamily = new FontFamily("Tahoma"),
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);
        }

        static void SaveAppSetting(string key, string value)
        {
            BlueMax.Presentation.Wpf.Services.AppSettingHelper.Save(key, value);
        }

        static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
