using System.Windows;
using System.Windows.Threading;
using System.IO;
using System; // Added for Environment
using System.Diagnostics; // Added for Debug.Listeners
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.Views;

namespace BlueMax.Presentation.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
    {
        bool _handlersAttached;
        Mutex? _singleInstanceMutex;
        bool _ownsSingleInstanceMutex;
        // bool _errorShown; // Reserved for future use
        public static AccessControlState AccessControl { get; } = new AccessControlState();
        static BackupService? _backupService;

        public static BackupService? BackupService => _backupService;

    // Static StreamWriter for direct file logging
    private static StreamWriter? _logFileWriter;
    private static readonly object _logLock = new object();
    private static string _logFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlueMax",
        "debug_output.log");

    public App()
        {
            try
            {
                // All numbers (dates, amounts, sizes, codes) must render with
                // English digits regardless of the operating system locale.
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                // Initialize direct file logging
                Log($"Attempting to initialize file logger. Log file path: {_logFilePath}");
                try
                {
                    System.Diagnostics.Debug.WriteLine("Before File.Exists check.");
                    if (File.Exists(_logFilePath))
                    {
                        Log($"Deleting existing log file: {_logFilePath}");
                        File.Delete(_logFilePath); // Clear previous log on startup
                    }
                    else
                    {
                        Log($"Log file does not exist, creating new one: {_logFilePath}");
                    }
                    System.Diagnostics.Debug.WriteLine("Before StreamWriter initialization.");
                    _logFileWriter = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };
                    Log("File logger assigned and AutoFlush set.");
                    Log($"File logger initialized successfully. Application Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                    System.Diagnostics.Debug.WriteLine("File logger initialized successfully.");
                }
                catch (Exception ex)
                {
                    Log($"Error initializing file logger: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Error initializing file logger: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine("App constructor started.");
                Log("App constructor: Before InitializeComponent().");
                InitializeComponent(); // This is where App.xaml is parsed
                Log("App constructor: After InitializeComponent().");

                AttachHandlers();
                System.Diagnostics.Debug.WriteLine("App constructor finished.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception in App constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                try { WriteStartupError(ex); } catch (Exception logEx) { System.Diagnostics.Debug.WriteLine($"Failed to write startup error: {logEx.Message}"); }
                MessageBox.Show($"An unhandled exception occurred during application startup: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(-1);
            }
        }

    // Centralized error handling
    public static void HandleError(Exception ex, string context, bool isFatal = false)
    {
        try
        {
            Log($"ERROR in {context}: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");

            if (isFatal)
            {
                WriteStartupError(ex);
                MessageBox.Show($"A fatal error occurred in {context}:\n{ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(-1);
            }
            else
            {
                MessageBox.Show($"An error occurred in {context}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to handle error: {logEx.Message}");
        }
    }

    // Custom static logging method
    public static void Log(string message)
    {
        lock (_logLock)
        {
            if (_logFileWriter != null)
            {
                _logFileWriter.WriteLine($"[{DateTime.Now}] {message}");
                _logFileWriter.Flush(); // Ensure logs are written immediately
            }
            else
            {
                // Fallback to Debug.WriteLine if file logger is not initialized
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] {message}");
            }
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnStartup method started.");
        var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.CurrentCulture.Clone();
        culture.NumberFormat = (System.Globalization.NumberFormatInfo)System.Globalization.CultureInfo.InvariantCulture.NumberFormat.Clone();
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        Log("OnStartup: Before base.OnStartup(e).");
        base.OnStartup(e);
        Log("OnStartup: After base.OnStartup(e).");

        try
        {
            var createdNew = false;
            _singleInstanceMutex = new Mutex(true, @"Global\BlueMax.Presentation.Wpf.SingleInstance", out createdNew);
            _ownsSingleInstanceMutex = createdNew;
            if (!createdNew)
            {
                MessageBox.Show("البرنامج يعمل بالفعل. لا يمكن فتح أكثر من نسخة في نفس الوقت.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
        }
        catch (Exception mutexEx)
        {
            Log($"Single-instance mutex not acquired (continuing without it): {mutexEx.Message}");
            _singleInstanceMutex = null;
            _ownsSingleInstanceMutex = false;
        }

        try
        {
            var store = new LicenseStore();
            var licenseService = new LicenseService(store);
            var isValid = licenseService.IsActivated();
            if (!isValid)
            {
                var win = new ActivationWindow();
                win.ShowDialog();
                isValid = licenseService.IsActivated();
                if (!isValid)
                {
                    Current.Shutdown(-1);
                    return;
                }
            }
            System.Diagnostics.Debug.WriteLine("Before creating MainWindow.");
            Log("OnStartup: Before new MainWindow().Show().");
            try
            {
                LegacyConfigCompanyMigration.MigrateOnce();
            }
            catch (Exception migrateEx)
            {
                Log($"LegacyConfigCompanyMigration error: {migrateEx.Message}");
            }
            try
            {
                var connectionString = SecureConnectionStringStore.LoadConnectionString()
                    ?? ConfigurationManager.ConnectionStrings["MyDb"]?.ConnectionString;
                var localDbPath = AppDbContext.GetLocalDbPath();
                _backupService = new BackupService(connectionString, localDbPath);
            }
            catch (Exception backupEx)
            {
                LogService.LogException(backupEx);
            }
            try
            {
                using var db = DbContextFactory.CreateDbContext();
                db.Database.EnsureCreated();
                db.EnsureSchemaCompatible();
            }
            catch (Exception schemaEx)
            {
                Log($"EnsureSchemaCompatible error: {schemaEx.Message}");
                LogService.LogException(schemaEx);
            }
            var mainWindow = new MainWindow();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Show();
            Log("OnStartup: After new MainWindow().Show().");
            System.Diagnostics.Debug.WriteLine("MainWindow created and shown.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in OnStartup: {ex.Message}");
            Log($"Exception in OnStartup: {ex.Message}");
            WriteStartupError(ex);
            Shutdown(-1);
        }
        System.Diagnostics.Debug.WriteLine("OnStartup method finished.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            TryBackupDatabaseOnExit();
        }
        catch (Exception ex)
        {
            Log($"Error in OnExit: {ex.Message}");
        }
        base.OnExit(e);
        // Close the log file writer
        lock (_logLock)
        {
            if (_logFileWriter != null)
            {
                _logFileWriter.Close();
                _logFileWriter.Dispose();
                _logFileWriter = null;
            }
        }

        if (_singleInstanceMutex != null)
        {
            if (_ownsSingleInstanceMutex)
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }

    static void TryBackupDatabaseOnExit()
    {
        try
        {
            var dbPath = BlueMax.Infrastructure.AppDbContext.GetLocalDbPath();
            if (!File.Exists(dbPath))
                return;
            var dstDir = AppPaths.Backups;
            Directory.CreateDirectory(dstDir);
            var fileName = $"local_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var dst = Path.Combine(dstDir, fileName);
            File.Copy(dbPath, dst, true);
        }
        catch (Exception ex)
        {
            Log($"Error in TryBackupDatabaseOnExit: {ex.Message}");
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = WriteRuntimeError(e.Exception);
        var isSuspendedError =
            e.Exception is InvalidOperationException &&
            e.Exception.Message.IndexOf("Dispatcher processing has been suspended", StringComparison.OrdinalIgnoreCase) >= 0;
        // Do not show UI popups for unhandled exceptions; rely on log file only
        // _errorShown = true;
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            var logPath = WriteRuntimeError(exception);
            // _errorShown = true; // suppress UI popups
        }

        Shutdown(-1);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var logPath = WriteRuntimeError(e.Exception);
        // _errorShown = true; // suppress UI popups
        e.SetObserved(); // Mark the exception as observed to prevent the process from terminating
    }

    private void AttachHandlers()
    {
        if (_handlersAttached)
        {
            return;
        }

        _handlersAttached = true;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static string WriteStartupError(Exception exception)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "startup-error.txt");
        var logDirectory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        File.WriteAllText(logPath, exception.ToString());
        return logPath;
    }

    private static string WriteRuntimeError(Exception exception)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "runtime-error.txt");
        var logDirectory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        File.AppendAllText(logPath, $"[{DateTime.Now}] {exception.ToString()}{Environment.NewLine}{Environment.NewLine}");
        return logPath;
    }

    public sealed class AccessControlState
    {
        const string AdminPinSaltKey = "AdminPinSalt";
        const string AdminPinHashKey = "AdminPinHash";
        const string UserAllowedSectionsKey = "UserAllowedSections";
        const string UsersJsonKey = "UsersJson";
        const string LastUserNameKey = "LastUserName";
        const string Pbkdf2Prefix = "PBKDF2$";
        const int Pbkdf2Iterations = 100_000;

        // Rate limiting for login attempts
        private static readonly Dictionary<string, List<DateTime>> _loginAttempts = new();
        private static readonly object _loginLock = new();
        private const int MaxLoginAttempts = 5;
        private const int LoginAttemptWindowMinutes = 15;

        public static bool CanAttemptLogin(string username)
        {
            lock (_loginLock)
            {
                var now = DateTime.Now;
                if (!_loginAttempts.ContainsKey(username))
                {
                    _loginAttempts[username] = new List<DateTime>();
                    return true;
                }

                // Remove attempts outside the time window
                _loginAttempts[username] = _loginAttempts[username]
                    .Where(t => (now - t).TotalMinutes < LoginAttemptWindowMinutes)
                    .ToList();

                // Check if too many attempts
                return _loginAttempts[username].Count < MaxLoginAttempts;
            }
        }

        public static void RecordLoginAttempt(string username, bool success)
        {
            lock (_loginLock)
            {
                if (success)
                {
                    // Clear attempts on successful login
                    if (_loginAttempts.ContainsKey(username))
                        _loginAttempts[username].Clear();
                }
                else
                {
                    // Record failed attempt
                    if (!_loginAttempts.ContainsKey(username))
                        _loginAttempts[username] = new List<DateTime>();
                    _loginAttempts[username].Add(DateTime.Now);
                }
            }
        }

        public static int GetRemainingAttempts(string username)
        {
            lock (_loginLock)
            {
                var now = DateTime.Now;
                if (!_loginAttempts.ContainsKey(username))
                    return MaxLoginAttempts;

                _loginAttempts[username] = _loginAttempts[username]
                    .Where(t => (now - t).TotalMinutes < LoginAttemptWindowMinutes)
                    .ToList();

                return Math.Max(0, MaxLoginAttempts - _loginAttempts[username].Count);
            }
        }

        static readonly IReadOnlyList<(string Code, string Title)> _sections = new List<(string, string)>
        {
            ("Home", "الرئيسية"),
            ("Certificates", "Certificates"),
            ("Clients", "العملاء"),
            ("Maintenance", "الصيانة"),
            ("Financials", "المالية"),
            ("DeviceHistory", "سجل الجهاز"),
            ("StickerDesigner", "مصمم الملصق"),
            ("ReceiptStickerDesigner", "مصمم استيكر الاستلام")
        };

        readonly HashSet<string> _userAllowedSections = new(StringComparer.OrdinalIgnoreCase);
        readonly List<UserAccount> _users = new();
        string _currentUserName = "";
        string _lastUserName = "";
        string _adminSalt = "";
        string _adminHash = "";
        bool _isAdminSession;

        public event EventHandler? Changed;

        public AccessControlState()
        {
            LoadOrInitialize();
        }

        public IReadOnlyList<(string Code, string Title)> Sections => _sections;

        public bool IsUserLoggedIn => !string.IsNullOrWhiteSpace(_currentUserName);

        public string CurrentUserName => _currentUserName;
        public string LastUserName => _lastUserName;

        public bool IsAdminSession
        {
            get => _isAdminSession;
            private set
            {
                if (_isAdminSession == value)
                    return;
                _isAdminSession = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public IReadOnlyList<string> GetUsernames()
        {
            return _users
                .Select(u => u.Username)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<UserAccountSnapshot> GetUsers()
        {
            return _users
                .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
                .Select(u => new UserAccountSnapshot(u.Username, u.AllowedSections?.ToArray() ?? Array.Empty<string>()))
                .ToList();
        }

        public bool IsUserSectionAllowed(string sectionCode)
        {
            if (string.IsNullOrWhiteSpace(sectionCode))
                return false;
            return _userAllowedSections.Contains(sectionCode);
        }

        public void SetUserSectionAllowed(string sectionCode, bool allowed)
        {
            if (string.IsNullOrWhiteSpace(sectionCode))
                return;

            var changed = allowed ? _userAllowedSections.Add(sectionCode) : _userAllowedSections.Remove(sectionCode);
            if (!changed)
                return;

            SaveAllowedSections();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool CanAccess(string sectionCode)
        {
            if (string.Equals(sectionCode, "Settings", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(sectionCode, "Home", StringComparison.OrdinalIgnoreCase))
                return true;
            if (IsAdminSession)
                return true;
            if (IsUserLoggedIn)
                return IsAllowedForCurrentUser(sectionCode);
            return false;
        }

        public bool EnsureAccess(string sectionCode, string sectionTitle)
        {
            if (CanAccess(sectionCode))
                return true;

            var pin = PromptForPin($"غير مصرح بالدخول إلى: {sectionTitle}", "أدخل الرقم السري للمدير لفتح هذا القسم:");
            if (string.IsNullOrWhiteSpace(pin))
                return false;

            if (TryLoginAsAdminWithPin(pin))
                return true;

            System.Windows.MessageBox.Show("الرقم السري غير صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        public bool TryLoginAsAdmin()
        {
            if (IsAdminSession)
                return true;

            var pin = PromptForPin("تسجيل دخول المدير", "أدخل الرقم السري للمدير:");
            if (string.IsNullOrWhiteSpace(pin))
                return false;

            if (!TryLoginAsAdminWithPin(pin))
            {
                System.Windows.MessageBox.Show("الرقم السري غير صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public void LogoutAdmin()
        {
            IsAdminSession = false;
        }

        public bool TryLoginUser(string username, string password)
        {
            try
            {
                var name = (username ?? "").Trim();
                if (!CanAttemptLogin(name))
                    return false;

                var u = FindUser(name);
                if (u == null)
                {
                    RecordLoginAttempt(name, false);
                    return false;
                }
                if (!VerifySecret(password, u.Salt, u.PasswordHash))
                {
                    RecordLoginAttempt(name, false);
                    return false;
                }

                RecordLoginAttempt(name, true);
                IsAdminSession = false;
                _currentUserName = u.Username;
                _lastUserName = u.Username;
                SaveAppSetting(LastUserNameKey, _lastUserName);
                Changed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void LogoutUser()
        {
            if (string.IsNullOrWhiteSpace(_currentUserName))
                return;
            _currentUserName = "";
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public bool TryLoginAsAdminWithPin(string pin)
        {
            if (IsAdminSession)
                return true;
            if (!CanAttemptLogin("__admin_pin__"))
                return false;
            if (!VerifyPin(pin ?? ""))
            {
                RecordLoginAttempt("__admin_pin__", false);
                return false;
            }
            RecordLoginAttempt("__admin_pin__", true);
            _currentUserName = "";
            IsAdminSession = true;
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool TryAddUser(string username, string password)
        {
            if (!IsAdminSession)
                return false;
            var name = (username ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (FindUser(name) != null)
                return false;

            var salt = GenerateSalt();
            var hash = HashSecret(password ?? "", salt);
            var user = new UserAccount
            {
                Username = name,
                Salt = salt,
                PasswordHash = hash,
                AllowedSections = new List<string>()
            };
            _users.Add(user);
            SaveUsers();
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool TryDeleteUser(string username)
        {
            if (!IsAdminSession)
                return false;
            var u = FindUser(username);
            if (u == null)
                return false;
            _users.Remove(u);
            if (string.Equals(_currentUserName, u.Username, StringComparison.OrdinalIgnoreCase))
                _currentUserName = "";
            SaveUsers();
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool TryChangeUserPassword(string username, string newPassword)
        {
            if (!IsAdminSession)
                return false;
            var u = FindUser(username);
            if (u == null)
                return false;
            var salt = GenerateSalt();
            u.Salt = salt;
            u.PasswordHash = HashSecret(newPassword ?? "", salt);
            SaveUsers();
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool IsUserSectionAllowed(string username, string sectionCode)
        {
            var u = FindUser(username);
            if (u == null)
                return false;
            return (u.AllowedSections ?? new List<string>())
                .Any(s => string.Equals(s, sectionCode, StringComparison.OrdinalIgnoreCase));
        }

        public bool TrySetUserSectionAllowed(string username, string sectionCode, bool allowed)
        {
            if (!IsAdminSession)
                return false;
            var u = FindUser(username);
            if (u == null)
                return false;
            u.AllowedSections ??= new List<string>();

            var changed = false;
            if (allowed)
            {
                if (!u.AllowedSections.Any(s => string.Equals(s, sectionCode, StringComparison.OrdinalIgnoreCase)))
                {
                    u.AllowedSections.Add(sectionCode);
                    changed = true;
                }
            }
            else
            {
                var before = u.AllowedSections.Count;
                u.AllowedSections.RemoveAll(s => string.Equals(s, sectionCode, StringComparison.OrdinalIgnoreCase));
                changed = u.AllowedSections.Count != before;
            }

            if (!changed)
                return true;

            SaveUsers();
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool TryChangeAdminPin()
        {
            var current = PromptForPin("تغيير الرقم السري", "أدخل الرقم السري الحالي للمدير:");
            if (string.IsNullOrWhiteSpace(current))
                return false;

            if (!VerifyPin(current))
            {
                System.Windows.MessageBox.Show("الرقم السري الحالي غير صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var next = PromptForPin("تغيير الرقم السري", "أدخل الرقم السري الجديد:");
            if (string.IsNullOrWhiteSpace(next))
                return false;

            if (next.Length < 4)
            {
                System.Windows.MessageBox.Show("الرقم السري يجب أن يكون 4 أرقام على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var confirm = PromptForPin("تغيير الرقم السري", "أعد إدخال الرقم السري الجديد للتأكيد:");
            if (!string.Equals(next, confirm, StringComparison.Ordinal))
            {
                System.Windows.MessageBox.Show("تأكيد الرقم السري غير مطابق.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            SetAdminPin(next);
            System.Windows.MessageBox.Show("تم تغيير الرقم السري بنجاح.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        void LoadOrInitialize()
        {
            try
            {
                _adminSalt = Services.AppSettingHelper.Load(AdminPinSaltKey) ?? "";
                _adminHash = Services.AppSettingHelper.Load(AdminPinHashKey) ?? "";

                if (string.IsNullOrWhiteSpace(_adminSalt) || string.IsNullOrWhiteSpace(_adminHash))
                {
                    _adminSalt = GenerateSalt();
                    var defaultPin = GenerateRandomPin();
                    _adminHash = HashPin(defaultPin, _adminSalt);
                    SaveAppSetting(AdminPinSaltKey, _adminSalt);
                    SaveAppSetting(AdminPinHashKey, _adminHash);
                    ShowDefaultPinNotice(defaultPin);
                }

                var raw = Services.AppSettingHelper.Load(UserAllowedSectionsKey) ?? "";
                _userAllowedSections.Clear();
                foreach (var code in ParseCsv(raw))
                    _userAllowedSections.Add(code);

                if (_userAllowedSections.Count == 0)
                {
                    foreach (var s in _sections)
                        _userAllowedSections.Add(s.Code);
                    SaveAllowedSections();
                }

                _users.Clear();
                var usersJson = Services.AppSettingHelper.Load(UsersJsonKey) ?? "";
                if (!string.IsNullOrWhiteSpace(usersJson))
                {
                    var parsed = JsonSerializer.Deserialize<List<UserAccount>>(usersJson);
                    if (parsed != null)
                        _users.AddRange(parsed.Where(u => !string.IsNullOrWhiteSpace(u.Username)));
                }
                _lastUserName = Services.AppSettingHelper.Load(LastUserNameKey) ?? "";
            }
            catch (Exception ex)
            {
                Log($"Error in LoadOrInitialize: {ex.Message}");
                _adminSalt = GenerateSalt();
                var defaultPin = GenerateRandomPin();
                _adminHash = HashPin(defaultPin, _adminSalt);
                ShowDefaultPinNotice(defaultPin);
                _userAllowedSections.Clear();
                foreach (var s in _sections)
                    _userAllowedSections.Add(s.Code);
                _users.Clear();
                _lastUserName = "";
            }
        }

        void SaveAllowedSections()
        {
            var csv = string.Join(",", _userAllowedSections.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
            SaveAppSetting(UserAllowedSectionsKey, csv);
        }

        void SetAdminPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(_adminSalt))
            {
                _adminSalt = GenerateSalt();
                SaveAppSetting(AdminPinSaltKey, _adminSalt);
            }

            _adminHash = HashPin(pin, _adminSalt);
            SaveAppSetting(AdminPinHashKey, _adminHash);
        }

        bool VerifyPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                return false;
            var stored = _adminHash ?? "";
            try
            {
                if (stored.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))
                {
                    var expected = Convert.FromBase64String(stored.Substring(Pbkdf2Prefix.Length));
                    var saltBytes = Convert.FromBase64String(_adminSalt ?? "");
                    using var derive = new Rfc2898DeriveBytes(pin, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
                    return FixedTimeEquals(derive.GetBytes(32), expected);
                }
            }
            catch
            {
                return false;
            }

            // Legacy SHA256 hash (backward compatibility)
            using (var sha = SHA256.Create())
            {
                var data = Encoding.UTF8.GetBytes($"{_adminSalt}:{pin}");
                var hash = Convert.ToBase64String(sha.ComputeHash(data));
                return string.Equals(hash, stored, StringComparison.Ordinal);
            }
        }

        static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        bool IsAllowedForCurrentUser(string sectionCode)
        {
            var u = FindUser(_currentUserName);
            if (u == null)
                return false;
            return (u.AllowedSections ?? new List<string>())
                .Any(s => string.Equals(s, sectionCode, StringComparison.OrdinalIgnoreCase));
        }

        UserAccount? FindUser(string username)
        {
            var name = (username ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return null;
            return _users.FirstOrDefault(u => string.Equals(u.Username, name, StringComparison.OrdinalIgnoreCase));
        }

        void SaveUsers()
        {
            try
            {
                var json = JsonSerializer.Serialize(_users);
                SaveAppSetting(UsersJsonKey, json);
            }
            catch
            {
            }
        }

        public void SaveUsersNow()
        {
            SaveUsers();
        }

        static string HashSecret(string secret, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt ?? "");
            using var derive = new Rfc2898DeriveBytes(secret ?? "", saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
            return Pbkdf2Prefix + Convert.ToBase64String(derive.GetBytes(32));
        }

        static bool VerifySecret(string secret, string salt, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(expectedHash))
                return false;
            var saltStr = salt ?? "";
            try
            {
                if (expectedHash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal))
                {
                    var expected = Convert.FromBase64String(expectedHash.Substring(Pbkdf2Prefix.Length));
                    var saltBytes = Convert.FromBase64String(saltStr);
                    using var derive = new Rfc2898DeriveBytes(secret ?? "", saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
                    return FixedTimeEquals(derive.GetBytes(32), expected);
                }
            }
            catch
            {
                return false;
            }

            // Legacy SHA256 hashes (kept for backward compatibility)
            using (var sha = SHA256.Create())
            {
                var data = Encoding.UTF8.GetBytes($"{saltStr}:{secret}");
                var hash = Convert.ToBase64String(sha.ComputeHash(data));
                return string.Equals(hash, expectedHash, StringComparison.Ordinal);
            }
        }

        static string HashPin(string pin, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt ?? "");
            using var derive = new Rfc2898DeriveBytes(pin ?? "", saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256);
            return Pbkdf2Prefix + Convert.ToBase64String(derive.GetBytes(32));
        }

        static string GenerateSalt()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        static string GenerateRandomPin()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var num = BitConverter.ToUInt32(bytes, 0);
            return (num % 900000 + 100000).ToString(); // 6-digit PIN between 100000-999999
        }

        static void ShowDefaultPinNotice(string defaultPin)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DEFAULT ADMIN PIN (CHANGE IMMEDIATELY): {defaultPin}");
                System.Windows.MessageBox.Show(
                    $"تم إنشاء رقم سري افتراضي للمدير: {defaultPin}\nيرجى تغييره فوراً من الإعدادات.",
                    "رقم سري افتراضي",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
            }
        }

        static IEnumerable<string> ParseCsv(string csv)
        {
            return (csv ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => (s ?? "").Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s));
        }

        static string PromptForPin(string title, string message)
        {
            try
            {
                var owner = System.Windows.Application.Current?.MainWindow;

                var pinBox = new PasswordBox
                {
                    Width = 240,
                    Margin = new Thickness(0, 8, 0, 0),
                    FontSize = 16
                };

                var ok = new Button
                {
                    Content = "موافق",
                    Width = 90,
                    Margin = new Thickness(0, 0, 8, 0),
                    IsDefault = true,
                    Background = new SolidColorBrush(Color.FromRgb(22, 160, 133)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(22, 160, 133))
                };

                var cancel = new Button
                {
                    Content = "إلغاء",
                    Width = 90,
                    IsCancel = true
                };

                var buttons = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 14, 0, 0)
                };
                buttons.Children.Add(ok);
                buttons.Children.Add(cancel);

                var root = new StackPanel
                {
                    Margin = new Thickness(18),
                    FlowDirection = FlowDirection.RightToLeft
                };
                root.Children.Add(new TextBlock { Text = message, FontSize = 14 });
                root.Children.Add(pinBox);
                root.Children.Add(buttons);

                var window = new Window
                {
                    Title = title,
                    Content = root,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow,
                    Owner = owner,
                    Topmost = true,
                    FlowDirection = FlowDirection.RightToLeft,
                    Background = Brushes.White
                };

                ok.Click += (_, __) => { window.DialogResult = true; window.Close(); };
                pinBox.KeyDown += (_, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        window.DialogResult = true;
                        window.Close();
                    }
                };

                pinBox.Loaded += (_, __) => pinBox.Focus();
                var result = window.ShowDialog();
                if (result == true)
                    return pinBox.Password ?? "";
                return "";
            }
            catch
            {
                return "";
            }
        }

        static void SaveAppSetting(string key, string value)
        {
            BlueMax.Presentation.Wpf.Services.AppSettingHelper.Save(key, value);
        }

        public sealed class UserAccountSnapshot
        {
            public UserAccountSnapshot(string username, IReadOnlyList<string> allowedSections)
            {
                Username = username ?? "";
                AllowedSections = allowedSections ?? Array.Empty<string>();
            }

            public string Username { get; }
            public IReadOnlyList<string> AllowedSections { get; }
        }

        sealed class UserAccount
        {
            public string Username { get; set; } = "";
            public string Salt { get; set; } = "";
            public string PasswordHash { get; set; } = "";
            public List<string>? AllowedSections { get; set; }
        }
    }
}
