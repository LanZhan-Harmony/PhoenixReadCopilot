using System.Drawing;
using System.Drawing.Text;

namespace PhoenixReadCopilot.Helpers;

internal static class FontHelper
{
    private static readonly List<string> _fonts = [];

    internal static async ValueTask<IReadOnlyList<string>> GetFontsAsync()
    {
        if (_fonts.Count == 0)
        {
            await Task.Run(() =>
            {
                InstalledFontCollection fonts = new();
                _fonts.AddRange(
                    fonts
                        .Families.Where(p => p.IsStyleAvailable(FontStyle.Regular))
                        .Select(f => f.Name)
                );
            });
        }

        return _fonts;
    }
}
