using System.Configuration;

namespace BlueMax.Presentation.Wpf.Services;

public static class AppSettingHelper
{
    public static void Save(string key, string value)
    {
        try
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, value ?? "");
            else
                config.AppSettings.Settings[key].Value = value ?? "";
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch
        {
        }
    }
}
