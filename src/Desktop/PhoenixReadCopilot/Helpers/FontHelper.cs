using System.Drawing;
using System.Drawing.Text;

namespace PhoenixReadCopilot.Helpers;

internal static class FontHelper
{
    private static readonly Lock _lock = new();
    private static List<string>? _fonts;

    internal static async ValueTask<IReadOnlyList<string>> GetFontsAsync()
    {
        if (_fonts is { Count: > 0 } cached)
        {
            return cached;
        }

        List<string> loaded = await Task.Run(() =>
        {
            InstalledFontCollection fonts = new();
            return fonts
                .Families.Where(p => p.IsStyleAvailable(FontStyle.Regular))
                .Select(f => f.Name)
                .ToList();
        });

        lock (_lock)
        {
            _fonts ??= loaded;
            return _fonts;
        }
    }
}
