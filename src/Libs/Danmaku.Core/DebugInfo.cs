namespace Danmaku.Core;

/// <summary>
/// 调试信息数据结构
/// </summary>
public struct DebugInfo
{
    /// <summary>
    /// 缓冲区宽度
    /// </summary>
    public int BufferWidth { get; set; }

    /// <summary>
    /// 缓冲区高度
    /// </summary>
    public int BufferHeight { get; set; }

    /// <summary>
    /// 逻辑宽度
    /// </summary>
    public double LogicalWidth { get; set; }

    /// <summary>
    /// 逻辑高度
    /// </summary>
    public double LogicalHeight { get; set; }

    /// <summary>
    /// X轴缩放比例
    /// </summary>
    public double ScaleX { get; set; }

    /// <summary>
    /// Y轴缩放比例
    /// </summary>
    public double ScaleY { get; set; }

    /// <summary>
    /// 弹幕数量
    /// </summary>
    public int DanmakuCount { get; set; }

    /// <summary>
    /// 运行时间
    /// </summary>
    public TimeSpan RunningTime { get; set; }

    /// <summary>
    /// 帧率
    /// </summary>
    public double FrameRate { get; set; }
}
