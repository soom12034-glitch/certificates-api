using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlueMax.Infrastructure;

public class BackupSettings
{
    public List<string> BackupPaths { get; set; } = new();
    public string LastBackupDate { get; set; } = "";
    public bool EnableEncryption { get; set; } = true;
    public string EncryptionPassword { get; set; } = "";
    public bool EnableAutoBackup { get; set; } = false;
    public string AutoBackupSchedule { get; set; } = "Daily"; // Daily, Weekly, Monthly
    public string AutoBackupTime { get; set; } = "02:00"; // HH:mm format
    public int MaxBackupCount { get; set; } = 10; // Maximum number of backups to keep
}

public static class BackupSettingsStore
{
    private const string SettingsPath = "BackupSettings.json";
    private const string PasswordPath = "BackupPassword.bin";

    public static string GetDefaultBackupPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Backups");
    }

    public static List<string> NormalizeBackupPaths(BackupSettings settings)
    {
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>();
        var changed = false;

        foreach (var path in settings.BackupPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                changed = true;
                continue;
            }

            var trimmed = path.Trim();
            if (!string.Equals(trimmed, path, StringComparison.Ordinal))
                changed = true;

            if (!unique.Add(trimmed))
            {
                changed = true;
                continue;
            }

            paths.Add(trimmed);
        }

        if (paths.Count == 0)
        {
            paths.Add(GetDefaultBackupPath());
            changed = true;
        }

        if (changed)
        {
            settings.BackupPaths = paths;
            Save(settings);
        }

        return paths;
    }

    public static BackupSettings Load()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, SettingsPath);

        if (!File.Exists(settingsPath))
            return new BackupSettings();

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<BackupSettings>(json) ?? new BackupSettings();
            var encryptedPassword = LoadEncryptedPassword(settingsDir);
            if (!string.IsNullOrWhiteSpace(encryptedPassword))
                settings.EncryptionPassword = encryptedPassword;
            return settings;
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new BackupSettings();
        }
    }

    public static void Save(BackupSettings settings)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, SettingsPath);

        try
        {
            SaveEncryptedPassword(settingsDir, settings.EncryptionPassword);
            var snapshot = new BackupSettings
            {
                BackupPaths = settings.BackupPaths,
                LastBackupDate = settings.LastBackupDate,
                EnableEncryption = settings.EnableEncryption,
                EncryptionPassword = string.Empty,
                EnableAutoBackup = settings.EnableAutoBackup,
                AutoBackupSchedule = settings.AutoBackupSchedule,
                AutoBackupTime = settings.AutoBackupTime,
                MaxBackupCount = settings.MaxBackupCount
            };
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    private static string? LoadEncryptedPassword(string settingsDir)
    {
        var passwordPath = Path.Combine(settingsDir, PasswordPath);
        if (!File.Exists(passwordPath))
            return null;
        try
        {
            var encryptedBytes = File.ReadAllBytes(passwordPath);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return null;
        }
    }

    private static void SaveEncryptedPassword(string settingsDir, string password)
    {
        var passwordPath = Path.Combine(settingsDir, PasswordPath);
        try
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                if (File.Exists(passwordPath))
                    File.Delete(passwordPath);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(password);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(passwordPath, encrypted);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }
}

public static class SecureConnectionStringStore
{
    private const string ConnectionStringPath = "EncryptedConnectionString.bin";

    public static string? LoadConnectionString()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var connectionStringPath = Path.Combine(settingsDir, ConnectionStringPath);

        if (!File.Exists(connectionStringPath))
            return null;

        try
        {
            var encryptedBytes = File.ReadAllBytes(connectionStringPath);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return null;
        }
    }

    public static void SaveConnectionString(string connectionString)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var connectionStringPath = Path.Combine(settingsDir, ConnectionStringPath);

        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(connectionString);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(connectionStringPath, encrypted);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }
}
