using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Models.Settings;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 应用程序设置服务实现.
/// </summary>
public sealed class SettingsService : ISettingsService, IDisposable
{
    private readonly ILogger<SettingsService>? _logger;
    private readonly string _folderPath = PathHelper.SettingsFolderPath;
    private readonly string _filePath;
    private readonly Debouncer _writeSettingsDebouncer;

    public AppSettings Settings
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            UnsubscribeFromSettings(field);
            field = value;
            SubscribeToSettings(value);
        }
    }

    public SettingsService(ILogger<SettingsService>? logger = null)
    {
        _logger = logger;
        _filePath = Path.Combine(_folderPath, "settings.json");
        _writeSettingsDebouncer = new Debouncer(
            TimeSpan.FromMilliseconds(500),
            ex => _logger?.LogError(ex, "[SettingsService] Failed to save settings to disk")
        );
        Settings = LoadFromDisk();
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger?.LogInformation(
                    "[SettingsService] Settings file does not exist, using default values"
                );
                return new AppSettings();
            }

            string json = File.ReadAllText(_filePath);
            AppSettings? loaded = JsonAotSerializer.Deserialize<AppSettings>(json);
            return loaded ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            _logger?.LogError(
                ex,
                "[SettingsService] JSON parsing failed, backup corrupted file and use default values"
            );
            BackupCorruptedFile();
            return new AppSettings();
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "[SettingsService] Failed to load settings, using default values"
            );
            return new AppSettings();
        }
    }

    public async Task SaveToDiskAsync()
    {
        await _writeSettingsDebouncer.RunAsync(SaveToDiskInternalAsync);
    }

    public async Task FlushAsync()
    {
        bool executed = await _writeSettingsDebouncer.FlushAsync();
        if (!executed)
        {
            await SaveToDiskInternalAsync();
        }
    }

    private async Task SaveToDiskInternalAsync()
    {
        try
        {
            string json = JsonAotSerializer.Serialize(Settings);
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            string tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json).ConfigureAwait(false);
            File.Move(tmpPath, _filePath, true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[SettingsService] Failed to save settings to disk");
        }
    }

    private void BackupCorruptedFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string backup = _filePath + $".corrupted_{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(_filePath, backup);
                _logger?.LogInformation(
                    $"[SettingsService] Corrupted settings backed up to: {backup}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[SettingsService] Failed to backup corrupted settings file");
        }
    }

    private void SubscribeToSettings(AppSettings? settings)
    {
        if (settings is null)
        {
            return;
        }

        settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void UnsubscribeFromSettings(AppSettings? settings)
    {
        if (settings is null)
        {
            return;
        }

        settings.PropertyChanged -= OnSettingsPropertyChanged;
    }

    private async void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await SaveToDiskAsync();
    }

    public void Dispose()
    {
        _writeSettingsDebouncer.Dispose();
        UnsubscribeFromSettings(Settings);
    }
}
