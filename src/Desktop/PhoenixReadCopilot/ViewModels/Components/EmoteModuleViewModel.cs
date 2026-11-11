using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Helpers;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 表情模块视图模型.
/// </summary>
public sealed partial class EmoteModuleViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 红果表情列表.
    /// </summary>
    public IReadOnlyList<EmojiItem> Emojis => EmojiHelper.AllEmojis;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmoteModuleViewModel"/> class.
    /// </summary>
    public EmoteModuleViewModel() { }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }
}
