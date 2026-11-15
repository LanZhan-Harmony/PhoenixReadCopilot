using System.ComponentModel;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 榜单区块项视图模型.
/// </summary>
public interface IRankSectionItemViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// 区块标题.
    /// </summary>
    string Title { get; }
}
