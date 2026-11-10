using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Danmaku.Core;

/// <summary>
/// WinUI 3 / XAML SwapChainPanel 原生互操作接口.
/// </summary>
[GeneratedComInterface]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
public partial interface ISwapChainPanelNative
{
    [PreserveSig]
    int SetSwapChain(nint swapChain);
}
