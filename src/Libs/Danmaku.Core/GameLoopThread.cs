using System.Collections.Concurrent;
using Microsoft.UI.Dispatching;

namespace Danmaku.Core;

/// <summary>
/// 游戏循环线程接口.
/// </summary>
internal interface IGameLoopThread : IDisposable
{
    /// <summary>
    /// 启动调度器.
    /// </summary>
    void StartDispatcher();

    /// <summary>
    /// 停止调度器.
    /// </summary>
    void StopDispatcher();

    /// <summary>
    /// 在游戏循环线程上异步运行处理器.
    /// </summary>
    Task RunAsync(Action action);

    /// <summary>
    /// 检查当前线程是否有游戏循环线程访问权限.
    /// </summary>
    bool HasThreadAccess();
}

/// <summary>
/// 游戏循环线程客户端接口.
/// </summary>
internal interface IGameLoopClient
{
    /// <summary>
    /// 游戏循环启动时调用.
    /// </summary>
    void OnGameLoopStarting();

    /// <summary>
    /// 游戏循环停止时调用.
    /// </summary>
    void OnGameLoopStopped();
}

/// <summary>
/// 游戏循环线程实现 - 为 DirectX 渲染提供专用的线程和调度器.
/// </summary>
internal sealed class GameLoopThread : IGameLoopThread
{
    private readonly IGameLoopClient _client;
    private readonly object _lock = new();
    private readonly ManualResetEventSlim _startedEvent = new(false);
    private readonly ManualResetEventSlim _shutdownEvent = new(false);
    private readonly ManualResetEventSlim _dispatcherStartedEvent = new(false);
    private readonly ManualResetEventSlim _dispatcherStoppedEvent = new(false);
    private readonly ConcurrentQueue<PendingAction> _pendingActions = new();

    private DispatcherQueueController? _dispatcherQueueController;
    private DispatcherQueue? _dispatcherQueue;
    private readonly Thread? _thread;
    private volatile bool _startDispatcher;
    private volatile bool _dispatcherStarted;
    private volatile bool _shutdownRequested;
    private volatile bool _isDisposed;

    /// <summary>
    /// 待处理的操作.
    /// </summary>
    private sealed class PendingAction
    {
        public Action Action { get; set; } = null!;
        public TaskCompletionSource<bool> CompletionSource { get; set; } = null!;
    }

