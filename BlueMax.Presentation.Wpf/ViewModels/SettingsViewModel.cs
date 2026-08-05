using System;
using System.Configuration;
using Microsoft.Win32;
using BlueMax.Infrastructure;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string printerName, out IntPtr hPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool PrinterProperties(IntPtr hWnd, IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool DocumentProperties(IntPtr hWnd, IntPtr hPrinter, string pDeviceName,
        IntPtr pDevModeOutput, IntPtr pDevModeInput, int fMode);

    private const int DM_IN_BUFFER = 8;
    private const int DM_OUT_BUFFER = 2;
    private const int DM_IN_PROMPT = 4;

    ReportDesignerSettings _report = new();
    PrinterSettings _printer = new();
    string _verificationBaseUrl = LoadVerificationBaseUrl();
    string _status = "";
    readonly ObservableCollection<string> _installedPrinters = new();
    string _selectedPrinterName = "";
    string _selectedA4PrinterName = "";
    readonly ObservableCollection<ChoiceItem> _printerProtocolOptions = new();
    ChoiceItem? _selectedPrinterProtocolItem;
    readonly ObservableCollection<ChoiceItem> _tsplMediaOptions = new();
    ChoiceItem? _selectedTsplMediaItem;
    readonly ObservableCollection<ChoiceItem> _tsplCodepageOptions = new();
    ChoiceItem? _selectedTsplCodepageItem;
    int _selectedTabIndex;
    readonly ObservableCollection<string> _loginUsernames = new();
    string _selectedLoginUsername = "";
    string _loginPasswordInput = "";
    readonly ObservableCollection<AdminUserItem> _adminUsers = new();
    AdminUserItem? _selectedAdminUser;
    readonly ObservableCollection<UserSectionPermissionItem> _selectedUserPermissions = new();
    bool _syncingUserPermissions;

    // License
    private string _licenseHardwareId = "";
    private string _licenseStatusDisplay = "";
    private string _licenseExpirationDate = "";
    private string _licenseKeyInput = "";

    // Backup
    private readonly ObservableCollection<BackupItem> _localBackups = new();
    private readonly ObservableCollection<BackupItem> _cloudBackups = new();
    private string _backupPath = "";
    private bool _enableBackupEncryption = true;
    private string _backupEncryptionPassword = "";
    private bool _enableAutoBackup = false;
    private string _autoBackupSchedule = "Daily";
    private string _autoBackupTime = "02:00";
    private int _maxBackupCount = 10;

    public string LicenseHardwareId { get => _licenseHardwareId; set => SetProperty(ref _licenseHardwareId, value); }
    public string LicenseStatusDisplay { get => _licenseStatusDisplay; set => SetProperty(ref _licenseStatusDisplay, value); }
    public string LicenseExpirationDate { get => _licenseExpirationDate; set => SetProperty(ref _licenseExpirationDate, value); }
    public string LicenseKeyInput { get => _licenseKeyInput; set => SetProperty(ref _licenseKeyInput, value); }

    public ObservableCollection<BackupItem> LocalBackups => _localBackups;
    public ObservableCollection<BackupItem> CloudBackups => _cloudBackups;
    public string BackupPath { get => _backupPath; set => SetProperty(ref _backupPath, value); }
    public bool EnableBackupEncryption { get => _enableBackupEncryption; set => SetProperty(ref _enableBackupEncryption, value); }
    public string BackupEncryptionPassword { get => _backupEncryptionPassword; set => SetProperty(ref _backupEncryptionPassword, value); }
    public bool EnableAutoBackup { get => _enableAutoBackup; set => SetProperty(ref _enableAutoBackup, value); }
    public string AutoBackupSchedule { get => _autoBackupSchedule; set => SetProperty(ref _autoBackupSchedule, value); }
    public string AutoBackupTime { get => _autoBackupTime; set => SetProperty(ref _autoBackupTime, value); }
    public int MaxBackupCount { get => _maxBackupCount; set => SetProperty(ref _maxBackupCount, value); }

    public System.Windows.Input.ICommand ActivateLicenseCommand { get; }
    public System.Windows.Input.ICommand CopyHardwareIdCommand { get; }
    public RelayCommand LoginAsAdminCommand { get; }
    public RelayCommand LogoutAdminCommand { get; }
    public RelayCommand ChangeAdminPinCommand { get; }
    public RelayCommand LoginUserCommand { get; }
    public RelayCommand LogoutUserCommand { get; }
    public RelayCommand AddUserCommand { get; }
    public RelayCommand DeleteUserCommand { get; }
    public RelayCommand ChangeUserPasswordCommand { get; }
    public RelayCommand SaveUserPermissionsCommand { get; }
    public RelayCommand CreateBackupCommand { get; }
    public RelayCommand RestoreBackupCommand { get; }
    public RelayCommand ChooseBackupPathCommand { get; }
    public RelayCommand DeleteBackupCommand { get; }
    public RelayCommand SaveBackupSettingsCommand { get; }
    public RelayCommand GenerateBackupPasswordCommand { get; }

    public SettingsViewModel()
    {
        var reportStore = new ReportDesignerSettingsStore();
        _report = reportStore.Load();

        var printerStore = new PrinterSettingsStore();
        _printer = printerStore.Load();

        SaveReportCommand = new RelayCommand(_ => SaveReport());
            ChooseLibreOfficeProgramCommand = new RelayCommand(_ => ChooseLibreOfficeProgram());
        UploadHeaderCommand = new RelayCommand(_ => UploadHeader());
        UploadFooterCommand = new RelayCommand(_ => UploadFooter());
        UploadAppIconCommand = new RelayCommand(_ => UploadAppIcon());
        SavePrinterCommand = new RelayCommand(_ => SavePrinter());
        SaveVerificationCommand = new RelayCommand(_ => SaveVerification());
        OpenPrinterPropertiesCommand = new RelayCommand(_ => OpenPrinterProperties(), _ => !string.IsNullOrWhiteSpace(SelectedPrinterName));
        OpenPrinterPreferencesCommand = new RelayCommand(_ => OpenPrinterPreferences(), _ => !string.IsNullOrWhiteSpace(SelectedPrinterName));
        OpenA4PrinterPropertiesCommand = new RelayCommand(_ => OpenA4PrinterProperties(), _ => !string.IsNullOrWhiteSpace(SelectedA4PrinterName));
        OpenA4PrinterPreferencesCommand = new RelayCommand(_ => OpenA4PrinterPreferences(), _ => !string.IsNullOrWhiteSpace(SelectedA4PrinterName));

        // License
        ActivateLicenseCommand = new RelayCommand(_ => ActivateLicense());
        CopyHardwareIdCommand = new RelayCommand(_ => CopyHardwareId());
        SaveDbConnectionCommand = new RelayCommand(_ => SaveDbConnection());

        LoginAsAdminCommand = new RelayCommand(_ => LoginAsAdmin());
        LogoutAdminCommand = new RelayCommand(_ => LogoutAdmin(), _ => IsAdminSession);
        ChangeAdminPinCommand = new RelayCommand(_ => ChangeAdminPin(), _ => IsAdminSession);
        LoginUserCommand = new RelayCommand(_ => LoginUser(), _ => CanLoginUser());
        LogoutUserCommand = new RelayCommand(_ => LogoutUser(), _ => IsUserLoggedIn);
        AddUserCommand = new RelayCommand(_ => AddUser(), _ => IsAdminSession);
        DeleteUserCommand = new RelayCommand(_ => DeleteUser(), _ => IsAdminSession && SelectedAdminUser != null);
        ChangeUserPasswordCommand = new RelayCommand(_ => ChangeUserPassword(), _ => IsAdminSession && SelectedAdminUser != null);
        SaveUserPermissionsCommand = new RelayCommand(_ => SaveUserPermissions(), _ => IsAdminSession);
        
        // Backup commands
        CreateBackupCommand = new RelayCommand(_ => CreateBackup());
        RestoreBackupCommand = new RelayCommand(_ => RestoreBackup());
        ChooseBackupPathCommand = new RelayCommand(_ => ChooseBackupPath());
        DeleteBackupCommand = new RelayCommand(param => DeleteBackup(param as BackupItem));
        SaveBackupSettingsCommand = new RelayCommand(_ => SaveBackupSettings());
        GenerateBackupPasswordCommand = new RelayCommand(_ => GenerateBackupPassword());
        
        var store = new LicenseStore();
        var licenseService = new LicenseService(store);
        LicenseHardwareId = licenseService.GetHardwareId();
        LoadLicenseInfo(licenseService);

        LoadInstalledPrinters();
        LoadA4PrinterName();
        LoadPrinterProtocols();
        LoadTsplMediaOptions();
        LoadTsplCodepageOptions();
        LoadUsersAndPermissions();
        LoadBackups();
        LoadBackupSettings();
        BlueMax.Presentation.Wpf.App.AccessControl.Changed += (_, __) => OnAccessControlChanged();

        try
        {
            using var db = DbContextFactory.CreateDbContext();
            var provider = db.Database.ProviderName ?? "";
            CurrentDatabaseProvider = provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ? "SQLite" :
                                      provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ? "SQL Server" :
                                      provider;
        }
        catch
        {
            CurrentDatabaseProvider = "";
        }
    }

    private void CopyHardwareId()
    {
        if (!string.IsNullOrEmpty(LicenseHardwareId))
        {
            System.Windows.Clipboard.SetText(LicenseHardwareId);
            Status = "تم نسخ بصمة الجهاز.";
        }
    }

    private void LoadLicenseInfo(LicenseService service)
    {
        var (isValid, expDate) = service.GetLicenseDetails();
        if (isValid)
        {
            LicenseStatusDisplay = "نشط (Active)";
            LicenseExpirationDate = expDate.ToString("yyyy-MM-dd");
        }
        else
        {
            LicenseStatusDisplay = "غير نشط / منتهي (Inactive)";
            LicenseExpirationDate = "-";
        }
    }

    private void ActivateLicense()
    {
        var store = new LicenseStore();
        var service = new LicenseService(store);
        if (service.TryActivate(LicenseKeyInput?.Trim() ?? string.Empty))
        {
            Status = "تم تفعيل البرنامج بنجاح!";
            LoadLicenseInfo(service);
            LicenseKeyInput = "";
        }
        else
        {
            Status = "فشل التفعيل. تأكد من صحة المفتاح.";
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public bool IsAdminSession => BlueMax.Presentation.Wpf.App.AccessControl.IsAdminSession;

    public bool IsUserLoggedIn => BlueMax.Presentation.Wpf.App.AccessControl.IsUserLoggedIn;

    public string SessionRoleDisplay
    {
        get
        {
            if (IsAdminSession)
                return "مدير";
            if (IsUserLoggedIn)
                return $"مستخدم: {BlueMax.Presentation.Wpf.App.AccessControl.CurrentUserName}";
            return "مستخدم";
        }
    }

    public ObservableCollection<string> LoginUsernames => _loginUsernames;

    public string SelectedLoginUsername
    {
        get => _selectedLoginUsername;
        set
        {
            if (!SetProperty(ref _selectedLoginUsername, value ?? ""))
                return;
            LoginUserCommand.RaiseCanExecuteChanged();
        }
    }

    public string LoginPasswordInput
    {
        get => _loginPasswordInput;
        set
        {
            if (!SetProperty(ref _loginPasswordInput, value ?? ""))
                return;
            LoginUserCommand.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<AdminUserItem> AdminUsers => _adminUsers;

    public AdminUserItem? SelectedAdminUser
    {
        get => _selectedAdminUser;
        set
        {
            if (!SetProperty(ref _selectedAdminUser, value))
                return;
            LoadSelectedUserPermissions();
            DeleteUserCommand.RaiseCanExecuteChanged();
            ChangeUserPasswordCommand.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<UserSectionPermissionItem> SelectedUserPermissions => _selectedUserPermissions;

    public ObservableCollection<string> InstalledPrinters => _installedPrinters;
    public ObservableCollection<ChoiceItem> PrinterProtocolOptions => _printerProtocolOptions;
    public ObservableCollection<ChoiceItem> TsplMediaOptions => _tsplMediaOptions;
    public ObservableCollection<ChoiceItem> TsplCodepageOptions => _tsplCodepageOptions;
    

    public string SelectedPrinterName
    {
        get => _selectedPrinterName;
        set
        {
            if (!SetProperty(ref _selectedPrinterName, value))
                return;
            _printer.PrinterName = value ?? "";
            if (string.IsNullOrWhiteSpace(_printer.ConnectionType))
                _printer.ConnectionType = "USB";
            try
            {
                var store = new PrinterSettingsStore();
                store.Save(_printer);
                Status = "تم حفظ اختيار الطابعة تلقائيًا.";
            }
            catch
            {
            }
        }
    }

    public string SelectedA4PrinterName
    {
        get => _selectedA4PrinterName;
        set
        {
            if (!SetProperty(ref _selectedA4PrinterName, value))
                return;
            SaveAppSetting("A4PrinterName", value ?? "");
            Status = "تم حفظ طابعة A4.";
        }
    }

    public bool PrintWithBackground
    {
        get => _report?.PrintWithBackground ?? false;
        set
        {
            if (_report?.PrintWithBackground != value)
            {
                if (_report != null)
                    _report.PrintWithBackground = value;
                OnPropertyChanged();
            }
        }
    }
    public string LibreOfficeProgramPath
    {
        get => _report?.LibreOfficeProgramPath ?? string.Empty;
        set
        {
            if (_report?.LibreOfficeProgramPath != value)
            {
                if (_report != null)
                    _report.LibreOfficeProgramPath = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public double MarginTopMm
    {
        get => _report?.MarginTopMm ?? 0;
        set
        {
            if (Math.Abs(_report?.MarginTopMm ?? 0 - value) > 0.001)
            {
                if (_report != null)
                    _report.MarginTopMm = value;
                OnPropertyChanged();
            }
        }
    }

    public double MarginBottomMm
    {
        get => _report?.MarginBottomMm ?? 0;
        set
        {
            if (Math.Abs(_report?.MarginBottomMm ?? 0 - value) > 0.001)
            {
                if (_report != null)
                    _report.MarginBottomMm = value;
                OnPropertyChanged();
            }
        }
    }

    public double MarginLeftMm
    {
        get => _report?.MarginLeftMm ?? 0;
        set
        {
            if (Math.Abs(_report?.MarginLeftMm ?? 0 - value) > 0.001)
            {
                if (_report != null)
                    _report.MarginLeftMm = value;
                OnPropertyChanged();
            }
        }
    }

    public double MarginRightMm
    {
        get => _report?.MarginRightMm ?? 0;
        set
        {
            if (Math.Abs(_report?.MarginRightMm ?? 0 - value) > 0.001)
            {
                if (_report != null)
                    _report.MarginRightMm = value;
                OnPropertyChanged();
            }
        }
    }

    public string HeaderImagePath
    {
        get => _report.HeaderImagePath ?? string.Empty;
        set
        {
            if (_report.HeaderImagePath != value)
            {
                _report.HeaderImagePath = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public string CompanyName
    {
        get => _report.CompanyName ?? string.Empty;
        set
        {
            if (_report.CompanyName != value)
            {
                _report.CompanyName = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public string CompanyHeader
    {
        get => _report.CompanyHeader ?? string.Empty;
        set
        {
            if (_report.CompanyHeader != value)
            {
                _report.CompanyHeader = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public string CompanyAddress
    {
        get => _report.CompanyAddress ?? string.Empty;
        set
        {
            if (_report.CompanyAddress != value)
            {
                _report.CompanyAddress = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public string CompanyPhone
    {
        get => _report.CompanyPhone ?? string.Empty;
        set
        {
            if (_report.CompanyPhone != value)
            {
                _report.CompanyPhone = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public string FooterImagePath
    {
        get => _report.FooterImagePath ?? string.Empty;
        set
        {
            if (_report.FooterImagePath != value)
            {
                _report.FooterImagePath = value ?? "";
                OnPropertyChanged();
            }
        }
    }
    public string AppIconPath
    {
        get => _report.AppIconPath ?? string.Empty;
        set
        {
            if (_report.AppIconPath != value)
            {
                _report.AppIconPath = value ?? "";
                OnPropertyChanged();
            }
        }
    }

    public string PrinterType
    {
        get => _printer.PrinterType ?? string.Empty;
        set
        {
            if (_printer.PrinterType != value)
            {
                _printer.PrinterType = value ?? "";
                OnPropertyChanged(nameof(PrinterType));
            }
        }
    }

    public string ConnectionType
    {
        get => _printer.ConnectionType ?? string.Empty;
        set
        {
            if (_printer.ConnectionType != value)
            {
                _printer.ConnectionType = value ?? "";
                OnPropertyChanged(nameof(ConnectionType));
            }
        }
    }

    public string PrinterName
    {
        get => _printer.PrinterName ?? string.Empty;
        set
        {
            if (_printer.PrinterName != value)
            {
                _printer.PrinterName = value ?? "";
                OnPropertyChanged(nameof(PrinterName));
            }
        }
    }

    public string NetworkHost
    {
        get => _printer.NetworkHost ?? string.Empty;
        set
        {
            if (_printer.NetworkHost != value)
            {
                _printer.NetworkHost = value ?? "";
                OnPropertyChanged(nameof(NetworkHost));
            }
        }
    }

    public int NetworkPort
    {
        get => _printer.NetworkPort;
        set
        {
            _printer.NetworkPort = value;
            SetProperty(ref value, value, nameof(NetworkPort));
        }
    }

    public int LabelWidthMm
    {
        get => _printer.LabelWidthMm;
        set
        {
            _printer.LabelWidthMm = value;
            SetProperty(ref value, value, nameof(LabelWidthMm));
        }
    }

    public int LabelHeightMm
    {
        get => _printer.LabelHeightMm;
        set
        {
            _printer.LabelHeightMm = value;
            SetProperty(ref value, value, nameof(LabelHeightMm));
        }
    }

    public int Darkness
    {
        get => _printer.Darkness;
        set
        {
            _printer.Darkness = value;
            SetProperty(ref value, value, nameof(Darkness));
        }
    }

    public int SpeedIps
    {
        get => _printer.SpeedIps;
        set
        {
            _printer.SpeedIps = value;
            SetProperty(ref value, value, nameof(SpeedIps));
        }
    }

    public bool ThermalSafeMode
    {
        get => _printer.ThermalSafeMode;
        set
        {
            _printer.ThermalSafeMode = value;
            SetProperty(ref value, value, nameof(ThermalSafeMode));
        }
    }

    public double ThermalCoolingDelaySeconds
    {
        get => _printer.ThermalCoolingDelaySeconds;
        set
        {
            _printer.ThermalCoolingDelaySeconds = value < 0 ? 0 : value;
            SetProperty(ref value, value, nameof(ThermalCoolingDelaySeconds));
        }
    }

    public int DotsPerMm
    {
        get => _printer.DotsPerMm;
        set
        {
            _printer.DotsPerMm = value;
            SetProperty(ref value, value, nameof(DotsPerMm));
        }
    }

    public string VerificationBaseUrl
    {
        get => _verificationBaseUrl;
        set => SetProperty(ref _verificationBaseUrl, value ?? "");
    }

    public RelayCommand SaveReportCommand { get; }
    public RelayCommand ChooseLibreOfficeProgramCommand { get; }
    public RelayCommand UploadHeaderCommand { get; }
    public RelayCommand UploadFooterCommand { get; }
    public RelayCommand UploadAppIconCommand { get; }
    public RelayCommand SavePrinterCommand { get; }
    public RelayCommand SaveVerificationCommand { get; }
    public RelayCommand OpenPrinterPropertiesCommand { get; }
    public RelayCommand OpenPrinterPreferencesCommand { get; }
    public RelayCommand OpenA4PrinterPropertiesCommand { get; }
    public RelayCommand OpenA4PrinterPreferencesCommand { get; }
    public RelayCommand SaveDbConnectionCommand { get; }

    void LoadInstalledPrinters()
    {
        try
        {
            _installedPrinters.Clear();
            foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                _installedPrinters.Add(name);

            var saved = _printer.PrinterName ?? "";
            var defaultName = new System.Drawing.Printing.PrinterSettings().PrinterName;
            SelectedPrinterName = string.IsNullOrWhiteSpace(saved) ? defaultName : saved;
        }
        catch
        {
        }
    }

    void LoadA4PrinterName()
    {
        try
        {
            var saved = ConfigurationManager.AppSettings["A4PrinterName"] ?? "";
            if (string.IsNullOrWhiteSpace(saved))
            {
                var defaultName = new System.Drawing.Printing.PrinterSettings().PrinterName;
                SelectedA4PrinterName = defaultName;
            }
            else
            {
                SelectedA4PrinterName = saved;
            }
        }
        catch
        {
        }
    }

    public ChoiceItem? SelectedPrinterProtocolItem
    {
        get => _selectedPrinterProtocolItem;
        set
        {
            if (!SetProperty(ref _selectedPrinterProtocolItem, value))
                return;
            if (value != null)
            {
                _printer.Protocol = value.Code;
                try
                {
                    new PrinterSettingsStore().Save(_printer);
                    Status = "تم حفظ موديل/بروتوكول الطابعة.";
                }
                catch
                {
                }
            }
        }
    }

    void LoadPrinterProtocols()
    {
        _printerProtocolOptions.Clear();
        _printerProtocolOptions.Add(new ChoiceItem { Code = "ZPL", Label = "Zebra (ZPL II)" });
        _printerProtocolOptions.Add(new ChoiceItem { Code = "TSPL", Label = "TSC/Xprinter (TSPL)" });
        _printerProtocolOptions.Add(new ChoiceItem { Code = "Windows", Label = "Generic Windows Driver" });
        var current = _printer.Protocol ?? "ZPL";
        SelectedPrinterProtocolItem = _printerProtocolOptions.FirstOrDefault(o => string.Equals(o.Code, current, System.StringComparison.OrdinalIgnoreCase))
                                       ?? _printerProtocolOptions[0];
    }

    public ChoiceItem? SelectedTsplMediaItem
    {
        get => _selectedTsplMediaItem;
        set
        {
            if (!SetProperty(ref _selectedTsplMediaItem, value))
                return;
            if (value != null)
            {
                _printer.TsplMediaType = value.Code;
                try { new PrinterSettingsStore().Save(_printer); } catch { }
            }
        }
    }

    void LoadTsplMediaOptions()
    {
        _tsplMediaOptions.Clear();
        _tsplMediaOptions.Add(new ChoiceItem { Code = "Gap", Label = "Gap" });
        _tsplMediaOptions.Add(new ChoiceItem { Code = "BlackMark", Label = "Black Mark" });
        _tsplMediaOptions.Add(new ChoiceItem { Code = "Continuous", Label = "Continuous" });
        var current = _printer.TsplMediaType ?? "Gap";
        SelectedTsplMediaItem = _tsplMediaOptions.FirstOrDefault(o => string.Equals(o.Code, current, StringComparison.OrdinalIgnoreCase))
                                ?? _tsplMediaOptions[0];
    }

    public ChoiceItem? SelectedTsplCodepageItem
    {
        get => _selectedTsplCodepageItem;
        set
        {
            if (!SetProperty(ref _selectedTsplCodepageItem, value))
                return;
            if (value != null)
            {
                _printer.TsplCodepage = value.Code;
                try { new PrinterSettingsStore().Save(_printer); } catch { }
            }
        }
    }

    void LoadTsplCodepageOptions()
    {
        _tsplCodepageOptions.Clear();
        _tsplCodepageOptions.Add(new ChoiceItem { Code = "1252", Label = "Latin (CP1252)" });
        _tsplCodepageOptions.Add(new ChoiceItem { Code = "1256", Label = "Arabic (CP1256)" });
        _tsplCodepageOptions.Add(new ChoiceItem { Code = "65001", Label = "UTF-8 (65001)" });
        _tsplCodepageOptions.Add(new ChoiceItem { Code = "936", Label = "GBK (936)" });
        var current = string.IsNullOrWhiteSpace(_printer.TsplCodepage) ? "65001" : _printer.TsplCodepage;
        SelectedTsplCodepageItem = _tsplCodepageOptions.FirstOrDefault(o => string.Equals(o.Code, current, StringComparison.OrdinalIgnoreCase))
                                   ?? _tsplCodepageOptions[2];
    }

    public double TsplGapMm
    {
        get => _printer.TsplGapMm;
        set
        {
            _printer.TsplGapMm = (int)Math.Max(0, value);
            SetProperty(ref value, value, nameof(TsplGapMm));
        }
    }

    public double TsplGapOffsetMm
    {
        get => _printer.TsplGapOffsetMm;
        set
        {
            _printer.TsplGapOffsetMm = (int)Math.Max(0, value);
            SetProperty(ref value, value, nameof(TsplGapOffsetMm));
        }
    }

    public double TsplBlackMarkOffsetMm
    {
        get => _printer.TsplBlackMarkOffsetMm;
        set
        {
            _printer.TsplBlackMarkOffsetMm = (int)Math.Max(0, value);
            SetProperty(ref value, value, nameof(TsplBlackMarkOffsetMm));
        }
    }

    public int TsplDirection
    {
        get => _printer.TsplDirection;
        set
        {
            _printer.TsplDirection = value < 0 ? 0 : (value > 1 ? 1 : value);
            SetProperty(ref value, value, nameof(TsplDirection));
        }
    }

    public int TsplCopies
    {
        get => _printer.TsplCopies;
        set
        {
            _printer.TsplCopies = value <= 0 ? 1 : value;
            SetProperty(ref value, value, nameof(TsplCopies));
        }
    }

    public int TsplReferenceX
    {
        get => _printer.TsplReferenceX;
        set
        {
            _printer.TsplReferenceX = Math.Max(0, value);
            SetProperty(ref value, value, nameof(TsplReferenceX));
        }
    }

    public int TsplReferenceY
    {
        get => _printer.TsplReferenceY;
        set
        {
            _printer.TsplReferenceY = Math.Max(0, value);
            SetProperty(ref value, value, nameof(TsplReferenceY));
        }
    }

    public bool TsplCalibrateOnPrint
    {
        get => _printer.TsplCalibrateOnPrint;
        set
        {
            _printer.TsplCalibrateOnPrint = value;
            SetProperty(ref value, value, nameof(TsplCalibrateOnPrint));
        }
    }

    void OpenPrinterProperties()
    {
        var name = SelectedPrinterName ?? _printer.PrinterName ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("الرجاء اختيار طابعة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            IntPtr hPrinter = IntPtr.Zero;
            if (OpenPrinter(name, out hPrinter, IntPtr.Zero))
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("لم يتم العثور على النافذة الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                if (helper.Handle == IntPtr.Zero)
                {
                    MessageBox.Show("مقبض النافذة غير صالح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (PrinterProperties(helper.Handle, hPrinter))
                {
                    Status = "تم فتح خصائص الطابعة بنجاح";
                }
                else
                {
                    MessageBox.Show("فشل فتح خصائص الطابعة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                ClosePrinter(hPrinter);
            }
            else
            {
                MessageBox.Show("فشل الوصول للطابعة المحددة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل فتح الخصائص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void OpenPrinterPreferences()
    {
        var name = SelectedPrinterName ?? _printer.PrinterName ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("الرجاء اختيار طابعة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            IntPtr hPrinter = IntPtr.Zero;
            if (OpenPrinter(name, out hPrinter, IntPtr.Zero))
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("لم يتم العثور على النافذة الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                if (helper.Handle == IntPtr.Zero)
                {
                    MessageBox.Show("مقبض النافذة غير صالح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (DocumentProperties(helper.Handle, hPrinter, name, IntPtr.Zero, IntPtr.Zero, DM_IN_PROMPT))
                {
                    Status = "تم فتح تفضيلات الطابعة بنجاح";
                }
                else
                {
                    MessageBox.Show("فشل فتح تفضيلات الطابعة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                ClosePrinter(hPrinter);
            }
            else
            {
                MessageBox.Show("فشل الوصول للطابعة المحددة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل فتح التفضيلات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void OpenA4PrinterProperties()
    {
        var name = SelectedA4PrinterName ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("الرجاء اختيار طابعة A4 أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            IntPtr hPrinter = IntPtr.Zero;
            if (OpenPrinter(name, out hPrinter, IntPtr.Zero))
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("لم يتم العثور على النافذة الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                if (helper.Handle == IntPtr.Zero)
                {
                    MessageBox.Show("مقبض النافذة غير صالح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (PrinterProperties(helper.Handle, hPrinter))
                {
                    Status = "تم فتح خصائص طابعة A4 بنجاح";
                }
                else
                {
                    MessageBox.Show("فشل فتح خصائص طابعة A4", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                ClosePrinter(hPrinter);
            }
            else
            {
                MessageBox.Show("فشل الوصول للطابعة المحددة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل فتح الخصائص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void OpenA4PrinterPreferences()
    {
        var name = SelectedA4PrinterName ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("الرجاء اختيار طابعة A4 أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            IntPtr hPrinter = IntPtr.Zero;
            if (OpenPrinter(name, out hPrinter, IntPtr.Zero))
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    MessageBox.Show("لم يتم العثور على النافذة الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var helper = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                if (helper.Handle == IntPtr.Zero)
                {
                    MessageBox.Show("مقبض النافذة غير صالح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (DocumentProperties(helper.Handle, hPrinter, name, IntPtr.Zero, IntPtr.Zero, DM_IN_PROMPT))
                {
                    Status = "تم فتح تفضيلات طابعة A4 بنجاح";
                }
                else
                {
                    MessageBox.Show("فشل فتح تفضيلات طابعة A4", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                ClosePrinter(hPrinter);
            }
            else
            {
                MessageBox.Show("فشل الوصول للطابعة المحددة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل فتح التفضيلات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void SaveReport()
    {
        var store = new ReportDesignerSettingsStore();
        var path = AppIconPath ?? "";
        try
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (System.IO.Directory.Exists(path))
                {
                    var file = System.IO.Directory.EnumerateFiles(path)
                        .FirstOrDefault(f =>
                        {
                            var ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                            return ext is ".ico" or ".png" or ".jpg" or ".jpeg" or ".bmp";
                        });
                    if (!string.IsNullOrWhiteSpace(file))
                        _report.AppIconPath = store.SaveAsset(file, "AppIcon");
                }
                else if (System.IO.File.Exists(path))
                {
                    _report.AppIconPath = store.SaveAsset(path, "AppIcon");
                }
            }
        }
        catch
        {
        }
        store.Save(_report);
        Status = "تم حفظ إعدادات التقرير.";
    }
    
    private string _dbConnectionStringInput = "";
    private string _currentDatabaseProvider = "";

    public string DbConnectionStringInput
    {
        get => _dbConnectionStringInput;
        set => SetProperty(ref _dbConnectionStringInput, value);
    }

    public string CurrentDatabaseProvider
    {
        get => _currentDatabaseProvider;
        set => SetProperty(ref _currentDatabaseProvider, value);
    }

    void SaveDbConnection()
    {
        try
        {
            var cs = DbConnectionStringInput ?? "";
            SecureConnectionStringStore.SaveConnectionString(cs);
            Status = "تم حفظ إعداد قاعدة البيانات. يرجى إعادة تشغيل البرنامج لتطبيق التغييرات.";
        }
        catch
        {
            Status = "تعذر حفظ إعداد قاعدة البيانات.";
        }
    }
    void ChooseLibreOfficeProgram()
    {
        var dialog = new OpenFileDialog { Filter = "soffice.exe|soffice.exe" };
        if (dialog.ShowDialog() != true)
            return;
        var file = dialog.FileName;
        if (!string.IsNullOrWhiteSpace(file))
        {
            var dir = System.IO.Path.GetDirectoryName(file) ?? "";
            LibreOfficeProgramPath = dir;
        }
    }

    void UploadHeader()
    {
        var dialog = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog() != true)
            return;
        var store = new ReportDesignerSettingsStore();
        HeaderImagePath = store.SaveAsset(dialog.FileName, "Header");
    }

    void UploadFooter()
    {
        var dialog = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog() != true)
            return;
        var store = new ReportDesignerSettingsStore();
        FooterImagePath = store.SaveAsset(dialog.FileName, "Footer");
    }

    void UploadAppIcon()
    {
        var dialog = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.ico" };
        if (dialog.ShowDialog() != true)
            return;
        var store = new ReportDesignerSettingsStore();
        AppIconPath = store.SaveAsset(dialog.FileName, "AppIcon");
    }

    void SavePrinter()
    {
        try
        {
            var store = new PrinterSettingsStore();
            store.Save(_printer);
            MessageBox.Show("تم حفظ إعدادات الطابعة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            Status = "تم حفظ إعدادات الطابعة.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل حفظ إعدادات الطابعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void SaveVerification()
    {
        BlueMax.Presentation.Wpf.Services.AppSettingHelper.Save("VerificationBaseUrl", VerificationBaseUrl ?? "");
        Status = "تم حفظ رابط التحقق.";
    }

    static string LoadVerificationBaseUrl()
    {
        return ConfigurationManager.AppSettings["VerificationBaseUrl"] ?? "";
    }

    static void SaveAppSetting(string key, string value)
    {
        BlueMax.Presentation.Wpf.Services.AppSettingHelper.Save(key, value);
    }

    public sealed class ChoiceItem
    {
        public string Code { get; set; } = "";
        public string Label { get; set; } = "";
    }

    void OnAccessControlChanged()
    {
        OnPropertyChanged(nameof(IsAdminSession));
        OnPropertyChanged(nameof(IsUserLoggedIn));
        OnPropertyChanged(nameof(SessionRoleDisplay));
        LogoutAdminCommand.RaiseCanExecuteChanged();
        ChangeAdminPinCommand.RaiseCanExecuteChanged();
        LoginUserCommand.RaiseCanExecuteChanged();
        LogoutUserCommand.RaiseCanExecuteChanged();
        AddUserCommand.RaiseCanExecuteChanged();
        DeleteUserCommand.RaiseCanExecuteChanged();
        ChangeUserPasswordCommand.RaiseCanExecuteChanged();
        SaveUserPermissionsCommand.RaiseCanExecuteChanged();
        LoadUsersAndPermissions();
    }

    void LoginAsAdmin()
    {
        if (BlueMax.Presentation.Wpf.App.AccessControl.TryLoginAsAdmin())
        {
            Status = "تم تسجيل الدخول كمدير.";
            SelectedTabIndex = 2;
            LoadUsersAndPermissions();
        }
    }

    void LogoutAdmin()
    {
        BlueMax.Presentation.Wpf.App.AccessControl.LogoutAdmin();
        Status = "تم تسجيل الخروج.";
    }

    void ChangeAdminPin()
    {
        if (BlueMax.Presentation.Wpf.App.AccessControl.TryChangeAdminPin())
            Status = "تم تغيير الرقم السري.";
    }

    void LoadUsersAndPermissions()
    {
        RefreshUserLists();
        LoadSelectedUserPermissions();
    }

    void RefreshUserLists()
    {
        _loginUsernames.Clear();
        foreach (var username in BlueMax.Presentation.Wpf.App.AccessControl.GetUsernames())
            _loginUsernames.Add(username);

        if (string.IsNullOrWhiteSpace(SelectedLoginUsername) && _loginUsernames.Count > 0)
            SelectedLoginUsername = _loginUsernames[0];

        _adminUsers.Clear();
        foreach (var u in BlueMax.Presentation.Wpf.App.AccessControl.GetUsers())
            _adminUsers.Add(new AdminUserItem(u.Username));

        if (SelectedAdminUser == null && _adminUsers.Count > 0)
            SelectedAdminUser = _adminUsers[0];
        else if (SelectedAdminUser != null && !_adminUsers.Any(u => string.Equals(u.Username, SelectedAdminUser.Username, StringComparison.OrdinalIgnoreCase)))
            SelectedAdminUser = _adminUsers.Count > 0 ? _adminUsers[0] : null;
    }

    void LoadSelectedUserPermissions()
    {
        _syncingUserPermissions = true;
        try
        {
            _selectedUserPermissions.Clear();
            var selected = SelectedAdminUser?.Username ?? "";
            if (string.IsNullOrWhiteSpace(selected))
                return;

            foreach (var (code, title) in BlueMax.Presentation.Wpf.App.AccessControl.Sections)
            {
                var allowed = BlueMax.Presentation.Wpf.App.AccessControl.IsUserSectionAllowed(selected, code);
                _selectedUserPermissions.Add(new UserSectionPermissionItem(selected, code, title, allowed, this));
            }
        }
        finally
        {
            _syncingUserPermissions = false;
        }
    }

    bool CanLoginUser()
    {
        if (IsAdminSession)
            return false;
        if (string.IsNullOrWhiteSpace(SelectedLoginUsername))
            return false;
        return !string.IsNullOrWhiteSpace(LoginPasswordInput);
    }

    void LoginUser()
    {
        if (!CanLoginUser())
            return;
        var ok = BlueMax.Presentation.Wpf.App.AccessControl.TryLoginUser(SelectedLoginUsername, LoginPasswordInput);
        LoginPasswordInput = "";
        Status = ok ? "تم تسجيل الدخول كمستخدم." : "فشل تسجيل الدخول. تأكد من اسم المستخدم وكلمة المرور.";
    }

    void LogoutUser()
    {
        BlueMax.Presentation.Wpf.App.AccessControl.LogoutUser();
        LoginPasswordInput = "";
        Status = "تم تسجيل الخروج.";
    }

    void AddUser()
    {
        if (!IsAdminSession)
            return;

        var (username, password) = PromptForNewUser();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return;

        if (!BlueMax.Presentation.Wpf.App.AccessControl.TryAddUser(username, password))
        {
            Status = "تعذر إضافة المستخدم. تأكد من أن الاسم غير مكرر.";
            return;
        }

        Status = "تم إضافة المستخدم.";
        LoadUsersAndPermissions();
        SelectedAdminUser = _adminUsers.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)) ?? SelectedAdminUser;
    }

    void DeleteUser()
    {
        if (!IsAdminSession || SelectedAdminUser == null)
            return;
        if (!BlueMax.Presentation.Wpf.App.AccessControl.TryDeleteUser(SelectedAdminUser.Username))
        {
            Status = "تعذر حذف المستخدم.";
            return;
        }
        Status = "تم حذف المستخدم.";
        LoadUsersAndPermissions();
    }

    void ChangeUserPassword()
    {
        if (!IsAdminSession || SelectedAdminUser == null)
            return;

        var password = PromptForPassword($"تغيير كلمة المرور: {SelectedAdminUser.Username}", "أدخل كلمة المرور الجديدة:");
        if (string.IsNullOrWhiteSpace(password))
            return;

        if (!BlueMax.Presentation.Wpf.App.AccessControl.TryChangeUserPassword(SelectedAdminUser.Username, password))
        {
            Status = "تعذر تغيير كلمة المرور.";
            return;
        }
        Status = "تم تغيير كلمة المرور.";
    }

    void SaveUserPermissions()
    {
        if (!IsAdminSession)
            return;
        BlueMax.Presentation.Wpf.App.AccessControl.SaveUsersNow();
        Status = "تم حفظ صلاحيات المستخدمين.";
    }

    static (string Username, string Password) PromptForNewUser()
    {
        try
        {
            var owner = System.Windows.Application.Current?.MainWindow;

            var usernameBox = new System.Windows.Controls.TextBox
            {
                Width = 260,
                Margin = new System.Windows.Thickness(0, 6, 0, 0),
                FontSize = 14
            };

            var passwordBox = new System.Windows.Controls.PasswordBox
            {
                Width = 260,
                Margin = new System.Windows.Thickness(0, 6, 0, 0),
                FontSize = 14
            };

            var confirmBox = new System.Windows.Controls.PasswordBox
            {
                Width = 260,
                Margin = new System.Windows.Thickness(0, 6, 0, 0),
                FontSize = 14
            };

            var ok = new System.Windows.Controls.Button
            {
                Content = "حفظ",
                Width = 90,
                Margin = new System.Windows.Thickness(0, 0, 8, 0),
                IsDefault = true
            };

            var cancel = new System.Windows.Controls.Button
            {
                Content = "إلغاء",
                Width = 90,
                IsCancel = true
            };

            var buttons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new System.Windows.Thickness(0, 14, 0, 0)
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var root = new System.Windows.Controls.StackPanel
            {
                Margin = new System.Windows.Thickness(18),
                FlowDirection = System.Windows.FlowDirection.RightToLeft
            };
            root.Children.Add(new System.Windows.Controls.TextBlock { Text = "اسم المستخدم", FontSize = 13 });
            root.Children.Add(usernameBox);
            root.Children.Add(new System.Windows.Controls.TextBlock { Text = "كلمة المرور", FontSize = 13, Margin = new System.Windows.Thickness(0, 8, 0, 0) });
            root.Children.Add(passwordBox);
            root.Children.Add(new System.Windows.Controls.TextBlock { Text = "تأكيد كلمة المرور", FontSize = 13, Margin = new System.Windows.Thickness(0, 8, 0, 0) });
            root.Children.Add(confirmBox);
            root.Children.Add(buttons);

            var window = new System.Windows.Window
            {
                Title = "إضافة مستخدم جديد",
                Content = root,
                SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                WindowStartupLocation = owner != null ? System.Windows.WindowStartupLocation.CenterOwner : System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.ToolWindow,
                Owner = owner,
                Topmost = true,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Background = System.Windows.Media.Brushes.White
            };

            ok.Click += (_, __) =>
            {
                var u = (usernameBox.Text ?? "").Trim();
                var p = passwordBox.Password ?? "";
                var c = confirmBox.Password ?? "";
                if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p) || !string.Equals(p, c, StringComparison.Ordinal))
                    return;
                window.DialogResult = true;
                window.Close();
            };

            usernameBox.Loaded += (_, __) => usernameBox.Focus();
            var result = window.ShowDialog();
            if (result == true)
                return ((usernameBox.Text ?? "").Trim(), passwordBox.Password ?? "");
            return ("", "");
        }
        catch
        {
            return ("", "");
        }
    }

    static string PromptForPassword(string title, string message)
    {
        try
        {
            var owner = System.Windows.Application.Current?.MainWindow;

            var passwordBox = new System.Windows.Controls.PasswordBox
            {
                Width = 260,
                Margin = new System.Windows.Thickness(0, 8, 0, 0),
                FontSize = 14
            };

            var ok = new System.Windows.Controls.Button
            {
                Content = "موافق",
                Width = 90,
                Margin = new System.Windows.Thickness(0, 0, 8, 0),
                IsDefault = true
            };

            var cancel = new System.Windows.Controls.Button
            {
                Content = "إلغاء",
                Width = 90,
                IsCancel = true
            };

            var buttons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new System.Windows.Thickness(0, 14, 0, 0)
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var root = new System.Windows.Controls.StackPanel
            {
                Margin = new System.Windows.Thickness(18),
                FlowDirection = System.Windows.FlowDirection.RightToLeft
            };
            root.Children.Add(new System.Windows.Controls.TextBlock { Text = message, FontSize = 14 });
            root.Children.Add(passwordBox);
            root.Children.Add(buttons);

            var window = new System.Windows.Window
            {
                Title = title,
                Content = root,
                SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                WindowStartupLocation = owner != null ? System.Windows.WindowStartupLocation.CenterOwner : System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.ToolWindow,
                Owner = owner,
                Topmost = true,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Background = System.Windows.Media.Brushes.White
            };

            ok.Click += (_, __) => { window.DialogResult = true; window.Close(); };
            passwordBox.Loaded += (_, __) => passwordBox.Focus();
            var result = window.ShowDialog();
            if (result == true)
                return passwordBox.Password ?? "";
            return "";
        }
        catch
        {
            return "";
        }
    }

    public sealed class AdminUserItem
    {
        public AdminUserItem(string username)
        {
            Username = username ?? "";
        }

        public string Username { get; }
    }

    public sealed class UserSectionPermissionItem : ViewModelBase
    {
        readonly SettingsViewModel _owner;
        readonly string _username;
        bool _isAllowed;

        public UserSectionPermissionItem(string username, string code, string title, bool allowed, SettingsViewModel owner)
        {
            _username = username ?? "";
            Code = code ?? "";
            Title = title ?? "";
            _owner = owner;
            _isAllowed = allowed;
        }

        public string Code { get; }
        public string Title { get; }

        public bool IsAllowed
        {
            get => _isAllowed;
            set
            {
                if (!SetProperty(ref _isAllowed, value))
                    return;
                if (_owner._syncingUserPermissions)
                    return;
                BlueMax.Presentation.Wpf.App.AccessControl.TrySetUserSectionAllowed(_username, Code, value);
                _owner.Status = "تم حفظ صلاحيات المستخدم.";
            }
        }
    }

    string ResolveBackupPath()
    {
        var settings = BackupSettingsStore.Load();
        var backupPaths = BackupSettingsStore.NormalizeBackupPaths(settings);
        var targetPath = !string.IsNullOrWhiteSpace(BackupPath) ? BackupPath : backupPaths[0];

        if (!backupPaths.Any(p => string.Equals(p, targetPath, StringComparison.OrdinalIgnoreCase)))
        {
            backupPaths.Insert(0, targetPath);
            settings.BackupPaths = backupPaths;
            BackupSettingsStore.Save(settings);
        }

        BackupPath = targetPath;
        return targetPath;
    }

    void LoadBackups()
    {
        try
        {
            _localBackups.Clear();

            var backupPath = ResolveBackupPath();
            Directory.CreateDirectory(backupPath);

            // Load both .db and .enc files
            var files = Directory.GetFiles(backupPath, "BlueMax_Backup_*")
                .Where(f => f.EndsWith(".db") || f.EndsWith(".enc"))
                .OrderByDescending(f => File.GetCreationTime(f));
            
            foreach (var file in files)
            {
                _localBackups.Add(new BackupItem
                {
                    FileName = Path.GetFileName(file),
                    FilePath = file,
                    CreatedDate = File.GetCreationTime(file),
                    Size = new FileInfo(file).Length,
                    IsEncrypted = file.EndsWith(".enc")
                });
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    void LoadBackupSettings()
    {
        try
        {
            var settings = BackupSettingsStore.Load();
            EnableBackupEncryption = settings.EnableEncryption;
            BackupEncryptionPassword = settings.EncryptionPassword;
            EnableAutoBackup = settings.EnableAutoBackup;
            AutoBackupSchedule = settings.AutoBackupSchedule;
            AutoBackupTime = settings.AutoBackupTime;
            MaxBackupCount = settings.MaxBackupCount;
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    void SaveBackupSettings()
    {
        try
        {
            var settings = BackupSettingsStore.Load();
            var backupPaths = BackupSettingsStore.NormalizeBackupPaths(settings);
            if (!string.IsNullOrWhiteSpace(BackupPath) && !backupPaths.Any(p => string.Equals(p, BackupPath, StringComparison.OrdinalIgnoreCase)))
            {
                backupPaths.Insert(0, BackupPath);
                settings.BackupPaths = backupPaths;
            }
            settings.EnableEncryption = EnableBackupEncryption;
            settings.EncryptionPassword = BackupEncryptionPassword;
            settings.EnableAutoBackup = EnableAutoBackup;
            settings.AutoBackupSchedule = AutoBackupSchedule;
            settings.AutoBackupTime = AutoBackupTime;
            settings.MaxBackupCount = MaxBackupCount;
            BackupSettingsStore.Save(settings);
            Status = Resources.Translations.Get(Resources.Translations.English.BackupSettingsSaved);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            Status = Resources.Translations.Get(Resources.Translations.English.BackupSettingsSaveFailed);
        }
    }

    void GenerateBackupPassword()
    {
        BackupEncryptionPassword = BackupEncryption.GenerateEncryptionPassword();
        Status = Resources.Translations.Get(Resources.Translations.English.BackupPasswordGenerated);
    }

    void CreateBackup()
    {
        try
        {
            var backupPath = ResolveBackupPath();
            Directory.CreateDirectory(backupPath);

            // Find the database file
            var dbPath = AppDbContext.GetLocalDbPath();
            if (!File.Exists(dbPath))
            {
                Status = Resources.Translations.Get(Resources.Translations.English.DatabaseFileNotFound);
                return;
            }

            var backupName = $"BlueMax_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var fullPath = Path.Combine(backupPath, backupName);

            BackupService.CreateSqliteBackup(dbPath, fullPath);

            // Check if encryption is enabled
            if (EnableBackupEncryption && !string.IsNullOrWhiteSpace(BackupEncryptionPassword))
            {
                var encryptedName = $"BlueMax_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.enc";
                var encryptedPath = Path.Combine(backupPath, encryptedName);
                BackupEncryption.EncryptFileToFile(fullPath, encryptedPath, BackupEncryptionPassword);
                try
                {
                    File.Delete(fullPath);
                }
                catch (Exception deleteEx)
                {
                    LogService.LogException(deleteEx);
                }
                Status = $"{Resources.Translations.Get(Resources.Translations.English.EncryptedBackupCreated)} {encryptedName}";
            }
            else
            {
                Status = $"{Resources.Translations.Get(Resources.Translations.English.BackupCreated)} {backupName}";
            }

            LoadBackups();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            Status = $"{Resources.Translations.Get(Resources.Translations.English.BackupCreationError)} {ex.Message}";
        }
    }

    void RestoreBackup()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Database Files|*.db;*.enc",
                Title = Resources.Translations.Get(Resources.Translations.English.SelectBackupFile),
                InitialDirectory = BackupPath
            };

            if (dialog.ShowDialog() != true)
                return;

            var selectedPath = dialog.FileName;
            var isEncrypted = selectedPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase);

            // Validate backup before restoring
            if (isEncrypted)
            {
                if (string.IsNullOrWhiteSpace(BackupEncryptionPassword))
                {
                    var password = PromptForPassword(
                        Resources.Translations.Get(Resources.Translations.English.BackupPasswordTitle),
                        Resources.Translations.Get(Resources.Translations.English.EnterBackupPassword));
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        Status = Resources.Translations.Get(Resources.Translations.English.PasswordRequiredForEncrypted);
                        return;
                    }
                    BackupEncryptionPassword = password;
                }

                if (!BackupService.ValidateBackupFile(selectedPath, BackupEncryptionPassword))
                {
                    Status = Resources.Translations.Get(Resources.Translations.English.BackupInvalidOrWrongPassword);
                    return;
                }
            }
            else
            {
                if (!BackupService.ValidateBackupFile(selectedPath))
                {
                    Status = Resources.Translations.Get(Resources.Translations.English.BackupInvalid);
                    return;
                }
            }

            var result = MessageBox.Show(
                Resources.Translations.Get(Resources.Translations.English.ConfirmRestoreBackup),
                Resources.Translations.Get(Resources.Translations.English.ConfirmRestoreTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // Find the database file location
            var dbPath = AppDbContext.GetLocalDbPath();
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            // Restore using BackupService
            BackupService.RestoreBackup(selectedPath, dbPath, isEncrypted ? BackupEncryptionPassword : null);

            Status = Resources.Translations.Get(Resources.Translations.English.BackupRestoredSuccessfully);
            MessageBox.Show("تمت الاستعادة بنجاح. سيتم إغلاق البرنامج لإعادة التشغيل.", "استعادة النسخة الاحتياطية", MessageBoxButton.OK, MessageBoxImage.Information);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            Status = $"{Resources.Translations.Get(Resources.Translations.English.BackupRestoreError)} {ex.Message}";
        }
    }

    void ChooseBackupPath()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All Files|*.*",
                Title = Resources.Translations.Get(Resources.Translations.English.SelectAnyFileInBackupFolder),
                CheckFileExists = false,
                FileName = "Folder Selection"
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    BackupPath = selectedPath;
                    Directory.CreateDirectory(selectedPath);
                    
                    // Update backup settings
                    var settings = BackupSettingsStore.Load();
                    if (!settings.BackupPaths.Contains(selectedPath))
                    {
                        settings.BackupPaths.Add(selectedPath);
                        BackupSettingsStore.Save(settings);
                    }
                    BackupSettingsStore.NormalizeBackupPaths(settings);
                    
                    LoadBackups();
                    Status = $"{Resources.Translations.Get(Resources.Translations.English.BackupPathSelected)} {BackupPath}";
                }
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            Status = $"{Resources.Translations.Get(Resources.Translations.English.BackupPathError)} {ex.Message}";
        }
    }

    void DeleteBackup(BackupItem? backupItem)
    {
        if (backupItem == null)
            return;

        try
        {
            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف النسخة الاحتياطية: {backupItem.FileName}؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            if (File.Exists(backupItem.FilePath))
            {
                File.Delete(backupItem.FilePath);
                LoadBackups();
                Status = $"تم حذف النسخة الاحتياطية: {backupItem.FileName}";
            }
            else
            {
                Status = "الملف غير موجود.";
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            Status = $"خطأ في حذف النسخة الاحتياطية: {ex.Message}";
        }
    }

    public sealed class BackupItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public long Size { get; set; }
        public bool IsEncrypted { get; set; }
        public string SizeDisplay => Size > 1024 * 1024 ? $"{Size / (1024 * 1024)} MB" : Size > 1024 ? $"{Size / 1024} KB" : $"{Size} B";
        public string CreatedDateDisplay => CreatedDate.ToString("yyyy-MM-dd HH:mm");
        public string StatusDisplay => IsEncrypted ? "مشفر" : "غير مشفر";
    }
}
