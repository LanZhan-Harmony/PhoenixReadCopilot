using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// 光标网格.
/// </summary>
public partial class CursorGrid : Grid
{
    /// <summary>
    /// 隐藏光标.
    /// </summary>
    public void HideCursor()
    {
        ProtectedCursor ??= InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        ProtectedCursor.Dispose();
    }

    /// <summary>
    /// 显示光标.
    /// </summary>
    public void ShowCursor() =>
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
}
