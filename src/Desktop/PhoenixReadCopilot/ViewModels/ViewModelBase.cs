using CommunityToolkit.Mvvm.ComponentModel;

namespace PhoenixReadCopilot.ViewModels;

/// <summary>
/// 视图模型基类 (统一继承自 <see cref="ObservableObject"/>，支持分部属性与通知调度).
/// </summary>
public abstract class ViewModelBase : ObservableObject { }

