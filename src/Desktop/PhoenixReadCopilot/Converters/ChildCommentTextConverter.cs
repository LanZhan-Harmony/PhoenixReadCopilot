using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 子评论数转换为展开提示文本.
    /// </summary>
    public static string ChildCommentText(int count) =>
        string.Format(Resources.MoreReplyDisplay, count);
}
