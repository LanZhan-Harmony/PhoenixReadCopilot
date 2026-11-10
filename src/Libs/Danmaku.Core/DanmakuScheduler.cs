using Danmaku.Core.Enums;
using Danmaku.Core.Models;

namespace Danmaku.Core;

/// <summary>
/// 弹幕调度器 - 负责时间队列管理和弹幕列表调度.
/// </summary>
internal sealed class DanmakuScheduler : IDisposable
{
    private readonly Queue<uint> _updateTimeQueue = new();
    private readonly AutoResetEvent _updateEvent = new(false);
    private List<DanmakuItem> _danmakuList = [];
    private volatile bool _isSeeking;
    private volatile bool _isClosing;
    private volatile int _lastIndex;
    private volatile uint _lastTimeMs;
    private volatile bool _isTopEnabled = true;
    private volatile bool _isBottomEnabled = true;
    private volatile bool _isRollingEnabled = true;
    private readonly Thread _updateThread;

    /// <summary>
    /// 当需要渲染弹幕时触发.
    /// </summary>
    public event Action<uint, DanmakuItem> DanmakuRendered;

    /// <summary>
    /// 当需要暂停时触发.
    /// </summary>
    public event Action PauseRequested;

    /// <summary>
    /// 当需要恢复时触发.
    /// </summary>
    public event Action ResumeRequested;

    /// <summary>
    /// 当需要停止时触发.
    /// </summary>
    public event Action StopRequested;

    public DanmakuScheduler()
    {
        _updateThread = new Thread(UpdaterWorker)
        {
            IsBackground = true,
            Name = "DanmakuScheduler",
        };
        _updateThread.Start();
    }

    /// <summary>
    /// 设置弹幕类型的启用状态.
    /// </summary>
    public void SetDanmakuEnabledType(
        bool? isRollingEnabled,
        bool? isTopEnabled,
        bool? isBottomEnabled
    )
    {
        if (isRollingEnabled.HasValue)
        {
            _isRollingEnabled = isRollingEnabled.Value;
        }

        if (isTopEnabled.HasValue)
        {
            _isTopEnabled = isTopEnabled.Value;
        }

        if (isBottomEnabled.HasValue)
        {
            _isBottomEnabled = isBottomEnabled.Value;
        }
    }

    /// <summary>
    /// 更新当前播放时间.
    /// </summary>
    public void UpdateTime(uint currentMs)
    {
        lock (_updateTimeQueue)
        {
            if (_updateTimeQueue.LastOrDefault() == currentMs)
            {
                // 避免重复更新
                return;
            }

            _updateTimeQueue.Enqueue(currentMs);
        }
        _updateEvent.Set();
    }

    /// <summary>
    /// 暂停调度.
    /// </summary>
    public void Pause()
    {
        if (!_isClosing)
        {
            PauseRequested?.Invoke();
        }
    }

    /// <summary>
    /// 恢复调度.
    /// </summary>
    public void Resume()
    {
        ResumeRequested?.Invoke();
    }

    /// <summary>
    /// 停止调度.
    /// </summary>
    public void Stop()
    {
        Pause();
        StopRequested?.Invoke();
        lock (_updateTimeQueue)
        {
            _updateTimeQueue.Clear();
        }
    }

    /// <summary>
    /// 重新开始（从0开始）.
    /// </summary>
    public void Restart()
    {
        Seek(0);
    }

    /// <summary>
    /// 跳转到指定时间.
    /// </summary>
    public void Seek(uint targetMs)
    {
        _isSeeking = true;
        Stop();
        lock (_danmakuList)
        {
            _lastIndex = 0;
            if (_danmakuList.Count > 0)
            {
                while (
                    _lastIndex < _danmakuList.Count && _danmakuList[_lastIndex].StartMs < targetMs
                )
                {
                    _lastIndex++;
                }
            }
            _lastTimeMs = targetMs;
            Resume();
            _isSeeking = false;
        }
    }

    /// <summary>
    /// 清空弹幕列表.
    /// </summary>
    public void Clear()
    {
        lock (_danmakuList)
        {
            _danmakuList?.Clear();
        }
        StopRequested?.Invoke();
    }

