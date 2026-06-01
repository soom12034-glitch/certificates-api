using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BlueMax.Presentation.Wpf.Services;

public class StickerLayoutSettings
{
    public double WidthMm { get; set; } = 60;
    public double HeightMm { get; set; } = 40;
    public bool UseLogo { get; set; }
    public string LogoPath { get; set; } = "";
    public double HeaderFontSize { get; set; } = 12;
    public double BodyFontSize { get; set; } = 9;
    public List<StickerItemSettings> Items { get; set; } = new();
}

public class StickerItemSettings
{
    public string Key { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double FontSize { get; set; }
    public bool IsVisible { get; set; } = true;
    public string DisplayText { get; set; } = "";
}

public class StickerLayoutStore
{
    private readonly string _filePath;

    public StickerLayoutStore()
    {
        // Store in AppData so it persists across updates
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "BlueMax", "Config");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "sticker_layout.json");
    }

    public void Save(StickerLayoutSettings settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_filePath, json);
    }

    public StickerLayoutSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return CreateDefaultSettings();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<StickerLayoutSettings>(json);
            return settings ?? CreateDefaultSettings();
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public StickerLayoutSettings CreateDefaultSettings()
    {
        // Default layout tuned for 60x40 mm (≈ 226x151 px at 96DPI)
        // Arabic-friendly defaults
        return new StickerLayoutSettings
        {
            WidthMm = 60,
            HeightMm = 40,
            HeaderFontSize = 14,
            BodyFontSize = 10,
            Items = new List<StickerItemSettings>
            {
                // Top section: "High accuracy" Header
                new StickerItemSettings { Key = "header", X = 0, Y = 5, Width = 226, Height = 30, FontSize = 14, DisplayText = "High accuracy" },
                new StickerItemSettings { Key = "line_top", X = 10, Y = 38, Width = 206, Height = 1.5, FontSize = 1, DisplayText = "" },

                // Middle section labels in English
                new StickerItemSettings { Key = "brand_label", X = 15, Y = 45, Width = 60, Height = 18, FontSize = 10, DisplayText = "Brand:" },
                new StickerItemSettings { Key = "brand_value", X = 80, Y = 45, Width = 100, Height = 18, FontSize = 10, DisplayText = "Brand" },

                new StickerItemSettings { Key = "model_label", X = 15, Y = 63, Width = 60, Height = 18, FontSize = 10, DisplayText = "Model:" },
                new StickerItemSettings { Key = "model_value", X = 80, Y = 63, Width = 100, Height = 18, FontSize = 10, DisplayText = "B20" },

                new StickerItemSettings { Key = "serial_label", X = 15, Y = 81, Width = 60, Height = 18, FontSize = 10, DisplayText = "Serial:" },
                new StickerItemSettings { Key = "serial_value", X = 80, Y = 81, Width = 100, Height = 18, FontSize = 10, DisplayText = "345676543" },

                new StickerItemSettings { Key = "cal_label", X = 15, Y = 99, Width = 60, Height = 18, FontSize = 10, DisplayText = "Cal Date:" },
                new StickerItemSettings { Key = "cal_value", X = 80, Y = 99, Width = 100, Height = 18, FontSize = 10, DisplayText = "20-02-2026" },

                new StickerItemSettings { Key = "exp_label", X = 15, Y = 117, Width = 60, Height = 18, FontSize = 10, DisplayText = "Valid until:" },
                new StickerItemSettings { Key = "exp_value", X = 80, Y = 117, Width = 100, Height = 18, FontSize = 10, DisplayText = "20-08-2026" },

                // QR code on the right of the middle block
                new StickerItemSettings { Key = "qr", X = 175, Y = 55, Width = 45, Height = 45, FontSize = 60, DisplayText = "QR" },

                // Bottom line and footer
                new StickerItemSettings { Key = "line_bottom", X = 10, Y = 140, Width = 206, Height = 1.5, FontSize = 1, DisplayText = "" },
                new StickerItemSettings { Key = "address", X = 0, Y = 145, Width = 226, Height = 18, FontSize = 10, DisplayText = "Address line" },
                new StickerItemSettings { Key = "phone", X = 0, Y = 168, Width = 226, Height = 18, FontSize = 10, DisplayText = "Phone: " },

                // Hidden items by default
                new StickerItemSettings { Key = "company", X = 10, Y = 10, Width = 100, Height = 10, FontSize = 10, DisplayText = "Company", IsVisible = false },
                new StickerItemSettings { Key = "logo", X = 10, Y = 10, Width = 30, Height = 30, FontSize = 10, DisplayText = "Logo", IsVisible = false },
                new StickerItemSettings { Key = "cert_label", X = 10, Y = 10, Width = 100, Height = 10, FontSize = 10, DisplayText = "CERT:", IsVisible = false },
                new StickerItemSettings { Key = "cert_value", X = 10, Y = 10, Width = 100, Height = 10, FontSize = 10, DisplayText = "Cert001", IsVisible = false }
            }
        };
    }
}
