using System.Configuration;

namespace BlueMax.Infrastructure;

public static class DocumentSequenceStore
{
    static readonly object Sync = new();

    public static string GetNextFormatted(string sequenceKey)
    {
        lock (Sync)
        {
            var key = $"{sequenceKey}.Counter";
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var raw = config.AppSettings.Settings[key]?.Value ?? "0";

            if (!int.TryParse(raw, out var value) || value < 0)
                value = 0;

            value++;
            var next = value.ToString("D4");

            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, value.ToString());
            else
                config.AppSettings.Settings[key].Value = value.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            return next;
        }
    }
}
