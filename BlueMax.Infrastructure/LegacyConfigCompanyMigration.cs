using System.Text.Json;

namespace BlueMax.Infrastructure;

/// <summary>
/// One-time migration of legacy company sticker fields from config.json into
/// ReportDesignerSettings so the sticker designer and preview use real company data.
/// </summary>
public static class LegacyConfigCompanyMigration
{
    public static void MigrateOnce()
    {
        try
        {
            var store = new ReportDesignerSettingsStore();
            var settings = store.Load();
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BlueMax",
                "config.json");
            if (Apply(settings, configPath))
                store.Save(settings);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    /// <summary>
    /// Fills company fields on <paramref name="settings"/> from the legacy config
    /// when they are empty. Returns true when at least one field was copied.
    /// </summary>
    internal static bool Apply(ReportDesignerSettings settings, string configPath)
    {
        if (settings == null || string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            return false;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(configPath));
        }
        catch
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            bool changed = false;

            if (string.IsNullOrWhiteSpace(settings.CompanyName) && TryGetString(root, "sticker_company_name", out var companyName))
            {
                settings.CompanyName = companyName;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(settings.CompanyHeader) && TryGetString(root, "sticker_tagline", out var tagline))
            {
                settings.CompanyHeader = tagline;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(settings.CompanyAddress) && TryGetString(root, "sticker_footer_address", out var address))
            {
                settings.CompanyAddress = address;
                changed = true;
            }
            if (string.IsNullOrWhiteSpace(settings.CompanyPhone) && TryGetString(root, "sticker_phone", out var phone))
            {
                settings.CompanyPhone = phone;
                changed = true;
            }
            return changed;
        }
    }

    static bool TryGetString(JsonElement root, string propertyName, out string value)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString() ?? "";
            return !string.IsNullOrWhiteSpace(value);
        }
        value = "";
        return false;
    }
}
