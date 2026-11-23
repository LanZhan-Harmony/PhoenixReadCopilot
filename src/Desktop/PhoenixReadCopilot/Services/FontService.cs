using System.Drawing;
using System.Drawing.Text;
using PhoenixReadCopilot.Contracts.Services;

namespace PhoenixReadCopilot.Services;

internal sealed class FontService : IFontService
{
    private static readonly List<string> _fonts = [];

    public async ValueTask<IReadOnlyList<string>> GetFontsAsync()
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
