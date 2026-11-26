using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Contracts.ViewModels;

/// <summary>
/// 历史记录详情区块.
/// </summary>
public interface IHistorySectionDetailViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 历史记录类型.
    /// </summary>
    ViewHistoryTabType Type { get; }

    /// <summary>
    /// 是否为空.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// 是否正在加载.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 尝试首次加载.
    /// </summary>
    Task TryFirstLoadAsync();

    /// <summary>
    /// 刷新命令.
    /// </summary>
    IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// 刷新数据.
    /// </summary>
    Task RefreshAsync();
}