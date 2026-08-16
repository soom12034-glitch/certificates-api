using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class RentalsViewModel : ViewModelBase
{
    string _customerName = "";
    string _company = "";
    string _phone = "";
    string _taxNumber = "";
    string _idNumber = "";
    string _deviceType = "";
    string _brand = "";
    string _model = "";
    string _serial = "";
    string _serial2 = "";
    DateTime _startDate = DateTime.Today;
    DateTime _endDate = DateTime.Today.AddDays(1);
    string _rentalType = "Daily";
    decimal _dailyPrice;
    decimal _monthlyPrice;
    decimal _deviceValue;
    decimal _price;
    decimal _paidAmount;
    decimal _remainingAmount;
    string _status = "Active";
    string _notes = "";
    string _searchText = "";
    RentalItem? _selectedRental;
    Task? _ensureDbCreatedTask;
    bool _isEditMode;
    int? _currentRentalId;
    System.Windows.Threading.DispatcherTimer? _alertTimer;

    public RentalsViewModel()
    {
        Rentals = new ObservableCollection<RentalItem>();
        StatusOptions = new ObservableCollection<string> { "Active", "Completed", "Returned" };
        DeviceTypes = new ObservableCollection<string> { "Total Station", "GPS", "Auto Level", "Scanner" };
        BrandOptions = new ObservableCollection<string> { "Leica", "Trimble", "Sokkia", "Topcon", "South", "CHC", "Kolida", "Stonex", "Ruide", "Sanding", "Hi-Target", "Foif", "Gowin", "CST/berger", "Nikon", "Pentax", "Spectra", "Geomax", "مختلف الاجهزة الصينيه" };
        RentalTypes = new ObservableCollection<string> { "Daily", "Monthly" };

        NewRentalCommand = new RelayCommand(_ => ClearInputs());
        EditRentalCommand = new RelayCommand(_ => EditSelectedRental(), _ => SelectedRental != null);
        SaveRentalCommand = new RelayCommand(_ => SaveRental());
        DeleteRentalCommand = new RelayCommand(_ => DeleteRental(), _ => SelectedRental != null);
        CompleteRentalCommand = new RelayCommand(_ => CompleteRental(), _ => SelectedRental != null);
        PrintRentalReceiptCommand = new RelayCommand(_ => PrintRentalReceipt());
        PrintRentalContractCommand = new RelayCommand(_ => PrintRentalContract(), _ => SelectedRental != null);
        PrintRentalHandoverCommand = new RelayCommand(p => PrintRentalHandover(string.Equals(p?.ToString(), "Return", StringComparison.OrdinalIgnoreCase)), _ => SelectedRental != null);
        OpenSelectedRentalFileCommand = new RelayCommand(_ => OpenSelectedRentalFile(), _ => SelectedRental != null);
        SearchCommand = new RelayCommand(_ => SearchRentals());

        try
        {
            _ = EnsureDbCreatedOnceAsync();
            RefreshRentals();
            CheckExpiredRentals();
        }
        catch { }

        StartAlertTimer();
    }

    static AppDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext();
    }

    static string GetLegacyRentalsDbPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "BlueMax.db");
    }

    static void TryMigrateLegacyRentals(AppDbContext db)
    {
        try
        {
            var legacyDbPath = GetLegacyRentalsDbPath();
            var primaryDbPath = AppDbContext.GetLocalDbPath();

            if (!File.Exists(legacyDbPath))
                return;

            var legacyFullPath = Path.GetFullPath(legacyDbPath);
            var primaryFullPath = Path.GetFullPath(primaryDbPath);
            if (string.Equals(legacyFullPath, primaryFullPath, StringComparison.OrdinalIgnoreCase))
                return;

            var legacyRentals = LoadLegacyRentals(legacyDbPath);
            if (legacyRentals.Count == 0)
                return;

            var existingRentalNumbers = db.Rentals
                .AsNoTracking()
                .Where(r => !string.IsNullOrWhiteSpace(r.RentalNumber))
                .Select(r => r.RentalNumber)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingCompositeKeys = db.Rentals
                .AsNoTracking()
                .Select(r => BuildRentalCompositeKey(r.CustomerName, r.Serial, r.StartDate, r.EndDate, r.Price))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var addedCount = 0;
            foreach (var legacy in legacyRentals)
            {
                var hasRentalNumber = !string.IsNullOrWhiteSpace(legacy.RentalNumber);
                if (hasRentalNumber && existingRentalNumbers.Contains(legacy.RentalNumber))
                    continue;

                var compositeKey = BuildRentalCompositeKey(legacy.CustomerName, legacy.Serial, legacy.StartDate, legacy.EndDate, legacy.Price);
                if (existingCompositeKeys.Contains(compositeKey))
                    continue;

                db.Rentals.Add(legacy);
                addedCount++;

                if (hasRentalNumber)
                    existingRentalNumbers.Add(legacy.RentalNumber);
                existingCompositeKeys.Add(compositeKey);
            }

            if (addedCount > 0)
            {
                db.SaveChanges();
                LogService.LogInfo($"Migrated {addedCount} legacy rental records from BlueMax.db to shared database.");
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    static List<Rental> LoadLegacyRentals(string legacyDbPath)
    {
        var rentals = new List<Rental>();

        using var connection = new SqliteConnection($"Data Source={legacyDbPath}");
        connection.Open();

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var columnsCommand = connection.CreateCommand())
        {
            columnsCommand.CommandText = "PRAGMA table_info(\"Rentals\")";
            using var columnsReader = columnsCommand.ExecuteReader();
            while (columnsReader.Read())
            {
                var columnName = columnsReader["name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(columnName))
                    columns.Add(columnName);
            }
        }

        if (columns.Count == 0)
            return rentals;

        string SelectColumn(string name, string fallbackSql)
            => columns.Contains(name) ? $"\"{name}\"" : $"{fallbackSql} AS \"{name}\"";

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT
                {SelectColumn("RentalNumber", "''")},
                {SelectColumn("CustomerName", "''")},
                {SelectColumn("Company", "''")},
                {SelectColumn("Phone", "''")},
                {SelectColumn("TaxNumber", "''")},
                {SelectColumn("IdNumber", "''")},
                {SelectColumn("DeviceType", "''")},
                {SelectColumn("Brand", "''")},
                {SelectColumn("Model", "''")},
                {SelectColumn("Serial", "''")},
                {SelectColumn("Serial2", "''")},
                {SelectColumn("StartDate", "''")},
                {SelectColumn("EndDate", "''")},
                {SelectColumn("RentalType", "'Daily'")},
                {SelectColumn("DailyPrice", "0")},
                {SelectColumn("MonthlyPrice", "0")},
                {SelectColumn("DeviceValue", "0")},
                {SelectColumn("Price", "0")},
                {SelectColumn("PaidAmount", "0")},
                {SelectColumn("RemainingAmount", "0")},
                {SelectColumn("Status", "'Active'")},
                {SelectColumn("Notes", "''")},
                {SelectColumn("CreatedAt", "''")}
            FROM ""Rentals"";";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var rentalNumber = ReadString(reader, "RentalNumber");
            if (string.IsNullOrWhiteSpace(rentalNumber))
                rentalNumber = GenerateLegacyRentalNumber();

            rentals.Add(new Rental
            {
                RentalNumber = rentalNumber,
                CustomerName = ReadString(reader, "CustomerName"),
                Company = ReadString(reader, "Company"),
                Phone = ReadString(reader, "Phone"),
                TaxNumber = ReadString(reader, "TaxNumber"),
                IdNumber = ReadString(reader, "IdNumber"),
                DeviceType = ReadString(reader, "DeviceType"),
                Brand = ReadString(reader, "Brand"),
                Model = ReadString(reader, "Model"),
                Serial = ReadString(reader, "Serial"),
                Serial2 = ReadString(reader, "Serial2"),
                StartDate = ReadDateTime(reader, "StartDate", DateTime.Today),
                EndDate = ReadDateTime(reader, "EndDate", DateTime.Today.AddDays(1)),
                RentalType = ReadString(reader, "RentalType"),
                DailyPrice = ReadDecimal(reader, "DailyPrice"),
                MonthlyPrice = ReadDecimal(reader, "MonthlyPrice"),
                DeviceValue = ReadDecimal(reader, "DeviceValue"),
                Price = ReadDecimal(reader, "Price"),
                PaidAmount = ReadDecimal(reader, "PaidAmount"),
                RemainingAmount = ReadDecimal(reader, "RemainingAmount"),
                Status = ReadString(reader, "Status"),
                Notes = ReadString(reader, "Notes"),
                CreatedAt = ReadDateTime(reader, "CreatedAt", DateTime.Now)
            });
        }

        return rentals;
    }

    static string BuildRentalCompositeKey(string? customerName, string? serial, DateTime startDate, DateTime endDate, decimal price)
    {
        return $"{(customerName ?? "").Trim().ToUpperInvariant()}|{(serial ?? "").Trim().ToUpperInvariant()}|{startDate:yyyy-MM-dd}|{endDate:yyyy-MM-dd}|{price:0.####}";
    }

    static string ReadString(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? "" : value?.ToString() ?? "";
    }

    static DateTime ReadDateTime(SqliteDataReader reader, string column, DateTime fallback)
    {
        var raw = ReadString(reader, column);
        if (DateTime.TryParse(raw, out var parsed))
            return parsed;
        return fallback;
    }

    static decimal ReadDecimal(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        if (value == DBNull.Value)
            return 0m;

        if (value is decimal d)
            return d;
        if (value is double db)
            return Convert.ToDecimal(db);
        if (value is float f)
            return Convert.ToDecimal(f);
        if (value is long l)
            return l;
        if (value is int i)
            return i;

        var text = value.ToString();
        return decimal.TryParse(text, out var parsed) ? parsed : 0m;
    }

    static string GenerateLegacyRentalNumber()
    {
        return $"MIG-{Guid.NewGuid():N}";
    }

    public ObservableCollection<RentalItem> Rentals { get; }
    public ObservableCollection<string> StatusOptions { get; }
    public ObservableCollection<string> DeviceTypes { get; }
    public ObservableCollection<string> BrandOptions { get; }
    public ObservableCollection<string> RentalTypes { get; }

    public RentalItem? SelectedRental
    {
        get => _selectedRental;
        set
        {
            if (!SetProperty(ref _selectedRental, value))
                return;
            RaiseSelectionCommandStates();
            if (value == null)
            {
                ClearInputs();
                return;
            }
            _isEditMode = false;
            _currentRentalId = value.Id;
            CustomerName = value.CustomerName;
            Company = value.Company;
            Phone = value.Phone;
            TaxNumber = value.TaxNumber;
            IdNumber = value.IdNumber;
            DeviceType = value.DeviceType;
            Brand = value.Brand;
            Model = value.Model;
            Serial = value.Serial;
            Serial2 = value.Serial2;
            StartDate = value.StartDate;
            EndDate = value.EndDate;
            RentalType = value.RentalType;
            DailyPrice = value.DailyPrice;
            MonthlyPrice = value.MonthlyPrice;
            DeviceValue = value.DeviceValue;
            Price = value.Price;
            PaidAmount = value.PaidAmount;
            RemainingAmount = value.RemainingAmount;
            Status = value.Status;
            Notes = value.Notes;
            OnPropertyChanged(nameof(IsInputsLocked));
            OnPropertyChanged(nameof(IsInputsEditable));
        }
    }

    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (SetProperty(ref _customerName, value))
                SaveRentalCommand.RaiseCanExecuteChanged();
        }
    }

    public string Company
    {
        get => _company;
        set => SetProperty(ref _company, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string IdNumber
    {
        get => _idNumber;
        set => SetProperty(ref _idNumber, value);
    }

    public string DeviceType
    {
        get => _deviceType;
        set
        {
            if (SetProperty(ref _deviceType, value))
            {
                SaveRentalCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(IsGpsDevice));
            }
        }
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

    public string Serial
    {
        get => _serial;
        set
        {
            if (SetProperty(ref _serial, value))
                SaveRentalCommand.RaiseCanExecuteChanged();
        }
    }

    public string Serial2
    {
        get => _serial2;
        set => SetProperty(ref _serial2, value);
    }

    public bool IsGpsDevice => string.Equals(DeviceType, "GPS", StringComparison.OrdinalIgnoreCase);

    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
                RecalculatePrice();
        }
    }

    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
                RecalculatePrice();
        }
    }

    public string RentalType
    {
        get => _rentalType;
        set
        {
            if (SetProperty(ref _rentalType, value))
            {
                RecalculatePrice();
                SaveRentalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public decimal DailyPrice
    {
        get => _dailyPrice;
        set
        {
            if (SetProperty(ref _dailyPrice, value))
            {
                RecalculatePrice();
                SaveRentalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public decimal MonthlyPrice
    {
        get => _monthlyPrice;
        set
        {
            if (SetProperty(ref _monthlyPrice, value))
            {
                RecalculatePrice();
                SaveRentalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public decimal DeviceValue
    {
        get => _deviceValue;
        set
        {
            if (SetProperty(ref _deviceValue, value))
                SaveRentalCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
            {
                CalculateRemainingAmount();
                SaveRentalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        set
        {
            if (SetProperty(ref _paidAmount, value))
                CalculateRemainingAmount();
        }
    }

    public decimal RemainingAmount
    {
        get => _remainingAmount;
        set => SetProperty(ref _remainingAmount, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsInputsLocked => _currentRentalId.HasValue && !_isEditMode;
    public bool IsInputsEditable => !IsInputsLocked;

    public ObservableCollection<RentalItem> ActiveRentals => new ObservableCollection<RentalItem>(Rentals.Where(r => r.Status == "Active"));

    public int TotalRentals => Rentals.Count;
    public int ActiveRentalsCount => Rentals.Count(r => r.Status == "Active");
    public int ExpiredRentalsCount => Rentals.Count(r => r.Status == "Active" && r.EndDate < DateTime.Today);
    public decimal TotalIncome => Rentals.Sum(r => r.PaidAmount);

    public bool HasExpiredRentals => ExpiredRentalsCount > 0;

    public RelayCommand NewRentalCommand { get; }
    public RelayCommand EditRentalCommand { get; }
    public RelayCommand SaveRentalCommand { get; }
    public RelayCommand DeleteRentalCommand { get; }
    public RelayCommand CompleteRentalCommand { get; }
    public RelayCommand PrintRentalReceiptCommand { get; }
    public RelayCommand PrintRentalContractCommand { get; }
    public RelayCommand PrintRentalHandoverCommand { get; }
    public RelayCommand OpenSelectedRentalFileCommand { get; }
    public RelayCommand SearchCommand { get; }

    void CalculateRemainingAmount()
    {
        RemainingAmount = Price - PaidAmount;
    }

    void RecalculatePrice()
    {
        var days = Math.Max(1, (EndDate.Date - StartDate.Date).Days);
        if (string.Equals(RentalType, "Monthly", StringComparison.OrdinalIgnoreCase))
        {
            var months = Math.Max(1, (int)Math.Ceiling(days / 30d));
            Price = MonthlyPrice * months;
        }
        else
        {
            Price = DailyPrice * days;
        }
    }

    public int RentalDaysCount => Math.Max(1, (EndDate.Date - StartDate.Date).Days);

    void RaiseSelectionCommandStates()
    {
        EditRentalCommand.RaiseCanExecuteChanged();
        DeleteRentalCommand.RaiseCanExecuteChanged();
        CompleteRentalCommand.RaiseCanExecuteChanged();
        PrintRentalContractCommand.RaiseCanExecuteChanged();
        PrintRentalHandoverCommand.RaiseCanExecuteChanged();
        OpenSelectedRentalFileCommand.RaiseCanExecuteChanged();
    }

    void ClearInputs()
    {
        _isEditMode = false;
        _currentRentalId = null;
        CustomerName = "";
        Company = "";
        Phone = "";
        TaxNumber = "";
        IdNumber = "";
        DeviceType = "";
        Brand = "";
        Model = "";
        Serial = "";
        StartDate = DateTime.Today;
        EndDate = DateTime.Today.AddDays(1);
        RentalType = "Daily";
        DailyPrice = 0;
        MonthlyPrice = 0;
        DeviceValue = 0;
        Price = 0;
        PaidAmount = 0;
        RemainingAmount = 0;
        Status = "Active";
        Notes = "";
        SelectedRental = null;
        OnPropertyChanged(nameof(IsInputsLocked));
        OnPropertyChanged(nameof(IsInputsEditable));
    }

    void EditSelectedRental()
    {
        if (SelectedRental == null)
            return;
        _isEditMode = true;
        OnPropertyChanged(nameof(IsInputsLocked));
        OnPropertyChanged(nameof(IsInputsEditable));
    }

    string? GetValidationMessage()
    {
        var missing = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(CustomerName))
            missing.Add(T("ClientNameField"));
        if (string.IsNullOrWhiteSpace(DeviceType))
            missing.Add(T("DeviceType"));
        if (string.IsNullOrWhiteSpace(Serial))
            missing.Add(T("SerialField"));
        if (EndDate.Date < StartDate.Date)
            return T("RentalEndDateBeforeStart");
        if (missing.Count > 0)
            return T("MissingRequiredFields") + ": " + string.Join(", ", missing);
        return null;
    }

    void SaveRental()
    {
        var validationMessage = GetValidationMessage();
        if (validationMessage != null)
        {
            MessageBox.Show(validationMessage, T("ValidationTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            db.EnsureSchemaCompatible();

            if (_currentRentalId.HasValue)
            {
                var rental = db.Rentals.FirstOrDefault(r => r.Id == _currentRentalId.Value);
                if (rental != null)
                {
                    rental.CustomerName = CustomerName;
                    rental.Company = Company;
                    rental.Phone = Phone;
                    rental.TaxNumber = TaxNumber;
                    rental.IdNumber = IdNumber;
                    rental.DeviceType = DeviceType;
                    rental.Brand = Brand;
                    rental.Model = Model;
                    rental.Serial = Serial;
                    rental.Serial2 = Serial2;
                    rental.StartDate = StartDate;
                    rental.EndDate = EndDate;
                    rental.RentalType = RentalType;
                    rental.DailyPrice = DailyPrice;
                    rental.MonthlyPrice = MonthlyPrice;
                    rental.DeviceValue = DeviceValue;
                    rental.Price = Price;
                    rental.PaidAmount = PaidAmount;
                    rental.RemainingAmount = RemainingAmount;
                    rental.Status = Status;
                    rental.Notes = Notes;
                    db.SaveChanges();
                    MessageBox.Show(T("RentalUpdatedSuccess"), T("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var rental = new Rental
                {
                    RentalNumber = GenerateRentalNumber(),
                    CustomerName = CustomerName,
                    Company = Company,
                    Phone = Phone,
                    TaxNumber = TaxNumber,
                    IdNumber = IdNumber,
                    DeviceType = DeviceType,
                    Brand = Brand,
                    Model = Model,
                    Serial = Serial,
                    Serial2 = Serial2,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    RentalType = RentalType,
                    DailyPrice = DailyPrice,
                    MonthlyPrice = MonthlyPrice,
                    DeviceValue = DeviceValue,
                    Price = Price,
                    PaidAmount = PaidAmount,
                    RemainingAmount = RemainingAmount,
                    Status = Status,
                    Notes = Notes,
                    CreatedAt = DateTime.Now
                };
                db.Rentals.Add(rental);
                db.SaveChanges();
                MessageBox.Show(T("RentalSavedSuccess"), T("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }

            RefreshRentals();
            ClearInputs();
            PlaySound("success");
        }
        catch (Exception ex)
        {
            var errorMessage = $"خطأ في الحفظ: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nتفاصيل إضافية: {ex.InnerException.Message}";
                if (ex.InnerException.InnerException != null)
                {
                    errorMessage += $"\n\n{ex.InnerException.InnerException.Message}";
                }
            }
            MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void DeleteRental()
    {
        if (SelectedRental == null)
            return;

        var result = MessageBox.Show("هل أنت متأكد من حذف هذا الإيجار؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            using var db = CreateDbContext();
            var rental = db.Rentals.FirstOrDefault(r => r.Id == SelectedRental.Id);
            if (rental != null)
            {
                db.Rentals.Remove(rental);
                db.SaveChanges();
                RefreshRentals();
                ClearInputs();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void CompleteRental()
    {
        if (SelectedRental == null)
            return;

        try
        {
            using var db = CreateDbContext();
            var rental = db.Rentals.FirstOrDefault(r => r.Id == SelectedRental.Id);
            if (rental != null)
            {
                rental.Status = "Completed";
                db.SaveChanges();
                RefreshRentals();
                ClearInputs();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنهاء الإيجار: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    async void PrintRentalContract()
    {
        try
        {
            if (SelectedRental == null)
            {
                MessageBox.Show(T("SelectRentalFirst"), T("Alert"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(T("GeneratingPdf"));
            try
            {
                var pdfPath = await Task.Run(() => BuildRentalContractPdf(SelectedRental));
                OpenOrPrintPdf(pdfPath, T("RentalContract"));
            }
            finally
            {
                SetIdle();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء عقد الإيجار: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    async void PrintRentalHandover(bool isReturn)
    {
        try
        {
            if (SelectedRental == null)
            {
                MessageBox.Show(T("SelectRentalFirst"), T("Alert"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(T("GeneratingPdf"));
            try
            {
                var pdfPath = await Task.Run(() => BuildRentalHandoverPdf(SelectedRental, isReturn));
                OpenOrPrintPdf(pdfPath, isReturn ? T("ReturnReceiptTitle") : T("ReceiveReceiptTitle"));
            }
            finally
            {
                SetIdle();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء الإيصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void OpenOrPrintPdf(string pdfPath, string docType)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            return;

        var a4Printer = GetA4PrinterName();
        if (!string.IsNullOrWhiteSpace(a4Printer) && IsPrinterInstalled(a4Printer))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    Verb = "printto",
                    Arguments = $"\"{a4Printer}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
                return;
            }
            catch
            {
            }
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "تعذر فتح الملف", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    static Dictionary<string, string> BuildPdfHeaderData()
    {
        var report = new BlueMax.Infrastructure.ReportDesignerSettingsStore().Load();
        return new Dictionary<string, string>
        {
            ["CompanyName"] = report.CompanyName?.Trim() ?? "",
            ["CompanyHeader"] = report.CompanyHeader?.Trim() ?? "",
            ["CompanyAddress"] = report.CompanyAddress?.Trim() ?? "",
            ["CompanyPhone"] = report.CompanyPhone?.Trim() ?? "",
            ["CompanyCommercialRecord"] = report.CompanyCommercialRecord?.Trim() ?? "",
            ["CompanyTaxNumber"] = report.CompanyTaxNumber?.Trim() ?? "",
            ["CompanyNationalAddress"] = report.CompanyNationalAddress?.Trim() ?? "",
            ["ContractRepresentativeName"] = report.ContractRepresentativeName?.Trim() ?? "",
            ["ContractRepresentativeId"] = report.ContractRepresentativeId?.Trim() ?? "",
            ["ContractRepresentativePhone"] = report.ContractRepresentativePhone?.Trim() ?? "",
            ["LogoPath"] = (report.ShowLogo && !string.IsNullOrWhiteSpace(report.LogoPath) && File.Exists(report.LogoPath)) ? report.LogoPath : "",
            ["GeneratedOn"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };
    }

    string BuildRentalContractPdf(RentalItem rental)
    {
        var dir = AppPaths.RentalsOutput;
        var rentalNo = (rental.RentalNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(rentalNo))
            rentalNo = "RENTAL";
        var pdfPath = System.IO.Path.Combine(dir, $"RentalContract_{rentalNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPdfFonts.EnsureCairoRegistered();

        var header = BuildPdfHeaderData();
        var companyName = header["CompanyName"];
        var companyHeader = header["CompanyHeader"];
        var companyAddress = header["CompanyAddress"];
        var companyPhone = header["CompanyPhone"];
        var companyCommercialRecord = header["CompanyCommercialRecord"];
        var companyTaxNumber = header["CompanyTaxNumber"];
        var companyNationalAddress = header["CompanyNationalAddress"];
        var representativeName = header["ContractRepresentativeName"];
        var representativeId = header["ContractRepresentativeId"];
        var representativePhone = header["ContractRepresentativePhone"];
        var logoPath = header["LogoPath"];
        var hasLogo = !string.IsNullOrWhiteSpace(logoPath);
        var reportNumber = rentalNo;
        var generatedOn = header["GeneratedOn"];

        var deviceText = string.Join(" ", new[] { rental.DeviceType, rental.Brand, rental.Model }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var periodDays = Math.Max(1, (rental.EndDate.Date - rental.StartDate.Date).Days);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842);
                page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Cairo").FontSize(9).FontColor("#1F2937"));

                page.Header().Element(h => h.Column(letterhead =>
                {
                    letterhead.Item().Row(row =>
                    {
                        if (hasLogo)
                        {
                            row.ConstantItem(92).Element(logo =>
                            {
                                try
                                {
                                    logo.Width(92).Height(64).AlignLeft().AlignTop().Image(logoPath);
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
                            if (companyCommercialRecord.Length > 0)
                                contact.Add((T("CompanyCommercialRecord"), companyCommercialRecord));
                            if (companyTaxNumber.Length > 0)
                                contact.Add((T("CompanyTaxNumber"), companyTaxNumber));
                            if (companyNationalAddress.Length > 0)
                                contact.Add((T("CompanyNationalAddress"), companyNationalAddress));
                            if (contact.Count > 0)
                                info.Item().Element(c => BuildHeaderContactLine(c, contact));
                        });
                    });
                    letterhead.Item().PaddingTop(4).Height(3).Background("#0F3A5F");
                    letterhead.Item().PaddingTop(1).Height(1).Background("#D1D5DB");
                }));

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().PaddingTop(2).AlignCenter().Text(T("RentalContract")).DirectionFromRightToLeft().Bold().FontSize(16).FontColor("#0F3A5F");
                        col.Item().Row(metaRow =>
                        {
                            metaRow.RelativeItem();
                            metaRow.AutoItem().Text(generatedOn).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ReportsGeneratedOn") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text("    |    ").FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(reportNumber).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ContractNumberLabel") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                        });

                        col.Item().PaddingTop(4).Row(meta =>
                        {
                            meta.Spacing(6);
                            meta.RelativeItem().Element(c => BuildRentalPartyBox(c, T("FirstParty"), new List<(string, string)>
                            {
                                (T("Company"), companyName),
                                (T("CompanyCommercialRecord"), companyCommercialRecord),
                                (T("CompanyTaxNumber"), companyTaxNumber),
                                (T("CompanyNationalAddress"), companyNationalAddress),
                                (T("RepresentativeOf"), representativeName),
                                (T("IdNumber"), representativeId),
                                (T("PhoneNumber"), representativePhone)
                            }));
                            meta.RelativeItem().Element(c => BuildRentalPartyBox(c, T("SecondParty"), new List<(string, string)>
                            {
                                (T("ClientNameField"), rental.CustomerName),
                                (T("Company"), rental.Company ?? ""),
                                (T("IdNumber"), rental.IdNumber ?? ""),
                                (T("PhoneNumber"), rental.Phone ?? "")
                            }));
                        });

                        col.Item().PaddingTop(4).Text(T("DeviceDetails")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(2f);
                            });
                            table.Header(h =>
                            {
                                BuildRentalTableHeaderCell(h.Cell(), T("DeviceType"));
                                BuildRentalTableHeaderCell(h.Cell(), T("Device"));
                                BuildRentalTableHeaderCell(h.Cell(), T("MainSerial"));
                                BuildRentalTableHeaderCell(h.Cell(), T("Serial2Optional"));
                            });
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.DeviceType).DirectionFromRightToLeft().FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).Text(deviceText).DirectionFromRightToLeft().FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.Serial).DirectionFromRightToLeft().FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.Serial2 ?? "").DirectionFromRightToLeft().FontSize(9);
                        });

                        col.Item().PaddingTop(4).Text(T("RentalPeriod")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                            });
                            table.Header(h =>
                            {
                                BuildRentalTableHeaderCell(h.Cell(), T("RentalType"));
                                BuildRentalTableHeaderCell(h.Cell(), T("StartDateField"));
                                BuildRentalTableHeaderCell(h.Cell(), T("EndDateField"));
                                BuildRentalTableHeaderCell(h.Cell(), T("RentalDaysCountLabel"));
                            });
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.RentalType).DirectionFromRightToLeft().FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.StartDate.ToString("yyyy-MM-dd")).FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.EndDate.ToString("yyyy-MM-dd")).FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(periodDays.ToString()).FontSize(9);
                        });

                        col.Item().PaddingTop(4).Text(T("PaymentSummary")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.2f);
                            });
                            table.Header(h =>
                            {
                                BuildRentalTableHeaderCell(h.Cell(), T("DeviceValue"));
                                BuildRentalTableHeaderCell(h.Cell(), T("DailyPrice"));
                                BuildRentalTableHeaderCell(h.Cell(), T("MonthlyPrice"));
                                BuildRentalTableHeaderCell(h.Cell(), T("PriceField"));
                            });
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.DeviceValue.ToString("N2")).Bold().FontColor("#0F3A5F").FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.DailyPrice.ToString("N2")).FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.MonthlyPrice.ToString("N2")).FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(3).AlignCenter().Text(rental.Price.ToString("N2")).Bold().FontColor("#0F3A5F").FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#F8FAFC").Padding(3).AlignCenter().Text(T("PaidAmount")).DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#F8FAFC").Padding(3).Text("").FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#F8FAFC").Padding(3).Text("").FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#F8FAFC").Padding(3).AlignCenter().Text(rental.PaidAmount.ToString("N2")).FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#E2E8F0").Padding(3).AlignCenter().Text(T("RemainingAmount")).DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#E2E8F0").Padding(3).Text("").FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#E2E8F0").Padding(3).Text("").FontSize(9);
                            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Background("#E2E8F0").Padding(3).AlignCenter().Text(rental.RemainingAmount.ToString("N2")).Bold().FontColor("#B91C1C").FontSize(9);
                        });

                        col.Item().PaddingTop(6).Text(T("ContractTermsTitle")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().PaddingTop(2).Column(termsCol =>
                        {
                            termsCol.Spacing(3);
                            foreach (var term in BuildRentalContractTerms(rental))
                            {
                                termsCol.Item().Text(term).DirectionFromRightToLeft().FontSize(9).FontColor("#374151");
                            }
                        });

                        col.Item().PaddingTop(8).EnsureSpace(100).Row(signatures =>
                        {
                            signatures.Spacing(14);
                            signatures.RelativeItem().Element(c => BuildRentalSignatureBox(c, T("ContractSignatureLessor")));
                            signatures.RelativeItem().Element(c => BuildRentalSignatureBox(c, T("ContractSignatureRenter")));
                        });
                    });
                });

                page.Footer().Element(f => f.Column(footerCol =>
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
                }));
            });
        }).GeneratePdf(pdfPath);

        return pdfPath;
    }

    List<string> BuildRentalContractTerms(RentalItem rental)
    {
        var deviceValue = rental.DeviceValue.ToString("N2");
        var terms = T("RentalContractTerms").Replace("{0}", deviceValue);
        var lines = new List<string>();
        foreach (var part in terms.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
                lines.Add(trimmed);
        }
        return lines;
    }

    string BuildRentalHandoverPdf(RentalItem rental, bool isReturn)
    {
        var dir = AppPaths.RentalsOutput;
        var rentalNo = (rental.RentalNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(rentalNo))
            rentalNo = "RENTAL";
        var mode = isReturn ? "Return" : "Receive";
        var pdfPath = System.IO.Path.Combine(dir, $"RentalHandover_{mode}_{rentalNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPdfFonts.EnsureCairoRegistered();

        var header = BuildPdfHeaderData();
        var companyName = header["CompanyName"];
        var companyHeader = header["CompanyHeader"];
        var companyAddress = header["CompanyAddress"];
        var companyPhone = header["CompanyPhone"];
        var logoPath = header["LogoPath"];
        var hasLogo = !string.IsNullOrWhiteSpace(logoPath);

        var title = isReturn ? T("ReturnReceiptTitle") : T("ReceiveReceiptTitle");
        var confirmation = isReturn ? T("ReturnConfirmationText") : T("ReceiveConfirmationText");
        var deviceText = string.Join(" ", new[] { rental.DeviceType, rental.Brand, rental.Model }.Where(x => !string.IsNullOrWhiteSpace(x)));

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842);
                page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Cairo").FontSize(9).FontColor("#1F2937"));

                page.Header().Element(h => h.Column(letterhead =>
                {
                    letterhead.Item().Row(row =>
                    {
                        if (hasLogo)
                        {
                            row.ConstantItem(92).Element(logo =>
                            {
                                try
                                {
                                    logo.Width(92).Height(64).AlignLeft().AlignTop().Image(logoPath);
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
                }));

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().PaddingTop(2).AlignCenter().Text(title).DirectionFromRightToLeft().Bold().FontSize(16).FontColor("#0F3A5F");
                        col.Item().Row(metaRow =>
                        {
                            metaRow.RelativeItem();
                            metaRow.AutoItem().Text(header["GeneratedOn"]).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ReportsGeneratedOn") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text("    |    ").FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(rentalNo).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("RentalNumber") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                        });

                        col.Item().PaddingTop(4).Text(confirmation).DirectionFromRightToLeft().FontSize(10).FontColor("#374151");

                        col.Item().PaddingTop(6).Row(meta =>
                        {
                            meta.Spacing(6);
                            meta.RelativeItem().Element(c => BuildRentalMetaBox(c, T("ClientNameField"), rental.CustomerName));
                            meta.RelativeItem().Element(c => BuildRentalMetaBox(c, T("PhoneNumber"), rental.Phone ?? ""));
                        });
                        col.Item().PaddingTop(4).Row(meta2 =>
                        {
                            meta2.Spacing(6);
                            meta2.RelativeItem().Element(c => BuildRentalMetaBox(c, T("Device"), deviceText));
                            meta2.RelativeItem().Element(c => BuildRentalMetaBox(c, T("MainSerial"), rental.Serial));
                        });

                        col.Item().PaddingTop(4).Row(meta3 =>
                        {
                            meta3.Spacing(6);
                            meta3.RelativeItem().Element(c => BuildRentalMetaBox(c, T("StartDateField"), rental.StartDate.ToString("yyyy-MM-dd")));
                            meta3.RelativeItem().Element(c => BuildRentalMetaBox(c, T("EndDateField"), rental.EndDate.ToString("yyyy-MM-dd")));
                            meta3.RelativeItem().Element(c => BuildRentalMetaBox(c, T("DeviceValue"), rental.DeviceValue.ToString("N2")));
                        });

                        col.Item().PaddingTop(8).EnsureSpace(100).Row(signatures =>
                        {
                            signatures.Spacing(14);
                            signatures.RelativeItem().Element(c => BuildRentalSignatureBox(c, T("ContractSignatureRenter")));
                            signatures.RelativeItem().Element(c => BuildRentalSignatureBox(c, T("ContractSignatureLessor")));
                        });
                    });
                });

                page.Footer().Element(f => f.Column(footerCol =>
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
                }));
            });
        }).GeneratePdf(pdfPath);

        return pdfPath;
    }

    void BuildRentalMetaBox(QuestPDF.Infrastructure.IContainer container, string label, string value)
    {
        container.Border(0.75f).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(6).Column(c =>
        {
            c.Item().Text(label).DirectionFromRightToLeft().FontSize(8).FontColor("#374151");
            c.Item().PaddingTop(2).Text(x => SpanOf(x, value).FontSize(10).Bold().FontColor("#0F3A5F"));
        });
    }

    void BuildRentalPartyBox(QuestPDF.Infrastructure.IContainer container, string title, List<(string Label, string Value)> fields)
    {
        container.Border(0.75f).BorderColor("#0F3A5F").Background("#F8FAFC").Padding(8).Column(c =>
        {
            c.Item().AlignCenter().Text(title).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
            foreach (var (label, value) in fields)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                c.Item().PaddingTop(4).Element(el => AddRtlPairRow(el, label, value, valueColor: "#1F2937", valueBold: true));
            }
        });
    }

    void BuildRentalTableHeaderCell(QuestPDF.Infrastructure.IContainer container, string text)
    {
        container.Border(0.5f).BorderColor("#0F3A5F").Background("#0F3A5F").Padding(4).AlignCenter().Text(text).DirectionFromRightToLeft().Bold().FontSize(9).FontColor("#FFFFFF");
    }

    void BuildRentalSignatureBox(QuestPDF.Infrastructure.IContainer container, string title)
    {
        container.Border(0.75f).BorderColor("#9CA3AF").Padding(8).Column(c =>
        {
            c.Item().AlignCenter().Text(title).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
            c.Item().PaddingTop(40).LineHorizontal(0.5f).LineColor("#9CA3AF");
        });
    }

    async void PrintRentalReceipt()
    {
        try
        {
            if (SelectedRental == null)
            {
                MessageBox.Show("يرجى اختيار إيجار أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rentalNo = (SelectedRental.RentalNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rentalNo))
                return;

            var outputDir = AppPaths.RentalsOutput;
            var pdfPath = System.IO.Path.Combine(outputDir, $"Rental_{rentalNo}.pdf");
            var docxPath = System.IO.Path.Combine(outputDir, $"Rental_{rentalNo}.docx");
            var openPath = System.IO.File.Exists(docxPath) ? docxPath : (System.IO.File.Exists(pdfPath) ? pdfPath : "");
            
            if (string.IsNullOrWhiteSpace(openPath))
            {
                // Generate document if it doesn't exist
                var templatePath = await LoadTemplatePathAsync("rental_receipt");
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    MessageBox.Show(
                        "يرجى تحديد قالب Word لإيصال الإيجار (rental_receipt) من شاشة إدارة القوالب أولاً",
                        "قالب غير موجود",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!System.IO.Directory.Exists(outputDir))
                    System.IO.Directory.CreateDirectory(outputDir);

                var data = BuildRentalTokenData(SelectedRental);
                var engine = new WordTemplateEngine("");
                var generatedPath = await Task.Run(() => engine.GenerateDocumentFromPath(templatePath, data));
                openPath = generatedPath;
            }

            if (string.IsNullOrWhiteSpace(openPath))
                return;

            // Print to A4 printer if configured
            var a4Printer = GetA4PrinterName();
            if (!string.IsNullOrWhiteSpace(a4Printer) && IsPrinterInstalled(a4Printer))
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = openPath,
                        Verb = "printto",
                        Arguments = $"\"{a4Printer}\"",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    return;
                }
                catch
                {
                    // Fallback to opening file
                }
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = openPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "تعذر فتح الملف", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في طباعة الإيصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    async void OpenSelectedRentalFile()
    {
        try
        {
            if (SelectedRental == null)
                return;

            var rentalNo = (SelectedRental.RentalNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rentalNo))
                return;

            var outputDir = AppPaths.RentalsOutput;
            var pdfPath = System.IO.Path.Combine(outputDir, $"Rental_{rentalNo}.pdf");
            var docxPath = System.IO.Path.Combine(outputDir, $"Rental_{rentalNo}.docx");
            var openPath = System.IO.File.Exists(docxPath) ? docxPath : (System.IO.File.Exists(pdfPath) ? pdfPath : "");
            
            if (string.IsNullOrWhiteSpace(openPath))
            {
                // Generate document if it doesn't exist
                var templatePath = await LoadTemplatePathAsync("rental_receipt");
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    MessageBox.Show(
                        "يرجى تحديد قالب Word لإيصال الإيجار (rental_receipt) من شاشة إدارة القوالب أولاً",
                        "قالب غير موجود",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!System.IO.Directory.Exists(outputDir))
                    System.IO.Directory.CreateDirectory(outputDir);

                var data = BuildRentalTokenData(SelectedRental);
                var engine = new WordTemplateEngine("");
                var generatedPath = await Task.Run(() => engine.GenerateDocumentFromPath(templatePath, data));
                openPath = generatedPath;
            }

            if (string.IsNullOrWhiteSpace(openPath))
                return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = openPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "تعذر فتح الملف", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح الملف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    static string GetA4PrinterName()
    {
        try
        {
            return (System.Configuration.ConfigurationManager.AppSettings["A4PrinterName"] ?? "").Trim();
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

    async Task<string> LoadTemplatePathAsync(string docKey)
    {
        try
        {
            var raw = await Task.Run(() => new DocumentPathMappingStore().GetPath(docKey)) ?? "";
            var path = raw.Trim();
            if (string.IsNullOrWhiteSpace(path))
                return "";
            if (System.IO.Path.IsPathRooted(path) && System.IO.File.Exists(path))
                return path;
            var fallback = System.IO.Path.Combine(AppContext.BaseDirectory, path);
            if (System.IO.File.Exists(fallback))
                return fallback;
            return "";
        }
        catch
        {
            return "";
        }
    }

    static Dictionary<string, object> BuildRentalTokenData(RentalItem rental)
    {
        var data = new Dictionary<string, object>
        {
            ["RentalNumber"] = rental.RentalNumber,
            ["rental_number"] = rental.RentalNumber,
            ["RentalNo"] = rental.RentalNumber,
            ["rental_no"] = rental.RentalNumber,
            ["ReceiptNumber"] = rental.RentalNumber,
            ["receipt_number"] = rental.RentalNumber,

            ["CustomerName"] = rental.CustomerName,
            ["customer_name"] = rental.CustomerName,
            ["ClientName"] = rental.CustomerName,
            ["client_name"] = rental.CustomerName,

            ["Company"] = rental.Company ?? "",
            ["company"] = rental.Company ?? "",

            ["Phone"] = rental.Phone ?? "",
            ["phone"] = rental.Phone ?? "",

            ["TaxNumber"] = rental.TaxNumber ?? "",
            ["tax_number"] = rental.TaxNumber ?? "",

            ["IdNumber"] = rental.IdNumber ?? "",
            ["id_number"] = rental.IdNumber ?? "",

            ["DeviceType"] = rental.DeviceType,
            ["device_type"] = rental.DeviceType,

            ["Brand"] = rental.Brand ?? "",
            ["brand"] = rental.Brand ?? "",

            ["Model"] = rental.Model ?? "",
            ["model"] = rental.Model ?? "",

            ["Serial"] = rental.Serial,
            ["serial"] = rental.Serial,
            ["SerialNumber"] = rental.Serial,
            ["serial_number"] = rental.Serial,

            ["Serial2"] = rental.Serial2 ?? "",
            ["serial2"] = rental.Serial2 ?? "",
            ["SerialNumber2"] = rental.Serial2 ?? "",
            ["serial_number_2"] = rental.Serial2 ?? "",

            ["StartDate"] = rental.StartDate.ToString("yyyy-MM-dd"),
            ["start_date"] = rental.StartDate.ToString("yyyy-MM-dd"),

            ["EndDate"] = rental.EndDate.ToString("yyyy-MM-dd"),
            ["end_date"] = rental.EndDate.ToString("yyyy-MM-dd"),

            ["RentalType"] = rental.RentalType,
            ["rental_type"] = rental.RentalType,

            ["Price"] = rental.Price.ToString("0.##"),
            ["price"] = rental.Price.ToString("0.##"),

            ["PaidAmount"] = rental.PaidAmount.ToString("0.##"),
            ["paid_amount"] = rental.PaidAmount.ToString("0.##"),

            ["RemainingAmount"] = rental.RemainingAmount.ToString("0.##"),
            ["remaining_amount"] = rental.RemainingAmount.ToString("0.##"),

            ["Status"] = rental.Status,
            ["status"] = rental.Status,

            ["Notes"] = rental.Notes ?? "",
            ["notes"] = rental.Notes ?? "",

            ["IssueDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["issue_date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["CreatedDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["created_date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["Date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["date"] = DateTime.Now.ToString("yyyy-MM-dd"),

            ["qr_code_img"] = ""
        };

        var logoImagePath = "";
        try
        {
            var settings = new BlueMax.Infrastructure.ReportDesignerSettingsStore().Load();
            if (settings.ShowLogo && !string.IsNullOrWhiteSpace(settings.LogoPath) && File.Exists(settings.LogoPath))
                logoImagePath = settings.LogoPath;
        }
        catch
        {
        }
        data["company_logo_path"] = logoImagePath;
        data["company_logo_img"] = string.IsNullOrWhiteSpace(logoImagePath)
            ? ""
            : BlueMax.Infrastructure.WordTemplateEngine.CompanyLogoMarker;

        // Log data for debugging
        try
        {
            var logDir = AppPaths.LogsDir;
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "rental_tokens.log");
            var logLines = new List<string>
            {
                $"=== Rental Token Data ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===",
                $"RentalNumber: {rental.RentalNumber}",
                $"CustomerName: {rental.CustomerName}",
                $"DeviceType: {rental.DeviceType}",
                $"Serial: {rental.Serial}",
                $"---"
            };
            foreach (var kvp in data)
            {
                logLines.Add($"{kvp.Key} = {kvp.Value}");
            }
            System.IO.File.AppendAllLines(logPath, logLines);
        }
        catch
        {
            // Ignore logging errors
        }

        return data;
    }

    void SearchRentals()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            RefreshRentals();
            return;
        }

        var filtered = Rentals.Where(r => 
            r.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            r.Phone.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            r.Serial.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            r.RentalNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Rentals.Clear();
        foreach (var item in filtered)
            Rentals.Add(item);
    }

    async void RefreshRentals()
    {
        if (IsBusy) return;
        SetBusy(T("LoadingData"));
        try
        {
            var rentals = await Task.Run(() =>
            {
                using var db = CreateDbContext();
                return db.Rentals.OrderByDescending(r => r.CreatedAt).ToList();
            });

            Rentals.Clear();
            foreach (var rental in rentals)
            {
                Rentals.Add(new RentalItem
                {
                    Id = rental.Id,
                    RentalNumber = rental.RentalNumber,
                    CustomerName = rental.CustomerName,
                    Company = rental.Company,
                    Phone = rental.Phone,
                    TaxNumber = rental.TaxNumber,
                    IdNumber = rental.IdNumber,
                    DeviceType = rental.DeviceType,
                    Brand = rental.Brand,
                    Model = rental.Model,
                    Serial = rental.Serial,
                    Serial2 = rental.Serial2,
                    StartDate = rental.StartDate,
                    EndDate = rental.EndDate,
                    RentalType = rental.RentalType,
                    Price = rental.Price,
                    DailyPrice = rental.DailyPrice,
                    MonthlyPrice = rental.MonthlyPrice,
                    DeviceValue = rental.DeviceValue,
                    PaidAmount = rental.PaidAmount,
                    RemainingAmount = rental.RemainingAmount,
                    Status = rental.Status,
                    Notes = rental.Notes
                });
            }

            OnPropertyChanged(nameof(ActiveRentals));
            OnPropertyChanged(nameof(TotalRentals));
            OnPropertyChanged(nameof(ActiveRentalsCount));
            OnPropertyChanged(nameof(ExpiredRentalsCount));
            OnPropertyChanged(nameof(ExpiringSoonRentalsCount));
            OnPropertyChanged(nameof(HasExpiringSoonRentals));
            OnPropertyChanged(nameof(TotalIncome));
            OnPropertyChanged(nameof(HasExpiredRentals));
        }
        catch { }
        finally
        {
            SetIdle();
        }
    }

    public int ExpiringSoonRentalsCount => Rentals.Count(r => r.Status == "Active" && r.EndDate >= DateTime.Today && r.EndDate <= DateTime.Today.AddDays(ExpiryWarningDays));
    public bool HasExpiringSoonRentals => ExpiringSoonRentalsCount > 0;

    public const int ExpiryWarningDays = 3;

    void StartAlertTimer()
    {
        try
        {
            _alertTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _alertTimer.Tick += (_, __) => CheckExpiredRentals();
            _alertTimer.Start();
        }
        catch
        {
        }
    }

    void CheckExpiredRentals()
    {
        var expired = ExpiredRentalsCount;
        var expiring = ExpiringSoonRentalsCount;
        if (expired > 0 || expiring > 0)
        {
            PlaySound("alert");
        }
    }

    void PlaySound(string type)
    {
        try
        {
            if (string.Equals(type, "alert", StringComparison.OrdinalIgnoreCase))
                System.Media.SystemSounds.Exclamation.Play();
            else
                System.Media.SystemSounds.Asterisk.Play();
        }
        catch
        {
        }
    }

    string GenerateRentalNumber()
    {
        return $"R-{DateTime.Now:yyyyMMdd-HHmmss}";
    }

    async Task EnsureDbCreatedOnceAsync()
    {
        if (_ensureDbCreatedTask == null)
        {
            _ensureDbCreatedTask = Task.Run(() =>
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
                TryMigrateLegacyRentals(db);
            });
        }

        await _ensureDbCreatedTask;
    }
}

public class RentalItem : ViewModelBase
{
    int _id;
    string _rentalNumber = "";
    string _customerName = "";
    string _company = "";
    string _phone = "";
    string _taxNumber = "";
    string _idNumber = "";
    string _deviceType = "";
    string _brand = "";
    string _model = "";
    string _serial = "";
    string _serial2 = "";
    DateTime _startDate;
    DateTime _endDate;
    string _rentalType = "";
    decimal _price;
    decimal _dailyPrice;
    decimal _monthlyPrice;
    decimal _deviceValue;
    decimal _paidAmount;
    decimal _remainingAmount;
    string _status = "";
    string _notes = "";

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string RentalNumber
    {
        get => _rentalNumber;
        set => SetProperty(ref _rentalNumber, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public string Company
    {
        get => _company;
        set => SetProperty(ref _company, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string IdNumber
    {
        get => _idNumber;
        set => SetProperty(ref _idNumber, value);
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

    public string Serial
    {
        get => _serial;
        set => SetProperty(ref _serial, value);
    }

    public string Serial2
    {
        get => _serial2;
        set => SetProperty(ref _serial2, value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public string RentalType
    {
        get => _rentalType;
        set => SetProperty(ref _rentalType, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public decimal DailyPrice
    {
        get => _dailyPrice;
        set => SetProperty(ref _dailyPrice, value);
    }

    public decimal MonthlyPrice
    {
        get => _monthlyPrice;
        set => SetProperty(ref _monthlyPrice, value);
    }

    public decimal DeviceValue
    {
        get => _deviceValue;
        set => SetProperty(ref _deviceValue, value);
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        set => SetProperty(ref _paidAmount, value);
    }

    public decimal RemainingAmount
    {
        get => _remainingAmount;
        set => SetProperty(ref _remainingAmount, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }
}

public sealed class RentalReceiptData
{
    public string ReceiptNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Company { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DeviceType { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string Serial { get; set; } = "";
    public string Serial2 { get; set; } = "";
    public string RentalType { get; set; } = "";
    public decimal Price { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "";
    public string Notes { get; set; } = "";
}
