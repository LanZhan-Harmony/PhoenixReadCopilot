using System.ComponentModel;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 播放器区块详情视图模型.
/// </summary>
public interface IPlayerSectionDetailViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 尝试首次加载.
    /// </summary>
    Task TryFirstLoadAsync();

    /// <summary>
    /// 标题.
    /// </summary>
    string Title { get; }
}
