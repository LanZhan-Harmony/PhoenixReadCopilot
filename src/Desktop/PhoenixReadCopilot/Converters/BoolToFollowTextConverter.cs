using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 关注布尔值转为提示文本（已关注 / 关注）.
    /// </summary>
    public static string BoolToFollowText(bool isFollowed) =>
        isFollowed ? Resources.Followed : Resources.Follow;
}
