using System;
using System.IO;

namespace BlueMax.Infrastructure;

public static class AppPaths
{
    static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlueMax");

    public static string CertificatesOutput => Ensure("Certificates_Output");
    public static string ReportsOutput => Ensure("Reports_Output");
    public static string RentalsOutput => Ensure("Rentals_Output");
    public static string TemplatePreview => Ensure("TemplatePreview");
    public static string LogsDir => Ensure("Logs");
    public static string Backups => Ensure("Backups");

    static string Ensure(string sub)
    {
        var dir = Path.Combine(Root, sub);
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch
        {
        }

        return dir;
    }
}
