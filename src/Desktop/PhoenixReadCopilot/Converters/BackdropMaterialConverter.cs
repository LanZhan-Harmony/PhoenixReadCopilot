using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 窗口背景材质类型转换为可读文本.
    /// </summary>
    public static string BackdropMaterialToText(BackdropMaterialType type) =>
        type switch
        {
            BackdropMaterialType.Pure => Resources.MaterialPure,
            BackdropMaterialType.Mica => Resources.MaterialMica,
            BackdropMaterialType.MicaAlt => Resources.MaterialMicaAlt,
            BackdropMaterialType.DesktopAcrylic => Resources.MaterialDesktopAcrylic,
            BackdropMaterialType.AcrylicBase => Resources.MaterialAcrylicBase,
            BackdropMaterialType.AcrylicThin => Resources.MaterialAcrylicThin,
            BackdropMaterialType.Blur => Resources.MaterialBlur,
            BackdropMaterialType.Transparent => Resources.MaterialTransparent,
            BackdropMaterialType.Animated => Resources.MaterialAnimated,
            _ => Resources.MaterialDesktopAcrylic,
        };
}
