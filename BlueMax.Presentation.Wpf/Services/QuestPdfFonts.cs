using System;
using System.IO;
using QuestPDF.Drawing;

namespace BlueMax.Presentation.Wpf.Services;

public static class QuestPdfFonts
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void EnsureCairoRegistered()
    {
        lock (Sync)
        {
            if (_registered)
                return;
            try
            {
                var windowsFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                var baseDir = AppContext.BaseDirectory;
                var candidates = new[]
                {
                    Path.Combine(windowsFonts, "Cairo-Regular.ttf"),
                    Path.Combine(baseDir, "Assets", "Fonts", "Cairo-Regular.ttf"),
                    Path.Combine(baseDir, "Fonts", "Cairo-Regular.ttf")
                };

                string? regular = null;
                foreach (var candidate in candidates)
                {
                    if (!File.Exists(candidate))
                        continue;
                    regular = candidate;
                    break;
                }
                if (regular == null)
                    return;

                using (var regularStream = File.OpenRead(regular))
                    FontManager.RegisterFont(regularStream);

                var boldCandidates = new[]
                {
                    Path.Combine(Path.GetDirectoryName(regular) ?? "", "Cairo-Bold.ttf"),
                    Path.Combine(windowsFonts, "Cairo-Bold.ttf")
                };
                foreach (var boldCandidate in boldCandidates)
                {
                    if (!File.Exists(boldCandidate))
                        continue;
                    using var boldStream = File.OpenRead(boldCandidate);
                    FontManager.RegisterFont(boldStream);
                    break;
                }

                _registered = true;
            }
            catch
            {
            }
        }
    }
}
