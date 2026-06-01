using Microsoft.Data.Sqlite;

namespace BlueMax.Infrastructure;

public class BackupService
{
    private readonly string? _serverConnectionString;
    private readonly string _localDbPath;
    private Timer? _backupTimer;

    public BackupService(string? serverConnectionString, string localDbPath)
    {
        _serverConnectionString = serverConnectionString;
        _localDbPath = localDbPath;

        // Initialize automated backup schedule based on settings
        InitializeScheduledBackup();
    }

    private void InitializeScheduledBackup()
    {
        var settings = BackupSettingsStore.Load();
        BackupSettingsStore.NormalizeBackupPaths(settings);
        
        if (!settings.EnableAutoBackup)
            return;

        var timeUntilNextBackup = GetTimeUntilNextBackup(settings);
        var interval = GetBackupInterval(settings);

        _backupTimer = new Timer(PerformScheduledBackup, null, timeUntilNextBackup, interval);
    }

    private TimeSpan GetTimeUntilNextBackup(BackupSettings settings)
    {
        var now = DateTime.Now;
        
        if (!TimeSpan.TryParse(settings.AutoBackupTime, out var backupTime))
            backupTime = TimeSpan.FromHours(2); // Default to 2 AM

        var nextBackup = new DateTime(now.Year, now.Month, now.Day, backupTime.Hours, backupTime.Minutes, backupTime.Seconds);

        // Adjust based on schedule
        switch (settings.AutoBackupSchedule.ToLower())
        {
            case "daily":
                if (now > nextBackup)
                    nextBackup = nextBackup.AddDays(1);
                break;
            case "weekly":
                var nextWeek = nextBackup.AddDays((7 - (int)nextBackup.DayOfWeek + (int)DayOfWeek.Sunday) % 7);
                if (now > nextWeek)
                    nextWeek = nextWeek.AddDays(7);
                nextBackup = nextWeek;
                break;
            case "monthly":
                var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                nextBackup = new DateTime(nextMonth.Year, nextMonth.Month, 1, backupTime.Hours, backupTime.Minutes, backupTime.Seconds);
                if (now > nextBackup)
                    nextBackup = nextBackup.AddMonths(1);
                break;
            default:
                if (now > nextBackup)
                    nextBackup = nextBackup.AddDays(1);
                break;
        }

        return nextBackup - now;
    }

    private TimeSpan GetBackupInterval(BackupSettings settings)
    {
        return settings.AutoBackupSchedule.ToLower() switch
        {
            "daily" => TimeSpan.FromDays(1),
            "weekly" => TimeSpan.FromDays(7),
            "monthly" => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(1)
        };
    }

