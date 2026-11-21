using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Search;

namespace PhoenixReadCopilot.Contracts.ViewModels;

/// <summary>
/// 搜索分区详情视图模型.
/// </summary>
public interface ISearchSectionDetailViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 分区类型.
    /// </summary>
    public SearchSectionType SectionType { get; }

    /// <summary>
    /// 是否为空.
    /// </summary>
    public bool IsEmpty { get; }

    /// <summary>
    /// 尝试首次加载.
    /// </summary>
    Task TryFirstLoadAsync();

    /// <summary>
    /// 刷新命令.
    /// </summary>
    IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// 初始化.
    /// </summary>
    void Initialize(string keyword, SearchPartition partition);

    /// <summary>
    /// 清理数据.
    /// </summary>
    void Clear();
}
