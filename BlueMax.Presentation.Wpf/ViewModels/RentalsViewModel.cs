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

    public RentalsViewModel()
    {
        Rentals = new ObservableCollection<RentalItem>();
        StatusOptions = new ObservableCollection<string> { "Active", "Completed", "Returned" };
        DeviceTypes = new ObservableCollection<string> { "Total Station", "GPS", "Auto Level", "Scanner" };
        BrandOptions = new ObservableCollection<string> { "Leica", "Trimble", "Sokkia", "Topcon", "South", "CHC", "Kolida", "Stonex", "Ruide", "Sanding", "Hi-Target", "Foif", "Gowin", "CST/berger", "Nikon", "Pentax", "Spectra", "Geomax", "مختلف الاجهزة الصينيه" };
        RentalTypes = new ObservableCollection<string> { "Daily", "Monthly" };

        NewRentalCommand = new RelayCommand(_ => ClearInputs());
        EditRentalCommand = new RelayCommand(_ => EditSelectedRental(), _ => SelectedRental != null);
        SaveRentalCommand = new RelayCommand(_ => SaveRental(), _ => CanSaveRental());
        DeleteRentalCommand = new RelayCommand(_ => DeleteRental(), _ => SelectedRental != null);
        CompleteRentalCommand = new RelayCommand(_ => CompleteRental(), _ => SelectedRental != null);
        PrintRentalReceiptCommand = new RelayCommand(_ => PrintRentalReceipt());
        OpenSelectedRentalFileCommand = new RelayCommand(_ => OpenSelectedRentalFile(), _ => SelectedRental != null);
        SearchCommand = new RelayCommand(_ => SearchRentals());

        try
        {
            _ = EnsureDbCreatedOnceAsync();
            RefreshRentals();
            CheckExpiredRentals();
        }
        catch { }
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
            if (value == null)
            {
                ClearInputs();
                return;
            }
            _isEditMode = false;
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
            Price = value.Price;
            PaidAmount = value.PaidAmount;
            RemainingAmount = value.RemainingAmount;
            Status = value.Status;
            Notes = value.Notes;
            _currentRentalId = value.Id;
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

    public bool IsInputsLocked => _isEditMode;
    public bool IsInputsEditable => !_isEditMode;

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
    public RelayCommand OpenSelectedRentalFileCommand { get; }
    public RelayCommand SearchCommand { get; }

    void CalculateRemainingAmount()
    {
        RemainingAmount = Price - PaidAmount;
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
        Price = 0;
        PaidAmount = 0;
        RemainingAmount = 0;
        Status = "Active";
        Notes = "";
        SelectedRental = null;
    }

    void EditSelectedRental()
    {
        if (SelectedRental == null)
            return;
        _isEditMode = true;
    }

    bool CanSaveRental()
    {
        var isValid = !string.IsNullOrWhiteSpace(CustomerName) &&
               !string.IsNullOrWhiteSpace(DeviceType) &&
               !string.IsNullOrWhiteSpace(Serial) &&
               Price > 0;
        return isValid;
    }

    void SaveRental()
    {
        try
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            
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
                    rental.Price = Price;
                    rental.PaidAmount = PaidAmount;
                    rental.RemainingAmount = RemainingAmount;
                    rental.Status = Status;
                    rental.Notes = Notes;
                    db.SaveChanges();
                    MessageBox.Show("تم تحديث الإيجار بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    Price = Price,
                    PaidAmount = PaidAmount,
                    RemainingAmount = RemainingAmount,
                    Status = Status,
                    Notes = Notes,
                    CreatedAt = DateTime.Now
                };
                db.Rentals.Add(rental);
                db.SaveChanges();
                MessageBox.Show("تم حفظ الإيجار بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
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

            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Rentals_Output");
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
                var engine = new WordTemplateEngine("", outputDir);
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

            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Rentals_Output");
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
                var engine = new WordTemplateEngine("", outputDir);
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

            ["company_logo_img"] = "",
            ["qr_code_img"] = ""
        };

        // Log data for debugging
        try
        {
            var logDir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Logs");
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

    void RefreshRentals()
    {
        try
        {
            using var db = CreateDbContext();
            var rentals = db.Rentals.OrderByDescending(r => r.CreatedAt).ToList();
            
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
            OnPropertyChanged(nameof(TotalIncome));
            OnPropertyChanged(nameof(HasExpiredRentals));
        }
        catch { }
    }

    void CheckExpiredRentals()
    {
        if (HasExpiredRentals)
        {
            PlaySound("alert");
        }
    }

    void PlaySound(string type)
    {
        try
        {
            // TODO: Implement sound alerts
            // For now, this is a placeholder
        }
        catch { }
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
