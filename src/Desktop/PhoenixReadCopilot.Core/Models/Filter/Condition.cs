namespace PhoenixReadCopilot.Core.Models.Filter;

/// <summary>
/// 筛选条件项.
/// </summary>
public sealed class Condition(string name, string value)
{
    /// <summary>
    /// 条件名称.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// 条件值.
    /// </summary>
    public string Value { get; init; } = value;

    /// <inheritdoc/>
    public override string ToString() => Name;
}
