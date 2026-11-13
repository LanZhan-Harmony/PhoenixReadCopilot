using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 短剧分集详情视图模型.
/// </summary>
public sealed partial class VideoPlayerPartSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    private readonly Action<Episode> _partSelectedAction;
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial List<Episode> Parts { get; set; }

    [ObservableProperty]
    public partial Episode? SelectedPart { get; set; }

    [ObservableProperty]
    public partial bool OnlyIndex { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerPartSectionDetailViewModel"/> class.
    /// </summary>
    public VideoPlayerPartSectionDetailViewModel(
        IReadOnlyList<Episode> parts,
        string currentVideoId,
        Action<Episode> action,
        ISettingsService settingsService
    )
    {
        _settings = settingsService.Settings;
        _partSelectedAction = action;
        Parts = [.. parts];
        SelectedPart = parts.FirstOrDefault(p => p.VideoId == currentVideoId);
        OnlyIndex = _settings.IsVideoPlayerPartsOnlyIndex;
    }

    /// <summary>
    /// 分集变更.
    /// </summary>
    public event EventHandler? PartChanged;

    /// <inheritdoc/>
    public string Title { get; } = Resources.Parts;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private void SelectPart(Episode part)
    {
        SelectedPart = part;
        _partSelectedAction?.Invoke(part);
        RaisePartChanged();
    }

    [RelayCommand]
    private void UpdateSelectedPart(Episode part)
    {
        if (SelectedPart != part)
        {
            SelectedPart = Parts.FirstOrDefault(p => p.VideoId == part.VideoId);
            RaisePartChanged();
        }
    }

    /// <summary>
    /// 发送分集变更事件.
    /// </summary>
    private void RaisePartChanged() => PartChanged?.Invoke(this, EventArgs.Empty);

    partial void OnOnlyIndexChanged(bool value) => _settings.IsVideoPlayerPartsOnlyIndex = value;
}