    private async void PerformScheduledBackup(object? state)
    {
        try
        {
            await PerformBackupAsync();
            LogService.LogAudit("AUTO_BACKUP", "Scheduled automatic backup completed");
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public async Task PerformBackupAsync()
    {
        var settings = BackupSettingsStore.Load();
        BackupSettingsStore.NormalizeBackupPaths(settings);

        if (!ShouldBackupToday(settings.LastBackupDate))
            return;

        if (CanUseSqlServer(_serverConnectionString))
            await BackupSqlServerAsync(_serverConnectionString!, settings.BackupPaths);
        else
            await BackupSqliteAsync(_localDbPath, settings.BackupPaths);

        settings.LastBackupDate = DateTime.Today.ToString("yyyy-MM-dd");
        BackupSettingsStore.Save(settings);
    }

    private bool ShouldBackupToday(string lastBackupDate)
    {
        if (string.IsNullOrWhiteSpace(lastBackupDate))
            return true;

        if (DateTime.TryParse(lastBackupDate, out var lastDate))
            return lastDate < DateTime.Today;

        return true;
    }

    private bool CanUseSqlServer(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task BackupSqlServerAsync(string connectionString, List<string> backupPaths)
    {
        foreach (var backupPath in backupPaths)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

                var backupName = $"BlueMax_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(backupPath, backupName);

                var query = $"BACKUP DATABASE MyAppDb TO DISK = '{fullPath}' WITH FORMAT, INIT, NAME = 'BlueMax Full Backup';";

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();

                LogService.LogInfo($"SQL Server backup completed: {fullPath}");
            }
            catch (Exception ex)
            {
                LogService.LogException(ex);
            }
        }
    }

    private async Task BackupSqliteAsync(string localDbPath, List<string> backupPaths)
    {
        var settings = BackupSettingsStore.Load();
        BackupSettingsStore.NormalizeBackupPaths(settings);

        foreach (var backupPath in backupPaths)
        {
            try
            {
                Directory.CreateDirectory(backupPath);

                var backupName = $"BlueMax_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var fullPath = Path.Combine(backupPath, backupName);

                if (!File.Exists(localDbPath))
                {
                    LogService.LogInfo($"SQLite database not found for backup: {localDbPath}");
                    continue;
                }

                await Task.Run(() => CreateSqliteBackup(localDbPath, fullPath));

                if (settings.EnableEncryption && !string.IsNullOrWhiteSpace(settings.EncryptionPassword))
                {
                    var encryptedName = $"BlueMax_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.enc";
                    var encryptedPath = Path.Combine(backupPath, encryptedName);
                    BackupEncryption.EncryptFileToFile(fullPath, encryptedPath, settings.EncryptionPassword);
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception deleteEx)
                    {
                        LogService.LogException(deleteEx);
                    }
                    LogService.LogInfo($"SQLite encrypted backup completed: {encryptedPath}");
                }
                else
                {
                    LogService.LogInfo($"SQLite backup completed: {fullPath}");
                }

                // Clean up old backups
                CleanupOldBackups(backupPath, settings.MaxBackupCount);
            }
            catch (Exception ex)
            {
                LogService.LogException(ex);
            }
        }
    }

    public static void CreateSqliteBackup(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        using var source = new SqliteConnection($"Data Source={sourcePath}");
        using var destination = new SqliteConnection($"Data Source={destinationPath}");
        source.Open();
        destination.Open();
        source.BackupDatabase(destination);
    }

    private void CleanupOldBackups(string backupPath, int maxCount)
    {
        try
        {
            var files = Directory.GetFiles(backupPath, "BlueMax_Backup_*")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            if (files.Count > maxCount)
            {
                var filesToDelete = files.Skip(maxCount);
                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                    LogService.LogInfo($"Deleted old backup: {file}");
                }
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public static bool ValidateBackupFile(string backupPath, string? password = null)
    {
        try
        {
            if (!File.Exists(backupPath))
                return false;

            var fileInfo = new FileInfo(backupPath);
            
            // Check if file is encrypted
            if (fileInfo.Extension.Equals(".enc", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(password))
                    return false;

                // Try to decrypt to validate
                var decryptedData = BackupEncryption.DecryptFile(backupPath, password);
                
                // Check if decrypted data is a valid SQLite database
                return IsValidSqliteDatabase(decryptedData);
            }
            else
            {
                // Check if file is a valid SQLite database
                return IsValidSqliteDatabase(File.ReadAllBytes(backupPath));
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidSqliteDatabase(byte[] data)
    {
        // SQLite database files start with "SQLite format 3\0"
        if (data.Length < 16)
            return false;

        var header = System.Text.Encoding.ASCII.GetString(data, 0, 16);
        return header.StartsWith("SQLite format 3");
    }

    public static void RestoreBackup(string backupPath, string targetDbPath, string? password = null)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file not found", backupPath);

        var fileInfo = new FileInfo(backupPath);

        if (fileInfo.Extension.Equals(".enc", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password required for encrypted backup");

            BackupEncryption.DecryptFileToFile(backupPath, targetDbPath, password);
        }
        else
        {
            File.Copy(backupPath, targetDbPath, true);
        }
    }
}
