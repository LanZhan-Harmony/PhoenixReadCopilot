using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PhoenixReadCopilot.Contracts.ViewModels;

/// <summary>
/// 收藏分区详情视图模型接口.
/// </summary>
public interface IFavoriteSectionDetailViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 尝试首次加载.
    /// </summary>
    Task TryFirstLoadAsync();

    /// <summary>
    /// 刷新命令.
    /// </summary>
    IAsyncRelayCommand RefreshCommand { get; }
}
