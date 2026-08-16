using System.Configuration;
using System.IO;
using System.Text.Json;

namespace BlueMax.Presentation.Wpf.Services;

public static class AppSettingHelper
{
    private static readonly object _sync = new();

    static string GetJsonPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings",
            "AppSettings.json");
    }

    public static string? Load(string key)
    {
        // 1) exe.config (legacy location)
        try
        {
            var value = ConfigurationManager.AppSettings[key];
            if (value != null)
                return value;
        }
        catch
        {
        }

        // 2) Per-user JSON fallback
        try
        {
            var map = LoadMap();
            return map.TryGetValue(key, out var v) ? v : null;
        }
        catch
        {
        }

        return null;
    }

    public static bool Save(string key, string value)
    {
        try
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, value ?? "");
            else
                config.AppSettings.Settings[key].Value = value ?? "";
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            return true;
        }
        catch (Exception ex)
        {
            try { App.Log($"AppSettingHelper.Save failed for '{key}': {ex.Message}"); } catch { }
        }

        // Fallback to a per-user file so admin secrets survive a read-only config file
        try
        {
            lock (_sync)
            {
                var map = LoadMap();
                map[key] = value ?? "";
                var path = GetJsonPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(tmp, path, overwrite: true);
            }
            return true;
        }
        catch (Exception ex)
        {
            try { App.Log($"AppSettingHelper.Save fallback failed for '{key}': {ex.Message}"); } catch { }
            return false;
        }
    }

    static Dictionary<string, string> LoadMap()
    {
        var path = GetJsonPath();
        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
