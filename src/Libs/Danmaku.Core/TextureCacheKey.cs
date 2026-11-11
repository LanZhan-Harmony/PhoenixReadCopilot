namespace Danmaku.Core;

/// <summary>
/// 🚀 GC 优化：零分配的纹理缓存键（使用值类型替代字符串）.
/// 优势：
/// - 零字符串分配（每个弹幕节省 7+ 次分配）
/// - 更快的哈希和比较（整数 vs 字符串）
/// - 更小的内存占用（32 字节 vs 字符串 100+ 字节）
/// </summary>
internal readonly struct TextureCacheKey : IEquatable<TextureCacheKey>
{
    /// <summary>
    /// 文本内容.
    /// </summary>
    public readonly string? Text;

    /// <summary>
    /// 文本颜色的 RGB 值（R &lt;&lt; 16 | G &lt;&lt; 8 | B）.
    /// 不包含 Alpha 通道，因为透明度在绘制时应用.
    /// </summary>
    public readonly uint TextColorRGB;

    /// <summary>
    /// 描边颜色的 RGB 值（R &lt;&lt; 16 | G &lt;&lt; 8 | B）.
    /// </summary>
    public readonly uint OutlineColorRGB;

    /// <summary>
    /// 字体大小.
    /// </summary>
    public readonly float FontSize;

    /// <summary>
    /// 是否有描边.
    /// </summary>
    public readonly bool HasOutline;

    /// <summary>
    /// 初始化纹理缓存键.
    /// </summary>
    public TextureCacheKey(
        string text,
        Windows.UI.Color textColor,
        Windows.UI.Color outlineColor,
        float fontSize,
        bool hasOutline
    )
    {
        Text = text;

        // RGB 打包（忽略 Alpha）
        TextColorRGB = (uint)(textColor.R << 16 | textColor.G << 8 | textColor.B);
        OutlineColorRGB = (uint)(outlineColor.R << 16 | outlineColor.G << 8 | outlineColor.B);

        FontSize = fontSize;
        HasOutline = hasOutline;
    }

    /// <summary>
    /// 高性能相等性比较（数值优先快速过滤，最后比对字符串文本，杜绝哈希碰撞错乱）.
    /// </summary>
    public bool Equals(TextureCacheKey other)
    {
        return TextColorRGB == other.TextColorRGB
            && OutlineColorRGB == other.OutlineColorRGB
            && FontSize == other.FontSize
            && HasOutline == other.HasOutline
            && string.Equals(Text, other.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// 重写 Equals（支持装箱比较）.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is TextureCacheKey other && Equals(other);
    }

    /// <summary>
    /// 高性能哈希函数.
    /// </summary>
    public override int GetHashCode()
    {
        int textHash =
            Text is not null ? string.GetHashCode(Text.AsSpan(), StringComparison.Ordinal) : 0;
        return HashCode.Combine(textHash, TextColorRGB, OutlineColorRGB, FontSize, HasOutline);
    }

    /// <summary>
    /// 相等性运算符.
    /// </summary>
    public static bool operator ==(TextureCacheKey left, TextureCacheKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 不等性运算符.
    /// </summary>
    public static bool operator !=(TextureCacheKey left, TextureCacheKey right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 调试用字符串表示（仅用于日志）.
    /// </summary>
    public override string ToString()
    {
        return $"[Text:'{Text}', RGB:{TextColorRGB:X6}/{OutlineColorRGB:X6}, Size:{FontSize:F1}, Outline:{HasOutline}]";
    }
}
