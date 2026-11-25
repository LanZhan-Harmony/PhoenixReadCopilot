using PhoenixReadCopilot.Core.Enums;
using Windows.UI;

namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 窗口背景材质管理服务契约.
/// </summary>
internal interface IMaterialSelectorService : IDisposable
{
    /// <summary>
    /// 当前窗口背景材质.
    /// </summary>
    BackdropMaterialType Material { get; }

    /// <summary>
    /// 是否启用窗口失去焦点后停止背景更新.
    /// </summary>
    bool IsFallbackEnabled { get; }

    /// <summary>
    /// 背景不透明度 (0-100).
    /// </summary>
    byte LuminosityOpacity { get; }

    /// <summary>
    /// 窗口背景着色颜色.
    /// </summary>
    Color TintColor { get; }

    /// <summary>
    /// 当前材质是否支持失焦回退配置.
    /// </summary>
    bool SupportsFallback { get; }

    /// <summary>
    /// 当前材质是否支持不透明度调节.
    /// </summary>
    bool SupportsLuminosityOpacity { get; }

    /// <summary>
    /// 当前材质是否支持自定义背景颜色.
    /// </summary>
    bool SupportsTintColor { get; }

    /// <summary>
    /// 材质或外观属性发生变更时触发.
    /// </summary>
    event EventHandler? AppearanceChanged;

    /// <summary>
    /// 初始化材质服务并挂载已注册窗口.
    /// </summary>
    void Initialize();

    /// <summary>
    /// 设置全局窗口背景材质.
    /// </summary>
    /// <param name="material">目标材质类型.</param>
    /// <param name="force">是否强制重新应用.</param>
    void SetMaterial(BackdropMaterialType material, bool force = false);

    /// <summary>
    /// 设置窗口失去焦点后是否停止更新背景.
    /// </summary>
    /// <param name="enabled">是否启用失焦回退.</param>
    void SetFallbackEnabled(bool enabled);

    /// <summary>
    /// 设置背景不透明度 (0-100).
    /// </summary>
    /// <param name="opacity">不透明度百分比.</param>
    void SetLuminosityOpacity(byte opacity);

    /// <summary>
    /// 设置窗口背景着色颜色.
    /// </summary>
    /// <param name="color">目标颜色.</param>
    void SetTintColor(Color color);

    /// <summary>
    /// 重置窗口材质及外观为默认值.
    /// </summary>
    void ResetMaterial();
}
