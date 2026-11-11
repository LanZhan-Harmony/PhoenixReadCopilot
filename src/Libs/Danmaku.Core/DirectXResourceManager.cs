using System.Numerics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;
using WinRT;

namespace Danmaku.Core;

/// <summary>
/// DirectX 资源管理器，负责创建和管理所有 DirectX 资源.
/// </summary>
internal sealed class DirectXResourceManager : IDisposable
{
    private readonly SwapChainPanel _swapChainPanel;
    private float _scale;
    private float _dpi;
    private float _canvasWidth;
    private float _canvasHeight;
    private double _logicalWidth;
    private double _logicalHeight;

    private bool _isDisposed;
    private CancellationTokenSource _delayedResizeCts;
    private readonly Lock _resizeLock = new();

    /// <summary>
    /// 渲染呈现与交换链尺寸调整的互斥锁.
    /// </summary>
    public Lock RenderLock { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectXResourceManager"/> class.
    /// </summary>
    /// <param name="swapChainPanel">SwapChainPanel 控件.</param>
    /// <param name="scale">缩放比例.</param>
    public DirectXResourceManager(SwapChainPanel swapChainPanel, float scale)
    {
        _swapChainPanel = swapChainPanel ?? throw new ArgumentNullException(nameof(swapChainPanel));
        _scale = scale;
        _dpi = 96.0f * _scale;
        _canvasWidth = (float)_swapChainPanel.ActualWidth * _scale;
        _canvasHeight = (float)_swapChainPanel.ActualHeight * _scale;
        _logicalWidth = _swapChainPanel.ActualWidth;
        _logicalHeight = _swapChainPanel.ActualHeight;

        // 订阅事件
        _swapChainPanel.CompositionScaleChanged += OnCompositionScaleChanged;
        _swapChainPanel.SizeChanged += OnSizeChanged;

        InitializeDirectX();
        CreateCaches();
    }

    #region DirectX Resources

    /// <summary>
    /// D3D11 设备.
    /// </summary>
    public ID3D11Device D3DDevice { get; private set; }

    /// <summary>
    /// D3D11 设备上下文.
    /// </summary>
    public ID3D11DeviceContext D3DContext { get; private set; }

    /// <summary>
    /// DXGI 交换链.
    /// </summary>
    public IDXGISwapChain1 SwapChain { get; private set; }

    /// <summary>
    /// D3D11 渲染目标视图.
    /// </summary>
    public ID3D11RenderTargetView RenderTargetView { get; private set; }

    /// <summary>
    /// D2D1 工厂.
    /// </summary>
    public ID2D1Factory1 D2DFactory { get; private set; }

    /// <summary>
    /// D2D1 设备.
    /// </summary>
    public ID2D1Device D2DDevice { get; private set; }

    /// <summary>
    /// D2D1 设备上下文.
    /// </summary>
    public ID2D1DeviceContext D2DContext { get; private set; }

    /// <summary>
    /// D2D1 目标位图.
    /// </summary>
    public ID2D1Bitmap1 D2DTargetBitmap { get; private set; }

    /// <summary>
    /// DirectWrite 工厂.
    /// </summary>
    public IDWriteFactory DWriteFactory { get; private set; }

    /// <summary>
    /// DPI 值.
    /// </summary>
    public float Dpi => _dpi;

    /// <summary>
    /// 缩放比例.
    /// </summary>
    public float Scale => _scale;

    /// <summary>
    /// 画布宽度（物理像素）.
    /// </summary>
    public float CanvasWidth => _canvasWidth;

    /// <summary>
    /// 画布高度（物理像素）.
    /// </summary>
    public float CanvasHeight => _canvasHeight;

    /// <summary>
    /// 逻辑宽度（线程安全的缓存值）.
    /// </summary>
    public double LogicalWidth => _logicalWidth;

    /// <summary>
    /// 逻辑高度（线程安全的缓存值）.
    /// </summary>
    public double LogicalHeight => _logicalHeight;

    #endregion

    #region Resource Caches

    /// <summary>
    /// 文本格式缓存.
    /// </summary>
    public Dictionary<string, IDWriteTextFormat> TextFormatCache { get; private set; }

    /// <summary>
    /// 画刷缓存.
    /// </summary>
    public Dictionary<uint, ID2D1SolidColorBrush> BrushCache { get; private set; }

    /// <summary>
    /// 对象池管理器（替代 LRU 缓存）.
    /// </summary>
    public ObjectPoolManager ObjectPool { get; private set; }

    /// <summary>
    /// 缓存锁.
    /// </summary>
    public Lock CacheLock { get; } = new();

    #endregion

    /// <summary>
    /// 初始化 DirectX 资源.
    /// </summary>
    private void InitializeDirectX()
    {
        CreateD3DDevice();
        CreateSwapChain();
        CreateRenderTargetView();
        CreateD2DResources();
        CreateDWriteFactory();

        Logger.Log("DirectX resources initialized with color font support");
    }

    /// <summary>
    /// 创建 D3D11 设备.
    /// </summary>
    private void CreateD3DDevice()
    {
        DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;
#if DEBUG
        creationFlags |= DeviceCreationFlags.Debug;
#endif

        Vortice.Direct3D.FeatureLevel[] featureLevels =
        [
            Vortice.Direct3D.FeatureLevel.Level_11_1,
            Vortice.Direct3D.FeatureLevel.Level_11_0,
            Vortice.Direct3D.FeatureLevel.Level_10_1,
        ];

        D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            creationFlags,
            featureLevels,
            out ID3D11Device device,
            out ID3D11DeviceContext context
        );

        D3DDevice = device;
        D3DContext = context;
    }

