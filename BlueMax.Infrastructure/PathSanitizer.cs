using System.Text;

namespace BlueMax.Infrastructure;

internal static class PathSanitizer
{
    public static string Segment(string value)
    {
        var raw = (value ?? "").Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
            sb.Append(invalid.Contains(c) ? '_' : c);
        var name = sb.ToString().Trim().TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(name) ? "Default" : name;
    }
}
