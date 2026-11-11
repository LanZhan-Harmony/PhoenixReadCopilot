using Windows.UI;

namespace Danmaku.Core;

/// <summary>
/// 内部弹幕配置状态，存储实际的配置值（非空类型）.
/// </summary>
internal sealed class DanmakuConfigurationState
{
    #region 私有字段

    private volatile bool _autoControlDensity;
    private volatile bool _isTextBold;
    private volatile bool _noOverlapSubtitle;
    private volatile bool _enableParallelUpdate;
    private volatile int _rollingDensity;
    private double _fontSizeScale;
    private volatile float _rollingAreaRatio;
    private volatile float _rollingSpeed;
    private double _textOpacity;
    private double _outlineSize;
    private Color _borderColor;
    private string _fontFamilyName;

    #endregion

    #region 公开属性

    /// <summary>
    /// 是否自动控制密度.
    /// </summary>
    public bool AutoControlDensity
    {
        get => _autoControlDensity;
        set => _autoControlDensity = value;
    }

    /// <summary>
    /// 文本是否粗体.
    /// </summary>
    public bool IsTextBold
    {
        get => _isTextBold;
        set => _isTextBold = value;
    }

    /// <summary>
    /// 字幕是否不重叠.
    /// </summary>
    public bool NoOverlapSubtitle
    {
        get => _noOverlapSubtitle;
        set => _noOverlapSubtitle = value;
    }

    /// <summary>
    /// 是否启用并行更新（多线程优化）.
    /// </summary>
    public bool EnableParallelUpdate
    {
        get => _enableParallelUpdate;
        set => _enableParallelUpdate = value;
    }

    /// <summary>
    /// 滚动弹幕密度限制.
    /// </summary>
    public int RollingDensity
    {
        get => _rollingDensity;
        set => _rollingDensity = value;
    }

    /// <summary>
    /// 弹幕字体大小缩放比例.
    /// </summary>
    public double FontSizeScale
    {
        get => _fontSizeScale;
        set => _fontSizeScale = value;
    }

    /// <summary>
    /// 滚动区域比例（占画布高度的比例）.
    /// </summary>
    public float RollingAreaRatio
    {
        get => _rollingAreaRatio;
        set
        {
            if (value is > 0 and <= 1.0f)
            {
                _rollingAreaRatio = value;
            }
        }
    }

    /// <summary>
    /// 滚动速度.
    /// </summary>
    public float RollingSpeed
    {
        get => _rollingSpeed;
        set => _rollingSpeed = value;
    }

    /// <summary>
    /// 文本不透明度.
    /// </summary>
    public double TextOpacity
    {
        get => _textOpacity;
        set
        {
            if (value is > 0 and <= 1.0)
            {
                _textOpacity = value;
            }
        }
    }

    /// <summary>
    /// 描边大小.
    /// </summary>
    public double OutlineSize
    {
        get => _outlineSize;
        set
        {
            if (value >= 0)
            {
                _outlineSize = value;
            }
        }
    }

    /// <summary>
    /// 边框颜色.
    /// </summary>
    public Color BorderColor
    {
        get => _borderColor;
        set => _borderColor = value;
    }

    /// <summary>
    /// 字体名称.
    /// </summary>
    public string FontFamilyName
    {
        get => _fontFamilyName;
        set =>
            _fontFamilyName = string.IsNullOrEmpty(value)
                ? DanmakuConfiguration.DefaultFontFamilyName
                : value;
    }

    #endregion

    /// <summary>
    /// 从 DanmakuConfiguration 创建配置状态.
    /// </summary>
    public static DanmakuConfigurationState FromConfiguration(DanmakuConfiguration config)
    {
        DanmakuConfigurationState state = new();
        state.ApplyConfiguration(config);
        return state;
    }

    /// <summary>
    /// 应用配置更新（只更新非空值）.
    /// </summary>
    /// <param name="config">配置对象.</param>
    /// <param name="scale">DPI 缩放比例（用于计算滚动速度）.</param>
    /// <param name="updateYSlotManager">更新 Y 轴槽管理器的回调（用于滚动区域比例变化）.</param>
    public void ApplyConfiguration(
        DanmakuConfiguration config,
        float? scale = null,
        Action<float>? updateYSlotManager = null
    )
    {
        if (config is null)
        {
            return;
        }

        if (config.AutoControlDensity.HasValue)
        {
            AutoControlDensity = config.AutoControlDensity.Value;
        }

        if (config.IsTextBold.HasValue)
        {
            IsTextBold = config.IsTextBold.Value;
        }

        if (config.NoOverlapSubtitle.HasValue)
        {
            NoOverlapSubtitle = config.NoOverlapSubtitle.Value;
        }

        if (config.EnableParallelUpdate.HasValue)
        {
            EnableParallelUpdate = config.EnableParallelUpdate.Value;
        }

        if (config.RollingDensity.HasValue)
        {
            RollingDensity = config.RollingDensity.Value;
        }

        if (config.FontSizeScale.HasValue)
        {
            FontSizeScale = config.FontSizeScale.Value;
        }

        if (config.RollingAreaRatio.HasValue)
        {
            RollingAreaRatio = config.RollingAreaRatio.Value;
            updateYSlotManager?.Invoke(RollingAreaRatio);
        }

        if (config.RollingSpeedBase.HasValue && scale.HasValue)
        {
            RollingSpeed = (float)(config.RollingSpeedBase.Value * 0.02f * scale.Value);
        }

        if (config.TextOpacity.HasValue)
        {
            TextOpacity = config.TextOpacity.Value;
        }

        if (config.OutlineSize.HasValue)
        {
            OutlineSize = config.OutlineSize.Value;
        }

        if (config.BorderColor.HasValue)
        {
            BorderColor = config.BorderColor.Value;
        }

        if (!string.IsNullOrEmpty(config.FontFamilyName))
        {
            FontFamilyName = config.FontFamilyName;
        }
    }
}
