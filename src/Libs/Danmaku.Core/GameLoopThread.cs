namespace Danmaku.Core;

/// <summary>
/// 独立的高性能游戏渲染循环线程.
/// 完全脱离 WinUI 3 UI 线程与消息循环，在独立工作线程中以硬件垂直同步直接驱动 DirectX 渲染.
/// </summary>
internal sealed class GameLoopThread : IDisposable
{
    private readonly Action _renderFrameAction;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _startedEvent = new(false);
    private readonly ManualResetEventSlim _shutdownEvent = new(false);
    private readonly AutoResetEvent _wakeEvent = new(false);

    private volatile bool _shutdownRequested;
    private volatile bool _isPaused;
    private volatile bool _isDisposed;

    /// <summary>
    /// 是否暂停渲染循环.
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            _isPaused = value;
            if (!value)
            {
                _wakeEvent.Set();
            }
        }
    }

    public GameLoopThread(Action renderFrameAction)
    {
        _renderFrameAction = renderFrameAction ?? throw new ArgumentNullException(nameof(renderFrameAction));

        _thread = new Thread(ThreadMain)
        {
            Name = "DanmakuIndependentRenderThread",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();

        _startedEvent.Wait(TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// 唤醒渲染线程（例如新增弹幕或尺寸发生变化时）.
    /// </summary>
    public void Wake()
    {
        if (!_isDisposed && !_shutdownRequested)
        {
            _wakeEvent.Set();
        }
    }

    /// <summary>
    /// 在无弹幕时让渲染线程进入低功耗等待状态.
    /// </summary>
    /// <param name="timeoutMs">超时毫秒数.</param>
    public void WaitForWork(int timeoutMs)
    {
        if (!_isDisposed && !_shutdownRequested)
        {
            _wakeEvent.WaitOne(timeoutMs);
        }
    }

    private void ThreadMain()
    {
        try
        {
            Logger.Log("Independent GameLoopThread started");
            _startedEvent.Set();

            while (!_shutdownRequested)
            {
                if (_isPaused)
                {
                    _wakeEvent.WaitOne(50);
                    continue;
                }

                _renderFrameAction();
            }

            Logger.Log("Independent GameLoopThread stopped");
        }
        catch (Exception ex)
        {
            Logger.Log($"GameLoopThread error: {ex.Message}");
        }
        finally
        {
            _shutdownEvent.Set();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _shutdownRequested = true;
        _wakeEvent.Set();

        // 等待线程完全关闭
        _shutdownEvent.Wait(TimeSpan.FromSeconds(2));

        _startedEvent.Dispose();
        _shutdownEvent.Dispose();
        _wakeEvent.Dispose();
    }
}
