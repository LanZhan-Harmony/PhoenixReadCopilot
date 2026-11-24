using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 表情模块视图模型.
/// </summary>
internal sealed partial class EmoteModuleViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 红果表情列表.
    /// </summary>
    public IReadOnlyList<EmojiItem> Emojis => EmojiHelper.AllEmojis;

    public EmoteModuleViewModel() { }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }
}
