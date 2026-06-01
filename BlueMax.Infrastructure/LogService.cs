using System.Text;
using System.Text.Json;

namespace BlueMax.Infrastructure;

public static class LogService
{
    private static readonly object LockObj = new();
    private const int MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxLogFilesToKeep = 5;

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static void LogInfo(string message, Dictionary<string, object>? context = null)
    {
        LogStructured(LogLevel.Info, message, context);
    }

    public static void LogWarning(string message, Dictionary<string, object>? context = null)
    {
        LogStructured(LogLevel.Warning, message, context);
    }

    public static void LogError(string message, Dictionary<string, object>? context = null)
    {
        LogStructured(LogLevel.Error, message, context);
    }

    public static void LogException(Exception ex, Dictionary<string, object>? context = null)
    {
        var exceptionContext = new Dictionary<string, object>(context ?? new())
        {
            ["ExceptionType"] = ex.GetType().Name,
            ["Message"] = ex.Message,
            ["StackTrace"] = ex.StackTrace ?? ""
        };
        LogStructured(LogLevel.Error, $"Exception occurred: {ex.Message}", exceptionContext);
    }

    private static void LogStructured(LogLevel level, string message, Dictionary<string, object>? context)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlueMax",
                "CertSystem",
                "Logs");

            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, "application.log");

            if (!File.Exists(logFile))
            {
                using (File.Create(logFile)) { }
                File.SetAttributes(logFile, File.GetAttributes(logFile) | FileAttributes.Hidden);
            }

            // Implement log rotation
            RotateLogIfNeeded(logFile);

            var contextStr = context != null && context.Count > 0
                ? $" | Context: {string.Join(", ", context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
                : "";

            var lines = new[]
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{contextStr}"
            };

            lock (LockObj)
            {
                File.AppendAllLines(logFile, lines, Encoding.UTF8);
            }

            // For critical errors, trigger error reporting
            if (level == LogLevel.Critical)
            {
                ErrorReportingService.ReportCriticalError(message, context);
            }
        }
        catch (Exception logEx)
        {
            // Fallback to Debug.WriteLine if logging fails
            System.Diagnostics.Debug.WriteLine($"Failed to log: {logEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
        }
    }

    public static void LogAudit(string action, string details, string? username = null)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlueMax",
                "CertSystem",
                "Logs");

            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, "audit.log");

            if (!File.Exists(logFile))
            {
                using (File.Create(logFile)) { }
                File.SetAttributes(logFile, File.GetAttributes(logFile) | FileAttributes.Hidden);
            }

            // Implement log rotation for audit log
            RotateLogIfNeeded(logFile);

            var user = username ?? "System";
            var lines = new[]
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] USER: {user} | ACTION: {action}",
                $"DETAILS: {details}",
                "---"
            };

            lock (LockObj)
            {
                File.AppendAllLines(logFile, lines, Encoding.UTF8);
            }
        }
        catch (Exception logEx)
        {
            // Fallback to Debug.WriteLine if logging fails
            System.Diagnostics.Debug.WriteLine($"Failed to log audit: {logEx.Message}");
            System.Diagnostics.Debug.WriteLine($"Action: {action}, Details: {details}");
        }
    }

    private static void RotateLogIfNeeded(string logFile)
    {
        try
        {
            var fileInfo = new FileInfo(logFile);
            if (fileInfo.Exists && fileInfo.Length > MaxLogFileSizeBytes)
            {
                // Archive current log
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var archiveFile = Path.Combine(fileInfo.DirectoryName!, $"{Path.GetFileNameWithoutExtension(logFile)}_{timestamp}.log");
                File.Move(logFile, archiveFile);

                // Clean up old logs
                var logFiles = Directory.GetFiles(fileInfo.DirectoryName!, $"{Path.GetFileNameWithoutExtension(logFile)}_*.log")
                    .OrderByDescending(f => f)
                    .Skip(MaxLogFilesToKeep);
                foreach (var oldFile in logFiles)
                {
                    File.Delete(oldFile);
                }

                // Create new log file
                using (File.Create(logFile)) { }
                File.SetAttributes(logFile, File.GetAttributes(logFile) | FileAttributes.Hidden);
            }
        }
        catch
        {
            // If rotation fails, continue with existing log
        }
    }
}

public static class ErrorReportingService
{
    private static readonly object _lock = new();
    private static DateTime _lastReportTime = DateTime.MinValue;
    private const int ReportCooldownMinutes = 60;

    public static void ReportCriticalError(string message, Dictionary<string, object>? context)
    {
        lock (_lock)
        {
            // Rate limit error reports to prevent spam
            if ((DateTime.Now - _lastReportTime).TotalMinutes < ReportCooldownMinutes)
                return;

            _lastReportTime = DateTime.Now;
        }

        try
        {
            var errorReport = new
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Message = message,
                Context = context,
                MachineName = Environment.MachineName,
                UserName = Environment.UserName
            };

            var reportDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlueMax",
                "CertSystem",
                "ErrorReports");

            Directory.CreateDirectory(reportDir);

            var reportFile = Path.Combine(reportDir, $"critical_error_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var json = JsonSerializer.Serialize(errorReport, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(reportFile, json, Encoding.UTF8);

            LogService.LogInfo("Critical error report generated", new Dictionary<string, object>
            {
                ["ReportFile"] = reportFile
            });
        }
        catch (Exception ex)
        {
            // If error reporting fails, fallback to debug output
            System.Diagnostics.Debug.WriteLine($"Failed to generate error report: {ex.Message}");
        }
    }
}
