using System.Text.RegularExpressions;
using PhoenixReadCopilot.Models;

namespace PhoenixReadCopilot.Helpers;

/// <summary>
/// 表情条目.
/// </summary>
public sealed class EmojiItem
{
    /// <summary>
    /// 表情名称 (如 "微笑").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 表情代码 (如 "[微笑]").
    /// </summary>
    public string Key => $"[{Name}]";

    /// <summary>
    /// 表情图片资源 URI.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// 对应的应用内图片模型.
    /// </summary>
    public AppImage Image { get; }

    public EmojiItem(string name, Uri uri)
    {
        Name = name;
        Uri = uri;
        Image = new AppImage(uri, 32, 32);
    }
}

/// <summary>
/// 表情辅助工具类.
/// </summary>
public static class EmojiHelper
{
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

    private static readonly Dictionary<string, EmojiItem> EmojiLookup =
    [
        with(StringComparer.OrdinalIgnoreCase),
    ];
    private static readonly List<EmojiItem> AllEmojis = [];

    // 常见 Unicode Emoji 到红果表情代码的反向映射表
    private static readonly Dictionary<string, string> UnicodeToEmojiMap = new()
    {
        { "🙂", "[微笑]" },
        { "😊", "[微笑]" },
        { "😄", "[大笑]" },
        { "😃", "[大笑]" },
        { "😀", "[大笑]" },
        { "😆", "[大笑]" },
        { "😂", "[笑哭]" },
        { "🤣", "[笑哭]" },
        { "😭", "[哭]" },
        { "🥺", "[快哭了]" },
        { "😡", "[怒]" },
        { "😠", "[怒]" },
        { "🤔", "[思考]" },
        { "👍", "[赞]" },
        { "👍🏻", "[赞]" },
        { "👍🏼", "[赞]" },
        { "👍🏽", "[赞]" },
        { "👍🏾", "[赞]" },
        { "👍🏿", "[赞]" },
        { "👏", "[鼓掌]" },
        { "👏🏻", "[鼓掌]" },
        { "👏🏼", "[鼓掌]" },
        { "👏🏽", "[鼓掌]" },
        { "👏🏾", "[鼓掌]" },
        { "👏🏿", "[鼓掌]" },
        { "❤️", "[爱心]" },
        { "💖", "[爱心]" },
        { "💗", "[爱心]" },
        { "💓", "[爱心]" },
        { "👌", "[OK]" },
        { "👌🏻", "[OK]" },
        { "👌🏼", "[OK]" },
        { "🤭", "[偷笑]" },
        { "😳", "[害羞]" },
        { "😫", "[抓狂]" },
        { "😩", "[抓狂]" },
        { "🙄", "[翻白眼]" },
        { "🌹", "[送花]" },
        { "💐", "[送花]" },
        { "🌸", "[送花]" },
        { "💋", "[飞吻]" },
        { "😘", "[飞吻]" },
        { "🍉", "[吃瓜]" },
        { "😎", "[酷]" },
        { "🤮", "[吐]" },
        { "🤢", "[吐]" },
        { "💔", "[伤心]" },
        { "😋", "[饱了]" },
        { "😜", "[做鬼脸]" },
        { "🤪", "[做鬼脸]" },
        { "😝", "[做鬼脸]" },
        { "😏", "[奸笑]" },
        { "😅", "[尬笑]" },
        { "😨", "[恐惧]" },
        { "😰", "[恐惧]" },
        { "😲", "[惊呆]" },
        { "🤯", "[震惊]" },
        { "😱", "[震惊]" },
        { "🤦", "[捂脸]" },
        { "👊", "[重拳出击]" },
        { "👀", "[盯]" },
        { "😍", "[爱慕]" },
    };

    static EmojiHelper()
    {
        string baseDir = AppContext.BaseDirectory;
        foreach (string name in EmojiNames)
        {
            string localFile = Path.Combine(baseDir, "Assets", "Emoji", $"{name}.webp");
            Uri uri;
            if (File.Exists(localFile))
            {
                uri = new Uri(localFile);
            }
            else
            {
                uri = new Uri($"ms-appx:///Assets/Emoji/{name}.webp");
            }

            EmojiItem item = new(name, uri);
            AllEmojis.Add(item);

            // 同时支持带中括号与不带中括号的索引
            EmojiLookup[name] = item;
            EmojiLookup[$"[{name}]"] = item;
        }
    }

    /// <summary>
    /// 获取所有表情列表.
    /// </summary>
    public static IReadOnlyList<EmojiItem> Emojis => AllEmojis;

    /// <summary>
    /// 尝试根据名称或代码查找表情.
    /// </summary>
    public static bool TryGetEmoji(string keyOrName, out EmojiItem? emoji)
    {
        if (string.IsNullOrEmpty(keyOrName))
        {
            emoji = null;
            return false;
        }

        string trimmed = keyOrName.Trim();
        if (EmojiLookup.TryGetValue(trimmed, out emoji))
        {
            return true;
        }

        // 去掉可能包含的首尾括号
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            string inner = trimmed.Substring(1, trimmed.Length - 2);
            return EmojiLookup.TryGetValue(inner, out emoji);
        }

        emoji = null;
        return false;
    }

    /// <summary>
    /// 发送评论前反向格式化文本中的表情（将 Unicode Emoji 等转换为 [表情名] 格式）.
    /// </summary>
    public static string FormatEmojiText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string result = text;

        // 1. 替换 HTML 图片标签形式的表情 (如 <img ... alt="微笑">)
        result = Regex.Replace(
            result,
            @"<img[^>]*alt=[""']?([a-zA-Z0-9_\u4e00-\u9fa5]+)[""']?[^>]*>",
            "[$1]"
        );

        // 2. 替换 Unicode Emoji 到 [表情名]
        foreach ((string? unicode, string? code) in UnicodeToEmojiMap)
        {
            if (result.Contains(unicode))
            {
                result = result.Replace(unicode, code);
            }
        }

        return result;
    }
}
