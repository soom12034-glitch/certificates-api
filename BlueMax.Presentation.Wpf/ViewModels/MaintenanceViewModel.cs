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
using BlueMax.Presentation.Wpf.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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
    decimal _partsCost;
    decimal _laborCost;
    decimal _totalCost;
    DateTime _receivedDate = DateTime.Today;
    DateTime _updatedDate = DateTime.Today;
    string _searchText = "";
    string _receiptGroupNumber = "";
    readonly ReceiptStickerDesignerViewModel _receiptStickerDesigner;
    readonly Action _navigateToReceiptStickerDesigner;
    WorkOrderItem? _selectedWorkOrder;
    Task? _ensureDbCreatedTask;
    bool _isEditModeForSelected;
    System.Windows.Threading.DispatcherTimer? _searchDebounceTimer;
    DateTime _reportsFromDate = DateTime.Today.AddMonths(-1);
    DateTime _reportsToDate = DateTime.Today;
    bool _isReportsBusy;
    string _reportsBusyMessage = "";
    int _totalOrdersCount;
    int _inProgressCount;
    int _completedDeliveredCount;
    decimal _reportsTotalIncome;
    decimal _reportsTotalPartsCost;
    decimal _reportsTotalLaborCost;

    public MaintenanceViewModel(ReceiptStickerDesignerViewModel receiptStickerDesigner, Action navigateToReceiptStickerDesigner)
    {
        _receiptStickerDesigner = receiptStickerDesigner;
        _navigateToReceiptStickerDesigner = navigateToReceiptStickerDesigner;
        WorkOrders = new ObservableCollection<WorkOrderItem>();
        StatusOptions = new ObservableCollection<string> { "Open", "In Progress", "Completed", "Delivered", "Rejected" };
        DeviceTypes = new ObservableCollection<string> { "Total Station", "Auto Level", "GPS" };
        BrandOptions = new ObservableCollection<string> { "Leica", "Trimble", "Sokkia", "Topcon", "South", "CHC", "Kolida", "Stonex", "Ruide", "Sanding", "Hi-Target", "Foif", "Gowin", "CST/berger", "Nikon", "Pentax", "Spectra", "Geomax", "مختلف الاجهزة الصينيه" };

        NewWorkOrderCommand = new RelayCommand(_ => ClearInputs());
        EditWorkOrderCommand = new RelayCommand(_ => BeginEditSelectedWorkOrder(), _ => SelectedWorkOrder != null);
        // The Save button stays enabled so clicking it always gives clear feedback
        // (validation or success/failure messages) instead of silently doing nothing.
        SaveWorkOrderCommand = new RelayCommand(_ => SaveWorkOrder());
        AddDeviceToSameReceiptCommand = new RelayCommand(_ => AddDeviceToSameReceipt(), _ => !string.IsNullOrWhiteSpace(CustomerName) && (!string.IsNullOrWhiteSpace(ReceiptGroupNumber) || SelectedWorkOrder != null));
        DeleteWorkOrderCommand = new RelayCommand(_ => DeleteWorkOrder(), _ => SelectedWorkOrder != null);
        RefreshWorkOrdersCommand = new RelayCommand(_ => RefreshWorkOrders());
        GenerateQuotationCommand = new RelayCommand(_ => GenerateQuotation(), _ => SelectedWorkOrder != null);
        GenerateInvoiceCommand = new RelayCommand(_ => GenerateInvoice(), _ => SelectedWorkOrder != null);
        GenerateMaintenanceReportCommand = new RelayCommand(_ => GenerateMaintenanceReport(), _ => SelectedWorkOrder != null);
        PreviewWorkOrderCommand = new RelayCommand(_ => PreviewWorkOrder(), _ => CanPreviewWorkOrder());
        // Print buttons stay enabled so clicking them always gives clear feedback and
        // printing is only allowed after the order has been saved (see TryEnsureSavedForPrint).
        PrintReceiptCommand = new RelayCommand(_ => PrintReceipt());
        OpenReceiptStickerDesignerCommand = new RelayCommand(_ => OpenReceiptStickerDesigner());
        PrintReceiptStickerCommand = new RelayCommand(_ => PrintReceiptSticker());
        RefreshReportsCommand = new RelayCommand(_ => RefreshReports());
        ExportReportsPdfCommand = new RelayCommand(_ => ExportReportsPdf());
        ExportReportsExcelCommand = new RelayCommand(_ => ExportReportsExcel());
        try
        {
            _ = EnsureDbCreatedOnceAsync();
            RefreshWorkOrders();
            RefreshReports();
        }
        catch { }
    }

    public ObservableCollection<WorkOrderItem> WorkOrders { get; }
    public ObservableCollection<ReportStatusRow> ReportStatusBreakdown { get; } = new();
    public ObservableCollection<ReportMonthRow> ReportMonthBreakdown { get; } = new();
    public ObservableCollection<MaintenanceReportRow> ReportsOrders { get; } = new();
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
            OpenReceiptStickerDesignerCommand.RaiseCanExecuteChanged();
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
            EnsureWorkOrderCertificateLinkTable(db);
            
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

            TryBackfillGpsCertificateFields(db);
        });
        return _ensureDbCreatedTask;
    }

    static void TryBackfillGpsCertificateFields(AppDbContext db)
    {
        try
        {
            var gpsWorkOrders = db.WorkOrders
                .Where(w => w.DeviceType == "GPS")
                .Select(w => new
                {
                    w.Id,
                    w.Brand,
                    w.SerialNumber,
                    w.SerialNumber2
                })
                .ToList();

            if (gpsWorkOrders.Count == 0)
                return;

            var updatedCount = 0;
            foreach (var workOrder in gpsWorkOrders)
            {
                Certificate? certificate = null;

                var linkedCertificateId = TryGetLinkedCertificateId(db, workOrder.Id);
                if (linkedCertificateId.HasValue)
                {
                    certificate = db.Certificates.FirstOrDefault(c => c.Id == linkedCertificateId.Value);
                }

                if (certificate == null)
                {
                    var tempMarker = $"TEMP-WO-{workOrder.Id:D6}";
                    certificate = db.Certificates.FirstOrDefault(c => c.CertificateNumber == tempMarker);
                    if (certificate != null)
                        TryUpsertWorkOrderCertificateLink(db, workOrder.Id, certificate.Id);
                }

                if (certificate == null)
                    continue;

                var changed = false;

                if (string.IsNullOrWhiteSpace(certificate.DeviceType))
                {
                    certificate.DeviceType = "GPS";
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(certificate.Brand) && !string.IsNullOrWhiteSpace(workOrder.Brand))
                {
                    certificate.Brand = workOrder.Brand;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(certificate.SerialText) && !string.IsNullOrWhiteSpace(workOrder.SerialNumber))
                {
                    certificate.SerialText = workOrder.SerialNumber;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(certificate.SerialText2) && !string.IsNullOrWhiteSpace(workOrder.SerialNumber2))
                {
                    certificate.SerialText2 = workOrder.SerialNumber2;
                    changed = true;
                }

                if (!changed)
                    continue;

                updatedCount++;
            }

            if (updatedCount > 0)
            {
                db.SaveChanges();
                LogService.LogInfo($"Backfilled GPS certificate fields for {updatedCount} records.");
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
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
        set
        {
            if (!SetProperty(ref _searchText, value ?? ""))
                return;
            RestartSearchDebounce();
        }
    }

    void RestartSearchDebounce()
    {
        _searchDebounceTimer ??= new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Tick -= OnSearchDebounceTick;
        _searchDebounceTimer.Tick += OnSearchDebounceTick;
        _searchDebounceTimer.Start();
    }

    void OnSearchDebounceTick(object? sender, EventArgs e)
    {
        _searchDebounceTimer?.Stop();
        RefreshWorkOrders();
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
    public RelayCommand OpenReceiptStickerDesignerCommand { get; }
    public RelayCommand PrintReceiptStickerCommand { get; }
    public RelayCommand RefreshReportsCommand { get; }
    public RelayCommand ExportReportsPdfCommand { get; }
    public RelayCommand ExportReportsExcelCommand { get; }

    public string ReceiptGroupNumber
    {
        get => _receiptGroupNumber;
        set => SetProperty(ref _receiptGroupNumber, value);
    }

    public DateTime ReportsFromDate
    {
        get => _reportsFromDate;
        set
        {
            if (!SetProperty(ref _reportsFromDate, value))
                return;
            RefreshReports();
        }
    }

    public DateTime ReportsToDate
    {
        get => _reportsToDate;
        set
        {
            if (!SetProperty(ref _reportsToDate, value))
                return;
            RefreshReports();
        }
    }

    public bool IsReportsBusy
    {
        get => _isReportsBusy;
        private set => SetProperty(ref _isReportsBusy, value);
    }

    public string ReportsBusyMessage
    {
        get => _reportsBusyMessage;
        private set => SetProperty(ref _reportsBusyMessage, value);
    }

    public int TotalOrdersCount
    {
        get => _totalOrdersCount;
        private set => SetProperty(ref _totalOrdersCount, value);
    }

    public int InProgressCount
    {
        get => _inProgressCount;
        private set => SetProperty(ref _inProgressCount, value);
    }

    public int CompletedDeliveredCount
    {
        get => _completedDeliveredCount;
        private set => SetProperty(ref _completedDeliveredCount, value);
    }

    public decimal ReportsTotalIncome
    {
        get => _reportsTotalIncome;
        private set => SetProperty(ref _reportsTotalIncome, value);
    }

    public decimal ReportsTotalPartsCost
    {
        get => _reportsTotalPartsCost;
        private set => SetProperty(ref _reportsTotalPartsCost, value);
    }

    public decimal ReportsTotalLaborCost
    {
        get => _reportsTotalLaborCost;
        private set => SetProperty(ref _reportsTotalLaborCost, value);
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

    async void SaveWorkOrder()
    {
        if (!ValidateSaveInputs())
            return;
        await TrySaveCoreAsync(showSuccess: true);
    }

    bool ValidateSaveInputs()
    {
        if (SelectedWorkOrder != null && !_isEditModeForSelected)
        {
            System.Windows.MessageBox.Show(T("PressEditFirstMessage"), T("Warning"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            System.Windows.MessageBox.Show(T("MissingClientNameMessage"), T("Warning"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(DeviceType))
        {
            System.Windows.MessageBox.Show(T("MissingDeviceTypeMessage"), T("Warning"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }

        if (PartsCost < 0 || LaborCost < 0)
        {
            System.Windows.MessageBox.Show(T("InvalidCostMessage"), T("Warning"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    // Saves the current form data (insert or update), refreshes the grid and selects the
    // saved row. Never throws: failures are shown in a message box and false is returned.
    async Task<bool> TrySaveCoreAsync(bool showSuccess)
    {
        try
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            db.EnsureSchemaCompatible();

            var typedName = (CustomerName ?? "").Trim();
            var typedPhone = (CustomerPhone ?? "").Trim();
            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(typedName))
            {
                customer = db.Customers.FirstOrDefault(c => c.Name == typedName && (string.IsNullOrWhiteSpace(typedPhone) || c.Phone == typedPhone));
                if (customer == null)
                {
                    customer = new Customer
                    {
                        CreatedAt = DateTime.Now,
                        Name = typedName,
                        Phone = typedPhone
                    };
                    db.Customers.Add(customer);
                    db.SaveChanges();
                }
            }

            // Generate a new receipt group number if this is a new session and we don't have one.
            // The number is derived from the actual data (max + 1): a deleted number is never
            // reused while a higher one exists, and is reused only when it was the last issued.
            if (string.IsNullOrWhiteSpace(ReceiptGroupNumber))
            {
                ReceiptGroupNumber = GetNextMaintenanceReceiptNumber();
            }

            WorkOrder workOrder;
            if (SelectedWorkOrder != null)
            {
                workOrder = db.WorkOrders.FirstOrDefault(w => w.Id == SelectedWorkOrder.Id) ?? new WorkOrder();
            }
            else
            {
                workOrder = new WorkOrder();
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
            workOrder.ReceivedDate = ReceivedDate != default ? ReceivedDate : DateTime.Now;
            workOrder.UpdatedDate = UpdatedDate != default ? UpdatedDate : DateTime.Now;

            db.SaveChanges();

            // The certificate sync is auxiliary: a failure here must never abort the saved
            // work order or hide the success from the user (it is only logged).
            try
            {
                var syncedCertificate = SyncWorkOrderToCertificate(db, workOrder);
                db.SaveChanges();
                if (syncedCertificate != null && syncedCertificate.Id > 0)
                {
                    TryUpsertWorkOrderCertificateLink(db, workOrder.Id, syncedCertificate.Id);
                }
            }
            catch (Exception certEx)
            {
                System.Diagnostics.Debug.WriteLine($"Certificate sync failed: {certEx}");
            }

            var savedId = workOrder.Id;
            // Wait for the refreshed list so the saved order is guaranteed to be selected
            // and visible; otherwise the user might think the save never happened.
            await RefreshWorkOrdersAsync();

            var savedItem = WorkOrders.FirstOrDefault(w => w.Id == savedId);
            if (savedItem == null)
            {
                // A search filter may hide the just-saved order: insert it so the user
                // always sees the row they just saved.
                savedItem = new WorkOrderItem
                {
                    Id = workOrder.Id,
                    CustomerName = workOrder.CustomerName,
                    CustomerPhone = workOrder.CustomerPhone,
                    DeviceType = workOrder.DeviceType,
                    Brand = workOrder.Brand,
                    Model = workOrder.Model,
                    SerialNumber = workOrder.SerialNumber,
                    SerialNumber2 = workOrder.SerialNumber2,
                    Accessories = workOrder.Accessories,
                    Complaint = workOrder.Complaint,
                    TechnicalReport = workOrder.TechnicalReport,
                    Status = workOrder.Status,
                    ReceiptGroupNumber = workOrder.ReceiptGroupNumber,
                    PartsCost = workOrder.PartsCost,
                    LaborCost = workOrder.LaborCost,
                    TotalCost = workOrder.TotalCost,
                    ReceivedDate = workOrder.ReceivedDate,
                    UpdatedDate = workOrder.UpdatedDate
                };
                WorkOrders.Insert(0, savedItem);
            }
            SelectedWorkOrder = savedItem;
            _isEditModeForSelected = false;
            SaveWorkOrderCommand.RaiseCanExecuteChanged();
            AddDeviceToSameReceiptCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsInputsLocked));
            OnPropertyChanged(nameof(IsInputsEditable));

            if (showSuccess)
                System.Windows.MessageBox.Show(T("SaveSuccessMessage"), T("SaveSuccessTitle"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                string.Format(T("SaveFailedMessage"), ex.Message),
                T("SaveFailedTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return false;
        }
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
            currentGroupNumber = GetNextMaintenanceReceiptNumber();
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
        
        System.Windows.MessageBox.Show(T("CloneDeviceSuccessMessage"), T("CloneDeviceSuccessTitle"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    static Certificate? SyncWorkOrderToCertificate(AppDbContext db, WorkOrder workOrder)
    {
        EnsureWorkOrderCertificateLinkTable(db);

        Certificate? certificate = null;
        var linkedCertificateId = TryGetLinkedCertificateId(db, workOrder.Id);
        if (linkedCertificateId.HasValue)
        {
            certificate = db.Certificates.FirstOrDefault(c => c.Id == linkedCertificateId.Value);
        }

        // Legacy fallback for existing data that used TEMP-WO marker only
        var tempMarker = $"TEMP-WO-{workOrder.Id:D6}";
        if (certificate == null)
        {
            certificate = db.Certificates.FirstOrDefault(c => c.CertificateNumber == tempMarker);
        }

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
        certificate.Brand = workOrder.Brand ?? "";
        certificate.Model = workOrder.Model ?? "";
        certificate.SerialText = workOrder.SerialNumber ?? "";
        certificate.SerialText2 = workOrder.SerialNumber2 ?? "";
        
        // Always set SpecValue to a proper default, never leave status text like 'In Progress'
        if (string.Equals(workOrder.DeviceType, "Total Station", StringComparison.OrdinalIgnoreCase))
            certificate.SpecValue = "1\""; // Default accuracy instead of 'In Progress'
        else
            certificate.SpecValue = "";

        if (isNew)
        {
            certificate.CreatedAt = DateTime.Now;
            certificate.IssueDate = DateTime.Today;
            certificate.ExpiryDate = DateTime.Today.AddMonths(6);
        }

        return certificate;
    }

    static void EnsureWorkOrderCertificateLinkTable(AppDbContext db)
    {
        try
        {
            var provider = db.Database.ProviderName ?? "";
            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS WorkOrderCertificateLinks (
                        WorkOrderId INTEGER NOT NULL PRIMARY KEY,
                        CertificateId INTEGER NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_WorkOrderCertificateLinks_CertificateId ON WorkOrderCertificateLinks (CertificateId);
                ");
            }
            else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                db.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID(N'WorkOrderCertificateLinks', N'U') IS NULL
                    BEGIN
                        CREATE TABLE WorkOrderCertificateLinks (
                            WorkOrderId INT NOT NULL PRIMARY KEY,
                            CertificateId INT NOT NULL UNIQUE
                        );
                    END
                ");
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    static int? TryGetLinkedCertificateId(AppDbContext db, int workOrderId)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT CertificateId FROM WorkOrderCertificateLinks WHERE WorkOrderId = @WorkOrderId";
            var workOrderParam = command.CreateParameter();
            workOrderParam.ParameterName = "@WorkOrderId";
            workOrderParam.Value = workOrderId;
            command.Parameters.Add(workOrderParam);

            var result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return null;
        }
    }

    static void TryUpsertWorkOrderCertificateLink(AppDbContext db, int workOrderId, int certificateId)
    {
        try
        {
            EnsureWorkOrderCertificateLinkTable(db);

            var provider = db.Database.ProviderName ?? "";
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText = @"
                    INSERT INTO WorkOrderCertificateLinks (WorkOrderId, CertificateId)
                    VALUES (@WorkOrderId, @CertificateId)
                    ON CONFLICT(WorkOrderId) DO UPDATE SET CertificateId = excluded.CertificateId;";
            }
            else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText = @"
                    MERGE WorkOrderCertificateLinks AS target
                    USING (SELECT @WorkOrderId AS WorkOrderId, @CertificateId AS CertificateId) AS source
                    ON target.WorkOrderId = source.WorkOrderId
                    WHEN MATCHED THEN UPDATE SET CertificateId = source.CertificateId
                    WHEN NOT MATCHED THEN INSERT (WorkOrderId, CertificateId)
                    VALUES (source.WorkOrderId, source.CertificateId);";
            }
            else
            {
                return;
            }

            var workOrderParam = command.CreateParameter();
            workOrderParam.ParameterName = "@WorkOrderId";
            workOrderParam.Value = workOrderId;
            command.Parameters.Add(workOrderParam);

            var certificateParam = command.CreateParameter();
            certificateParam.ParameterName = "@CertificateId";
            certificateParam.Value = certificateId;
            command.Parameters.Add(certificateParam);

            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    static void TryDeleteWorkOrderCertificateLink(AppDbContext db, int workOrderId)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM WorkOrderCertificateLinks WHERE WorkOrderId = @WorkOrderId";
            var workOrderParam = command.CreateParameter();
            workOrderParam.ParameterName = "@WorkOrderId";
            workOrderParam.Value = workOrderId;
            command.Parameters.Add(workOrderParam);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    void DeleteWorkOrder()
    {
        if (SelectedWorkOrder == null)
            return;

        var confirm = System.Windows.MessageBox.Show(
            string.Format(T("DeleteConfirmMessage"), SelectedWorkOrder.ReceiptGroupNumber ?? "", SelectedWorkOrder.CustomerName ?? ""),
            T("DeleteConfirmTitle"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            var entity = db.WorkOrders.FirstOrDefault(w => w.Id == SelectedWorkOrder.Id);
            if (entity != null)
            {
                var linkedCertificateId = TryGetLinkedCertificateId(db, entity.Id);
                TryDeleteWorkOrderCertificateLink(db, entity.Id);
                if (linkedCertificateId.HasValue)
                {
                    var linkedCertificate = db.Certificates.FirstOrDefault(c => c.Id == linkedCertificateId.Value);
                    if (linkedCertificate != null)
                        db.Certificates.Remove(linkedCertificate);
                }
                db.WorkOrders.Remove(entity);
                db.SaveChanges();
            }
            RefreshWorkOrders();
            ClearInputs();
            System.Windows.MessageBox.Show(T("DeletedSuccessMessage"), T("DeletedSuccessTitle"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, T("SaveFailedTitle"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    void RefreshWorkOrders() => _ = RefreshWorkOrdersAsync();

    async Task RefreshWorkOrdersAsync()
    {
        BeginLoad();
        try
        {
            var items = await Task.Run(() =>
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
                var query = db.WorkOrders.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var text = SearchText.Trim();
                    query = query.Where(w => w.CustomerName.Contains(text) || w.SerialNumber.Contains(text));
                }

                return query
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
            });

            WorkOrders.Clear();
            foreach (var item in items)
                WorkOrders.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshWorkOrders failed: {ex}");
        }
        finally
        {
            EndLoad();
        }
    }

    int _activeLoads;

    void BeginLoad()
    {
        if (System.Threading.Interlocked.Increment(ref _activeLoads) == 1)
            SetBusy(T("LoadingData"));
    }

    void EndLoad()
    {
        if (System.Threading.Interlocked.Decrement(ref _activeLoads) == 0)
            SetIdle();
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
        OpenReceiptStickerDesignerCommand.RaiseCanExecuteChanged();
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
        string? templatePath;
        try
        {
            templatePath = await System.Threading.Tasks.Task.Run(() => new DocumentPathMappingStore().GetPath("price_quotation"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Missing template", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        templatePath = ResolveTemplatePath(templatePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            MessageBox.Show(
                T("MissingTemplateMessage"),
                "Missing template",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string? outputPath = null;
        try
        {
            await EnsureDbCreatedOnceAsync();
            var engine = new WordTemplateEngine("");
            using var db = CreateDbContext();
            var docService = new CertificateDocumentService(db, engine);
            var result = await docService.GenerateWorkOrderReportDocxAndPdfFromPathAsync(
                SelectedWorkOrder.Id,
                "Price Quotation",
                templatePath);
            outputPath = MoveReportToReportsOutput(result, "Quotation");
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
        string? templatePath;
        try
        {
            templatePath = await System.Threading.Tasks.Task.Run(() => new DocumentPathMappingStore().GetPath("invoice"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Missing template", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        templatePath = ResolveTemplatePath(templatePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            MessageBox.Show(
                T("MissingTemplateMessage"),
                "Missing template",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string? outputPath = null;
        try
        {
            await EnsureDbCreatedOnceAsync();
            var engine = new WordTemplateEngine("");
            using var db = CreateDbContext();
            var docService = new CertificateDocumentService(db, engine);
            var result = await docService.GenerateWorkOrderReportDocxAndPdfFromPathAsync(
                SelectedWorkOrder.Id,
                "Invoice",
                templatePath);
            outputPath = MoveReportToReportsOutput(result, "Invoice");
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
        string? templatePath;
        try
        {
            templatePath = await System.Threading.Tasks.Task.Run(() => new DocumentPathMappingStore().GetPath("maintenance_report"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Missing template", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        templatePath = ResolveTemplatePath(templatePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            MessageBox.Show(
                T("MissingTemplateMessage"),
                "Missing template",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string? outputPath = null;
        try
        {
            await EnsureDbCreatedOnceAsync();
            var engine = new WordTemplateEngine("");
            using var db = CreateDbContext();
            var docService = new CertificateDocumentService(db, engine);
            var result = await docService.GenerateWorkOrderReportDocxAndPdfFromPathAsync(
                SelectedWorkOrder.Id,
                "Maintenance Report",
                templatePath);
            outputPath = MoveReportToReportsOutput(result, "MaintenanceReport");
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

    static string? MoveReportToReportsOutput(string docxPath, string reportType)
    {
        if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
            return null;

        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        var dir = AppPaths.ReportsOutput;
        var baseName = reportType + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var finalPdf = Path.Combine(dir, baseName + ".pdf");

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch
        {
        }

        try
        {
            if (File.Exists(pdfPath))
                File.Copy(pdfPath, finalPdf, true);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }

        try
        {
            File.Copy(docxPath, Path.Combine(dir, baseName + ".docx"), true);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }

        try
        {
            File.Delete(docxPath);
        }
        catch
        {
        }

        try
        {
            File.Delete(pdfPath);
        }
        catch
        {
        }

        return File.Exists(finalPdf) ? finalPdf : null;
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

    // Printing the A4 receipt, printing the receipt sticker, or opening the receipt sticker
    // designer with data is only allowed AFTER the order has been saved. This prevents an
    // employee from printing a receipt/sticker for data that was never stored in the database.
    async Task<bool> TryEnsureSavedForPrint()
    {
        if (SelectedWorkOrder != null && !_isEditModeForSelected)
            return true;

        if (SelectedWorkOrder != null && _isEditModeForSelected)
        {
            // Unsaved edits on a saved order: force saving them first so the printed
            // document always matches the stored data.
            var confirmEdit = System.Windows.MessageBox.Show(
                T("SaveBeforePrintPrompt"),
                T("PrintRequiresSaveTitle"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (confirmEdit != System.Windows.MessageBoxResult.Yes)
                return false;
            return await TrySaveCoreAsync(showSuccess: false);
        }

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(DeviceType))
        {
            System.Windows.MessageBox.Show(
                T("PrintRequiresSaveMessage"),
                T("PrintRequiresSaveTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return false;
        }

        var confirmNew = System.Windows.MessageBox.Show(
            T("SaveBeforePrintPrompt"),
            T("PrintRequiresSaveTitle"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (confirmNew != System.Windows.MessageBoxResult.Yes)
            return false;

        return await TrySaveCoreAsync(showSuccess: false);
    }

    async void PrintReceipt()
    {
        if (!await TryEnsureSavedForPrint())
            return;

        try
        {
            var source = SelectedWorkOrder ?? BuildSnapshotFromCurrent();
            var receiptGroupNumber = source.ReceiptGroupNumber;

            // Generate receipt number if missing for unsaved snapshot and reuse it on save
            if (string.IsNullOrWhiteSpace(receiptGroupNumber))
            {
                receiptGroupNumber = GetUnifiedReceiptNumber(source);
                if (string.IsNullOrWhiteSpace(ReceiptGroupNumber))
                    ReceiptGroupNumber = receiptGroupNumber;
            }

            var devices = LoadReceiptGroupDevices(receiptGroupNumber);

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

    async void OpenReceiptStickerDesigner()
    {
        if (!await TryEnsureSavedForPrint())
            return;

        try
        {
            var data = BuildReceiptData();
            var first = data.Devices.FirstOrDefault();
            if (first != null)
                PushReceiptData(data, first);
            _navigateToReceiptStickerDesigner?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Receipt sticker failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    async void PrintReceiptSticker()
    {
        if (!await TryEnsureSavedForPrint())
            return;

        try
        {
            var data = BuildReceiptData();
            var reportSettings = new ReportDesignerSettingsStore().Load();
            var printerSettings = new PrinterSettingsStore(PrinterSettingsStore.ReceiptFileName).Load();
            var service = new StickerPrintService();

            foreach (var device in data.Devices)
            {
                PushReceiptData(data, device, reportSettings);
                _ = service.PrintStickerAsync(_receiptStickerDesigner, printerSettings).ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                            MessageBox.Show(t.Exception?.GetBaseException()?.Message ?? "Receipt sticker failed",
                                "Receipt sticker failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    },
                    System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Receipt sticker failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    MaintenanceReceiptData BuildReceiptData()
    {
        var source = SelectedWorkOrder ?? BuildSnapshotFromCurrent();
        var receiptGroupNumber = source.ReceiptGroupNumber;

        // Generate receipt number if missing for unsaved snapshot and reuse it on save
        if (string.IsNullOrWhiteSpace(receiptGroupNumber))
        {
            receiptGroupNumber = GetUnifiedReceiptNumber(source);
            if (string.IsNullOrWhiteSpace(ReceiptGroupNumber))
                ReceiptGroupNumber = receiptGroupNumber;
        }

        var devices = LoadReceiptGroupDevices(receiptGroupNumber);

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

        return new MaintenanceReceiptData
        {
            ReceiptNumber = receiptGroupNumber,
            CustomerName = source.CustomerName,
            CustomerPhone = source.CustomerPhone,
            ReceivedDate = source.ReceivedDate,
            Devices = devices
        };
    }

    // Push the current receipt/device data onto the (isolated) receipt sticker designer so the
    // receipt sticker previews and prints EXACTLY the layout the user built for the receipt sticker.
    void PushReceiptData(MaintenanceReceiptData data, DeviceReceiptItem device, ReportDesignerSettings? reportSettings = null)
    {
        var vm = _receiptStickerDesigner;
        if (reportSettings != null)
        {
            vm.CompanyName = reportSettings.CompanyName ?? "";
            vm.CompanyHeader = reportSettings.CompanyHeader ?? "";
            vm.CompanyAddress = reportSettings.CompanyAddress ?? "";
            vm.CompanyPhone = reportSettings.CompanyPhone ?? "";
        }

        vm.ApplyReceiptData(
            data.ReceiptNumber,
            data.CustomerName,
            data.CustomerPhone,
            device.Brand,
            device.Model,
            device.SerialNumber,
            data.ReceivedDate);

        vm.UpdateItemText("company", vm.CompanyName);
        vm.UpdateItemText("header", vm.CompanyHeader);
        vm.UpdateItemText("address", vm.CompanyAddress);
        vm.UpdateItemText("phone", vm.CompanyPhone);
        vm.ApplyVariableBindings();
        vm.UpdateQrContent();
    }

    // Called when the user opens the receipt sticker designer from the main navigation.
    // ONLY saved work-order data is pushed onto the designer; unsaved form data is never
    // shown/printed there (prevents printing orders that were never stored).
    public void RefreshReceiptStickerDesigner()
    {
        try
        {
            var order = SelectedWorkOrder;
            var vm = _receiptStickerDesigner;
            var reportSettings = new ReportDesignerSettingsStore().Load();
            vm.CompanyName = reportSettings.CompanyName ?? "";
            vm.CompanyHeader = reportSettings.CompanyHeader ?? "";
            vm.CompanyAddress = reportSettings.CompanyAddress ?? "";
            vm.CompanyPhone = reportSettings.CompanyPhone ?? "";

            if (order != null && !string.IsNullOrWhiteSpace(order.ReceiptGroupNumber))
            {
                var data = BuildReceiptDataFrom(order);
                var first = data.Devices.FirstOrDefault();
                if (first != null)
                    PushReceiptData(data, first, reportSettings);
            }
            else
            {
                // No saved order selected: show a blank designer (data cannot be printed
                // until the order is saved from the maintenance screen).
                vm.ApplyReceiptData("", "", "", "", "", "", DateTime.Today);
                vm.ApplyVariableBindings();
                vm.UpdateQrContent();
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    // Builds receipt data from a SAVED work order (and its group devices) only.
    MaintenanceReceiptData BuildReceiptDataFrom(WorkOrderItem order)
    {
        var receiptGroupNumber = (order.ReceiptGroupNumber ?? "").Trim();
        var devices = LoadReceiptGroupDevices(receiptGroupNumber);
        if (devices.Count == 0)
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

        return new MaintenanceReceiptData
        {
            ReceiptNumber = receiptGroupNumber,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            ReceivedDate = order.ReceivedDate,
            Devices = devices
        };
    }

    static string GetUnifiedReceiptNumber(WorkOrderItem source)
    {
        // Saved orders keep their existing receipt group number (the sticker number is the
        // same receipt number). Legacy saved orders without a group fall back to their stable Id.
        if (source.Id > 0)
        {
            var group = (source.ReceiptGroupNumber ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(group))
                return group;
            return source.Id.ToString("D4");
        }
        return GetNextMaintenanceReceiptNumber();
    }

    // The receipt/sticker number is derived from the actual data (max existing + 1):
    //  - a deleted number is NEVER reused while a higher number still exists (it stays vacant);
    //  - a number is reused ONLY when the deleted entry was the last/maximum issued.
    static string GetNextMaintenanceReceiptNumber()
    {
        try
        {
            int maxNumber = 0;
            using (var db = CreateDbContext())
            {
                db.Database.EnsureCreated();
                var numbers = db.WorkOrders.AsNoTracking()
                    .Where(w => w.ReceiptGroupNumber != null && w.ReceiptGroupNumber != "")
                    .Select(w => w.ReceiptGroupNumber)
                    .ToList();
                foreach (var n in numbers)
                {
                    if (int.TryParse(n?.Trim(), out var value) && value > maxNumber)
                        maxNumber = value;
                }
            }
            return (maxNumber + 1).ToString("D4");
        }
        catch
        {
            // Fallback to the persistent counter if the database is unavailable.
            return DocumentSequenceStore.GetNextFormatted("MaintenanceReceipt");
        }
    }

    List<DeviceReceiptItem> LoadReceiptGroupDevices(string receiptGroupNumber)
    {
        var devices = new System.Collections.Generic.List<DeviceReceiptItem>();
        if (string.IsNullOrWhiteSpace(receiptGroupNumber))
            return devices;

        try
        {
            using var db = CreateDbContext();
            var orders = db.WorkOrders.AsNoTracking()
                .Where(w => w.ReceiptGroupNumber == receiptGroupNumber)
                .OrderBy(w => w.Id)
                .ToList();

            foreach (var order in orders)
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
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }

        return devices;
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

    async void RefreshReports()
    {
        if (IsReportsBusy)
            return;
        IsReportsBusy = true;
        ReportsBusyMessage = T("LoadingData");
        try
        {
            var from = ReportsFromDate.Date;
            var to = ReportsToDate.Date.AddDays(1);
            var data = await Task.Run(() =>
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
                var query = db.WorkOrders.AsNoTracking()
                    .Where(w => w.ReceivedDate >= from && w.ReceivedDate < to)
                    .OrderBy(w => w.ReceivedDate)
                    .ThenBy(w => w.Id)
                    .ToList();

                var rows = query.Select(w => new MaintenanceReportRow
                {
                    Id = w.Id,
                    ReceiptGroupNumber = w.ReceiptGroupNumber ?? "",
                    CustomerName = w.CustomerName ?? "",
                    DeviceType = w.DeviceType ?? "",
                    Brand = w.Brand ?? "",
                    Model = w.Model ?? "",
                    SerialNumber = w.SerialNumber ?? "",
                    Status = w.Status ?? "",
                    PartsCost = w.PartsCost,
                    LaborCost = w.LaborCost,
                    TotalCost = w.TotalCost,
                    ReceivedDate = w.ReceivedDate
                }).ToList();

                var statusRows = query
                    .GroupBy(w => string.IsNullOrWhiteSpace(w.Status) ? "-" : w.Status)
                    .OrderBy(g => g.Key)
                    .Select(g => new ReportStatusRow
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(x => x.TotalCost)
                    })
                    .ToList();

                var monthRows = query
                    .GroupBy(w => new DateTime(w.ReceivedDate.Year, w.ReceivedDate.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new ReportMonthRow
                    {
                        Month = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(x => x.TotalCost)
                    })
                    .ToList();

                var income = query
                    .Where(w => w.Status == "Completed" || w.Status == "Delivered")
                    .Sum(x => x.TotalCost);

                return new { Rows = rows, Status = statusRows, Months = monthRows, Income = income };
            });

            ReportsOrders.Clear();
            foreach (var row in data.Rows)
                ReportsOrders.Add(row);

            ReportStatusBreakdown.Clear();
            foreach (var row in data.Status)
                ReportStatusBreakdown.Add(row);

            ReportMonthBreakdown.Clear();
            foreach (var row in data.Months)
                ReportMonthBreakdown.Add(row);

            TotalOrdersCount = data.Rows.Count;
            InProgressCount = data.Rows.Count(r => r.Status == "Open" || r.Status == "In Progress");
            CompletedDeliveredCount = data.Rows.Count(r => r.Status == "Completed" || r.Status == "Delivered");
            ReportsTotalIncome = data.Income;
            ReportsTotalPartsCost = data.Rows.Sum(r => r.PartsCost);
            ReportsTotalLaborCost = data.Rows.Sum(r => r.LaborCost);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
        finally
        {
            IsReportsBusy = false;
            ReportsBusyMessage = "";
        }
    }

    async void ExportReportsPdf()
    {
        try
        {
            IsReportsBusy = true;
            ReportsBusyMessage = T("ExportPdf");

            System.IO.File.WriteAllText(
                System.IO.Path.Combine(AppPaths.ReportsOutput, "last_export_debug.txt"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ExportReportsPdf called. ReportsOrders.Count={ReportsOrders.Count}");

            if (ReportsOrders.Count == 0)
            {
                System.Windows.MessageBox.Show(T("NoReportsData"), T("ExportPdf"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var ordersSnapshot = ReportsOrders.ToList();
            var statusSnapshot = ReportStatusBreakdown.ToList();
            var monthSnapshot = ReportMonthBreakdown.ToList();
            var fromDate = ReportsFromDate;
            var toDate = ReportsToDate;
            var totalOrders = TotalOrdersCount;
            var totalIncome = ReportsTotalIncome;
            var totalParts = ReportsTotalPartsCost;
            var totalLabor = ReportsTotalLaborCost;

            var pdfPath = BuildReportsPdf(ordersSnapshot, statusSnapshot, monthSnapshot, fromDate, toDate, totalOrders, totalIncome, totalParts, totalLabor);

            try
            {
                if (!string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath))
                    Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogService.LogException(ex);
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(AppPaths.ReportsOutput, "last_export_debug.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} PDF ERROR:\r\n{ex}"); } catch { }
            System.Windows.MessageBox.Show(ex.ToString(), T("ReportsExportFailed"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsReportsBusy = false;
            ReportsBusyMessage = "";
        }
    }

    string BuildReportsPdf(
        List<MaintenanceReportRow> orders,
        List<ReportStatusRow> statusRows,
        List<ReportMonthRow> monthRows,
        DateTime fromDate, DateTime toDate,
        int totalOrders, decimal totalIncome, decimal totalParts, decimal totalLabor)
    {
        var dir = AppPaths.ReportsOutput;
        var pdfPath = Path.Combine(dir, "MaintenanceReports_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPDF.Settings.EnableDebugging = true;
        QuestPdfFonts.EnsureCairoRegistered();

        var report = new ReportDesignerSettingsStore().Load();
        var companyName = report.CompanyName?.Trim() ?? "";
        var companyHeader = report.CompanyHeader?.Trim() ?? "";
        var companyAddress = report.CompanyAddress?.Trim() ?? "";
        var companyPhone = report.CompanyPhone?.Trim() ?? "";
        var hasLogo = report.ShowLogo && !string.IsNullOrWhiteSpace(report.LogoPath) && File.Exists(report.LogoPath);
        var reportNumber = "RPT-" + DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var generatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842);
                page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Cairo").FontSize(9).FontColor("#1F2937"));

                page.Header().Column(letterhead =>
                {
                    letterhead.Item().Row(row =>
                    {
                        if (hasLogo)
                        {
                            row.ConstantItem(92).Element(logo =>
                            {
                                try
                                {
                                    logo.Width(92).Height(64).AlignLeft().AlignTop().Image(report.LogoPath!);
                                }
                                catch
                                {
                                }
                            });
                        }
                        row.RelativeItem().PaddingTop(2).Column(info =>
                        {
                            info.Spacing(2);
                            if (companyName.Length > 0)
                                info.Item().AlignRight().Text(companyName).DirectionFromRightToLeft().Bold().FontSize(17).FontColor("#0F3A5F");
                            if (companyHeader.Length > 0)
                                info.Item().AlignRight().Text(companyHeader).DirectionFromRightToLeft().FontSize(11).FontColor("#374151");
                            var contact = new System.Collections.Generic.List<(string Label, string Value)>();
                            if (companyAddress.Length > 0)
                                contact.Add(("", companyAddress));
                            if (companyPhone.Length > 0)
                                contact.Add((T("PhoneNumber"), companyPhone));
                            if (contact.Count > 0)
                                info.Item().Element(c => BuildHeaderContactLine(c, contact));
                        });
                    });
                    letterhead.Item().PaddingTop(4).Height(3).Background("#0F3A5F");
                    letterhead.Item().PaddingTop(1).Height(1).Background("#D1D5DB");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                        col.Item().PaddingTop(2).AlignCenter().Text(T("ReportsTitle")).DirectionFromRightToLeft().Bold().FontSize(15).FontColor("#0F3A5F");
                        col.Item().Row(periodRow =>
                        {
                            periodRow.RelativeItem();
                            periodRow.AutoItem().Text(toDate.ToString("yyyy-MM-dd")).DirectionFromLeftToRight().FontSize(10).FontColor("#374151");
                            periodRow.AutoItem().Text(T("ReportsTo") + "  ").DirectionFromRightToLeft().FontSize(10).FontColor("#374151");
                            periodRow.AutoItem().Text("    ").FontSize(10).FontColor("#374151");
                            periodRow.AutoItem().Text(fromDate.ToString("yyyy-MM-dd")).DirectionFromLeftToRight().FontSize(10).FontColor("#374151");
                            periodRow.AutoItem().Text(T("ReportsFrom") + "  ").DirectionFromRightToLeft().FontSize(10).FontColor("#374151");
                            periodRow.RelativeItem();
                        });
                        col.Item().Row(metaRow =>
                        {
                            metaRow.RelativeItem();
                            metaRow.AutoItem().Text(generatedOn).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ReportsGeneratedOn") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text("    |    ").FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(reportNumber).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ReportNumberLabel") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                        });

                        col.Item().PaddingTop(2).Row(summary =>
                        {
                            summary.Spacing(6);
                            summary.RelativeItem().Element(c => BuildSummaryCard(c, T("TotalOrders"), totalOrders.ToString()));
                            summary.RelativeItem().Element(c => BuildSummaryCard(c, T("TotalIncome"), totalIncome.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)));
                            summary.RelativeItem().Element(c => BuildSummaryCard(c, T("TotalPartsCost"), totalParts.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)));
                            summary.RelativeItem().Element(c => BuildSummaryCard(c, T("TotalLaborCost"), totalLabor.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)));
                        });

                        col.Item().PaddingTop(4).Text(T("MaintenanceRequests")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(t => BuildRequestsTable(t, orders));

                        col.Item().PaddingTop(4).Text(T("StatusBreakdown")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(t => BuildStatusBreakdownTable(t, statusRows));

                        col.Item().PaddingTop(4).Text(T("MonthlyBreakdown")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(t => BuildMonthlyBreakdownTable(t, monthRows));

                        col.Item().PaddingTop(8).Row(signatures =>
                        {
                            signatures.Spacing(14);
                            signatures.RelativeItem().Element(c => BuildSignatureBox(c, T("SignatureMaintenanceResponsible")));
                            signatures.RelativeItem().Element(c => BuildSignatureBox(c, T("SignatureManager")));
                        });
                    });

                page.Footer().Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(0.5f).LineColor("#CBD5E1");
                    footerCol.Item().PaddingTop(3).Row(footerRow =>
                    {
                        footerRow.RelativeItem().AlignLeft().Text(x => SpanOf(x, companyName).FontSize(8).FontColor("#6B7280"));
                        footerRow.RelativeItem().AlignRight().DefaultTextStyle(s => s.FontSize(8).FontColor("#6B7280")).Text(x =>
                        {
                            x.Span(T("PageLabel") + " ").DirectionFromRightToLeft();
                            x.CurrentPageNumber();
                        });
                    });
                });
            });
        }).GeneratePdf(pdfPath);

        return pdfPath;
    }

    void BuildSummaryCard(QuestPDF.Infrastructure.IContainer container, string label, string value)
    {
        container.Border(0.75f).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(6).Column(c =>
        {
            c.Item().AlignCenter().Text(label).DirectionFromRightToLeft().FontSize(8).FontColor("#374151");
            c.Item().AlignCenter().PaddingTop(2).Text(value).FontSize(12).Bold().FontColor("#0F3A5F");
        });
    }

    void BuildTableHeaderCell(QuestPDF.Infrastructure.IContainer container, string text)
    {
        container.Border(0.5f).BorderColor("#0F3A5F").Background("#0F3A5F").Padding(4).AlignCenter().Text(text).DirectionFromRightToLeft().Bold().FontSize(9).FontColor("#FFFFFF");
    }

    void BuildStatusBreakdownTable(QuestPDF.Fluent.TableDescriptor table, List<ReportStatusRow> statusRows)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(3);
            c.RelativeColumn(1);
            c.RelativeColumn(1.5f);
        });
        table.Header(h =>
        {
            BuildTableHeaderCell(h.Cell(), T("StatusField"));
            BuildTableHeaderCell(h.Cell(), T("OrdersCount"));
            BuildTableHeaderCell(h.Cell(), T("TotalCostColumn"));
        });
        foreach (var s in statusRows)
        {
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(4).Text(s.Status).DirectionFromRightToLeft();
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(4).AlignCenter().Text(s.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(4).AlignRight().Text(s.TotalCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    void BuildMonthlyBreakdownTable(QuestPDF.Fluent.TableDescriptor table, List<ReportMonthRow> monthRows)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(1);
            c.RelativeColumn(1);
            c.RelativeColumn(1.5f);
        });
        table.Header(h =>
        {
            BuildTableHeaderCell(h.Cell(), T("MonthColumn"));
            BuildTableHeaderCell(h.Cell(), T("OrdersCount"));
            BuildTableHeaderCell(h.Cell(), T("TotalCostColumn"));
        });
        foreach (var m in monthRows)
        {
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(4).AlignCenter().Text(m.MonthLabel);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(4).AlignCenter().Text(m.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(4).AlignRight().Text(m.TotalCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    void BuildRequestsTable(QuestPDF.Fluent.TableDescriptor table, List<MaintenanceReportRow> orders)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(1);
            c.RelativeColumn(2);
            c.RelativeColumn(1.5f);
            c.RelativeColumn(1.5f);
            c.RelativeColumn(1);
            c.RelativeColumn(1);
            c.RelativeColumn(1);
            c.RelativeColumn(1.5f);
        });
        table.Header(h =>
        {
            BuildTableHeaderCell(h.Cell(), T("ReceiptNumber"));
            BuildTableHeaderCell(h.Cell(), T("ClientNameField"));
            BuildTableHeaderCell(h.Cell(), T("DeviceType"));
            BuildTableHeaderCell(h.Cell(), T("Model"));
            BuildTableHeaderCell(h.Cell(), T("StatusField"));
            BuildTableHeaderCell(h.Cell(), T("PartsCost"));
            BuildTableHeaderCell(h.Cell(), T("LaborCost"));
            BuildTableHeaderCell(h.Cell(), T("Total"));
        });
        foreach (var r in ReportsOrders)
            {
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(r.ReceiptGroupNumber).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(r.CustomerName).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(r.DeviceType).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(r.Model).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignCenter().Text(r.Status).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignRight().Text(r.PartsCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)).FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignRight().Text(r.LaborCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)).FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignRight().Text(r.TotalCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)).FontSize(9);
        }
    }

    void BuildSignatureBox(QuestPDF.Infrastructure.IContainer container, string title)
    {
        container.Border(0.75f).BorderColor("#9CA3AF").Padding(8).Column(c =>
        {
            c.Item().AlignCenter().Text(title).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
            c.Item().PaddingTop(16).AlignCenter().Text(T("NameLabel") + ":  ........................").DirectionFromRightToLeft().FontSize(9).FontColor("#374151");
            c.Item().PaddingTop(12).AlignCenter().Text(T("SignatureLabel") + ":  ........................").DirectionFromRightToLeft().FontSize(9).FontColor("#374151");
        });
    }

    void ExportReportsExcel()
    {
        try
        {
            if (ReportsOrders.Count == 0)
            {
                System.Windows.MessageBox.Show(T("NoReportsData"), T("ExportExcel"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var dir = AppPaths.ReportsOutput;
            var path = Path.Combine(dir, "MaintenanceReports_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{T("ReceiptNumber")};{T("ClientNameField")};{T("DeviceType")};{T("Brand")};{T("Model")};{T("SerialField")};{T("StatusField")};{T("PartsCost")};{T("LaborCost")};{T("Total")};{T("ReceiptDate")}");

            foreach (var r in ReportsOrders)
            {
                sb.AppendLine(string.Join(";",
                    EscapeCsv(r.ReceiptGroupNumber),
                    EscapeCsv(r.CustomerName),
                    EscapeCsv(r.DeviceType),
                    EscapeCsv(r.Brand),
                    EscapeCsv(r.Model),
                    EscapeCsv(r.SerialNumber),
                    EscapeCsv(r.Status),
                    r.PartsCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                    r.LaborCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                    r.TotalCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                    r.ReceivedDate.ToString("yyyy-MM-dd")));
            }

            System.IO.File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogService.LogException(ex);
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(AppPaths.ReportsOutput, "last_export_debug.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Excel ERROR:\r\n{ex}"); } catch { }
            System.Windows.MessageBox.Show(ex.ToString(), T("ReportsExportFailed"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    static string EscapeCsv(string value)
    {
        if (value == null)
            return "";
        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
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

    public sealed class MaintenanceReportRow
    {
        public int Id { get; set; }
        public string ReceiptGroupNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal PartsCost { get; set; }
        public decimal LaborCost { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime ReceivedDate { get; set; }
    }

    public sealed class ReportStatusRow
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalCost { get; set; }
    }

    public sealed class ReportMonthRow
    {
        public DateTime Month { get; set; }
        public int Count { get; set; }
        public decimal TotalCost { get; set; }
        public string MonthLabel => Month.ToString("yyyy-MM");
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