    /// <summary>
    /// 添加实时弹幕.
    /// </summary>
    public void AddRealtimeDanmaku(DanmakuItem item, bool insertToList)
    {
        item.AllowDensityControl = false;
        item.IsRealtime = true;

        // 获取图层 ID
        uint layerId = GetLayerId(item.Mode);
        DanmakuRendered?.Invoke(layerId, item);

        if (insertToList)
        {
            lock (_danmakuList)
            {
                bool added = false;
                for (int i = 0; i < _danmakuList.Count; i++)
                {
                    if (_danmakuList[i].StartMs > item.StartMs)
                    {
                        _danmakuList.Insert(i, item);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    _danmakuList.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// 获取弹幕列表.
    /// </summary>
    public List<DanmakuItem> GetDanmakuList()
    {
        lock (_danmakuList)
        {
            return _danmakuList.ToList();
        }
    }

    /// <summary>
    /// 设置弹幕列表（必须按 StartMs 排序）.
    /// </summary>
    public void SetDanmakuList(List<DanmakuItem> danmakuList)
    {
        Clear();
        lock (_danmakuList)
        {
            _danmakuList = danmakuList ?? [];
        }
    }

    /// <summary>
    /// 添加弹幕列表.
    /// </summary>
    public void AddDanmakuList(List<DanmakuItem> danmakuList)
    {
        if (danmakuList == null || danmakuList.Count == 0)
        {
            return;
        }

        lock (_danmakuList)
        {
            _danmakuList.AddRange(danmakuList);
            _danmakuList.Sort(static (a, b) => a.StartMs.CompareTo(b.StartMs));
        }
    }

    public void Dispose()
    {
        if (!_isClosing)
        {
            _isClosing = true;
            _updateEvent?.Set();
            _updateEvent?.Dispose();
        }
    }

    private void UpdaterWorker()
    {
        try
        {
            while (!_isClosing)
            {
                _updateEvent.WaitOne();

                if (_isClosing)
                {
                    break;
                }

                uint currentTimeMs = 0;
                lock (_updateTimeQueue)
                {
                    if (_updateTimeQueue.Count > 0)
                    {
                        currentTimeMs = _updateTimeQueue.Dequeue();
                    }
                }

                if (currentTimeMs == 0)
                {
                    continue;
                }

                lock (_danmakuList)
                {
                    // 检查是否长时间暂停或重启
                    if (currentTimeMs < _lastTimeMs || currentTimeMs - _lastTimeMs > 5000)
                    {
                        Logger.Log("Reseek after a long time suspension");
                        Seek(currentTimeMs);
                        continue;
                    }

                    _lastTimeMs = currentTimeMs;

                    while (
                        _lastIndex < _danmakuList.Count
                        && _danmakuList[_lastIndex].StartMs <= currentTimeMs
                    )
                    {
                        if (_isClosing || _isSeeking)
                        {
                            break;
                        }

                        DanmakuItem item = _danmakuList[_lastIndex];
                        bool skip = false;

                        if (item.IsRealtime)
                        {
                            item.IsRealtime = false;
                            skip = true;
                        }

                        if (!skip)
                        {
                            var shouldShow = item.Mode switch
                            {
                                DanmakuMode.Bottom => _isBottomEnabled,
                                DanmakuMode.Top => _isTopEnabled,
                                _ => _isRollingEnabled,
                            };

                            if (shouldShow)
                            {
                                uint layerId = GetLayerId(item.Mode);
                                DanmakuRendered?.Invoke(layerId, item);
                            }
                        }

                        _lastIndex++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"DanmakuScheduler error: {ex.Message}");
        }
        finally
        {
            Logger.Log("DanmakuScheduler exited");
        }
    }

    private static uint GetLayerId(DanmakuMode mode)
    {
        return mode switch
        {
            DanmakuMode.Bottom => DanmakuDefaultLayerDef.BottomLayerId,
            DanmakuMode.Top => DanmakuDefaultLayerDef.TopLayerId,
            DanmakuMode.ReverseRolling => DanmakuDefaultLayerDef.ReverseRollingLayerId,
            DanmakuMode.Advanced => DanmakuDefaultLayerDef.AdvancedLayerId,
            _ => DanmakuDefaultLayerDef.RollingLayerId,
        };
    }
}
