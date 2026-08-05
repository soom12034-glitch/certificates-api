using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlueMax.Infrastructure;

public class LicenseStore
{
    class LicenseSettings
    {
        public string ProtectedActivationCode { get; set; } = "";
        public string ProtectedTrialStartDate { get; set; } = "";
    }

    static string GetSettingsPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "License.json");
    }

    public string? LoadActivationCode()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<LicenseSettings>(json);
            if (settings == null || string.IsNullOrWhiteSpace(settings.ProtectedActivationCode))
                return null;
            return Unprotect(settings.ProtectedActivationCode);
        }
        catch
        {
            return null;
        }
    }

    public void SaveActivationCode(string activationCode)
    {
        var settings = LoadSettings();
        settings.ProtectedActivationCode = Protect(activationCode);
        SaveSettings(settings);
    }

    static string Protect(string plain)
    {
        var bytes = Encoding.UTF8.GetBytes(plain);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(protectedBytes);
    }

    static string Unprotect(string protectedBase64)
    {
        var protectedBytes = Convert.FromBase64String(protectedBase64);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(bytes);
    }

    static LicenseSettings LoadSettings()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return new LicenseSettings();

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<LicenseSettings>(json) ?? new LicenseSettings();
        }
        catch
        {
            return new LicenseSettings();
        }
    }

    static void SaveSettings(LicenseSettings settings)
    {
        var path = GetSettingsPath();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        File.Move(tempPath, path, overwrite: true);
    }
}
