using System.Text.Json;

namespace BlueMax.Infrastructure;

public class CalibrationDefaults
{
    public string? DeviceType { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int CalibrationPeriodMonths { get; set; } = 6;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["DeviceType"] = DeviceType ?? "",
            ["Brand"] = Brand ?? "",
            ["Model"] = Model ?? "",
            ["CalibrationPeriodMonths"] = CalibrationPeriodMonths
        };
    }
}

public class CalibrationDefaultsRow
{
    public string? Item { get; set; }
    public decimal? MeasuredValue { get; set; }
}

public class CalibrationDefaultsStore
{
    private const string SettingsPath = "CalibrationDefaults.json";

    public CalibrationDefaults Load()
    {
        return Load(string.Empty);
    }

    public CalibrationDefaults Load(string deviceType)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, SettingsPath);

        if (!File.Exists(settingsPath))
            return new CalibrationDefaults();

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<CalibrationDefaults>(json) ?? new CalibrationDefaults();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new CalibrationDefaults();
        }
    }

    public void Save(CalibrationDefaults settings)
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

    public List<CalibrationDefaultsRow> LoadRows(string deviceType)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, $"CalibrationDefaults_{deviceType}.json");

        if (!File.Exists(settingsPath))
            return new List<CalibrationDefaultsRow>();

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<List<CalibrationDefaultsRow>>(json) ?? new List<CalibrationDefaultsRow>();
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
            return new List<CalibrationDefaultsRow>();
        }
    }

    public void Save(string deviceType, List<CalibrationDefaultsRow> rows)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var settingsPath = Path.Combine(settingsDir, $"CalibrationDefaults_{deviceType}.json");

        try
        {
            var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }
}
