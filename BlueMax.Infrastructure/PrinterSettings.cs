using System.Text.Json;

namespace BlueMax.Infrastructure;

public class PrinterSettings
{
    public string? PrinterName { get; set; }
    public string Protocol { get; set; } = "ZPL";
    public int DotsPerMm { get; set; } = 8;
    public int TsplCopies { get; set; } = 1;
    public string? TsplMediaType { get; set; }
    public string? TsplCodepage { get; set; }
    public string ConnectionType { get; set; } = "USB";
    public string? A4PrinterName { get; set; }
    public int TsplReferenceY { get; set; }
    public bool TsplCalibrateOnPrint { get; set; }
    public int TsplBlackMarkOffsetMm { get; set; }
    public int TsplGapOffsetMm { get; set; }
    public int TsplGapMm { get; set; }
    public int TsplDirection { get; set; }
    public int TsplReferenceX { get; set; }
    public int SpeedIps { get; set; }
    public int LabelHeightMm { get; set; }
    public int LabelWidthMm { get; set; }
    public int Darkness { get; set; }
    public bool ThermalSafeMode { get; set; }
    public double ThermalCoolingDelaySeconds { get; set; } = 0.8;
    public int NetworkPort { get; set; }
    public string? PrinterType { get; set; }
    public string? NetworkHost { get; set; }
}

public class PrinterSettingsStore
{
    public const string DefaultFileName = "PrinterSettings.json";
    public const string ReceiptFileName = "ReceiptPrinterSettings.json";

    readonly string _settingsPath;

    public PrinterSettingsStore(string fileName = DefaultFileName)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        _settingsPath = Path.Combine(settingsDir, string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName);
    }

    public PrinterSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new PrinterSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<PrinterSettings>(json) ?? new PrinterSettings();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new PrinterSettings();
        }
    }

    public void Save(PrinterSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }
}
