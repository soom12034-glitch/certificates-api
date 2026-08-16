using System.Configuration;
using System.Text.Json;

namespace BlueMax.Infrastructure;

public static class DocumentSequenceStore
{
    static readonly object Sync = new();

    static string GetMapPath() => Path.Combine(AppPaths.CertificatesOutput, "sequence_counters.json");

    public static string GetNextFormatted(string sequenceKey)
    {
        lock (Sync)
        {
            var map = LoadMap();

            if (!map.ContainsKey(sequenceKey))
            {
                // Migrate a legacy exe.config counter if present so numbering stays continuous.
                var legacy = TryReadLegacyCounter(sequenceKey);
                if (legacy.HasValue)
                    map[sequenceKey] = legacy.Value;
            }

            if (!map.TryGetValue(sequenceKey, out var value) || value < 0)
                value = 0;

            value++;
            map[sequenceKey] = value;
            SaveMap(map);

            return value.ToString("D4");
        }
    }

    static int? TryReadLegacyCounter(string sequenceKey)
    {
        try
        {
            var key = $"{sequenceKey}.Counter";
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var raw = config.AppSettings.Settings[key]?.Value;
            if (raw != null && int.TryParse(raw, out var value) && value >= 0)
                return value;
        }
        catch
        {
        }
        return null;
    }

    static Dictionary<string, int> LoadMap()
    {
        try
        {
            var path = GetMapPath();
            if (!File.Exists(path))
                return new Dictionary<string, int>(StringComparer.Ordinal);

            var json = File.ReadAllText(path);
            var map = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            return map ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }

    static void SaveMap(Dictionary<string, int> map)
    {
        try
        {
            var path = GetMapPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(map));
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
        }
    }
}
