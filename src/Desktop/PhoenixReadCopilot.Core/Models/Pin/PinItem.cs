using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Models.Pin;

/// <summary>
/// 固定项.
/// </summary>
public sealed class PinItem
{
    public PinItem() { }

    public PinItem(string id, string title, string cover, PinType type)
    {
        Cover = cover;
        Title = title;
        Id = id;
        Type = type;
    }

    /// <summary>
    /// 封面.
    /// </summary>
    public string Cover { get; set; } = string.Empty;

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 标识符.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 类型.
    /// </summary>
    public PinType Type { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PinItem item && Id == item.Id;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}