    /// <summary>
    /// 创建交换链.
    /// </summary>
    private void CreateSwapChain()
    {
        using IDXGIDevice dxgiDevice = D3DDevice.QueryInterface<IDXGIDevice>();
        using IDXGIAdapter dxgiAdapter = dxgiDevice.GetAdapter();
        using IDXGIFactory2 dxgiFactory = dxgiAdapter.GetParent<IDXGIFactory2>();

        // 超采样模式：使用物理像素大小创建 SwapChain 以获得更清晰的渲染效果
        var bufferWidth = (uint)Math.Max(1, (int)_canvasWidth);
        var bufferHeight = (uint)Math.Max(1, (int)_canvasHeight);

        SwapChainDescription1 swapChainDesc = new()
        {
            Width = bufferWidth,
            Height = bufferHeight,
            Format = Format.B8G8R8A8_UNorm,
            BufferCount = 2,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = new SampleDescription(1, 0),
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Premultiplied,
        };

        Logger.Log($"Creating SwapChain: Physical={bufferWidth}x{bufferHeight}, Scale={_scale}");

        SwapChain = dxgiFactory.CreateSwapChainForComposition(D3DDevice, swapChainDesc);

        // Associate swap chain with SwapChainPanel
        SetSwapChainOnPanel();

        Logger.Log(
            $"SwapChain set successfully, physical size: {swapChainDesc.Width}x{swapChainDesc.Height}"
        );
    }

