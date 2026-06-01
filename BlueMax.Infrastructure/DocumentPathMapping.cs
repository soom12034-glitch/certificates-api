using System.Text.Json;

namespace BlueMax.Infrastructure;

public class DocumentPathMapping
{
    public string? CertificateNumber { get; set; }
    public string? DocxPath { get; set; }
    public string? PdfPath { get; set; }
}

public class DocumentPathMappingStore
{
    private const string SettingsPath = "DocumentPathMapping.json";

    public Dictionary<string, DocumentPathMapping> Load()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, SettingsPath);

        if (!File.Exists(settingsPath))
            return new Dictionary<string, DocumentPathMapping>();

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<Dictionary<string, DocumentPathMapping>>(json) ?? new Dictionary<string, DocumentPathMapping>();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new Dictionary<string, DocumentPathMapping>();
        }
    }

    public void Save(Dictionary<string, DocumentPathMapping> mappings)
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
            var json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public string? GetPath(string certificateNumber)
    {
        var mappings = Load();
        return mappings.TryGetValue(certificateNumber, out var mapping) ? mapping.PdfPath : null;
    }

    public void SetPath(string certificateNumber, string path)
    {
        var mappings = Load();
        if (!mappings.ContainsKey(certificateNumber))
            mappings[certificateNumber] = new DocumentPathMapping();
        mappings[certificateNumber].PdfPath = path;
        Save(mappings);
    }
}
