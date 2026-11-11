using System.Diagnostics;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WinUIKernel.Share.Base;

/// <summary>
/// 图片扩展基类.
/// </summary>
public abstract partial class ImageExBase
{
    // 用于去重,避免重复请求同一个URL
    private static readonly Dictionary<string, Task<byte[]?>> _pendingRequests = [];
    private static readonly Lock _pendingRequestsLock = new();

    // 调试统计信息
    private static int _totalRequestsStarted;
    private static int _totalRequestsCompleted;
    private static int _totalRequestsFailed;
    private static int _totalRequestsCancelled;
    private static int _totalRequestsFromCache;
    private static int _totalRequestsDeduplicated;
    private static int _totalLocalDecodes;

    /// <summary>
    /// 获取当前请求统计信息(仅用于调试).
    /// </summary>
    public static (
        int ActiveRequests,
        int QueuedRequests,
        int PendingUrls,
        int TotalStarted,
        int TotalCompleted,
        int TotalFailed,
        int TotalCancelled,
        int FromCache,
        int Deduplicated,
        int LocalDecodes,
        int ActiveLocalDecodes
    ) GetRequestStats()
    {
        int pendingUrls;
        lock (_pendingRequestsLock)
        {
            pendingUrls = _pendingRequests.Count;
        }

        return (
            0,
            0,
            pendingUrls,
            _totalRequestsStarted,
            _totalRequestsCompleted,
            _totalRequestsFailed,
            _totalRequestsCancelled,
            _totalRequestsFromCache,
            _totalRequestsDeduplicated,
            _totalLocalDecodes,
            0
        );
    }

    /// <summary>
    /// 重置请求统计信息.
    /// </summary>
    public static void ResetRequestStats()
    {
        _totalRequestsStarted = 0;
        _totalRequestsCompleted = 0;
        _totalRequestsFailed = 0;
        _totalRequestsCancelled = 0;
        _totalRequestsFromCache = 0;
        _totalRequestsDeduplicated = 0;
        _totalLocalDecodes = 0;
    }

    /// <summary>
    /// 检查连接池是否处于高负载状态.
    /// </summary>
    /// <returns>
    /// 返回一个元组，包含：
    /// - IsUnderPressure: 连接池是否处于压力状态（使用率 > 80%）
    /// - UsagePercentage: 当前使用率（0-100）
    /// - Message: 描述信息
    /// </returns>
    public static (
        bool IsUnderPressure,
        double UsagePercentage,
        string Message
    ) CheckConnectionPoolPressure()
    {
        (
            int ActiveRequests,
            int QueuedRequests,
            int PendingUrls,
            int TotalStarted,
            int TotalCompleted,
            int TotalFailed,
            int TotalCancelled,
            int FromCache,
            int Deduplicated,
            int LocalDecodes,
            int ActiveLocalDecodes
        ) stats = GetRequestStats();
        int pendingUrls = stats.PendingUrls;

        string message = pendingUrls switch
        {
            >= 100 => $"⚠️ 大量待处理请求！({pendingUrls} 个 URL)",
            >= 50 => $"⚠️ 较多待处理请求 ({pendingUrls} 个 URL)",
            >= 10 => $"ℹ️ 中等待处理请求 ({pendingUrls} 个 URL)",
            _ => $"✅ 请求正常 ({pendingUrls} 个 URL)",
        };

        bool isUnderPressure = pendingUrls >= 50;
        int usagePercentage = Math.Min(100, pendingUrls);

        return (isUnderPressure, usagePercentage, message);
    }

    /// <summary>
    /// 打印详细的连接池诊断信息（仅 Debug 模式）.
    /// </summary>
    public static void PrintConnectionPoolDiagnostics()
    {
#if DEBUG
        (
            int ActiveRequests,
            int QueuedRequests,
            int PendingUrls,
            int TotalStarted,
            int TotalCompleted,
            int TotalFailed,
            int TotalCancelled,
            int FromCache,
            int Deduplicated,
            int LocalDecodes,
            int ActiveLocalDecodes
        ) stats = GetRequestStats();
        (bool IsUnderPressure, double UsagePercentage, string Message) pressure =
            CheckConnectionPoolPressure();

        Debug.WriteLine("========== ImageEx 连接池诊断 ==========");
        Debug.WriteLine($"连接池状态: {pressure.Message}");
        Debug.WriteLine($"去重缓存: {stats.PendingUrls} 个 URL");
        Debug.WriteLine("--- 累计统计 ---");
        Debug.WriteLine($"总启动: {stats.TotalStarted}");
        Debug.WriteLine($"总完成: {stats.TotalCompleted}");
        Debug.WriteLine($"总失败: {stats.TotalFailed}");
        Debug.WriteLine($"总取消: {stats.TotalCancelled}");
        Debug.WriteLine($"缓存命中: {stats.FromCache}");
        Debug.WriteLine($"请求去重: {stats.Deduplicated}");
        Debug.WriteLine($"本地解码: {stats.LocalDecodes}");

        if (pressure.IsUnderPressure)
        {
            Debug.WriteLine("⚠️ 建议：考虑调用 CancelAllLoading() 释放资源");
        }

        Debug.WriteLine("==========================================");
#endif
    }

