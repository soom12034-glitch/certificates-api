using System.Text.Json;

namespace BlueMax.Infrastructure;

public class ReportDesignerSettings
{
    public string? TemplatePath { get; set; }
    public string? LogoPath { get; set; }
    public string? HeaderImagePath { get; set; }
    public string? FooterImagePath { get; set; }
    public string CompanyName { get; set; } = "";
    public string CompanyHeader { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string CompanyPhone { get; set; } = "";
    public bool ShowLogo { get; set; }
    public string? LayoutTemplatePath { get; set; }
    public List<ReportLayoutItem> LayoutItems { get; set; } = new();
    public bool PrintWithBackground { get; set; }
    public double OffsetXmm { get; set; }
    public double OffsetYmm { get; set; }
    public double MarginLeftMm { get; set; }
    public double MarginTopMm { get; set; }
    public double MarginRightMm { get; set; }
    public double MarginBottomMm { get; set; }
    public double PageWidthMm { get; set; }
    public double PageHeightMm { get; set; }
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public string? AppIconPath { get; set; }
    public string? LibreOfficeProgramPath { get; set; }
}

public class ReportLayoutItem
{
    public string? Key { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double FontSize { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? DisplayText { get; set; }
    public string? Title { get; set; }
    public double Xmm { get; set; }
    public double Ymm { get; set; }
}

public class ReportDesignerSettingsStore
{
    private const string SettingsPath = "ReportDesignerSettings.json";

    public ReportDesignerSettings Load()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, SettingsPath);

        if (!File.Exists(settingsPath))
            return new ReportDesignerSettings();

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<ReportDesignerSettings>(json) ?? new ReportDesignerSettings();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new ReportDesignerSettings();
        }
    }

    public void Save(ReportDesignerSettings settings)
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
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public string SaveAsset(string assetName, string assetPath)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Assets");

        Directory.CreateDirectory(settingsDir);

        var assetFilePath = Path.Combine(settingsDir, assetName);
        try
        {
            if (File.Exists(assetPath))
            {
                File.Copy(assetPath, assetFilePath, true);
            }
            return assetFilePath;
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return string.Empty;
        }
    }
}
