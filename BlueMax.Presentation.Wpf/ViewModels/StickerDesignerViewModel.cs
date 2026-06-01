using System;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;
using BlueMax.Presentation.Wpf.Services;
using System.Collections.Generic;
using System.ComponentModel;
using BlueMax.Infrastructure;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BlueMax.Presentation.Wpf.ViewModels;

public class StickerDesignerViewModel : ViewModelBase
    {
        bool _editEnabled = true;
        private string _qrDebugInfo = string.Empty;
        public string QrDebugInfo
        {
            get => _qrDebugInfo;
            set => SetProperty(ref _qrDebugInfo, value);
        }

    const double PixelToMm = 0.264583;
    double _stickerWidthMm = 50;
    double _stickerHeightMm = 30;
    double _canvasWidthPx;
    double _canvasHeightPx;
    double _previewScale = 1.0;
    bool _useLogo;
    string _logoPath = "";
    string _companyName = "اسم الشركة";
    string _companyHeader = "اختصار الشركة";
    string _companyAddress = "العنوان";
    string _companyPhone = "رقم الهاتف";
    string _brand = "";
    string _model = "";
    string _serial = "";
    string _certificateNumber = "";
    DateTime _calDate = DateTime.Today;
    DateTime _expDate = DateTime.Today.AddMonths(6);
    double _headerFontSize = 12;
    double _bodyFontSize = 12;
    StickerItem? _selectedStickerItem;
    PropertyChangedEventHandler? _selectedStickerItemHandler;
    readonly ObservableCollection<StickerItem> _stickerItems = new();
    readonly StickerLayoutStore _store;
    bool _showGrid = false;
    bool _showQr = true;

    public StickerDesignerViewModel()
    {
        _store = new StickerLayoutStore();
        PickLogoCommand = new RelayCommand(_ => PickLogo());
        SaveLayoutCommand = new RelayCommand(_ => SaveLayout());
        ResetLayoutCommand = new RelayCommand(_ => ResetLayout());
        TestPrintCommand = new RelayCommand(_ => TestPrint());
        AutoLayoutCommand = new RelayCommand(_ => AutoLayout());
        PreviewCommand = new RelayCommand(_ => ShowPreview());
        GenerateAndSaveQrCodeCommand = new RelayCommand(_ => GenerateAndSaveQrCode());
        ZoomInCommand = new RelayCommand(_ => ZoomIn());
        ZoomOutCommand = new RelayCommand(_ => ZoomOut());
        AddTextItemCommand = new RelayCommand(_ => AddTextItem());
        AddLineItemCommand = new RelayCommand(_ => AddLineItem());
        DeleteSelectedItemCommand = new RelayCommand(_ => DeleteSelectedItem(), _ => SelectedStickerItem != null);
        ApplySizeTemplateCommand = new RelayCommand(param => ApplySizeTemplate(param?.ToString()));
        SaveCustomTemplateCommand = new RelayCommand(_ => SaveCustomTemplate());
        SnapToGrid = true;
        SnapStepMm = 1;
        LoadLayout();
    }

    public double StickerWidthMm
    {
        get => _stickerWidthMm;
        set
        {
            var clamped = Math.Max(10, Math.Min(200, value));
            // Always recalc canvas even if value did not change to avoid zero-size canvas on first load
            SetProperty(ref _stickerWidthMm, clamped);
            CanvasWidthPx = clamped / PixelToMm;
            ClampAllItems();
        }
    }

    public double StickerHeightMm
    {
        get => _stickerHeightMm;
        set
        {
            var clamped = Math.Max(10, Math.Min(200, value));
            // Always recalc canvas even if value did not change to avoid zero-size canvas on first load
            SetProperty(ref _stickerHeightMm, clamped);
            CanvasHeightPx = clamped / PixelToMm;
            ClampAllItems();
        }
    }

    public double CanvasWidthPx
    {
        get => _canvasWidthPx;
        private set => SetProperty(ref _canvasWidthPx, value);
    }

    public double CanvasHeightPx
    {
        get => _canvasHeightPx;
        private set => SetProperty(ref _canvasHeightPx, value);
    }

    public double PreviewScale
    {
        get => _previewScale;
        set
        {
            var clamped = Math.Max(0.5, Math.Min(2.0, value));
            if (!SetProperty(ref _previewScale, clamped))
                return;
        }
    }

    double _zoom = 1.0;
    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, Math.Max(0.5, Math.Min(4.0, value)));
    }

    void ZoomIn()
    {
        Zoom = Math.Min(4.0, Zoom + 0.25);
    }

    void ZoomOut()
    {
        Zoom = Math.Max(0.5, Zoom - 0.25);
    }

    void AddTextItem()
    {
        var newItem = new StickerItem
        {
            Key = $"text_{Guid.NewGuid().ToString().Substring(0, 8)}",
            X = 10,
            Y = 10,
            Width = 50,
            Height = 15,
            FontSize = 10,
            DisplayText = "نص جديد",
            IsVisible = true,
            FontFamily = "Segoe UI",
            FontColor = "#000000",
            TextAlignment = "Right",
            ZIndex = _stickerItems.Count
        };
        _stickerItems.Add(newItem);
        SelectedStickerItem = newItem;
        OnPropertyChanged(nameof(StickerItems));
    }

    void AddLineItem()
    {
        var newItem = new StickerItem
        {
            Key = $"line_{Guid.NewGuid().ToString().Substring(0, 8)}",
            X = 10,
            Y = 10,
            Width = 50,
            Height = 1.5,
            FontSize = 1,
            DisplayText = "",
            IsVisible = true,
            ZIndex = _stickerItems.Count
        };
        _stickerItems.Add(newItem);
        SelectedStickerItem = newItem;
        OnPropertyChanged(nameof(StickerItems));
    }

    void DeleteSelectedItem()
    {
        if (_selectedStickerItem != null)
        {
            _stickerItems.Remove(_selectedStickerItem);
            SelectedStickerItem = _stickerItems.Count > 0 ? _stickerItems[0] : null;
            OnPropertyChanged(nameof(StickerItems));
        }
    }

    void ApplySizeTemplate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return;

        var (width, height) = template switch
        {
            "50x30" => (50.0, 30.0),
            "60x40" => (60.0, 40.0),
            "70x50" => (70.0, 50.0),
            "80x60" => (80.0, 60.0),
            "100x70" => (100.0, 70.0),
            _ => (50.0, 30.0)
        };

        StickerWidthMm = width;
        StickerHeightMm = height;
    }

    void SaveCustomTemplate()
    {
        // For now, this is a placeholder. In a full implementation, this would:
        // 1. Prompt the user for a template name
        // 2. Save the current StickerWidthMm and StickerHeightMm to a custom templates list
        // 3. Persist the custom templates to storage
        // For simplicity, we'll just save the current layout which includes the size
        SaveLayout();
    }

    public bool UseLogo
    {
        get => _useLogo;
        set
        {
            if (SetProperty(ref _useLogo, value))
            {
                UpdateItemVisibility("logo", value);
            }
        }
    }

    public string LogoPath
    {
        get => _logoPath;
        set
        {
            if (SetProperty(ref _logoPath, value))
            {
                UpdateItemText("logo", value);
            }
        }
    }

    bool _snapToGrid;
    double _snapStepMm;

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => SetProperty(ref _snapToGrid, value);
    }

    public double SnapStepMm
    {
        get => _snapStepMm;
        set => SetProperty(ref _snapStepMm, Math.Max(0.1, Math.Min(10, value)));
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set => SetProperty(ref _showGrid, value);
    }

    public bool EditEnabled
    {
        get => _editEnabled;
        set => SetProperty(ref _editEnabled, value);
    }

    public bool ShowQr
    {
        get => _showQr;
        set
        {
            if (SetProperty(ref _showQr, value))
            {
                UpdateItemVisibility("qr", value);
                OnPropertyChanged(nameof(StickerItems));
            }
        }
    }

    double _qrSizeMm = 16;

    public double QrSizeMm
    {
        get => _qrSizeMm;
        set
        {
            if (SetProperty(ref _qrSizeMm, value))
            {
                var qrItem = FindItem("qr");
                if (qrItem != null)
                {
                    var qrSizePx = value / PixelToMm;
                    qrItem.Width = qrSizePx;
                    qrItem.Height = qrSizePx;
                }
            }
        }
    }


    public string CompanyName
    {
        get => _companyName;
        set
        {
            if (SetProperty(ref _companyName, value))
            {
                UpdateItemText("company", value ?? "");
                UpdateQrContent();
            }
        }
    }

    public string CompanyHeader
    {
        get => _companyHeader;
        set
        {
            if (SetProperty(ref _companyHeader, value))
            {
                UpdateItemText("header", value ?? "");
            }
        }
    }

    public string CompanyAddress
    {
        get => _companyAddress;
        set
        {
            if (SetProperty(ref _companyAddress, value))
            {
                UpdateItemText("address", value ?? "");
            }
        }
    }

    public string CompanyPhone
    {
        get => _companyPhone;
        set
        {
            if (SetProperty(ref _companyPhone, value))
            {
                UpdateItemText("phone", value ?? "");
                UpdateQrContent();
            }
        }
    }

    public string Brand
    {
        get => _brand;
        set
        {
            if (SetProperty(ref _brand, value))
            {
                UpdateItemText("brand_value", value ?? "");
                UpdateQrContent();
            }
        }
    }

    public string Model
    {
        get => _model;
        set
        {
            if (SetProperty(ref _model, value))
            {
                UpdateItemText("model_value", value ?? "");
                UpdateQrContent();
            }
        }
    }

    public string Serial
    {
        get => _serial;
        set
        {
            if (SetProperty(ref _serial, value))
            {
                UpdateItemText("serial_value", value ?? "");
                UpdateQrContent();
            }
        }
    }

    public string CertificateNumber
    {
        get => _certificateNumber;
        set
        {
            if (SetProperty(ref _certificateNumber, value))
            {
                UpdateItemText("cert_value", value ?? "");
            }
        }
    }

    public DateTime CalDate
    {
        get => _calDate;
        set
        {
            if (SetProperty(ref _calDate, value))
            {
                UpdateItemText("cal_value", value.ToString("dd-MM-yyyy"));
                UpdateQrContent();
            }
        }
    }

    public DateTime ExpDate
    {
        get => _expDate;
        set
        {
            if (SetProperty(ref _expDate, value))
            {
                UpdateItemText("exp_value", value.ToString("dd-MM-yyyy"));
                UpdateQrContent();
            }
        }
    }

    public double HeaderFontSize
    {
        get => _headerFontSize;
        set
        {
            if (!SetProperty(ref _headerFontSize, Math.Max(8, Math.Min(24, value))))
                return;
            UpdateItemFontSize("company", _headerFontSize);
            UpdateItemFontSize("header", _headerFontSize);
        }
    }

    public double BodyFontSize
    {
        get => _bodyFontSize;
        set
        {
            if (!SetProperty(ref _bodyFontSize, Math.Max(8, Math.Min(24, value))))
                return;
            // Update font size for body items
            foreach(var key in new[] { "brand_label", "brand_value", "model_label", "model_value", "serial_label", "serial_value", "cert_label", "cert_value", "cal_label", "cal_value", "exp_label", "exp_value", "address", "phone" })
            {
                UpdateItemFontSize(key, _bodyFontSize);
            }
        }
    }

    public RelayCommand PickLogoCommand { get; }
    public RelayCommand SaveLayoutCommand { get; }
    public RelayCommand ResetLayoutCommand { get; }
    public RelayCommand TestPrintCommand { get; }
    public RelayCommand AutoLayoutCommand { get; }
    public RelayCommand PreviewCommand { get; }
    public RelayCommand ZoomInCommand { get; }
    public RelayCommand ZoomOutCommand { get; }
    public RelayCommand AddTextItemCommand { get; }
    public RelayCommand AddLineItemCommand { get; }
    public RelayCommand DeleteSelectedItemCommand { get; }
    public RelayCommand ApplySizeTemplateCommand { get; }
    public RelayCommand SaveCustomTemplateCommand { get; }
    public ObservableCollection<StickerItem> StickerItems => _stickerItems;
    public StickerItem? SelectedStickerItem
    {
        get => _selectedStickerItem;
        set
        {
            if (_selectedStickerItem != null && _selectedStickerItemHandler != null)
                _selectedStickerItem.PropertyChanged -= _selectedStickerItemHandler;

            if (!SetProperty(ref _selectedStickerItem, value))
                return;

            if (_selectedStickerItem != null)
            {
                _selectedStickerItemHandler = SelectedStickerItemOnPropertyChanged;
                _selectedStickerItem.PropertyChanged += _selectedStickerItemHandler;
            }
            foreach (var it in _stickerItems)
                it.IsSelected = it == value;
            RaiseSelectedItemFieldsChanged();
        }
    }
    
    public double SelectedItemX
    {
        get => _selectedStickerItem?.X ?? 0;
        set
        {
            if (_selectedStickerItem == null) return;
            _selectedStickerItem.X = value;
            RaiseSelectedItemFieldsChanged();
        }
    }
    
    public double SelectedItemY
    {
        get => _selectedStickerItem?.Y ?? 0;
        set
        {
            if (_selectedStickerItem == null) return;
            _selectedStickerItem.Y = value;
            RaiseSelectedItemFieldsChanged();
        }
    }

    public double SelectedItemXMm
    {
        get => _selectedStickerItem == null ? 0 : Math.Round(_selectedStickerItem.X * PixelToMm, 2);
        set
        {
            if (_selectedStickerItem == null) return;
            _selectedStickerItem.X = value / PixelToMm;
            ClampAllItems();
            RaiseSelectedItemFieldsChanged();
        }
    }

    public double SelectedItemYMm
    {
        get => _selectedStickerItem == null ? 0 : Math.Round(_selectedStickerItem.Y * PixelToMm, 2);
        set
        {
            if (_selectedStickerItem == null) return;
            _selectedStickerItem.Y = value / PixelToMm;
            ClampAllItems();
            RaiseSelectedItemFieldsChanged();
        }
    }

    public double SelectedItemWidthMm
    {
        get => _selectedStickerItem == null ? 0 : Math.Round(_selectedStickerItem.Width * PixelToMm, 2);
        set
        {
            if (_selectedStickerItem == null) return;
            _selectedStickerItem.Width = value / PixelToMm;
            ClampAllItems();
            RaiseSelectedItemFieldsChanged();
        }
    }

    public double SelectedItemHeightMm
    {
        get => _selectedStickerItem == null ? 0 : Math.Round(_selectedStickerItem.Height * PixelToMm, 2);
        set
        {
            if (_selectedStickerItem == null) return;
            _selectedStickerItem.Height = value / PixelToMm;
            ClampAllItems();
            RaiseSelectedItemFieldsChanged();
        }
    }

    void RaiseSelectedItemFieldsChanged()
    {
        OnPropertyChanged(nameof(SelectedItemX));
        OnPropertyChanged(nameof(SelectedItemY));
        OnPropertyChanged(nameof(SelectedItemXMm));
        OnPropertyChanged(nameof(SelectedItemYMm));
        OnPropertyChanged(nameof(SelectedItemWidthMm));
        OnPropertyChanged(nameof(SelectedItemHeightMm));
    }

    void SelectedStickerItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StickerItem.X)
            or nameof(StickerItem.Y)
            or nameof(StickerItem.Width)
            or nameof(StickerItem.Height))
        {
            RaiseSelectedItemFieldsChanged();
        }
    }

    void PickLogo()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
        };
        if (dialog.ShowDialog() == true)
            LogoPath = dialog.FileName;
    }

    void ShowPreview()
    {
        try
        {
            // Ensure data is fresh
            UpdateQrContent();
            OnPropertyChanged(nameof(StickerItems));

            Debug.WriteLine($"StickerDesignerViewModel: ShowPreview called. StickerItems count: {StickerItems.Count}");
            foreach (var item in StickerItems)
            {
                Debug.WriteLine($"  Item Key: {item.Key}, DisplayText: '{item.DisplayText}', IsVisible: {item.IsVisible}, X: {item.X}, Y: {item.Y}, Width: {item.Width}, Height: {item.Height}, FontSize: {item.FontSize}");
            }

            var previewWindow = new BlueMax.Presentation.Wpf.Views.StickerPreviewWindow(this)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to show preview: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    async void TestPrint()
    {
        try
        {
            var printerSettingsStore = new PrinterSettingsStore();
            var printerSettings = printerSettingsStore.Load();
            
            var service = new StickerPrintService();
            await service.PrintStickerAsync(this, printerSettings);
            System.Windows.MessageBox.Show("Test print sent successfully.", "Print", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Test print failed: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    void LoadLayout()
    {
        var settings = _store.Load();
        StickerWidthMm = settings.WidthMm;
        StickerHeightMm = settings.HeightMm;
        UseLogo = settings.UseLogo;
        LogoPath = settings.LogoPath;
        HeaderFontSize = settings.HeaderFontSize;
        BodyFontSize = settings.BodyFontSize;

        _stickerItems.Clear();
        foreach (var item in settings.Items)
        {
            _stickerItems.Add(new StickerItem
            {
                Key = item.Key,
                X = item.X,
                Y = item.Y,
                Width = item.Width,
                Height = item.Height,
                FontSize = item.FontSize,
                DisplayText = item.DisplayText,
                IsVisible = item.IsVisible
            });
        }
        ClampAllItems();
        OnPropertyChanged(nameof(StickerItems));

        if (_stickerItems.Count == 0)
        {
            ResetLayout();
        }
        else
        {
            var qrItem = FindItem("qr");
            if (qrItem == null)
            {
                var qrSize = Math.Min(16 / PixelToMm, Math.Min(CanvasWidthPx * 0.28, CanvasHeightPx * 0.45));
                _stickerItems.Add(new StickerItem
                {
                    Key = "qr",
                    X = Math.Max(0, CanvasWidthPx - qrSize - (2 / PixelToMm)),
                    Y = Math.Max(0, (CanvasHeightPx - qrSize) / 2),
                    Width = qrSize,
                    Height = qrSize,
                    FontSize = _bodyFontSize,
                    IsVisible = _showQr,
                    DisplayText = "QR"
                });
            }
            else
            {
                if (qrItem.Width < 5 || qrItem.Height < 5)
                {
                    var qrSize = Math.Min(16 / PixelToMm, Math.Min(CanvasWidthPx * 0.28, CanvasHeightPx * 0.45));
                    qrItem.Width = qrSize;
                    qrItem.Height = qrSize;
                    if (qrItem.X + qrItem.Width > CanvasWidthPx)
                        qrItem.X = Math.Max(0, CanvasWidthPx - qrItem.Width - (2 / PixelToMm));
                    if (qrItem.Y + qrItem.Height > CanvasHeightPx)
                        qrItem.Y = Math.Max(0, (CanvasHeightPx - qrItem.Height) / 2);
                }
                qrItem.IsVisible = _showQr;
            }
            UpdateItemVisibility("cert_label", false);
            UpdateItemVisibility("cert_value", false);
            UpdateItemVisibility("qr", _showQr);
            UpdateItemText("company", _companyName);
            UpdateItemText("header", _companyHeader);
            UpdateItemText("address", _companyAddress);
            UpdateItemText("phone", _companyPhone);
            UpdateItemVisibility("brand_label", true);
            UpdateItemVisibility("brand_value", true);
            UpdateItemText("brand_value", _brand);
            UpdateItemText("model_value", _model);
            UpdateItemText("serial_value", _serial);
            UpdateItemText("cert_value", _certificateNumber);
            UpdateItemText("cal_value", _calDate.ToString("dd-MM-yyyy"));
            UpdateItemText("exp_value", _expDate.ToString("dd-MM-yyyy"));
            UpdateQrContent();
        }

        SelectedStickerItem = _stickerItems.Count > 0 ? _stickerItems[0] : null;
    }

    void SaveLayout()
    {
        var settings = new StickerLayoutSettings
        {
            WidthMm = StickerWidthMm,
            HeightMm = StickerHeightMm,
            UseLogo = UseLogo,
            LogoPath = LogoPath,
            HeaderFontSize = HeaderFontSize,
            BodyFontSize = BodyFontSize,
            Items = _stickerItems.Select(it => new StickerItemSettings
            {
                Key = it.Key,
                X = it.X,
                Y = it.Y,
                Width = it.Width,
                Height = it.Height,
                FontSize = it.FontSize,
                IsVisible = it.IsVisible,
                DisplayText = it.DisplayText,
                FontFamily = it.FontFamily,
                FontColor = it.FontColor,
                TextAlignment = it.TextAlignment,
                IsBold = it.IsBold,
                IsItalic = it.IsItalic,
                IsUnderline = it.IsUnderline,
                VariableBinding = it.VariableBinding,
                ZIndex = it.ZIndex
            }).ToList()
        };
        _store.Save(settings);
    }

    void ResetLayout()
    {
        var defaultSettings = _store.CreateDefaultSettings();

        StickerWidthMm = defaultSettings.WidthMm;
        StickerHeightMm = defaultSettings.HeightMm;
        UseLogo = defaultSettings.UseLogo;
        LogoPath = defaultSettings.LogoPath;
        HeaderFontSize = defaultSettings.HeaderFontSize;
        BodyFontSize = defaultSettings.BodyFontSize;

        _stickerItems.Clear();
        foreach (var item in defaultSettings.Items)
        {
            _stickerItems.Add(new StickerItem
            {
                Key = item.Key,
                X = item.X,
                Y = item.Y,
                Width = item.Width,
                Height = item.Height,
                FontSize = item.FontSize,
                DisplayText = item.DisplayText,
                IsVisible = item.IsVisible,
                FontFamily = item.FontFamily,
                FontColor = item.FontColor,
                TextAlignment = item.TextAlignment,
                IsBold = item.IsBold,
                IsItalic = item.IsItalic,
                IsUnderline = item.IsUnderline,
                VariableBinding = item.VariableBinding,
                ZIndex = item.ZIndex
            });
        }

        UpdateItemText("company", _companyName);
        UpdateItemText("header", _companyHeader);
        UpdateItemText("address", _companyAddress);
        UpdateItemText("phone", _companyPhone);
        UpdateItemVisibility("brand_label", true);
        UpdateItemVisibility("brand_value", true);
        UpdateItemText("brand_value", _brand);
        UpdateItemText("model_value", _model);
        UpdateItemText("serial_value", _serial);
        UpdateItemText("cert_value", _certificateNumber);
        UpdateItemText("cal_value", _calDate.ToString("dd-MM-yyyy"));
        UpdateItemText("exp_value", _expDate.ToString("dd-MM-yyyy"));
        UpdateQrContent();

        StrictLayout();

        SelectedStickerItem = _stickerItems.Count > 0 ? _stickerItems[0] : null;
        OnPropertyChanged(nameof(StickerItems));
    }

    public void UpdateItemText(string key, string? text)
    {
        var it = FindItem(key);
        if (it != null)
        {
            it.DisplayText = text ?? "";
        }
    }

    void UpdateItemFontSize(string key, double size)
    {
        var it = FindItem(key);
        if (it != null) 
        {
            it.FontSize = size;
        }
    }

    public void UpdateItemVisibility(string key, bool isVisible)
    {
        var it = FindItem(key);
        if (it != null)
        {
            it.IsVisible = isVisible;
        }
    }

    public void ApplyVariableBindings()
    {
        foreach (var item in _stickerItems)
        {
            if (!string.IsNullOrWhiteSpace(item.VariableBinding))
            {
                string value = item.VariableBinding switch
                {
                    "brand" => _brand,
                    "model" => _model,
                    "serial" => _serial,
                    "cert_value" => _certificateNumber,
                    "cal_value" => _calDate.ToString("dd-MM-yyyy"),
                    "exp_value" => _expDate.ToString("dd-MM-yyyy"),
                    _ => item.DisplayText
                };
                item.DisplayText = value;
            }
        }
    }

    public void UpdateQrContent()
        {
            var qrItem = FindItem("qr");
        if (qrItem == null) return;
        
        // Construct payload compatible with the system
        var payload = $"{_companyName}\n{_brand}\n{_model}\n{_serial}\n{_expDate:dd-MM-yyyy}\n{_companyPhone}";
        qrItem.DisplayText = payload;
        QrDebugInfo = $"QR Item Debug: DisplayText='{qrItem.DisplayText}', X={qrItem.X}, Y={qrItem.Y}, Width={qrItem.Width}, Height={qrItem.Height}, IsVisible={qrItem.IsVisible}";
    }

    public RelayCommand GenerateAndSaveQrCodeCommand { get; }

    private void GenerateAndSaveQrCode()
    {
        var qrItem = FindItem("qr");
        if (qrItem == null)
        {
            System.Windows.MessageBox.Show("QR item not found in sticker layout.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        // Construct payload including company name/acronym
        var payload = $"{_companyName}\n{_brand}\n{_model}\n{_serial}\n{_expDate:dd-MM-yyyy}\n{_companyPhone}";

        try
        {
            var converter = new Converters.QrCodeConverter();
            var qrImage = converter.Convert(payload, typeof(System.Windows.Media.ImageSource), null!, System.Globalization.CultureInfo.CurrentCulture) as BitmapSource;

            if (qrImage == null)
            {
                System.Windows.MessageBox.Show("Failed to generate QR code image.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp",
                FileName = $"QR_Code_{_serial}.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder(); // Default to PNG
                    if (saveFileDialog.FilterIndex == 2) // JPEG
                    {
                        encoder = new JpegBitmapEncoder();
                    }
                    else if (saveFileDialog.FilterIndex == 3) // BMP
                    {
                        encoder = new BmpBitmapEncoder();
                    }
                    encoder.Frames.Add(BitmapFrame.Create(qrImage));
                    encoder.Save(fileStream);
                }
                System.Windows.MessageBox.Show($"QR Code saved to {saveFileDialog.FileName}", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error generating or saving QR code: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }


    public void StrictLayout()
    {
        var mm2px = 1.0 / PixelToMm;
        var left = 2 * mm2px;
        var right = 2 * mm2px;
        var top = 2.0 * mm2px; // Increased top margin from 1.0 to 2.0
        var bottom = 1.0 * mm2px;
        var contentWidth = Math.Max(0.0, CanvasWidthPx - left - right);
        var lineGap = 1.0 * mm2px;
        var rowStep = 4.8 * mm2px; 
        var bodyFont = Math.Max(10.0, _bodyFontSize);
        var labelFont = Math.Max(10.0, bodyFont);
        var valueFont = Math.Max(10.0, bodyFont);
        var titleFont = Math.Max(bodyFont * 1.2, _headerFontSize);
        var headerFont = titleFont;
        var headerH = Math.Max(20.0, headerFont * 1.5); // Increased height to prevent truncation
        var bodyH = Math.Max(12.0, bodyFont * 1.4); // Slightly more height for body items too

        var header = EnsureItem("header");
        var brandValue = EnsureItem("brand_value");
        var modelValue = EnsureItem("model_value");
        var serialValue = EnsureItem("serial_value");
        var calValue = EnsureItem("cal_value");
        var expValue = EnsureItem("exp_value");
        var address = EnsureItem("address");
        var phone = EnsureItem("phone");
        var qr = EnsureItem("qr");
        var brandLabel = EnsureItem("brand_label");
        var modelLabel = EnsureItem("model_label");
        var serialLabel = EnsureItem("serial_label");
        var calLabel = EnsureItem("cal_label");
        var expLabel = EnsureItem("exp_label");
        var lineTop = FindItem("line_top") ?? EnsureItem("line_top");
        var lineBottom = FindItem("line_bottom") ?? EnsureItem("line_bottom");

        // Visibility settings
        UpdateItemVisibility("brand_label", true);
        UpdateItemVisibility("brand_value", true);
        UpdateItemVisibility("model_label", true);
        UpdateItemVisibility("serial_label", true);
        UpdateItemVisibility("cal_label", true);
        UpdateItemVisibility("exp_label", true);
        UpdateItemVisibility("cert_label", false);
        UpdateItemVisibility("cert_value", false);
        UpdateItemVisibility("qr", _showQr);
        UpdateItemVisibility("address", true);
        UpdateItemVisibility("phone", true);
        UpdateItemVisibility("company", false);
        UpdateItemVisibility("header", true);

        // Header - DO NOT OVERWRITE DisplayText here, it should come from _companyHeader or loaded settings
        // header.DisplayText = "High accuracy"; // REMOVED: This was overwriting user input
        header.FontSize = headerFont;
        header.X = left;
        header.Y = top;
        header.Width = contentWidth;
        header.Height = headerH;

        // Top line
        lineTop.X = left;
        lineTop.Y = header.Y + header.Height + (0.5 * mm2px);
        lineTop.Width = contentWidth;
        lineTop.Height = Math.Max(1.0, 0.5 * mm2px);
        lineTop.IsVisible = true;

        var qrSize = Math.Min(16 * mm2px, Math.Min(CanvasWidthPx * 0.28, CanvasHeightPx * 0.45));
        qr.Width = qrSize;
        qr.Height = qrSize;
        qr.X = CanvasWidthPx - right - qr.Width - (1 * mm2px);

        var reservedForQr = qr.Width + (3 * mm2px);
        var availableForText = Math.Max(0.0, contentWidth - reservedForQr);
        var labelColWidth = Math.Max(18 * mm2px, availableForText * 0.4);
        var valueColX = left + labelColWidth + (0.5 * mm2px);
        var valueColWidth = Math.Max(0.0, contentWidth - valueColX - (1 * mm2px)); // Maximize value width

        brandLabel.DisplayText = "Brand :";
        modelLabel.DisplayText = "Model :";
        serialLabel.DisplayText = "Serial :";
        calLabel.DisplayText = "Cal Date :";
        expLabel.DisplayText = "Valid until :";
        
        brandValue.DisplayText = _brand ?? "";
        modelValue.DisplayText = _model ?? "";
        serialValue.DisplayText = _serial ?? "";
        calValue.DisplayText = _calDate.ToString("dd-MM-yyyy");
        expValue.DisplayText = _expDate.ToString("dd-MM-yyyy");

        var y = lineTop.Y + lineTop.Height + (1.5 * mm2px);
        
        // QR centered vertically with the rows
        qr.Y = y + (rowStep * 0.2);

        // Rows
        var items = new[] { 
            (brandLabel, brandValue),
            (modelLabel, modelValue), 
            (serialLabel, serialValue), 
            (calLabel, calValue), 
            (expLabel, expValue) 
        };

        rowStep = 4.5 * mm2px; // Slightly smaller step for 5 rows
        bodyFont = Math.Max(10.0, _bodyFontSize * 0.95);

        foreach (var (lbl, val) in items)
        {
            lbl.X = left + (1 * mm2px);
            lbl.Y = y;
            lbl.Width = labelColWidth;
            lbl.Height = bodyH;
            lbl.FontSize = bodyFont;
            
            val.X = valueColX;
            val.Y = y;
            val.Width = valueColWidth;
            val.Height = bodyH;
            val.FontSize = bodyFont;
            
            y += rowStep;
        }

        // Bottom line
        lineBottom.X = left;
        lineBottom.Y = y + (0.5 * mm2px);
        lineBottom.Width = contentWidth;
        lineBottom.Height = Math.Max(1.0, 0.5 * mm2px);
        lineBottom.IsVisible = true;

        // Footer
        address.DisplayText = _companyAddress ?? "Address line";
        phone.DisplayText = $"Phone: {_companyPhone ?? ""}";

        address.X = 0;
        address.Width = CanvasWidthPx;
        address.Height = bodyH;
        address.FontSize = bodyFont * 0.9;
        address.Y = lineBottom.Y + (2.5 * mm2px);
        
        phone.X = 0;
        phone.Width = CanvasWidthPx;
        phone.Height = bodyH;
        phone.FontSize = bodyFont * 0.85; // Slightly smaller to be safe
        phone.Y = address.Y + (bodyH * 2.2); // Much larger gap

        ClampAllItems();
        OnPropertyChanged(nameof(StickerItems));
    }

    StickerItem EnsureItem(string key)
    {
        var it = FindItem(key);
        if (it != null) return it;
        var fontSize = key is "company" or "header" ? _headerFontSize : _bodyFontSize;
        it = new StickerItem
        {
            Key = key,
            FontSize = fontSize,
            IsVisible = true,
            Width = CanvasWidthPx * 0.8,
            Height = fontSize * 1.6
        };
        _stickerItems.Add(it);
        return it;
    }

    public StickerItem? FindItem(string key)
    {
        foreach (var it in _stickerItems)
            if (string.Equals(it.Key, key, StringComparison.OrdinalIgnoreCase))
                return it;
        return null;
    }

    void ClampAllItems()
    {
        foreach (var item in _stickerItems)
        {
            var itemWidth = Math.Max(0.0, item.Width);
            var itemHeight = Math.Max(0.0, item.Height);

            // Clamp item dimensions first
            item.Width = Math.Min(itemWidth, CanvasWidthPx);
            item.Height = Math.Min(itemHeight, CanvasHeightPx);

            // Recalculate itemWidth and itemHeight after clamping dimensions
            itemWidth = item.Width;
            itemHeight = item.Height;

            var maxX = Math.Max(0.0, CanvasWidthPx - itemWidth);
            var maxY = Math.Max(0.0, CanvasHeightPx - itemHeight);

            item.X = Math.Max(0.0, Math.Min(item.X, maxX));
            item.Y = Math.Max(0.0, Math.Min(item.Y, maxY));
        }
    }

    void SetItem(string key, double x, double y, double width, double height, double fontSize)
    {
        var it = FindItem(key);
        if (it == null) return;
        it.X = x;
        it.Y = y;
        it.Width = width;
        it.Height = height;
        it.FontSize = fontSize;
        it.IsVisible = true;
    }

    void AutoLayout()
    {
        var defaultSettings = _store.CreateDefaultSettings();
        foreach (var itemSetting in defaultSettings.Items)
        {
            SetItem(itemSetting.Key, itemSetting.X, itemSetting.Y, itemSetting.Width, itemSetting.Height, itemSetting.FontSize);
            UpdateItemVisibility(itemSetting.Key, itemSetting.IsVisible);
        }
        ClampAllItems();
    }
}

public sealed class StickerItem : ViewModelBase
{
    string _key = "";
    string _displayText = "";
    double _x;
    double _y;
    double _fontSize = 12;
    bool _isSelected;
    bool _isVisible = true;
    double _width;
    double _height;
    string _fontFamily = "Segoe UI";
    string _fontColor = "#000000";
    string _textAlignment = "Right";
    bool _isBold = false;
    bool _isItalic = false;
    bool _isUnderline = false;
    string _variableBinding = "";
    int _zIndex = 0;

    public void NotifyChanges()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(XMm));
        OnPropertyChanged(nameof(YMm));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(WidthMm));
        OnPropertyChanged(nameof(HeightMm));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(IsVisible));
    }

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value ?? "");
    }

    public string DisplayText
    {
        get => _displayText;
        set => SetProperty(ref _displayText, value ?? "");
    }

    public double XMm
    {
        get => Math.Round(_x * 0.264583, 2);
        set
        {
            var px = value / 0.264583;
            if (SetProperty(ref _x, px))
            {
                OnPropertyChanged(nameof(X));
                OnPropertyChanged(nameof(XMm));
            }
        }
    }

    public double YMm
    {
        get => Math.Round(_y * 0.264583, 2);
        set
        {
            var px = value / 0.264583;
            if (SetProperty(ref _y, px))
            {
                OnPropertyChanged(nameof(Y));
                OnPropertyChanged(nameof(YMm));
            }
        }
    }

    public double X
    {
        get => _x;
        set
        {
            if (SetProperty(ref _x, value))
            {
                OnPropertyChanged(nameof(XMm));
                OnPropertyChanged(nameof(X));
            }
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (SetProperty(ref _y, value))
            {
                OnPropertyChanged(nameof(YMm));
                OnPropertyChanged(nameof(Y));
            }
        }
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, Math.Max(0, value));
    }

    public double WidthMm
    {
        get => Math.Round(_width * 0.264583, 2);
        set
        {
            var px = value / 0.264583;
            if (SetProperty(ref _width, Math.Max(0, px)))
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(WidthMm));
            }
        }
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, Math.Max(0, value));
    }

    public double HeightMm
    {
        get => Math.Round(_height * 0.264583, 2);
        set
        {
            var px = value / 0.264583;
            if (SetProperty(ref _height, Math.Max(0, px)))
            {
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(HeightMm));
            }
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, Math.Max(8, Math.Min(300, value)));
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value ?? "Segoe UI");
    }

    public string FontColor
    {
        get => _fontColor;
        set => SetProperty(ref _fontColor, value ?? "#000000");
    }

    public string TextAlignment
    {
        get => _textAlignment;
        set => SetProperty(ref _textAlignment, value ?? "Right");
    }

    public bool IsBold
    {
        get => _isBold;
        set => SetProperty(ref _isBold, value);
    }

    public bool IsItalic
    {
        get => _isItalic;
        set => SetProperty(ref _isItalic, value);
    }

    public bool IsUnderline
    {
        get => _isUnderline;
        set => SetProperty(ref _isUnderline, value);
    }

    public string VariableBinding
    {
        get => _variableBinding;
        set => SetProperty(ref _variableBinding, value ?? "");
    }

    public int ZIndex
    {
        get => _zIndex;
        set => SetProperty(ref _zIndex, value);
    }
}
