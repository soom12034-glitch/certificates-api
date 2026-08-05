using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace BlueMax.Presentation.Wpf.Services;

public class StickerPrinterInfo
{
    public string Name { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string PortName { get; set; } = "";
    public bool IsThermal { get; set; }
    public string Protocol { get; set; } = "Windows";
    public int Dpi { get; set; }
    public int DotsPerMm { get; set; }
    public bool IsDefault { get; set; }
}

public static class StickerPrinterDetector
{
    static readonly string[] ZplKeywords = { "zebra", "zpl", "zdesigner", "zd-series", "zp-", "zt-" };
    static readonly string[] TsplKeywords = { "tsc", "tspl", "xprinter", "x-printer", "godex", "argox", "bixolon", "idprt", "rongta", "hprt", "gain" };
    static readonly string[] ThermalKeywords = { "thermal", "receipt", "label", "sticker", "barcode", "pos", "tm-", "datamax", "intermec", "sato" };

    public static List<StickerPrinterInfo> DetectAll()
    {
        var result = new List<StickerPrinterInfo>();
        var defaultName = SafeDefaultPrinterName();

        try
        {
            foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                result.Add(BuildInfo(name, defaultName));
            }
        }
        catch
        {
        }

        return result;
    }

    public static StickerPrinterInfo? Detect(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return null;
        var defaultName = SafeDefaultPrinterName();
        try
        {
            foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(name, printerName, StringComparison.OrdinalIgnoreCase))
                    return BuildInfo(name, defaultName);
            }
        }
        catch
        {
        }
        return null;
    }

    static StickerPrinterInfo BuildInfo(string name, string defaultName)
    {
        var (driver, port) = ReadDriverAndPort(name);
        var dpi = GetPrinterDpi(name);
        var hay = BuildHaystack(name, driver);
        var isThermal = LooksThermal(hay);
        var protocol = InferProtocol(hay, isThermal);

        return new StickerPrinterInfo
        {
            Name = name,
            DriverName = driver,
            PortName = port,
            IsThermal = isThermal,
            Protocol = protocol,
            Dpi = dpi,
            DotsPerMm = dpi > 0 ? Math.Max(1, (int)Math.Round(dpi / 25.4)) : 0,
            IsDefault = string.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase)
        };
    }

    static string SafeDefaultPrinterName()
    {
        try
        {
            return new System.Drawing.Printing.PrinterSettings().PrinterName ?? "";
        }
        catch
        {
            return "";
        }
    }

    static (string Driver, string Port) ReadDriverAndPort(string printerName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Control\Print\Printers\{printerName}");
            if (key == null)
                return ("", "");
            var driver = (key.GetValue("Printer Driver") as string) ?? "";
            var port = (key.GetValue("Port") as string) ?? "";
            return (driver, port);
        }
        catch
        {
            return ("", "");
        }
    }

    static int GetPrinterDpi(string printerName)
    {
        try
        {
            var ps = new System.Drawing.Printing.PrinterSettings { PrinterName = printerName };
            var res = ps.DefaultPageSettings.PrinterResolution;
            if (res.X > 0 && res.Y > 0)
                return Math.Max(res.X, res.Y);

            var hDevMode = ps.GetHdevmode();
            try
            {
                var dm = Marshal.PtrToStructure<DevMode>(hDevMode);
                if (dm.DmYResolution > 0)
                    return dm.DmYResolution;
                if (dm.DmPrintQuality > 0)
                    return dm.DmPrintQuality;
            }
            finally
            {
                try { Marshal.FreeHGlobal(hDevMode); } catch { }
            }
        }
        catch
        {
        }
        return 0;
    }

    static bool LooksThermal(string hay)
    {
        foreach (var kw in ZplKeywords)
            if (hay.Contains(kw)) return true;
        foreach (var kw in TsplKeywords)
            if (hay.Contains(kw)) return true;
        foreach (var kw in ThermalKeywords)
            if (hay.Contains(kw)) return true;
        return false;
    }

    static string InferProtocol(string hay, bool isThermal)
    {
        foreach (var kw in ZplKeywords)
            if (hay.Contains(kw)) return "ZPL";
        foreach (var kw in TsplKeywords)
            if (hay.Contains(kw)) return "TSPL";
        if (isThermal)
            return "TSPL";
        return "Windows";
    }

    static string BuildHaystack(string name, string driver)
    {
        var sb = new StringBuilder();
        sb.Append(name.ToLowerInvariant()).Append(' ').Append(driver.ToLowerInvariant());
        return sb.ToString();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DmDeviceName;
        public ushort DmSpecVersion;
        public ushort DmDriverVersion;
        public ushort DmSize;
        public ushort DmDriverExtra;
        public uint DmFields;
        public short DmOrientation;
        public short DmPaperSize;
        public short DmPaperLength;
        public short DmPaperWidth;
        public short DmScale;
        public short DmCopies;
        public short DmDefaultSource;
        public short DmPrintQuality;
        public short DmColor;
        public short DmDuplex;
        public short DmYResolution;
        public short DmTTOption;
        public short DmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DmFormName;
        public ushort DmLogPixels;
        public uint DmBitsPerPel;
        public uint DmPelsWidth;
        public uint DmPelsHeight;
        public uint DmDisplayFlags;
        public uint DmDisplayFrequency;
        public uint DmICMMethod;
        public uint DmICMIntent;
        public uint DmMediaType;
        public uint DmDitherType;
        public uint DmReserved1;
        public uint DmReserved2;
        public uint DmPanningWidth;
        public uint DmPanningHeight;
    }
}