    /// <summary>
    /// 清空所有正在进行的网络请求.
    /// </summary>
    /// <remarks>
    /// 此方法会：
    /// 1. 清空所有正在排队和进行中的网络请求
    /// 2. 重置请求统计信息
    ///
    /// 适用场景：
    /// - 用户切换配置（账号、图片源等）
    /// - 应用进入后台需要释放资源
    /// - 清理卡住的请求
    ///
    /// 注意：此操作会导致所有正在加载的图片失败，请谨慎使用。
    /// </remarks>
    public static void ClearAllRequests()
    {
        // 清空去重字典中的所有待处理请求
        lock (_pendingRequestsLock)
        {
            _pendingRequests.Clear();
        }

        // 重置统计信息
        ResetRequestStats();

        Debug.WriteLine("[ImageEx] 已清空所有请求");
    }

    [Conditional("DEBUG")]
    private static void LogRequestStats(string context)
    {
        if (!EnableDebugLog)
        {
            return;
        }

        (
            int ActiveRequests,
            int QueuedRequests,
            int PendingUrls,
            int TotalStarted,
            int TotalCompleted,
            int TotalFailed,
            int TotalCancelled,
            int FromCache,
            int Deduplicated,
            int LocalDecodes,
            int ActiveLocalDecodes
        ) stats = GetRequestStats();
        Debug.WriteLine($"[ImageEx] {context}");
        Debug.WriteLine($"  去重URL数: {stats.PendingUrls}");
        Debug.WriteLine(
            $"  总启动: {stats.TotalStarted}, 完成: {stats.TotalCompleted}, 失败: {stats.TotalFailed}, 取消: {stats.TotalCancelled}"
        );
        Debug.WriteLine(
            $"  缓存命中: {stats.FromCache}, 去重: {stats.Deduplicated}, 本地解码: {stats.LocalDecodes}"
        );
    }

    private static async void OnSourceChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ImageExBase? instance = d as ImageExBase;
        Uri? uri = e.NewValue as Uri;
        if (uri is null && instance.HolderImage is Uri holderUri)
        {
            uri = holderUri;
        }

