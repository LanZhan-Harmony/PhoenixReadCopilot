namespace PhoenixReadCopilot.Models;

/// <summary>
/// 富文本（支持表情与图片）.
/// </summary>
public sealed class EmoteText
{
    /// <summary>
    /// 完整文本.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// 表情索引.
    /// </summary>
    public Dictionary<string, AppImage>? Emotes { get; }

    /// <summary>
    /// 评论图片.
    /// </summary>
    public IList<AppImage>? Pictures { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmoteText"/> class.
    /// </summary>
    public EmoteText(string text, Dictionary<string, AppImage>? emotes, IList<AppImage>? pictures = null)
    {
        Text = text ?? string.Empty;
        Emotes = emotes;
        Pictures = pictures;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmoteText"/> class with pictures.
    /// </summary>
    public EmoteText(string text, IList<AppImage>? pictures = null)
        : this(text, null, pictures)
    {
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is EmoteText emoteText)
        {
            return Text == emoteText.Text;
        }

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Text);
    }

    /// <summary>
    /// 从纯文本隐式转换.
    /// </summary>
    public static implicit operator EmoteText(string text) => new(text);

    /// <summary>
    /// 从旧版 BiliKernel EmoteText 隐式转换.
    /// </summary>
    public static implicit operator EmoteText?(Richasy.BiliKernel.Models.Appearance.EmoteText? bili)
    {
        if (bili is null)
        {
            return null;
        }

        var emotes = bili.Emotes?.ToDictionary(
            k => k.Key,
            v => new AppImage(v.Value.Uri, v.Value.Width, v.Value.Height)
        );
        var pictures = bili.Pictures?.Select(p => new AppImage(p.Uri, p.Width, p.Height)).ToList();
        return new EmoteText(bili.Text, emotes, pictures);
    }
}
