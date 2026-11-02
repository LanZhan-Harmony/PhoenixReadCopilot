using System.Runtime.InteropServices;
using SharpGen.Runtime;

namespace Danmaku.Core;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
public interface ISwapChainPanelNative
{
    [PreserveSig]
    Result SetSwapChain([In] IntPtr swapChain);

    [PreserveSig]
    ulong Release();
}
