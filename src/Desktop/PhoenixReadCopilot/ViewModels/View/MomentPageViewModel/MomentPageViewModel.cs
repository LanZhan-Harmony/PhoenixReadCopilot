using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Moment;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
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
        CommentMainViewModel comment,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        ILoggerFactory loggerFactory
    )
    {
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

        Sections =
        [
            new VideoMomentSectionDetailViewModel(
                _dispatcherQueue,
                this,
                _settingsService,
                _loggerFactory.CreateLogger<VideoMomentSectionDetailViewModel>()
            ),
            new ComprehensiveMomentSectionDetailViewModel(
                _dispatcherQueue,
                this,
                _loggerFactory.CreateLogger<ComprehensiveMomentSectionDetailViewModel>(),
                _loggerFactory.CreateLogger<MomentUperSectionViewModel>(),
                _settingsService
            ),
        ];

        bool isLastVideoSection = _settings.LastMomentSectionIsVideo;
        IMomentSectionDetailViewModel section = isLastVideoSection
            ? Sections.First()
            : Sections.Last();
        SelectSectionCommand.Execute(section);
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            Initialized?.Invoke(this, EventArgs.Empty)
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
        SelectedSection.InitializeCommand.Execute(null);
    }

    [RelayCommand]
    private void ShowComment(MomentInformation data)
    {
        if (string.IsNullOrEmpty(data.CommentId) || CommentModule.Id == data.CommentId)
        {
            return;
        }

        CommentModule.Initialize(data.CommentId);
        IsCommentsOpened = true;
    }
}
