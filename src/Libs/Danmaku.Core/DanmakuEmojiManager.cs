using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Danmaku.Core;

/// <summary>
/// 弹幕分段类型.
/// </summary>
internal enum DanmakuSegmentType
{
    Text,
    Emoji,
}

/// <summary>
/// 弹幕图文分段.
/// </summary>
internal sealed class DanmakuSegment : IDisposable
{
    public DanmakuSegmentType Type { get; }
    public string Content { get; }
    public IDWriteTextLayout? TextLayout { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public DanmakuSegment(
        DanmakuSegmentType type,
        string content,
        float width,
        float height,
        IDWriteTextLayout? textLayout = null
    )
    {
        Type = type;
        Content = content;
        Width = width;
        Height = height;
        TextLayout = textLayout;
    }

    public void Dispose()
    {
        TextLayout?.Dispose();
        TextLayout = null;
    }
}

/// <summary>
/// 红果短剧表情管理器 - 负责表情资源管理、WebP 异步解码与 Direct2D GPU 纹理缓存.
/// </summary>
internal sealed partial class DanmakuEmojiManager : IDisposable
{
    /// <summary>
    /// 红果短剧内置全部 53 种表情名称.
    /// </summary>
    private static readonly string[] EmojiNames =
    [
        "微笑",
        "偷笑",
        "笑",
        "什么",
        "害羞",
        "爱慕",
        "飞吻",
        "奸笑",
        "尬笑",
        "思考",
        "撇嘴",
        "做鬼脸",
        "酷",
        "翻白眼",
        "惊呆",
        "震惊",
        "送心",
        "委屈",
        "快哭了",
        "笑哭",
        "哭",
        "大笑",
        "舔屏",
        "怒",
        "捂脸",
        "吐",
        "恐惧",
        "抓狂",
        "敬礼",
        "石化",
        "OK",
        "赞",
        "爱心",
        "伤心",
        "KISS",
        "懂了",
        "探究",
        "重拳出击",
        "吃瓜",
        "盯",
        "你细品",
        "赶稿中",
        "注意",
        "饱了",
        "码住",
        "学会了",
        "顶帖",
        "求爆更",
        "求关注",
        "爽",
        "鼓掌",
        "没看够",
        "送花",
    ];

    private static readonly HashSet<string> ValidEmojiNames =
    [
        with(StringComparer.Ordinal),
        .. EmojiNames,
    ];

    // 内存原始像素缓存（全局共享，避免重复磁盘 IO 和重复 WebP 解码）
    private static readonly ConcurrentDictionary<
        string,
        (byte[] PixelData, uint Width, uint Height)
    > _rawPixelCache = new(StringComparer.Ordinal);

    // Direct2D GPU 纹理缓存（绑定到当前 DirectXResourceManager）
    private readonly ConcurrentDictionary<string, ID2D1Bitmap1> _gpuBitmapCache = new(
        StringComparer.Ordinal
    );

    private readonly DirectXResourceManager _resourceManager;
    private bool _isDisposed;

    public DanmakuEmojiManager(DirectXResourceManager resourceManager)
    {
        _resourceManager =
            resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    }

    /// <summary>
    /// 检查文本是否可能包含表情代码.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasEmojiPattern(string? text)
    {
        return !string.IsNullOrEmpty(text) && text.Contains('[') && text.Contains(']');
    }

    /// <summary>
    /// 检查是否为合法的红果表情名.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidEmoji(string name)
    {
        return ValidEmojiNames.Contains(name);
    }

