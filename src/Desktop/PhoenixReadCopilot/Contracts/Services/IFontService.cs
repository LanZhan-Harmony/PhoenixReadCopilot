namespace PhoenixReadCopilot.Contracts.Services;

internal interface IFontService
{
    /// <summary>
    /// Get font families.
    /// </summary>
    /// <returns>Fonts.</returns>
    ValueTask<IReadOnlyList<string>> GetFontsAsync();
}
