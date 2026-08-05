using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;
using BlueMax.Infrastructure;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class InventoryViewModel : ViewModelBase
{
    string _code = "";
    string _name = "";
    int _quantity;
    DateTime _purchaseDate = DateTime.Today;
    decimal _costPrice;
    decimal _sellingPrice;
    string _brand = "";
    string _location = "";
    int _minThreshold = 2;
    string _searchText = "";
    int _lowStockCount;
    bool _showLowStockOnly;
    SparePartItem? _selectedItem;

    public InventoryViewModel()
    {
        Items = new ObservableCollection<SparePartItem>();
        NewItemCommand = new RelayCommand(_ => ClearInputs());
        SaveItemCommand = new RelayCommand(_ => SaveItem());
        DeleteItemCommand = new RelayCommand(_ => DeleteItem(), _ => SelectedItem != null);
        RefreshItemsCommand = new RelayCommand(_ => RefreshItems());
        PrintReportCommand = new RelayCommand(_ => PrintReport());
        PrintBarcodeCommand = new RelayCommand(_ => PrintBarcode(), _ => SelectedItem != null);
        try { RefreshItems(); } catch { }
    }

    public ObservableCollection<SparePartItem> Items { get; }

    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
    public DateTime PurchaseDate { get => _purchaseDate; set => SetProperty(ref _purchaseDate, value); }
    public decimal CostPrice { get => _costPrice; set => SetProperty(ref _costPrice, value); }
    public decimal SellingPrice { get => _sellingPrice; set => SetProperty(ref _sellingPrice, value); }
    public string Brand { get => _brand; set => SetProperty(ref _brand, value); }
    public string Location { get => _location; set => SetProperty(ref _location, value); }
    public int MinThreshold { get => _minThreshold; set => SetProperty(ref _minThreshold, value); }
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    
    public int LowStockCount { get => _lowStockCount; set { SetProperty(ref _lowStockCount, value); OnPropertyChanged(nameof(HasLowStockAlert)); } }
    public bool HasLowStockAlert => LowStockCount > 0;
    
    public bool ShowLowStockOnly
    {
        get => _showLowStockOnly;
        set
        {
            if (SetProperty(ref _showLowStockOnly, value))
                RefreshItems();
        }
    }

    public SparePartItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
                return;
            if (value == null)
            {
                ClearInputs();
                return;
            }
            Code = value.Code;
            Name = value.Name;
            Quantity = value.Quantity;
            PurchaseDate = value.PurchaseDate;
            CostPrice = value.CostPrice;
            SellingPrice = value.SellingPrice;
            Brand = value.Brand;
            Location = value.Location;
            MinThreshold = value.MinThreshold;
            DeleteItemCommand.RaiseCanExecuteChanged();
            PrintBarcodeCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand NewItemCommand { get; }
    public RelayCommand SaveItemCommand { get; }
    public RelayCommand DeleteItemCommand { get; }
    public RelayCommand RefreshItemsCommand { get; }
    public RelayCommand PrintReportCommand { get; }
    public RelayCommand PrintBarcodeCommand { get; }

    void SaveItem()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            System.Windows.MessageBox.Show("كود واسم القطعة مطلوبان", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        
        // Ensure Code is unique
        var existingWithCode = db.SpareParts.FirstOrDefault(s => s.Code == Code && (SelectedItem == null || s.Id != SelectedItem.Id));
        if (existingWithCode != null)
        {
            System.Windows.MessageBox.Show("كود القطعة موجود مسبقاً", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        SparePart item;
        if (SelectedItem != null)
        {
            item = db.SpareParts.FirstOrDefault(e => e.Id == SelectedItem.Id) ?? new SparePart();
        }
        else
        {
            item = new SparePart();
            db.SpareParts.Add(item);
        }

        item.Code = Code;
        item.Name = Name;
        item.Quantity = Quantity;
        item.PurchaseDate = PurchaseDate;
        item.CostPrice = CostPrice;
        item.SellingPrice = SellingPrice;
        item.Brand = Brand;
        item.Location = Location;
        item.MinThreshold = MinThreshold;

        db.SaveChanges();
        RefreshItems();
        ClearInputs();
    }

    void DeleteItem()
    {
        if (SelectedItem == null)
            return;
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        var entity = db.SpareParts.FirstOrDefault(e => e.Id == SelectedItem.Id);
        if (entity != null)
        {
            db.SpareParts.Remove(entity);
            db.SaveChanges();
        }
        RefreshItems();
        ClearInputs();
    }

    async void RefreshItems()
    {
        await Task.Run(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[Inventory] RefreshItems started in background thread");
                using var db = CreateDbContext();
            db.Database.EnsureCreated();

            var query = db.SpareParts.AsNoTracking();

            var lowStockCount = query.Count(e => e.Quantity <= e.MinThreshold);

            if (ShowLowStockOnly)
            {
                query = query.Where(e => e.Quantity <= e.MinThreshold);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var text = SearchText.Trim();
                query = query.Where(e => e.Name.Contains(text) || e.Code.Contains(text) || e.Brand.Contains(text));
            }

            var itemsList = query
                .OrderBy(e => e.Name)
                .Take(500)
                .Select(e => new SparePartItem
                {
                    Id = e.Id,
                    Code = e.Code,
                    Name = e.Name,
                    Quantity = e.Quantity,
                    PurchaseDate = e.PurchaseDate,
                    CostPrice = e.CostPrice,
                    SellingPrice = e.SellingPrice,
                    Brand = e.Brand,
                    Location = e.Location,
                    MinThreshold = e.MinThreshold
                })
                .ToList();

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LowStockCount = lowStockCount;
                    Items.Clear();
                    foreach (var item in itemsList)
                        Items.Add(item);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show("Error loading inventory:\n" + ex.Message + "\n\n" + ex.StackTrace, "Inventory Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        });
    }

    void ClearInputs()
    {
        Code = "";
        Name = "";
        Quantity = 0;
        PurchaseDate = DateTime.Today;
        CostPrice = 0;
        SellingPrice = 0;
        Brand = "";
        Location = "";
        MinThreshold = 2;
        SelectedItem = null;
        DeleteItemCommand.RaiseCanExecuteChanged();
        PrintBarcodeCommand.RaiseCanExecuteChanged();
    }

    void PrintBarcode()
    {
        if (SelectedItem == null) return;
        
        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;

            // Barcode dimensions (e.g. 50x25 mm)
            const double dpi = 96.0;
            double width = (50.0 / 25.4) * dpi;
            double height = (25.0 / 25.4) * dpi;

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(width, height);

            var page = new FixedPage { Width = width, Height = height, Background = Brushes.White };
            
            var grid = new Grid { Width = width, Height = height, Margin = new Thickness(4) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Generate Barcode Image using ZXing via BarcodeGenerator
            var bitmap = BarcodeGenerator.GenerateBarcode(SelectedItem.Code, 200, 60);
            var imageSrc = new BitmapImage();
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                imageSrc.BeginInit();
                imageSrc.CacheOption = BitmapCacheOption.OnLoad;
                imageSrc.StreamSource = ms;
                imageSrc.EndInit();
            }

            var img = new Image { Source = imageSrc, Stretch = Stretch.Uniform, Margin = new Thickness(0,0,0,2) };
            Grid.SetRow(img, 0);
            grid.Children.Add(img);

            var nameText = new TextBlock
            {
                Text = SelectedItem.Name,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            };
            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);

            var infoText = new TextBlock
            {
                Text = $"السعر: {SelectedItem.SellingPrice} | الشراء: {SelectedItem.PurchaseDate:yyyy-MM}",
                FontSize = 8,
                TextAlignment = TextAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            };
            Grid.SetRow(infoText, 2);
            grid.Children.Add(infoText);

            page.Children.Add(grid);
            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            doc.Pages.Add(content);

            dialog.PrintDocument(doc.DocumentPaginator, "طباعة باركود القطعة");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Print Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void PrintReport()
    {
        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;

            const double dpi = 96.0;
            const double a4Width = 8.27 * dpi;
            const double a4Height = 11.69 * dpi;
            const double pageMargin = 40;
            const double rowHeight = 40;
            const double firstPageHeaderHeight = 125;
            const double continuationHeaderHeight = 30;

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(a4Width, a4Height);

            var rows = Items.ToList();
            var chunks = new List<int>();
            double contentHeight = a4Height - 2 * pageMargin;
            int firstPageRows = Math.Max(1, (int)Math.Floor((contentHeight - firstPageHeaderHeight) / rowHeight));
            int nextPageRows = Math.Max(1, (int)Math.Floor((contentHeight - continuationHeaderHeight) / rowHeight));
            int remaining = rows.Count;
            if (remaining == 0)
            {
                chunks.Add(0);
            }
            else
            {
                int first = Math.Min(firstPageRows, remaining);
                chunks.Add(first);
                remaining -= first;
                while (remaining > 0)
                {
                    int chunk = Math.Min(nextPageRows, remaining);
                    chunks.Add(chunk);
                    remaining -= chunk;
                }
            }

            int start = 0;
            for (int p = 0; p < chunks.Count; p++)
            {
                var page = new FixedPage { Width = a4Width, Height = a4Height, FlowDirection = FlowDirection.RightToLeft, Background = Brushes.White };

                var stack = new StackPanel { Width = a4Width - 2 * pageMargin, Margin = new Thickness(pageMargin) };

                if (p == 0)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = "تقرير المخزون وقطع الغيار",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20)
                    });

                    var infoGrid = new Grid();
                    infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    infoGrid.Children.Add(new TextBlock { Text = $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}", FontSize = 14 });
                    var comp = new TextBlock { Text = $"إجمالي القطع: {rows.Count}", FontSize = 14, TextAlignment = TextAlignment.Right };
                    Grid.SetColumn(comp, 1);
                    infoGrid.Children.Add(comp);

                    stack.Children.Add(infoGrid);
                    stack.Children.Add(new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 2), Margin = new Thickness(0, 10, 0, 20) });
                }

                var tableGrid = new Grid();
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                AddReportCell(tableGrid, 0, 0, "الكود", true);
                AddReportCell(tableGrid, 0, 1, "اسم القطعة", true);
                AddReportCell(tableGrid, 0, 2, "الكمية", true);
                AddReportCell(tableGrid, 0, 3, "التكلفة", true);
                AddReportCell(tableGrid, 0, 4, "سعر البيع", true);
                AddReportCell(tableGrid, 0, 5, "المكان", true);
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                int r = 1;
                for (int i = start; i < start + chunks[p] && i < rows.Count; i++)
                {
                    var item = rows[i];
                    tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    AddReportCell(tableGrid, r, 0, item.Code, false);
                    AddReportCell(tableGrid, r, 1, item.Name, false);

                    var qtyCell = AddReportCell(tableGrid, r, 2, item.Quantity.ToString(), false);
                    if (item.IsLowStock) qtyCell.Foreground = Brushes.Red;

                    AddReportCell(tableGrid, r, 3, item.CostPrice.ToString("0.00"), false);
                    AddReportCell(tableGrid, r, 4, item.SellingPrice.ToString("0.00"), false);
                    AddReportCell(tableGrid, r, 5, item.Location, false);
                    r++;
                }
                start += chunks[p];

                stack.Children.Add(tableGrid);

                if (chunks.Count > 1)
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"الصفحة {p + 1} من {chunks.Count}",
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 16, 0, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
                    });
                }

                page.Children.Add(stack);

                var content = new PageContent();
                ((IAddChild)content).AddChild(page);
                doc.Pages.Add(content);
            }

            dialog.PrintDocument(doc.DocumentPaginator, "تقرير المخزون");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Print Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    static TextBlock AddReportCell(Grid grid, int row, int col, string text, bool isHeader)
    {
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(col == 0 ? 1 : 0, row == 0 ? 1 : 0, 1, 1),
            Padding = new Thickness(4),
            Background = isHeader ? new SolidColorBrush(Color.FromRgb(240, 240, 240)) : Brushes.White
        };
        var tb = new TextBlock 
        { 
            Text = text, 
            FontSize = 12, 
            FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        border.Child = tb;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
        return tb;
    }

    static AppDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext();
    }
}

public sealed class SparePartItem : ViewModelBase
{
    int _id;
    string _code = "";
    string _name = "";
    int _quantity;
    DateTime _purchaseDate;
    decimal _costPrice;
    decimal _sellingPrice;
    string _brand = "";
    string _location = "";
    int _minThreshold;
    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
    public DateTime PurchaseDate { get => _purchaseDate; set => SetProperty(ref _purchaseDate, value); }
    public decimal CostPrice { get => _costPrice; set => SetProperty(ref _costPrice, value); }
    public decimal SellingPrice { get => _sellingPrice; set => SetProperty(ref _sellingPrice, value); }
    public string Brand { get => _brand; set => SetProperty(ref _brand, value); }
    public string Location { get => _location; set => SetProperty(ref _location, value); }
    public int MinThreshold { get => _minThreshold; set => SetProperty(ref _minThreshold, value); }
    
    // Helper property to highlight low stock
    public bool IsLowStock => Quantity <= MinThreshold;
}
