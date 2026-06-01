using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;
using BlueMax.Infrastructure;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class MaintenanceViewModel : ViewModelBase
{
    string _customerName = "";
    string _customerPhone = "";
    string _deviceType = "";
    string _brand = "";
    string _model = "";
    string _serialNumber = "";
    string _serialNumber2 = "";
    string _accessories = "";
    string _complaint = "";
    string _technicalReport = "";
    string _status = "Open";
    string _originalStatus = "Open";
    decimal _partsCost;
    decimal _laborCost;
    decimal _totalCost;
    DateTime _receivedDate = DateTime.Today;
    DateTime _updatedDate = DateTime.Today;
    string _searchText = "";
    string _receiptGroupNumber = "";
    WorkOrderItem? _selectedWorkOrder;
    Task? _ensureDbCreatedTask;
    bool _isEditModeForSelected;

    public MaintenanceViewModel()
    {
        WorkOrders = new ObservableCollection<WorkOrderItem>();
        StatusOptions = new ObservableCollection<string> { "Open", "In Progress", "Completed", "Delivered", "Rejected" };
        DeviceTypes = new ObservableCollection<string> { "Total Station", "Auto Level", "GPS" };
        BrandOptions = new ObservableCollection<string> { "Leica", "Trimble", "Sokkia", "Topcon", "South", "CHC", "Kolida", "Stonex", "Ruide", "Sanding", "Hi-Target", "Foif", "Gowin", "CST/berger", "Nikon", "Pentax", "Spectra", "Geomax", "مختلف الاجهزة الصينيه" };

        NewWorkOrderCommand = new RelayCommand(_ => ClearInputs());
        EditWorkOrderCommand = new RelayCommand(_ => BeginEditSelectedWorkOrder(), _ => SelectedWorkOrder != null);
        SaveWorkOrderCommand = new RelayCommand(_ => SaveWorkOrder(), _ => CanSaveWorkOrder());
        AddDeviceToSameReceiptCommand = new RelayCommand(_ => AddDeviceToSameReceipt(), _ => !string.IsNullOrWhiteSpace(CustomerName) && (!string.IsNullOrWhiteSpace(ReceiptGroupNumber) || SelectedWorkOrder != null));
        DeleteWorkOrderCommand = new RelayCommand(_ => DeleteWorkOrder(), _ => SelectedWorkOrder != null);
        RefreshWorkOrdersCommand = new RelayCommand(_ => RefreshWorkOrders());
        GenerateQuotationCommand = new RelayCommand(_ => GenerateQuotation(), _ => SelectedWorkOrder != null);
        GenerateInvoiceCommand = new RelayCommand(_ => GenerateInvoice(), _ => SelectedWorkOrder != null);
        GenerateMaintenanceReportCommand = new RelayCommand(_ => GenerateMaintenanceReport(), _ => SelectedWorkOrder != null);
        PreviewWorkOrderCommand = new RelayCommand(_ => PreviewWorkOrder(), _ => CanPreviewWorkOrder());
        PrintReceiptCommand = new RelayCommand(_ => PrintReceipt(), _ => CanPrintReceipt());
        OpenReceiptStickerSettingsCommand = new RelayCommand(_ => OpenReceiptStickerSettings());
        PrintReceiptStickerCommand = new RelayCommand(_ => PrintReceiptSticker());
        try
        {
            _ = EnsureDbCreatedOnceAsync();
            RefreshWorkOrders();
        }
        catch { }
    }

    public ObservableCollection<WorkOrderItem> WorkOrders { get; }
    public ObservableCollection<string> StatusOptions { get; }
    public ObservableCollection<string> DeviceTypes { get; }
    public ObservableCollection<string> BrandOptions { get; }

    public WorkOrderItem? SelectedWorkOrder
    {
        get => _selectedWorkOrder;
        set
        {
            if (!SetProperty(ref _selectedWorkOrder, value))
                return;
            if (value == null)
            {
                ClearInputs();
                return;
            }
            _isEditModeForSelected = false;
            CustomerName = value.CustomerName;
            CustomerPhone = value.CustomerPhone;
            DeviceType = value.DeviceType;
            Brand = value.Brand;
            Model = value.Model;
            SerialNumber = value.SerialNumber;
            SerialNumber2 = value.SerialNumber2;
            Accessories = value.Accessories;
            Complaint = value.Complaint;
            TechnicalReport = value.TechnicalReport;
            Status = value.Status;
            _originalStatus = value.Status ?? "";
            PartsCost = value.PartsCost;
            LaborCost = value.LaborCost;
            ReceivedDate = value.ReceivedDate;
            UpdatedDate = value.UpdatedDate;
            DeleteWorkOrderCommand.RaiseCanExecuteChanged();
            EditWorkOrderCommand.RaiseCanExecuteChanged();
            GenerateQuotationCommand.RaiseCanExecuteChanged();
            GenerateInvoiceCommand.RaiseCanExecuteChanged();
            GenerateMaintenanceReportCommand.RaiseCanExecuteChanged();
            PreviewWorkOrderCommand.RaiseCanExecuteChanged();
            PrintReceiptCommand.RaiseCanExecuteChanged();
            OpenReceiptStickerSettingsCommand.RaiseCanExecuteChanged();
            PrintReceiptStickerCommand.RaiseCanExecuteChanged();
            SaveWorkOrderCommand.RaiseCanExecuteChanged();
            AddDeviceToSameReceiptCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsInputsLocked));
            OnPropertyChanged(nameof(IsInputsEditable));
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
            
            try
            {
                var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                
                // Try to add each column separately to avoid errors if they already exist
                var columnsToAdd = new[]
                {
                    "ALTER TABLE WorkOrders ADD COLUMN ReceiptGroupNumber TEXT DEFAULT '';",
                    "ALTER TABLE WorkOrders ADD COLUMN Brand TEXT DEFAULT '';",
                    "ALTER TABLE WorkOrders ADD COLUMN SerialNumber2 TEXT DEFAULT '';"
                };
                
                foreach (var sql in columnsToAdd)
                {
                    try
                    {
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        // Column probably already exists, ignore
                    }
                }
            }
            catch
            {
                // Column probably already exists or not supported, ignore
            }
        });
        return _ensureDbCreatedTask;
    }

    static string NormalizePhone(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? "";
        var text = value ?? "";
        var buffer = new char[text.Length];
        var len = 0;
        foreach (var ch in text)
        {
            var mapped = ch;
            if (ch >= '\u0660' && ch <= '\u0669')
                mapped = (char)('0' + (ch - '\u0660'));
            else if (ch >= '\u06F0' && ch <= '\u06F9')
                mapped = (char)('0' + (ch - '\u06F0'));

            if (char.IsDigit(mapped) || mapped == '+' || mapped == ' ' || mapped == '-' || mapped == '(' || mapped == ')')
                buffer[len++] = mapped;
        }
        return new string(buffer, 0, len);
    }

    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (!SetProperty(ref _customerName, value ?? ""))
                return;
            SaveWorkOrderCommand.RaiseCanExecuteChanged();
            PrintReceiptCommand.RaiseCanExecuteChanged();
            PreviewWorkOrderCommand.RaiseCanExecuteChanged();
        }
    }

    public string CustomerPhone
    {
        get => _customerPhone;
        set => SetProperty(ref _customerPhone, NormalizePhone(value));
    }

    public string DeviceType
    {
        get => _deviceType;
        set
        {
            if (!SetProperty(ref _deviceType, value ?? ""))
                return;
            SaveWorkOrderCommand.RaiseCanExecuteChanged();
            PrintReceiptCommand.RaiseCanExecuteChanged();
            PreviewWorkOrderCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsGpsDevice));
        }
    }

    public bool IsGpsDevice => DeviceType == "GPS";

    public string Brand
    {
        get => _brand;
        set => SetProperty(ref _brand, value ?? "");
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value ?? "");
    }

    public string SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(ref _serialNumber, value ?? "");
    }

    public string SerialNumber2
    {
        get => _serialNumber2;
        set => SetProperty(ref _serialNumber2, value ?? "");
    }

    public string Accessories
    {
        get => _accessories;
        set => SetProperty(ref _accessories, value ?? "");
    }

    public string Complaint
    {
        get => _complaint;
        set => SetProperty(ref _complaint, value ?? "");
    }

    public string TechnicalReport
    {
        get => _technicalReport;
        set => SetProperty(ref _technicalReport, value ?? "");
    }

    public string Status
    {
        get => _status;
        set
        {
            if (!SetProperty(ref _status, value ?? ""))
                return;
            SaveWorkOrderCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal PartsCost
    {
        get => _partsCost;
        set
        {
            if (!SetProperty(ref _partsCost, value))
                return;
            TotalCost = _partsCost + _laborCost;
        }
    }

    public decimal LaborCost
    {
        get => _laborCost;
        set
        {
            if (!SetProperty(ref _laborCost, value))
                return;
            TotalCost = _partsCost + _laborCost;
        }
    }

    public decimal TotalCost
    {
        get => _totalCost;
        private set => SetProperty(ref _totalCost, value);
    }

    public DateTime ReceivedDate
    {
        get => _receivedDate;
        set => SetProperty(ref _receivedDate, value);
    }

    public DateTime UpdatedDate
    {
        get => _updatedDate;
        set => SetProperty(ref _updatedDate, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value ?? "");
    }

    public bool IsInputsLocked => SelectedWorkOrder != null && !_isEditModeForSelected;
    public bool IsInputsEditable => !IsInputsLocked;

    public RelayCommand NewWorkOrderCommand { get; }
    public RelayCommand EditWorkOrderCommand { get; }
    public RelayCommand SaveWorkOrderCommand { get; }
    public RelayCommand AddDeviceToSameReceiptCommand { get; }
    public RelayCommand DeleteWorkOrderCommand { get; }
    public RelayCommand RefreshWorkOrdersCommand { get; }
    public RelayCommand GenerateQuotationCommand { get; }
    public RelayCommand GenerateInvoiceCommand { get; }
    public RelayCommand GenerateMaintenanceReportCommand { get; }
    public RelayCommand PreviewWorkOrderCommand { get; }
    public RelayCommand PrintReceiptCommand { get; }
    public RelayCommand OpenReceiptStickerSettingsCommand { get; }
    public RelayCommand PrintReceiptStickerCommand { get; }

    public string ReceiptGroupNumber
    {
        get => _receiptGroupNumber;
        set => SetProperty(ref _receiptGroupNumber, value);
    }

    bool CanSaveWorkOrder()
    {
        if (SelectedWorkOrder != null)
            return _isEditModeForSelected;
        return !string.IsNullOrWhiteSpace(CustomerName) && !string.IsNullOrWhiteSpace(DeviceType);
    }

    void BeginEditSelectedWorkOrder()
    {
        if (SelectedWorkOrder == null)
            return;

        _isEditModeForSelected = true;
        OnPropertyChanged(nameof(IsInputsLocked));
        OnPropertyChanged(nameof(IsInputsEditable));
        SaveWorkOrderCommand.RaiseCanExecuteChanged();
    }

    void SaveWorkOrder()
    {
        if (!CanSaveWorkOrder())
            return;

        using var db = CreateDbContext();
        db.Database.EnsureCreated();

        var typedName = (CustomerName ?? "").Trim();
        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(typedName))
        {
            customer = db.Customers.FirstOrDefault(c => c.Name == typedName);
            if (customer == null)
            {
                customer = new Customer
                {
                    CreatedAt = DateTime.Now,
                    Name = typedName,
                    Phone = CustomerPhone ?? ""
                };
                db.Customers.Add(customer);
                db.SaveChanges();
            }
        }

        // Generate a new receipt group number if this is a new session and we don't have one
        if (string.IsNullOrWhiteSpace(ReceiptGroupNumber))
        {
            ReceiptGroupNumber = DocumentSequenceStore.GetNextFormatted("MaintenanceReceipt");
        }

        WorkOrder workOrder;
        if (SelectedWorkOrder != null)
        {
            workOrder = db.WorkOrders.FirstOrDefault(w => w.Id == SelectedWorkOrder.Id) ?? new WorkOrder();
        }
        else
        {
            workOrder = new WorkOrder { ReceivedDate = DateTime.Now };
            db.WorkOrders.Add(workOrder);
        }

        workOrder.CustomerId = customer?.Id;
        workOrder.CustomerName = customer?.Name ?? CustomerName ?? "";
        workOrder.CustomerPhone = string.IsNullOrWhiteSpace(CustomerPhone) ? (customer?.Phone ?? "") : (CustomerPhone ?? "");
        workOrder.ReceiptGroupNumber = ReceiptGroupNumber ?? "";
        workOrder.DeviceType = DeviceType ?? "";
        workOrder.Brand = Brand ?? "";
        workOrder.Model = Model ?? "";
        workOrder.SerialNumber = SerialNumber ?? "";
        workOrder.SerialNumber2 = SerialNumber2 ?? "";
        workOrder.Accessories = Accessories ?? "";
        workOrder.Complaint = Complaint;
        workOrder.TechnicalReport = TechnicalReport;
        workOrder.Status = Status;
        workOrder.PartsCost = PartsCost;
        workOrder.LaborCost = LaborCost;
        workOrder.TotalCost = PartsCost + LaborCost;
        workOrder.UpdatedDate = DateTime.Now;

        db.SaveChanges();

        SyncWorkOrderToCertificate(db, workOrder);
        db.SaveChanges();

        var savedId = workOrder.Id;
        RefreshWorkOrders();
        
        SelectedWorkOrder = WorkOrders.FirstOrDefault(w => w.Id == savedId);
        _isEditModeForSelected = false;
        _originalStatus = Status ?? "";
        SaveWorkOrderCommand.RaiseCanExecuteChanged();
        AddDeviceToSameReceiptCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(IsInputsLocked));
        OnPropertyChanged(nameof(IsInputsEditable));
    }

    void AddDeviceToSameReceipt()
    {
        // 1. Capture everything we want to copy
        var currentCustomerName = CustomerName ?? "";
        var currentCustomerPhone = CustomerPhone ?? "";
        var currentDeviceType = DeviceType ?? "";
        var currentBrand = Brand ?? "";
        var currentModel = Model ?? "";
        var currentSerialNumber = SerialNumber ?? "";
        var currentSerialNumber2 = SerialNumber2 ?? "";
        var currentAccessories = Accessories ?? "";
        var currentComplaint = Complaint ?? "";
        var currentGroupNumber = ReceiptGroupNumber ?? "";
        
        if (string.IsNullOrWhiteSpace(currentGroupNumber) && SelectedWorkOrder != null)
        {
            currentGroupNumber = SelectedWorkOrder.ReceiptGroupNumber ?? "";
        }
        
        if (string.IsNullOrWhiteSpace(currentGroupNumber))
        {
            currentGroupNumber = DocumentSequenceStore.GetNextFormatted("MaintenanceReceipt");
        }
        
        // 2. Disconnect from the selected work order so the next "Save" inserts a new record.
        // NOTE: Setting SelectedWorkOrder to null will trigger ClearInputs() which wipes the fields.
        SelectedWorkOrder = null;
        _isEditModeForSelected = false;
        
        // 3. Restore the copied data into the UI
        CustomerName = currentCustomerName;
        CustomerPhone = currentCustomerPhone;
        DeviceType = currentDeviceType;
        Brand = currentBrand;
        Model = currentModel;
        SerialNumber = currentSerialNumber;
        SerialNumber2 = currentSerialNumber2;
        Accessories = currentAccessories;
        Complaint = currentComplaint;
        
        // Force the same receipt group number
        ReceiptGroupNumber = currentGroupNumber;
        
        // Unlock the inputs so the user can change the Model or Serial Number
        OnPropertyChanged(nameof(IsInputsLocked));
        OnPropertyChanged(nameof(IsInputsEditable));
        SaveWorkOrderCommand.RaiseCanExecuteChanged();
        
        System.Windows.MessageBox.Show("تم استنساخ البيانات! قم بتغيير (الجهاز/الموديل/السيريال) حسب الحاجة، ثم اضغط على زر [حفظ] ليتم إضافته كجهاز جديد لنفس الإيصال.", "جهاز جديد لنفس العميل", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    static void SyncWorkOrderToCertificate(AppDbContext db, WorkOrder workOrder)
    {
        // Use a unique temporary marker to find if this work order already has a certificate
        // but don't use WO- prefix to decouple from work order after initial export
        var tempMarker = $"TEMP-WO-{workOrder.Id:D6}";
        var certificate = db.Certificates.FirstOrDefault(c => c.CertificateNumber == tempMarker);
        var isNew = certificate == null;

        if (certificate == null)
        {
            certificate = new Certificate
            {
                CertificateNumber = tempMarker, // Temporary marker, will be replaced with HAT number on first save
                CreatedAt = DateTime.Now,
                IssueDate = DateTime.Today,
                ExpiryDate = DateTime.Today.AddMonths(6)
            };
            db.Certificates.Add(certificate);
        }

        certificate.ClientName = workOrder.CustomerName ?? "";
        certificate.Phone = workOrder.CustomerPhone ?? "";
        certificate.DeviceType = workOrder.DeviceType ?? "";
        certificate.Model = workOrder.Model ?? "";
        certificate.SerialText = workOrder.SerialNumber ?? "";
        
        // Always set SpecValue to a proper default, never leave status text like 'In Progress'
        if (string.Equals(workOrder.DeviceType, "Total Station", StringComparison.OrdinalIgnoreCase))
            certificate.SpecValue = "1\""; // Default accuracy instead of 'In Progress'
        else
            certificate.SpecValue = "";

        // Always use Today's date when syncing so it reflects the actual printing/finishing date
        certificate.IssueDate = DateTime.Today;
        certificate.ExpiryDate = DateTime.Today.AddMonths(6);

        if (isNew)
            certificate.CreatedAt = DateTime.Now;
    }

    void DeleteWorkOrder()
    {
        if (SelectedWorkOrder == null)
            return;
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        var entity = db.WorkOrders.FirstOrDefault(w => w.Id == SelectedWorkOrder.Id);
        if (entity != null)
        {
            db.WorkOrders.Remove(entity);
            db.SaveChanges();
        }
        RefreshWorkOrders();
        ClearInputs();
    }

    void RefreshWorkOrders()
    {
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        var query = db.WorkOrders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var text = SearchText.Trim();
            query = query.Where(w => w.CustomerName.Contains(text) || w.SerialNumber.Contains(text));
        }

        var items = query
            .OrderByDescending(w => w.ReceivedDate)
            .Take(300)
            .Select(w => new WorkOrderItem
            {
                Id = w.Id,
                CustomerName = w.CustomerName,
                CustomerPhone = w.CustomerPhone,
                DeviceType = w.DeviceType,
                Brand = w.Brand,
                Model = w.Model,
                SerialNumber = w.SerialNumber,
                SerialNumber2 = w.SerialNumber2,
                Accessories = w.Accessories,
                Complaint = w.Complaint,
                TechnicalReport = w.TechnicalReport,
                Status = w.Status,
                ReceiptGroupNumber = w.ReceiptGroupNumber,
                PartsCost = w.PartsCost,
                LaborCost = w.LaborCost,
                TotalCost = w.TotalCost,
                ReceivedDate = w.ReceivedDate,
                UpdatedDate = w.UpdatedDate
            })
            .ToList();

        WorkOrders.Clear();
        foreach (var item in items)
            WorkOrders.Add(item);
    }

    void ClearInputs()
    {
        CustomerName = "";
        CustomerPhone = "";
        DeviceType = "";
        Brand = "";
        Model = "";
        SerialNumber = "";
        SerialNumber2 = "";
        Accessories = "";
        Complaint = "";
        TechnicalReport = "";
        Status = "Open";
        _originalStatus = "Open";
        ReceiptGroupNumber = "";
        PartsCost = 0;
        LaborCost = 0;
        ReceivedDate = DateTime.Today;
        UpdatedDate = DateTime.Today;
        SelectedWorkOrder = null;
        _isEditModeForSelected = false;
        DeleteWorkOrderCommand.RaiseCanExecuteChanged();
        EditWorkOrderCommand.RaiseCanExecuteChanged();
        GenerateQuotationCommand.RaiseCanExecuteChanged();
        GenerateInvoiceCommand.RaiseCanExecuteChanged();
        GenerateMaintenanceReportCommand.RaiseCanExecuteChanged();
        PreviewWorkOrderCommand.RaiseCanExecuteChanged();
        PrintReceiptCommand.RaiseCanExecuteChanged();
        OpenReceiptStickerSettingsCommand.RaiseCanExecuteChanged();
        PrintReceiptStickerCommand.RaiseCanExecuteChanged();
        SaveWorkOrderCommand.RaiseCanExecuteChanged();
        AddDeviceToSameReceiptCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(IsInputsLocked));
        OnPropertyChanged(nameof(IsInputsEditable));
    }

    static AppDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext();
    }

    async void GenerateQuotation()
    {
        if (SelectedWorkOrder == null)
            return;
        var templatePath = await System.Threading.Tasks.Task.Run(() => new DocumentPathMappingStore().GetPath("price_quotation"));
        templatePath = ResolveTemplatePath(templatePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            MessageBox.Show(
                "يرجى تحديد قالب Word لهذا المستند من شاشة إدارة القوالب أولاً",
                "Missing template",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string? outputPath = null;
        try
        {
            await EnsureDbCreatedOnceAsync();
            var outputDir = Path.Combine(AppContext.BaseDirectory, "Reports_Output");
            var engine = new WordTemplateEngine("", outputDir);
            using var db = CreateDbContext();
            var docService = new CertificateDocumentService(db, engine);
            var result = await docService.GenerateWorkOrderReportDocxAndPdfFromPathAsync(
                SelectedWorkOrder.Id,
                "Price Quotation",
                templatePath);
            outputPath = result;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Quotation failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    async void GenerateInvoice()
    {
        if (SelectedWorkOrder == null)
            return;
        var templatePath = await System.Threading.Tasks.Task.Run(() => new DocumentPathMappingStore().GetPath("invoice"));
        templatePath = ResolveTemplatePath(templatePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            MessageBox.Show(
                "يرجى تحديد قالب Word لهذا المستند من شاشة إدارة القوالب أولاً",
                "Missing template",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string? outputPath = null;
        try
        {
            await EnsureDbCreatedOnceAsync();
            var outputDir = Path.Combine(AppContext.BaseDirectory, "Reports_Output");
            var engine = new WordTemplateEngine("", outputDir);
            using var db = CreateDbContext();
            var docService = new CertificateDocumentService(db, engine);
            var result = await docService.GenerateWorkOrderReportDocxAndPdfFromPathAsync(
                SelectedWorkOrder.Id,
                "Invoice",
                templatePath);
            outputPath = result;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invoice failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    async void GenerateMaintenanceReport()
    {
        if (SelectedWorkOrder == null)
            return;
        var templatePath = await System.Threading.Tasks.Task.Run(() => new DocumentPathMappingStore().GetPath("maintenance_report"));
        templatePath = ResolveTemplatePath(templatePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            MessageBox.Show(
                "يرجى تحديد قالب Word لهذا المستند من شاشة إدارة القوالب أولاً",
                "Missing template",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string? outputPath = null;
        try
        {
            await EnsureDbCreatedOnceAsync();
            var outputDir = Path.Combine(AppContext.BaseDirectory, "Reports_Output");
            var engine = new WordTemplateEngine("", outputDir);
            using var db = CreateDbContext();
            var docService = new CertificateDocumentService(db, engine);
            var result = await docService.GenerateWorkOrderReportDocxAndPdfFromPathAsync(
                SelectedWorkOrder.Id,
                "Maintenance Report",
                templatePath);
            outputPath = result;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Report failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
        catch
        {
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

    bool CanPreviewWorkOrder()
    {
        if (SelectedWorkOrder != null)
            return true;
        return !string.IsNullOrWhiteSpace(CustomerName) ||
               !string.IsNullOrWhiteSpace(DeviceType) ||
               !string.IsNullOrWhiteSpace(SerialNumber) ||
               !string.IsNullOrWhiteSpace(TechnicalReport) ||
               !string.IsNullOrWhiteSpace(Complaint);
    }

    void PreviewWorkOrder()
    {
        try
        {
            var item = SelectedWorkOrder ?? BuildSnapshotFromCurrent();
            var win = new BlueMax.Presentation.Wpf.Views.MaintenancePreviewWindow(item)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Preview failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    bool CanPrintReceipt()
    {
        if (SelectedWorkOrder != null)
            return !string.IsNullOrWhiteSpace(SelectedWorkOrder.CustomerName) && !string.IsNullOrWhiteSpace(SelectedWorkOrder.DeviceType);
        return !string.IsNullOrWhiteSpace(CustomerName) && !string.IsNullOrWhiteSpace(DeviceType);
    }

    void PrintReceipt()
    {
        try
        {
            var source = SelectedWorkOrder ?? BuildSnapshotFromCurrent();
            var receiptGroupNumber = source.ReceiptGroupNumber;
            
            // Generate receipt number if missing for unsaved snapshot
            if (string.IsNullOrWhiteSpace(receiptGroupNumber))
            {
                receiptGroupNumber = GetUnifiedReceiptNumber(source);
            }

            var devices = new System.Collections.Generic.List<DeviceReceiptItem>();

            // Find all devices with the same receipt group number
            if (!string.IsNullOrWhiteSpace(source.ReceiptGroupNumber))
            {
                var relatedOrders = WorkOrders.Where(w => w.ReceiptGroupNumber == source.ReceiptGroupNumber).ToList();
                foreach (var order in relatedOrders)
                {
                    devices.Add(new DeviceReceiptItem
                    {
                        DeviceType = order.DeviceType,
                        Brand = order.Brand,
                        Model = order.Model,
                        SerialNumber = order.SerialNumber,
                        SerialNumber2 = order.SerialNumber2,
                        Accessories = order.Accessories,
                        Complaint = order.Complaint
                    });
                }
            }
            
            // If the group is empty (e.g., new unsaved device), add it as the only device
            if (devices.Count == 0)
            {
                devices.Add(new DeviceReceiptItem
                {
                    DeviceType = source.DeviceType,
                    Brand = source.Brand,
                    Model = source.Model,
                    SerialNumber = source.SerialNumber,
                    SerialNumber2 = source.SerialNumber2,
                    Accessories = source.Accessories,
                    Complaint = source.Complaint
                });
            }

            var data = new MaintenanceReceiptData
            {
                ReceiptNumber = receiptGroupNumber,
                CustomerName = source.CustomerName,
                CustomerPhone = source.CustomerPhone,
                ReceivedDate = source.ReceivedDate,
                Devices = devices
            };
            
            var win = new BlueMax.Presentation.Wpf.Views.MaintenanceReceiptWindow(data)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Receipt failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void OpenReceiptStickerSettings()
    {
        try
        {
            var source = SelectedWorkOrder ?? BuildSnapshotFromCurrent();
            var receiptGroupNumber = source.ReceiptGroupNumber;
            
            // Generate receipt number if missing for unsaved snapshot
            if (string.IsNullOrWhiteSpace(receiptGroupNumber))
            {
                receiptGroupNumber = GetUnifiedReceiptNumber(source);
            }

            var devices = new System.Collections.Generic.List<DeviceReceiptItem>();

            // Find all devices with the same receipt group number
            if (!string.IsNullOrWhiteSpace(source.ReceiptGroupNumber))
            {
                var relatedOrders = WorkOrders.Where(w => w.ReceiptGroupNumber == source.ReceiptGroupNumber).ToList();
                foreach (var order in relatedOrders)
                {
                    devices.Add(new DeviceReceiptItem
                    {
                        DeviceType = order.DeviceType,
                        Brand = order.Brand,
                        Model = order.Model,
                        SerialNumber = order.SerialNumber,
                        SerialNumber2 = order.SerialNumber2,
                        Accessories = order.Accessories,
                        Complaint = order.Complaint
                    });
                }
            }
            
            // If the group is empty (e.g., new unsaved device), add it as the only device
            if (devices.Count == 0)
            {
                devices.Add(new DeviceReceiptItem
                {
                    DeviceType = source.DeviceType,
                    Brand = source.Brand,
                    Model = source.Model,
                    SerialNumber = source.SerialNumber,
                    SerialNumber2 = source.SerialNumber2,
                    Accessories = source.Accessories,
                    Complaint = source.Complaint
                });
            }

            var data = new MaintenanceReceiptData
            {
                ReceiptNumber = receiptGroupNumber,
                CustomerName = source.CustomerName,
                CustomerPhone = source.CustomerPhone,
                ReceivedDate = source.ReceivedDate,
                Devices = devices
            };
            var win = new BlueMax.Presentation.Wpf.Views.MaintenanceReceiptStickerWindow(data)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Receipt sticker failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void PrintReceiptSticker()
    {
        OpenReceiptStickerSettings();
    }

    static string GetUnifiedReceiptNumber(WorkOrderItem source)
    {
        if (source.Id > 0)
            return source.Id.ToString("D4");
        return DocumentSequenceStore.GetNextFormatted("MaintenanceReceipt");
    }

    WorkOrderItem BuildSnapshotFromCurrent()
    {
        var item = new WorkOrderItem
        {
            Id = SelectedWorkOrder?.Id ?? 0,
            CustomerName = CustomerName,
            CustomerPhone = CustomerPhone,
            DeviceType = DeviceType,
            Brand = Brand,
            Model = Model,
            SerialNumber = SerialNumber,
            SerialNumber2 = SerialNumber2,
            Accessories = Accessories,
            Complaint = Complaint,
            TechnicalReport = TechnicalReport,
            Status = Status,
            ReceiptGroupNumber = ReceiptGroupNumber,
            PartsCost = PartsCost,
            LaborCost = LaborCost,
            TotalCost = TotalCost,
            ReceivedDate = ReceivedDate,
            UpdatedDate = UpdatedDate
        };
        return item;
    }

    public sealed class MaintenanceReceiptData
    {
        public string ReceiptNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public DateTime ReceivedDate { get; set; }
        public System.Collections.Generic.List<DeviceReceiptItem> Devices { get; set; } = new();
    }

    public sealed class DeviceReceiptItem
    {
        public string DeviceType { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string SerialNumber2 { get; set; } = "";
        public string Accessories { get; set; } = "";
        public string Complaint { get; set; } = "";
    }
}

public sealed class WorkOrderItem : ViewModelBase
{
    int _id;
    string _customerName = "";
    string _customerPhone = "";
    string _deviceType = "";
    string _brand = "";
    string _model = "";
    string _serialNumber = "";
    string _serialNumber2 = "";
    string _accessories = "";
    string _complaint = "";
    string _technicalReport = "";
    string _status = "";
    string _receiptGroupNumber = "";
    decimal _partsCost;
    decimal _laborCost;
    decimal _totalCost;
    DateTime _receivedDate;
    DateTime _updatedDate;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public string CustomerPhone
    {
        get => _customerPhone;
        set => SetProperty(ref _customerPhone, value);
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

    public string SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(ref _serialNumber, value);
    }

    public string SerialNumber2
    {
        get => _serialNumber2;
        set => SetProperty(ref _serialNumber2, value);
    }

    public string Accessories
    {
        get => _accessories;
        set => SetProperty(ref _accessories, value);
    }

    public string Complaint
    {
        get => _complaint;
        set => SetProperty(ref _complaint, value);
    }

    public string TechnicalReport
    {
        get => _technicalReport;
        set => SetProperty(ref _technicalReport, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string ReceiptGroupNumber
    {
        get => _receiptGroupNumber;
        set => SetProperty(ref _receiptGroupNumber, value);
    }

    public decimal PartsCost
    {
        get => _partsCost;
        set => SetProperty(ref _partsCost, value);
    }

    public decimal LaborCost
    {
        get => _laborCost;
        set => SetProperty(ref _laborCost, value);
    }

    public decimal TotalCost
    {
        get => _totalCost;
        set => SetProperty(ref _totalCost, value);
    }

    public DateTime ReceivedDate
    {
        get => _receivedDate;
        set => SetProperty(ref _receivedDate, value);
    }

    public DateTime UpdatedDate
    {
        get => _updatedDate;
        set => SetProperty(ref _updatedDate, value);
    }
}
