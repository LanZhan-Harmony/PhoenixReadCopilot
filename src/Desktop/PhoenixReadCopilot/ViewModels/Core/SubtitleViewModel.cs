using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Core.Models.Subtitle;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 视频播放器字幕视图模型.
/// </summary>
public sealed partial class SubtitleViewModel : ViewModelBase
{
    private readonly AppSettings Settings;

    private readonly ISubtitleService _service;
    private readonly ILogger<SubtitleViewModel> _logger;

    private string _aid;
    private string _cid;
    private int _position;
    private List<SubtitleInformation> _subtitles;
    private Action? _initializedAction;

    [ObservableProperty]
    public partial bool IsAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial List<SubtitleMeta>? Metas { get; set; }

    [ObservableProperty]
    public partial SubtitleMeta? SelectedMeta { get; set; }

    [ObservableProperty]
    public partial string? CurrentSubtitle { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleViewModel"/> class.
    /// </summary>
    public SubtitleViewModel(
        ISubtitleService service,
        ISettingsService settingsService,
        ILogger<SubtitleViewModel> logger
    )
    {
        _service = service;
        Settings = settingsService.Settings;
        _logger = logger;
    }

    /// <summary>
    /// 重置字幕目标数据.
    /// </summary>
    public void ResetData(string aid, string cid)
    {
        ClearAll();
        _aid = aid;
        _cid = cid;
        IsEnabled = Settings.IsSubtitleEnabled;
    }

    /// <summary>
    /// 设置初始化回调.
    /// </summary>
    public void SetInitializedCallback(Action action) => _initializedAction = action;

    /// <summary>
    /// 更新当前播放位置.
    /// </summary>
    public void UpdatePosition(int position)
    {
        _position = position;
        if (SelectedMeta is null || _subtitles is null || _subtitles.Count == 0 || !IsEnabled)
        {
            return;
        }

        SubtitleInformation? subtitle = _subtitles.FirstOrDefault(p =>
            p.StartPosition <= _position && p.EndPosition >= _position
        );
        CurrentSubtitle = subtitle?.Content;
    }

    /// <summary>
    /// 清除字幕.
    /// </summary>
    public void ClearSubtitle() => CurrentSubtitle = null;

    /// <summary>
    /// 清除所有数据.
    /// </summary>
    public void ClearAll()
    {
        _aid = null;
        _cid = null;
        _subtitles = null;
        SelectedMeta = null;
        _position = 0;
        Metas = null;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Metas is not null)
        {
            return;
        }

        try
        {
            Metas = [.. await _service.GetSubtitleMetasAsync(_aid, _cid)];
            IsAvailable = Metas?.Count > 0;
            SubtitleMeta? firstMeta = GetInitialMeta();
            if (firstMeta is not null)
            {
                ChangeSubtitleCommand.Execute(firstMeta);
            }

            _initializedAction?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载字幕元数据失败");
        }
    }

    [RelayCommand]
    private void DeselectSubtitle()
    {
        SelectedMeta = null;
        CurrentSubtitle = string.Empty;
    }

    [RelayCommand]
    private async Task ChangeSubtitleAsync(SubtitleMeta meta)
    {
        if (meta is null || meta == SelectedMeta)
        {
            return;
        }

        try
        {
            IReadOnlyList<SubtitleInformation>? subtitles = await _service.GetSubtitleDetailAsync(
                meta
            );
            if (subtitles is not null)
            {
                _subtitles = [.. subtitles];
                SelectedMeta = meta;
                UpdatePosition(_position);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载字幕失败");
        }
    }

    private SubtitleMeta? GetInitialMeta()
    {
        SubtitleMeta? firstNonAIMeta = Metas?.FirstOrDefault(p => !p.IsAI);
        if (firstNonAIMeta is not null)
        {
            return firstNonAIMeta;
        }

        return null;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        DeselectSubtitle();
        Settings.IsSubtitleEnabled = value;
    }
}
