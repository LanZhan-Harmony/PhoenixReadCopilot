using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 动态分区详情视图模型.
/// </summary>
public interface IMomentSectionDetailViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 分区类型.
    /// </summary>
    MomentSectionType SectionType { get; }

    /// <summary>
    /// 初始化.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 刷新命令.
    /// </summary>
    IRelayCommand RefreshCommand { get; }
}
