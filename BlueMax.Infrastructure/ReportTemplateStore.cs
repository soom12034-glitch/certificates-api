using System.Text.Json;

namespace BlueMax.Infrastructure;

public class ReportTemplate
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? LayoutTemplatePath { get; set; }
    public List<ReportLayoutItem> LayoutItems { get; set; } = new();
}

public class ReportTemplateStore
{
    private const string SettingsPath = "ReportTemplates.json";

    public string GetDeviceDir(string deviceType)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Templates",
            deviceType ?? "");
        Directory.CreateDirectory(settingsDir);
        return settingsDir;
    }


    public List<ReportTemplate> Load()
    {
        return Load(string.Empty);
    }

    public List<ReportTemplate> Load(string templateName)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, SettingsPath);

        if (!File.Exists(settingsPath))
            return new List<ReportTemplate>();

        try
        {
            var json = File.ReadAllText(settingsPath);
            var templates = JsonSerializer.Deserialize<List<ReportTemplate>>(json) ?? new List<ReportTemplate>();
            if (string.IsNullOrWhiteSpace(templateName))
                return templates;
            return templates.Where(t => string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new List<ReportTemplate>();
        }
    }

    public ReportTemplateXml? Load(string deviceType, string templateName)
    {
        var deviceDir = GetDeviceDir(deviceType ?? "");
        if (!Directory.Exists(deviceDir) || string.IsNullOrWhiteSpace(templateName))
            return null;

        try
        {
            var filePath = Directory.GetFiles(deviceDir, "*.json")
                .FirstOrDefault(f => string.Equals(
                    Path.GetFileNameWithoutExtension(f),
                    templateName,
                    StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ReportTemplateXml>(json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return null;
        }
    }

    public void Save(List<ReportTemplate> templates)
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
            var json = JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public void Save(string deviceType, string templateName, ReportTemplateXml template)
    {
        var deviceDir = GetDeviceDir(deviceType);
        var filePath = Path.Combine(deviceDir, $"{templateName}.json");
        try
        {
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public List<string> ListTemplates(string deviceType)
    {
        var deviceDir = GetDeviceDir(deviceType);
        if (!Directory.Exists(deviceDir))
            return new List<string>();

        try
        {
            return Directory.GetFiles(deviceDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new List<string>();
        }
    }

    public int? FindTemplateId(string deviceType, string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return null;
        if (Load(deviceType, templateName) == null)
            return null;
        return StableHash($"{deviceType ?? ""}|{templateName.Trim()}".ToLowerInvariant());
    }

    private static int StableHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt32(bytes, 0) & int.MaxValue;
    }
}