        if (uri is not null && instance.IsLoaded)
        {
            await instance.RedrawAsync();
        }
    }

    /// <summary>
    /// 重绘.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected async Task RedrawAsync()
    {
        _lastUri = null;
        await TryLoadImageAsync(Source);
    }

    private async Task TryLoadImageAsync(Uri uri)
    {
        if (uri is null && HolderImage is not null)
        {
            uri = HolderImage;
        }

        if (_backgroundBrush is null || _lastUri == uri || !IsLoaded)
        {
            return;
        }

        _lastUri = uri;
        _backgroundBrush.ImageSource = null;
        (long requestId, CancellationToken cancellationToken) = BeginNewRequest();
        await LoadImageInternalAsync(requestId, cancellationToken);
    }

    private async Task LoadImageInternalAsync(long requestId, CancellationToken cancellationToken)
    {
        if (_lastUri is null)
        {
            _lastUri = HolderImage;
        }

        if (
            !IsCurrentRequest(requestId)
            || !IsLoaded
            || _lastUri is null
            || cancellationToken.IsCancellationRequested
            || !TryInitialize()
        )
        {
            if (IsCurrentRequest(requestId))
            {
                IsImageLoading = false;
            }
            return;
        }

        IsImageLoading = true;

        // ⚠️ 在进入后台线程之前，先捕获所有依赖属性和可能不安全的实例成员的值
        // 因为依赖属性只能在 UI 线程访问，后台线程访问会触发 COM 线程错误
        bool enableDiskCache = EnableDiskCache; // 依赖属性
        Uri currentUri = _lastUri; // URI 引用 - 保存当前要加载的URI用于后续验证
        Dictionary<string, string> headers = GetHeaders(); // 虚方法可能访问依赖属性
        string cacheSubFolder = GetCacheSubFolder(); // 虚方法可能访问依赖属性

        CanvasBitmap? bitmap = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 根据配置决定是否在后台线程执行图片获取和解码
            if (EnableBackgroundDecoding)
            {
                // 在后台线程执行图片获取和解码,避免阻塞 UI 线程
                // 将捕获的依赖属性值传递给后台任务
                bitmap = await Task.Run(
                    async () =>
                        await FetchImageAsync(
                            currentUri,
                            enableDiskCache,
                            headers,
                            cacheSubFolder,
                            requestId,
                            cancellationToken
                        ),
                    cancellationToken
                );
            }
            else
            {
                // 在 UI 线程执行(原有行为)
                bitmap = await FetchImageAsync(
                    currentUri,
                    enableDiskCache,
                    headers,
                    cacheSubFolder,
                    requestId,
                    cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            if (
                HolderImage is not null
                && !cancellationToken.IsCancellationRequested
                && IsCurrentRequest(requestId)
            )
            {
                await TryLoadImageAsync(HolderImage);
            }

            ImageFailed?.Invoke(this, EventArgs.Empty);
        }

        if (bitmap is null)
        {
            if (IsCurrentRequest(requestId))
            {
                IsImageLoading = false;
            }
            return;
        }

        try
        {
            if (CanvasImageSource is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (EnableBackgroundDecoding)
                {
                    // 解码在后台线程完成后，将绘制操作调度回 UI 线程
                    // 这样派生类可以安全地访问依赖属性（如 DecodeWidth/DecodeHeight）
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            // ⚠️ 关键修复：在绘制前检查URI是否仍然匹配
                            // 在虚拟化列表中，控件可能已被复用显示其他图片
                            // 如果_lastUri已经改变，说明这是一个过期的请求，应该丢弃
                            if (
                                _lastUri != currentUri
                                || !IsCurrentRequest(requestId)
                                || cancellationToken.IsCancellationRequested
                            )
                            {
                                Debug.WriteLine(
                                    $"[ImageEx] 丢弃过期图片: 期望 {currentUri}, 当前 {_lastUri}"
                                );
                                return;
                            }

                            DrawImage(bitmap);
                            _backgroundBrush.ImageSource = CanvasImageSource;
                            _wasCancelledGlobally = false; // 清除全局取消标记
                            ImageLoaded?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            ImageFailed?.Invoke(this, EventArgs.Empty);
                        }
                        finally
                        {
                            // 绘制完成后才释放 bitmap
                            bitmap?.Dispose();
                        }
                    });
                }
                else
                {
                    // 在 UI 线程执行绘制(原有行为)
                    // 同样需要检查URI匹配
                    if (
                        _lastUri != currentUri
                        || !IsCurrentRequest(requestId)
                        || cancellationToken.IsCancellationRequested
                    )
                    {
                        Debug.WriteLine(
                            $"[ImageEx] 丢弃过期图片: 期望 {currentUri}, 当前 {_lastUri}"
                        );
                        bitmap.Dispose();
                        return;
                    }

                    DrawImage(bitmap);
                    _backgroundBrush.ImageSource = CanvasImageSource;
                    _wasCancelledGlobally = false; // 清除全局取消标记
                    ImageLoaded?.Invoke(this, EventArgs.Empty);

                    // 立即释放 bitmap
                    bitmap.Dispose();
                }
            }
            else
            {
                // 如果 CanvasImageSource 为 null，也要释放 bitmap
                bitmap.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore the cancellation exception, as it is expected when the control is unloaded
            // or when a new request is made before the previous one has completed.
#if DEBUG
            string canceledUrl = currentUri?.ToString() ?? _lastUri?.ToString() ?? "unknown";
            Debug.WriteLine(
                $"[ImageEx] 图片加载已取消: {canceledUrl[Math.Max(0, canceledUrl.Length - 50)..]}"
            );
#endif
            bitmap?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            if (HolderImage is not null && IsCurrentRequest(requestId))
            {
                await TryLoadImageAsync(HolderImage);
            }

            if (IsCurrentRequest(requestId))
            {
                ImageFailed?.Invoke(this, EventArgs.Empty);
            }
            bitmap?.Dispose();
        }
        finally
        {
            if (IsCurrentRequest(requestId))
            {
                IsImageLoading = false;
            }
            // 不在这里 Dispose bitmap，因为它可能在异步回调中使用
        }
    }

    private bool TryInitialize()
    {
        if (_compositionTargetSequenceNumber == CompositionTargetMonitor.SequenceNumber)
        {
            return true;
        }

        try
        {
            CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
            sharedDevice.DeviceLost -= OnSharedDeviceLost;
            sharedDevice.DeviceLost += OnSharedDeviceLost;
            CreateCanvasImageSource(sharedDevice);
            _compositionTargetSequenceNumber = CompositionTargetMonitor.SequenceNumber;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }

    private async void OnSharedDeviceLost(CanvasDevice sender, object args)
    {
        if (
            Interlocked.CompareExchange(ref LostDevicesMap.GetOrCreateValue(sender).Value, 1, 0)
            == 0
        )
        {
            Debug.WriteLine(
                $"Shared Win2D CanvasDevice instance was lost (HRESULT: 0x{sender.GetDeviceLostReason():X8})."
            );
        }

        sender.DeviceLost -= OnSharedDeviceLost;

        DateTime currentDeviceLostTime = DateTime.Now;
        DateTime lastLostTime = _lastDeviceLostTime;

        _lastDeviceLostTime = currentDeviceLostTime;
        _compositionTargetSequenceNumber = CompositionTargetMonitor.UninitializedValue;

        if ((currentDeviceLostTime - lastLostTime) > TimeSpan.FromSeconds(3))
        {
            await RedrawAsync();
        }
    }

    private void CreateCanvasImageSource(CanvasDevice device)
    {
        if (CanvasImageSource is Microsoft.Graphics.Canvas.UI.Xaml.CanvasImageSource imageSource)
        {
            _backgroundBrush.ImageSource = null;
            imageSource.Recreate(device);
        }
        else
        {
            CanvasImageSource = new Microsoft.Graphics.Canvas.UI.Xaml.CanvasImageSource(
                resourceCreator: device,
                width: (float)DecodeWidth,
                height: (float)DecodeHeight,
                dpi: 96,
                CanvasAlphaMode.Ignore
            );
        }
    }

    private HttpClient GetHttpClient()
    {
        // 不能标记为 static,因为调用了虚方法 GetCustomHttpClient()
        return GetCustomHttpClient() ?? _httpClient;
    }

    /// <summary>
    /// 获取或创建网络请求任务,避免重复请求同一个URL.
    /// </summary>
    private static async Task<byte[]?> GetOrCreateRequestAsync(
        string url,
        Func<Task<byte[]?>> requestFactory,
        CancellationToken cancellationToken
    )
    {
        Task<byte[]?> task;
        bool isDuplicate = false;

        lock (_pendingRequestsLock)
        {
            if (_pendingRequests.TryGetValue(url, out Task<byte[]?>? existingTask))
            {
                // 如果已经有相同URL的请求在进行,返回该任务(在lock外await)
                task = existingTask;
                isDuplicate = true;
                Interlocked.Increment(ref _totalRequestsDeduplicated);
            }
            else
            {
                // 创建新的请求任务包装器,不使用 Task.Run 避免线程切换
                TaskCompletionSource<byte[]?> tcs = new();
                task = tcs.Task;
                _pendingRequests[url] = task;

                // 在当前上下文中执行请求
                _ = ExecuteRequestAsync(url, requestFactory, tcs, cancellationToken);
            }
        }

        LogRequestStats(
            isDuplicate
                ? $"请求去重: {url[Math.Max(0, url.Length - 50)..]}"
                : $"新建请求: {url[Math.Max(0, url.Length - 50)..]}"
        );
        return await task;
    }

    /// <summary>
    /// 执行实际的请求操作.
    /// </summary>
    private static async Task ExecuteRequestAsync(
        string url,
        Func<Task<byte[]?>> requestFactory,
        TaskCompletionSource<byte[]?> tcs,
        CancellationToken cancellationToken
    )
    {
        Interlocked.Increment(ref _totalRequestsStarted);
        try
        {
            byte[]? result = await requestFactory();
            Interlocked.Increment(ref _totalRequestsCompleted);
            tcs.TrySetResult(result);
        }
        catch (OperationCanceledException)
        {
            // 单独统计取消的请求
            Interlocked.Increment(ref _totalRequestsCancelled);
#if DEBUG
            Debug.WriteLine($"[ImageEx] HTTP 请求已取消: {url[Math.Max(0, url.Length - 50)..]}");
#endif
            tcs.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalRequestsFailed);
            tcs.TrySetException(ex);
        }
        finally
        {
            // 请求完成后从字典中移除
            lock (_pendingRequestsLock)
            {
                _pendingRequests.Remove(url);
            }
            LogRequestStats($"请求完成: {url[Math.Max(0, url.Length - 50)..]}");
        }
    }

    private async Task<CanvasBitmap?> FetchImageAsync(
        Uri uri,
        bool enableDiskCache,
        Dictionary<string, string> headers,
        string cacheSubFolder,
        long requestId,
        CancellationToken cancellationToken
    )
    {
        if (uri is null)
        {
            return null;
        }

        Uri requestUri = uri;
        CanvasBitmap? canvasBitmap = null;

        if (IsRequestObsolete())
        {
            return null;
        }

        // 区分本地链接和网络链接.
        if (uri!.IsFile)
        {
            await LoadLocalFile();
        }
        else if (enableDiskCache)
        {
            string? cacheFile = await GetCacheFilePathAsync(uri.ToString(), cacheSubFolder);
            if (cacheFile is not null)
            {
                Interlocked.Increment(ref _totalRequestsFromCache);
                LogRequestStats(
                    $"缓存命中: {uri.ToString()[Math.Max(0, uri.ToString().Length - 50)..]}"
                );
                if (IsRequestObsolete())
                {
                    return null;
                }

                StorageFile file = await StorageFile.GetFileFromPathAsync(cacheFile);
                using IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();

                // 使用独立设备解码以避免竞争共享设备
                canvasBitmap = await LoadBitmapAsync(stream, cancellationToken);
            }
            else
            {
                // 使用去重机制避免重复请求
                string url = uri.ToString();
                byte[]? content = await GetOrCreateRequestAsync(
                    url,
                    async () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        CheckImageHeaders(uri, headers);
                        if (uri.IsFile)
                        {
                            // 这种情况应该不会发生,但保持原有逻辑
                            return null;
                        }

                        HttpResponseMessage response = await GetHttpClient()
                            .GetAsync(uri, cancellationToken)
                            .ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            byte[] data = await response.Content.ReadAsByteArrayAsync(
                                cancellationToken
                            );
                            if (data.Length > 0)
                            {
                                // 🚀 异步写入缓存，不阻塞主流程（Fire-and-Forget）
                                if (
                                    !cancellationToken.IsCancellationRequested
                                    && IsCurrentRequest(requestId)
                                )
                                {
                                    _ = Task.Run(
                                        async () =>
                                        {
                                            if (
                                                cancellationToken.IsCancellationRequested
                                                || !IsCurrentRequest(requestId)
                                            )
                                            {
                                                return;
                                            }

                                            try
                                            {
                                                await WriteCacheAsync(
                                                    uri.ToString(),
                                                    data,
                                                    cacheSubFolder,
                                                    cancellationToken
                                                );
                                            }
                                            catch (OperationCanceledException)
                                            {
                                                // Ignore cancellation from cache writing
                                            }
                                            catch (Exception ex)
                                            {
                                                // 缓存写入失败不影响图片显示，仅记录日志
                                                Debug.WriteLine(
                                                    $"[ImageEx] 缓存写入失败: {ex.Message}"
                                                );
                                            }
                                        },
                                        CancellationToken.None
                                    );
                                }

                                return data;
                            }
                            else
                            {
                                throw new InvalidOperationException("Image content is empty.");
                            }
                        }
                        else
                        {
                            throw new HttpRequestException(
                                $"Failed to fetch image from {uri}. Status code: {response.StatusCode}"
                            );
                        }
                    },
                    cancellationToken
                );

                if (content is not null && content.Length > 0 && !IsRequestObsolete())
                {
                    await using MemoryStream memoryStream = new(content);
                    using IRandomAccessStream randomStream = memoryStream.AsRandomAccessStream();

                    // 使用独立设备解码以避免竞争共享设备
                    canvasBitmap = await LoadBitmapAsync(randomStream, cancellationToken);
                }
            }
        }
        else
        {
            // 没有启用磁盘缓存的情况,同样使用去重机制
            string url = uri.ToString();
            byte[]? content = await GetOrCreateRequestAsync(
                url,
                async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    CheckImageHeaders(uri, headers);
                    int initialCapacity = 32 * 1024;
                    using ArrayPoolBufferWriter<byte> bufferWriter = new(initialCapacity);
                    using Stream imageStream = await GetHttpClient()
                        .GetStreamAsync(uri, cancellationToken)
                        .ConfigureAwait(false);
                    await using Stream streamForRead = imageStream
                        .AsInputStream()
                        .AsStreamForRead();
                    await using Stream streamForWrite = IBufferWriterExtensions.AsStream(
                        bufferWriter
                    );

                    await streamForRead.CopyToAsync(streamForWrite, cancellationToken);

                    return bufferWriter.WrittenMemory.ToArray();
                },
                cancellationToken
            );

            if (content is not null && content.Length > 0 && !IsRequestObsolete())
            {
                await using MemoryStream memoryStream = new(content);
                using IRandomAccessStream randomStream = memoryStream.AsRandomAccessStream();

                // 使用独立设备解码以避免竞争共享设备
                canvasBitmap = await LoadBitmapAsync(randomStream, cancellationToken);
            }
        }

        if (uri != requestUri || IsRequestObsolete())
        {
            canvasBitmap?.Dispose();
            canvasBitmap = null;
        }

        return canvasBitmap;

        async Task LoadLocalFile()
        {
            try
            {
                if (IsRequestObsolete())
                {
                    return;
                }

                StorageFile file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                using IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();

                // 使用独立设备解码以避免竞争共享设备
                canvasBitmap = await LoadBitmapAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        bool IsRequestObsolete() =>
            cancellationToken.IsCancellationRequested || !IsCurrentRequest(requestId);
    }

    /// <summary>
    /// 使用独立的 CanvasDevice 加载位图，避免竞争共享设备.
    /// </summary>
    /// <remarks>
    /// 在后台解码模式下，每个图片使用独立的临时设备进行解码，
    /// 解码完成后将位图转移到共享设备，这样可以真正实现并行解码.
    /// </remarks>
    private static async Task<CanvasBitmap?> LoadBitmapAsync(
        IRandomAccessStream stream,
        CancellationToken cancellationToken
    )
    {
        if (!EnableBackgroundDecoding)
        {
            // 传统模式：直接使用共享设备（会串行化）
            return await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), stream).AsTask();
        }

        // 并行模式
        Interlocked.Increment(ref _totalLocalDecodes);

        CanvasBitmap? tempBitmap = null;
        CanvasDevice? tempDevice = null;

        try
        {
            // 在后台线程创建临时设备并解码
            // 这样多个图片可以在不同线程同时解码
            (tempDevice, tempBitmap) = await Task.Run(
                async () =>
                {
                    CanvasDevice device = new();
                    CanvasBitmap bitmap = await CanvasBitmap.LoadAsync(device, stream).AsTask();
                    return (device, bitmap);
                },
                cancellationToken
            );

            // 将解码后的位图数据复制到共享设备
            // 这个操作很快，不会造成明显阻塞
            CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();

            // 优化：使用 using 确保像素数据尽快释放
            byte[] pixelBytes = tempBitmap.GetPixelBytes();
            try
            {
                CanvasBitmap finalBitmap = CanvasBitmap.CreateFromBytes(
                    sharedDevice,
                    pixelBytes,
                    (int)tempBitmap.SizeInPixels.Width,
                    (int)tempBitmap.SizeInPixels.Height,
                    tempBitmap.Format
                );

                return finalBitmap;
            }
            finally
            {
                // 像素数据使用完立即释放，减少内存占用
                // pixelBytes 是值类型数组，会被 GC 回收
            }
        }
        finally
        {
            // 清理临时资源
            tempBitmap?.Dispose();
            tempDevice?.Dispose();
        }
    }

    private void CheckImageHeaders(Uri uri, Dictionary<string, string> headers)
    {
        HttpClient client = GetHttpClient();
        if (headers.Count > 0)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                if (client.DefaultRequestHeaders.Contains(header.Key))
                {
                    client.DefaultRequestHeaders.Remove(header.Key);
                }

                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (uri?.Host.Contains("pximg.net") == true)
        {
            client.DefaultRequestHeaders.Referrer = new("https://app-api.pixiv.net/");
        }
        else
        {
            client.DefaultRequestHeaders.Referrer = null;
        }
    }
}
