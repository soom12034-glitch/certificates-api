using System.Diagnostics;
using System.IO;

namespace BlueMax.Infrastructure;

public static class PdfConverter
{
    public static string ConvertDocxToPdf(string docxPath)
    {
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
            return pdfPath;

        var sofficePath = ResolveSofficePath();
        if (string.IsNullOrWhiteSpace(sofficePath))
            return pdfPath;

        try
        {
            var outDir = Path.Combine(Path.GetTempPath(), $"BlueMax_DocxToPdf_{Guid.NewGuid():N}");
            Directory.CreateDirectory(outDir);

            var psi = new ProcessStartInfo
            {
                FileName = sofficePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = $"--headless --norestore --convert-to pdf --outdir \"{outDir}\" \"{docxPath}\""
            };

            using var process = Process.Start(psi);
            if (process == null)
                return pdfPath;
            if (!process.WaitForExit(120000))
            {
                try { process.Kill(); } catch { }
                return pdfPath;
            }

            var converted = Directory.GetFiles(outDir, "*.pdf").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(converted) || !File.Exists(converted))
                return pdfPath;

            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
            File.Copy(converted, pdfPath, overwrite: true);
            try { Directory.Delete(outDir, recursive: true); } catch { }
            return pdfPath;
        }
        catch
        {
            return pdfPath;
        }
    }

    public static string? TryConvertPdfToPng(string pdfPath)
    {
        return TryConvertPdfToPng(pdfPath, 300, 90);
    }

    public static string? TryConvertPdfToPng(string pdfPath, int dpi, int quality)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return null;

            var sofficePath = ResolveSofficePath();
            if (string.IsNullOrWhiteSpace(sofficePath))
                return null;

            var outDir = Path.Combine(Path.GetTempPath(), $"BlueMax_PdfPreview_{Guid.NewGuid():N}");
            Directory.CreateDirectory(outDir);

            var psi = new ProcessStartInfo
            {
                FileName = sofficePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = $"--headless --norestore --convert-to png --outdir \"{outDir}\" \"{pdfPath}\""
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;
            if (!process.WaitForExit(120000))
            {
                try { process.Kill(); } catch { }
                return null;
            }

            var png = Directory.GetFiles(outDir, "*.png").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(png) || !File.Exists(png))
                return null;

            var target = Path.Combine(
                Path.GetDirectoryName(pdfPath) ?? outDir,
                Path.GetFileNameWithoutExtension(pdfPath) + ".png");
            try
            {
                File.Copy(png, target, overwrite: true);
                return target;
            }
            catch
            {
                return png;
            }
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveSofficePath()
    {
        try
        {
            var configured = new ReportDesignerSettingsStore().Load()?.LibreOfficeProgramPath;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var candidate = Path.Combine(configured.Trim(), "soffice.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        catch
        {
        }

        var defaultInstall = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program", "soffice.exe");
        if (File.Exists(defaultInstall))
            return defaultInstall;

        var x86Install = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LibreOffice", "program", "soffice.exe");
        if (File.Exists(x86Install))
            return x86Install;

        return "soffice";
    }
}