    /// <summary>
    /// 将交换链关联到 SwapChainPanel.
    /// </summary>
    private void SetSwapChainOnPanel()
    {
        SharpGen.Runtime.Result setSwapChainResult;
        unsafe
        {
            IWinRTObject winrtObj = _swapChainPanel.As<IWinRTObject>();
            IObjectReference objRef = winrtObj.NativeObject;
            var panelPtr = objRef.ThisPtr;

            Guid guid = typeof(ISwapChainPanelNative).GUID;
            void* nativePanelPtr;
            var hr = (
                (delegate* unmanaged[Stdcall]<IntPtr, Guid*, void**, int>)(*(void***)panelPtr)[0]
            )(panelPtr, &guid, &nativePanelPtr);
            if (hr < 0)
            {
                Logger.Log($"QueryInterface for ISwapChainPanelNative failed: 0x{hr:X8}");
#pragma warning disable CA2201 // 不要引发保留的异常类型
                throw new System.Runtime.InteropServices.COMException(
                    $"Failed to query ISwapChainPanelNative interface",
                    hr
                );
#pragma warning restore CA2201
            }

            try
            {
                delegate* unmanaged[Stdcall]<void*, nint, int> setSwapChainFunc =
                    (delegate* unmanaged[Stdcall]<void*, IntPtr, int>)(*(void***)nativePanelPtr)[3];
                hr = setSwapChainFunc(nativePanelPtr, SwapChain.NativePointer);
                setSwapChainResult = new SharpGen.Runtime.Result(hr);
            }
            finally
            {
                ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)nativePanelPtr)[2])(
                    nativePanelPtr
                );
            }
        }

        if (setSwapChainResult.Failure)
        {
            Logger.Log($"SetSwapChain failed: {setSwapChainResult}");
#pragma warning disable CA2201 // 不要引发保留的异常类型
            throw new Exception($"Failed to set swap chain: {setSwapChainResult}");
#pragma warning restore CA2201
        }

        // 设置变换矩阵
        SetTransformMatrix();
    }

    /// <summary>
    /// 设置变换矩阵：将物理像素坐标映射到逻辑像素空间.
    /// 在超采样模式下，SwapChain 使用物理像素大小，需要通过矩阵变换映射回 SwapChainPanel 的逻辑像素坐标系.
    /// </summary>
    private void SetTransformMatrix()
    {
        if (SwapChain is not null)
        {
            // 超采样渲染：SwapChain 是物理像素，需要缩小到逻辑像素以匹配 SwapChainPanel
            // 例如：200% DPI 下，SwapChain=1920x1080(物理)，SwapChainPanel=960x540(逻辑)
            // MatrixTransform = (0.5, 0.5) 将物理坐标映射到逻辑坐标
            Matrix3x2 matrix = new() { M11 = (float)(1.0 / _scale), M22 = (float)(1.0 / _scale) };

            // AOT 兼容：直接使用 QueryInterface 而不是 As<T>()
            using IDXGISwapChain2 swapChain2 = SwapChain.QueryInterface<IDXGISwapChain2>();
            swapChain2.MatrixTransform = matrix;
            Logger.Log($"SetTransformMatrix: scale={_scale}, matrix=({matrix.M11},{matrix.M22})");
        }
    }

    /// <summary>
    /// 创建渲染目标视图.
    /// </summary>
    private void CreateRenderTargetView()
    {
        using ID3D11Texture2D backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        RenderTargetView = D3DDevice.CreateRenderTargetView(backBuffer, null);
    }

    /// <summary>
    /// 创建 D2D 资源.
    /// </summary>
    private void CreateD2DResources()
    {
        D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();

        using IDXGIDevice dxgiDevice = D3DDevice.QueryInterface<IDXGIDevice>();
        D2DDevice = D2DFactory.CreateDevice(dxgiDevice);

        D2DContext = D2DDevice.CreateDeviceContext(
            DeviceContextOptions.EnableMultithreadedOptimizations
        );
        D2DContext.TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode.Grayscale;

        CreateD2DRenderTarget();
    }

    /// <summary>
    /// 创建 D2D 渲染目标.
    /// </summary>
    private void CreateD2DRenderTarget()
    {
        using IDXGISurface dxgiBackBuffer = SwapChain.GetBuffer<IDXGISurface>(0);

        // 🔑 关键修复：使用标准 96 DPI，因为 CanvasWidth/Height 已经是物理像素
        // 这样 D2D 的逻辑坐标就直接对应物理像素，避免"双重缩放"问题
        BitmapProperties1 bitmapProperties = new(
            new Vortice.DCommon.PixelFormat(
                Format.B8G8R8A8_UNorm,
                Vortice.DCommon.AlphaMode.Premultiplied
            ),
            96.0f, // 使用标准 DPI，不使用 _dpi
            96.0f,
            BitmapOptions.Target | BitmapOptions.CannotDraw
        );

        D2DTargetBitmap = D2DContext.CreateBitmapFromDxgiSurface(dxgiBackBuffer, bitmapProperties);
        D2DContext.Target = D2DTargetBitmap;

        D2DContext.AntialiasMode = AntialiasMode.PerPrimitive;
        D2DContext.TextAntialiasMode = Vortice.Direct2D1.TextAntialiasMode.Cleartype;

        Logger.Log(
            $"D2D Render Target created: Bitmap DPI=96 (physical pixels), Display DPI={_dpi}, Scale={_scale}"
        );
    }

    /// <summary>
    /// 创建 DirectWrite 工厂.
    /// </summary>
    private void CreateDWriteFactory()
    {
        DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
    }

    /// <summary>
    /// 创建缓存字典.
    /// </summary>
    private void CreateCaches()
    {
        TextFormatCache = [];
        BrushCache = [];
        ObjectPool = new ObjectPoolManager();
    }

    /// <summary>
    /// 重新创建渲染目标视图（当窗口大小改变时调用）.
    /// </summary>
    public void RecreateRenderTargetView()
    {
        RenderTargetView?.Dispose();
        CreateRenderTargetView();
    }

    /// <summary>
    /// 重新创建 D2D 渲染目标（当窗口大小改变时调用）.
    /// </summary>
    public void RecreateD2DRenderTarget()
    {
        D2DTargetBitmap?.Dispose();
        CreateD2DRenderTarget();
    }

    /// <summary>
    /// 更新缩放比例和 DPI.
    /// </summary>
    /// <param name="scale">新的缩放比例.</param>
    public void UpdateScale(float scale)
    {
        _scale = scale;
        _dpi = 96.0f * _scale;

        // 更新交换链的变换矩阵
        SetTransformMatrix();

        Logger.Log($"Scale updated: {_scale}, DPI={_dpi}");
    }

    /// <summary>
    /// 调整交换链大小.
    /// </summary>
    /// <param name="width">新宽度（逻辑像素）.</param>
    /// <param name="height">新高度（逻辑像素）.</param>
    public void ResizeSwapChain(float width, float height)
    {
        lock (RenderLock)
        {
            if (SwapChain is null)
            {
                return;
            }

            // 更新画布大小（物理像素，用于超采样渲染）
            _canvasWidth = width * _scale;
            _canvasHeight = height * _scale;

            D2DContext.Target = null;
            D2DTargetBitmap?.Dispose();
            D2DTargetBitmap = null;
            RenderTargetView?.Dispose();
            RenderTargetView = null;

            // 超采样模式：SwapChain 使用物理像素大小
            var physicalWidth = (uint)Math.Max(1, (int)_canvasWidth);
            var physicalHeight = (uint)Math.Max(1, (int)_canvasHeight);

            SwapChain.ResizeBuffers(
                2,
                physicalWidth,
                physicalHeight,
                Format.B8G8R8A8_UNorm,
                SwapChainFlags.None
            );

            // 重新设置变换矩阵
            SetTransformMatrix();

            CreateRenderTargetView();
            CreateD2DRenderTarget();

            Logger.Log(
                $"SwapChain resized: Logical={width}x{height}, Physical={physicalWidth}x{physicalHeight}, Scale={_scale}"
            );
        }
    }

    /// <summary>
    /// 处理 CompositionScaleChanged 事件.
    /// </summary>
    private void OnCompositionScaleChanged(SwapChainPanel sender, object args)
    {
        var newScale = (sender.CompositionScaleX + sender.CompositionScaleY) / 2f;
        if (Math.Abs(newScale - _scale) < 0.001f)
        {
            return;
        }

        _scale = newScale;
        _dpi = 96.0f * _scale;

        // 更新画布大小和逻辑大小
        _canvasWidth = (float)_swapChainPanel.ActualWidth * _scale;
        _canvasHeight = (float)_swapChainPanel.ActualHeight * _scale;
        _logicalWidth = _swapChainPanel.ActualWidth;
        _logicalHeight = _swapChainPanel.ActualHeight;

        // 更新交换链的变换矩阵
        SetTransformMatrix();

        Logger.Log(
            $"CompositionScale changed: {_scale}, DPI={_dpi}, Canvas={_canvasWidth}x{_canvasHeight}"
        );

        // 触发缩放变化事件
        ScaleChanged?.Invoke(this, _scale);
    }

    /// <summary>
    /// 处理 SizeChanged 事件.
    /// </summary>
    private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        var newWidth = (float)e.NewSize.Width * _scale;
        var newHeight = (float)e.NewSize.Height * _scale;
        if (Math.Abs(newWidth - _canvasWidth) < 0.1f && Math.Abs(newHeight - _canvasHeight) < 0.1f)
        {
            return;
        }

        _canvasWidth = newWidth;
        _canvasHeight = newHeight;
        _logicalWidth = e.NewSize.Width;
        _logicalHeight = e.NewSize.Height;

        _swapChainPanel.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                ResizeSwapChain((float)_logicalWidth, (float)_logicalHeight);

                Logger.Log(
                    $"Size changed: Logical={_logicalWidth}x{_logicalHeight}, Canvas={_canvasWidth}x{_canvasHeight}"
                );

                // 触发尺寸变化事件
                SizeChanged?.Invoke(this, (_logicalWidth, _logicalHeight));
            }
        );

        // 启动延迟 resize 机制
        ScheduleDelayedResize();
    }

    /// <summary>
    /// 调度延迟 resize 操作（2秒后执行，多次触发会重置计时器）.
    /// </summary>
    private void ScheduleDelayedResize()
    {
        lock (_resizeLock)
        {
            // 取消之前的延迟任务
            _delayedResizeCts?.Cancel();
            _delayedResizeCts?.Dispose();
            _delayedResizeCts = new CancellationTokenSource();

            CancellationTokenSource cts = _delayedResizeCts;
            var width = _logicalWidth;
            var height = _logicalHeight;

            // 异步执行延迟 resize
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(2000, cts.Token).ConfigureAwait(false);

                        // 2秒后在 UI 线程上执行 resize
                        _swapChainPanel.DispatcherQueue.TryEnqueue(
                            DispatcherQueuePriority.Low,
                            () =>
                            {
                                if (!_isDisposed && !cts.IsCancellationRequested)
                                {
                                    // 检查并更新 RasterizationScale
                                    var rasterizationScale = _swapChainPanel
                                        .XamlRoot
                                        ?.RasterizationScale;
                                    if (
                                        rasterizationScale.HasValue
                                        && Math.Abs(rasterizationScale.Value - _scale) > 0.001f
                                    )
                                    {
                                        Logger.Log(
                                            $"RasterizationScale mismatch detected: Current={_scale}, XamlRoot={rasterizationScale.Value}"
                                        );
                                        UpdateScale((float)rasterizationScale.Value);
                                    }

                                    ResizeSwapChain((float)width, (float)height);
                                    Logger.Log(
                                        $"Delayed resize executed: Logical={width}x{height}, Scale={_scale}"
                                    );
                                }
                            }
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消，这是正常情况
                        Logger.Log("Delayed resize cancelled (new size change detected)");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Delayed resize failed: {ex.Message}");
                    }
                },
                cts.Token
            );
        }
    }

    /// <summary>
    /// 缩放变化事件.
    /// </summary>
    public event EventHandler<float> ScaleChanged;

    /// <summary>
    /// 尺寸变化事件.
    /// </summary>
    public event EventHandler<(double Width, double Height)> SizeChanged;

    /// <summary>
    /// 清空所有缓存.
    /// </summary>
    public void ClearAllCaches()
    {
        lock (CacheLock)
        {
            // 清空文本格式缓存
            foreach (IDWriteTextFormat format in TextFormatCache.Values)
            {
                format?.Dispose();
            }
            TextFormatCache.Clear();

            // 清空画刷缓存
            foreach (ID2D1SolidColorBrush brush in BrushCache.Values)
            {
                brush?.Dispose();
            }
            BrushCache.Clear();

            // 清空对象池
            ObjectPool?.Dispose();
            ObjectPool = new ObjectPoolManager();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // 取消并清理延迟 resize 任务
        lock (_resizeLock)
        {
            _delayedResizeCts?.Cancel();
            _delayedResizeCts?.Dispose();
            _delayedResizeCts = null;
        }

        // 取消订阅事件
        if (_swapChainPanel is not null)
        {
            _swapChainPanel.CompositionScaleChanged -= OnCompositionScaleChanged;
            _swapChainPanel.SizeChanged -= OnSizeChanged;
        }

        ClearAllCaches();

        DWriteFactory?.Dispose();
        D2DTargetBitmap?.Dispose();
        D2DContext?.Dispose();
        D2DDevice?.Dispose();
        D2DFactory?.Dispose();
        RenderTargetView?.Dispose();
        SwapChain?.Dispose();
        D3DContext?.Dispose();
        D3DDevice?.Dispose();

        Logger.Log("DirectX resources disposed");
    }
}
