using Microsoft.Windows.ApplicationModel.Resources;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.Services;

internal sealed class LocalizationService : ILocalizationService
{
    private readonly ResourceLoader _loader = new();

    public string GetString(string resourceKey)
    {
        return _loader.GetString(resourceKey);
    }

    public string GetString(string resourceKey, params object[] args)
    {
        string format = _loader.GetString(resourceKey);
        return string.Format(format, args);
    }
}