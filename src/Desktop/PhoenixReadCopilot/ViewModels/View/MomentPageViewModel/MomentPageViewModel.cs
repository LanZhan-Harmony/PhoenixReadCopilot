using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Moment;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Moment;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 动态页面视图模型.
/// </summary>
public sealed partial class MomentPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentPageViewModel"/> class.
    /// </summary>
    public MomentPageViewModel(
        IMomentDiscoveryService discoveryService,
        CommentMainViewModel comment,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        ILoggerFactory loggerFactory
    )
    {
        _momentDiscoveryService = discoveryService;
        CommentModule = comment;
        _dispatcherQueue = dispatcherQueue;
        _settingsService = settingsService;
        _loggerFactory = loggerFactory;
    }

    [RelayCommand]
    private void Initialize()
    {
        if (Sections?.Count > 0)
        {
            return;
        }

        Sections = new List<IMomentSectionDetailViewModel>
        {
            new VideoMomentSectionDetailViewModel(
                _momentDiscoveryService,
                _dispatcherQueue,
                this,
                _settingsService,
                _loggerFactory.CreateLogger<VideoMomentSectionDetailViewModel>()
            ),
            new ComprehensiveMomentSectionDetailViewModel(
                _momentDiscoveryService,
                _dispatcherQueue,
                this,
                _loggerFactory.CreateLogger<ComprehensiveMomentSectionDetailViewModel>(),
                _loggerFactory.CreateLogger<MomentUperSectionViewModel>(),
                _settingsService
            ),
        };

        var isLastVideoSection = _settings.LastMomentSectionIsVideo;
        var section = isLastVideoSection ? Sections.First() : Sections.Last();
        SelectSectionCommand.Execute(section);
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => Initialized?.Invoke(this, EventArgs.Empty)
        );
    }

    [RelayCommand]
    private void SelectSection(IMomentSectionDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        var isVideo = section is VideoMomentSectionDetailViewModel;
        _settings.LastMomentSectionIsVideo = isVideo;
        SelectedSection.InitializeCommand.Execute(default);
    }

    [RelayCommand]
    private void ShowComment(MomentInformation data)
    {
        IsCommentsOpened = true;
        if (CommentModule.Id == data.CommentId)
        {
            return;
        }

        CommentModule.Initialize(data.CommentId, data.CommentType!.Value, CommentSortType.Hot);
        CommentModule.RefreshCommand.Execute(default);
    }
}