    /// <summary>
    /// 解析文本为混合图文分段. 如果不包含任何有效表情，返回 null 以便走纯文本快速路径.
    /// </summary>
    public List<DanmakuSegment>? ParseSegments(
        string rawText,
        IDWriteTextFormat textFormat,
        float fontSize,
        float maxWidth,
        float maxHeight
    )
    {
        if (string.IsNullOrEmpty(rawText) || !HasEmojiPattern(rawText))
        {
            return null;
        }

        MatchCollection matches = EmojiRegex().Matches(rawText);
        if (matches.Count == 0)
        {
            return null;
        }

        bool hasValidEmoji = false;
        foreach (Match match in matches)
        {
            if (IsValidEmoji(match.Groups[1].Value))
            {
                hasValidEmoji = true;
                break;
            }
        }

        if (!hasValidEmoji)
        {
            return null;
        }

        List<DanmakuSegment> segments = [with(matches.Count * 2 + 1)];
        int lastIndex = 0;

        // 表情大小：微调为 1.15 倍字体大小，确保高度尽量与汉字两边文字视觉协调一致
        float emojiSize = MathF.Round(fontSize * 1.15f);
        float spacing = 2f; // 图文微小间距

        foreach (Match match in matches)
        {
            string emojiName = match.Groups[1].Value;
            if (!IsValidEmoji(emojiName))
            {
                continue;
            }

            // 处理表情前面的文字
            if (match.Index > lastIndex)
            {
                string textBefore = rawText[lastIndex..match.Index];
                IDWriteTextLayout layout = _resourceManager.DWriteFactory.CreateTextLayout(
                    textBefore,
                    textFormat,
                    maxWidth,
                    maxHeight
                );
                TextMetrics m = layout.Metrics;
                segments.Add(
                    new DanmakuSegment(
                        DanmakuSegmentType.Text,
                        textBefore,
                        m.Width + spacing,
                        m.Height,
                        layout
                    )
                );
            }

            // 添加表情分段
            segments.Add(
                new DanmakuSegment(
                    DanmakuSegmentType.Emoji,
                    emojiName,
                    emojiSize + spacing,
                    emojiSize
                )
            );
            lastIndex = match.Index + match.Length;
        }

        // 处理末尾剩余文字
        if (lastIndex < rawText.Length)
        {
            string textAfter = rawText[lastIndex..];
            IDWriteTextLayout layout = _resourceManager.DWriteFactory.CreateTextLayout(
                textAfter,
                textFormat,
                maxWidth,
                maxHeight
            );
            TextMetrics m = layout.Metrics;
            segments.Add(
                new DanmakuSegment(DanmakuSegmentType.Text, textAfter, m.Width, m.Height, layout)
            );
        }

        return segments;
    }

    /// <summary>
    /// 获取或加载表情的 Direct2D GPU 纹理.
    /// </summary>
    public ID2D1Bitmap1? GetOrCreateEmojiBitmap(string emojiName)
    {
        if (_isDisposed || _resourceManager.D2DContext is null)
        {
            return null;
        }

        if (_gpuBitmapCache.TryGetValue(emojiName, out ID2D1Bitmap1? bitmap))
        {
            return bitmap;
        }

        // 查找或解码原始像素数据
        if (
            !_rawPixelCache.TryGetValue(
                emojiName,
                out (byte[] PixelData, uint Width, uint Height) rawData
            )
        )
        {
            (byte[] PixelData, uint Width, uint Height)? loaded = LoadWebpPixels(emojiName);
            if (loaded is null)
            {
                return null;
            }
            rawData = loaded.Value;
            _rawPixelCache[emojiName] = rawData;
        }

        // 在 Direct2D 上下文中创建 GPU 位图
        try
        {
            fixed (byte* p = rawData.PixelData)
            {
                BitmapProperties1 props = new(
                    new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
                    96,
                    96,
                    BitmapOptions.None
                );

                ID2D1Bitmap1 gpuBmp = _resourceManager.D2DContext.CreateBitmap(
                    new SizeI((int)rawData.Width, (int)rawData.Height),
                    (nint)p,
                    rawData.Width * 4,
                    props
                );

                _gpuBitmapCache[emojiName] = gpuBmp;
                return gpuBmp;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to create D2D bitmap for emoji '{emojiName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从文件加载并解码 WebP 图片像素.
    /// </summary>
    private static (byte[] PixelData, uint Width, uint Height)? LoadWebpPixels(string emojiName)
    {
        try
        {
            string filePath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Emoji",
                $"{emojiName}.webp"
            );

            if (!File.Exists(filePath))
            {
                Logger.Log($"Emoji WebP file not found: {emojiName}");
                return null;
            }

            using FileStream fs = File.OpenRead(filePath);
            using IRandomAccessStream ras = fs.AsRandomAccessStream();
            BitmapDecoder decoder = BitmapDecoder
                .CreateAsync(ras)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            PixelDataProvider pixelData = decoder
                .GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                )
                .AsTask()
                .GetAwaiter()
                .GetResult();

            byte[] bytes = pixelData.DetachPixelData();
            return (bytes, decoder.PixelWidth, decoder.PixelHeight);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error decoding emoji '{emojiName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 清理 GPU 资源（例如设备丢失重建时）.
    /// </summary>
    public void ClearGpuCache()
    {
        foreach ((_, ID2D1Bitmap1? bmp) in _gpuBitmapCache)
        {
            bmp?.Dispose();
        }
        _gpuBitmapCache.Clear();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ClearGpuCache();
    }

    [GeneratedRegex(@"\[([a-zA-Z0-9_\u4e00-\u9fa5]+)\]")]
    private static partial Regex EmojiRegex();
}