    public GameLoopThread(object swapChainPanel, IGameLoopClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));

        // 启动专用线程
        _thread = new Thread(ThreadMain)
        {
            Name = "DanmakuGameLoopThread",
            IsBackground = true,
            Priority = ThreadPriority.Normal,
        };
        _thread.Start(swapChainPanel);

        // 等待线程启动完成
        _startedEvent.Wait();
    }

    public void StartDispatcher()
    {
        lock (_lock)
        {
            if (_isDisposed || _shutdownRequested)
            {
                return;
            }

            _startDispatcher = true;
            _dispatcherStoppedEvent.Reset();

            // 等待调度器启动
            Monitor.PulseAll(_lock);
        }

        // 等待调度器完全启动
        _dispatcherStartedEvent.Wait();
    }

    public void StopDispatcher()
    {
        lock (_lock)
        {
            if (_isDisposed || !_startDispatcher)
            {
                return;
            }

            _startDispatcher = false;

            if (_dispatcherStarted && _dispatcherQueue != null)
            {
                // 退出事件循环
                // _dispatcherQueue.TryEnqueue(() => _dispatcherQueueController?.ShutdownQueueAsync());
            }

            Monitor.PulseAll(_lock);
        }

        // 等待调度器完全停止
        _dispatcherStoppedEvent.Wait();
    }

    public Task RunAsync(Action action)
    {
        if (_isDisposed)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource<bool> tcs = new();
        PendingAction pendingAction = new() { Action = action, CompletionSource = tcs };

        lock (_lock)
        {
            if (_startDispatcher && _dispatcherQueue != null)
            {
                // 调度器模式：通过 DispatcherQueue 排队
                _dispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Normal,
                    () =>
                    {
                        try
                        {
                            action?.Invoke();
                            tcs.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }
                );
            }
            else
            {
                // 手动模式：添加到待处理队列
                _pendingActions.Enqueue(pendingAction);
                Monitor.PulseAll(_lock);
            }
        }

        return tcs.Task;
    }

    public bool HasThreadAccess()
    {
        if (_isDisposed || _dispatcherQueue == null)
        {
            return false;
        }

        return _dispatcherQueue.HasThreadAccess;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            _isDisposed = true;
            _shutdownRequested = true;
            Monitor.PulseAll(_lock);
        }

        // 等待线程完全关闭
        _shutdownEvent.Wait(TimeSpan.FromSeconds(5));

        // 清理资源
        _startedEvent?.Dispose();
        _shutdownEvent?.Dispose();
        _dispatcherStartedEvent?.Dispose();
        _dispatcherStoppedEvent?.Dispose();
    }

    private void ThreadMain(object? state)
    {
        try
        {
            // 在这个线程上创建 DispatcherQueue
            _dispatcherQueueController = DispatcherQueueController.CreateOnDedicatedThread();
            _dispatcherQueue = _dispatcherQueueController.DispatcherQueue;

            // 通知客户端线程已启动
            _client.OnGameLoopStarting();

            // 标记线程已启动
            _startedEvent.Set();

            // 主循环
            while (!_shutdownRequested)
            {
                lock (_lock)
                {
                    // 等待工作：关闭请求、待处理操作或启动调度器
                    while (!_shutdownRequested && _pendingActions.IsEmpty && !_startDispatcher)
                    {
                        Monitor.Wait(_lock);
                    }

                    if (_shutdownRequested)
                    {
                        break;
                    }

                    // 处理待处理的操作
                    if (!_pendingActions.IsEmpty)
                    {
                        ProcessPendingActions();
                    }

                    // 如果请求启动调度器
                    if (_startDispatcher && !_dispatcherStarted)
                    {
                        _dispatcherStarted = true;
                        _dispatcherStartedEvent.Set();
                    }
                }

                // 在锁外运行调度器循环
                if (_dispatcherStarted)
                {
                    try
                    {
                        // 简单的轮询循环，让 DispatcherQueue 处理消息
                        // 注意：WinUI 3 的 DispatcherQueue 工作方式与 UWP 不同
                        while (_startDispatcher && !_shutdownRequested)
                        {
                            // 让调度器有机会处理排队的工作
                            Thread.Sleep(1);
                        }
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _dispatcherStarted = false;
                            _dispatcherStoppedEvent.Set();
                        }
                    }
                }
            }

            // 取消所有待处理的操作
            CancelPendingActions();

            // 关闭调度器
            if (_dispatcherQueueController != null)
            {
                try
                {
                    _dispatcherQueueController
                        .ShutdownQueueAsync()
                        .AsTask()
                        .Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    // 忽略异常
                }
            }

            // 通知客户端线程已停止
            _client.OnGameLoopStopped();
        }
        catch (Exception ex)
        {
            Logger.Log($"GameLoopThread error: {ex.Message}");
        }
        finally
        {
            // 标记线程已完全关闭
            _shutdownEvent.Set();
        }
    }

    private void ProcessPendingActions()
    {
        while (_pendingActions.TryDequeue(out PendingAction? pendingAction))
        {
            try
            {
                pendingAction.Action?.Invoke();
                pendingAction.CompletionSource?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                pendingAction.CompletionSource?.TrySetException(ex);
            }
        }
    }

    private void CancelPendingActions()
    {
        while (_pendingActions.TryDequeue(out PendingAction? pendingAction))
        {
            pendingAction.CompletionSource?.TrySetCanceled();
        }
    }
}
