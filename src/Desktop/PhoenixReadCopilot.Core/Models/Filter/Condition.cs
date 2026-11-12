namespace PhoenixReadCopilot.Core.Models.Filter;

/// <summary>
/// 筛选条件项.
/// </summary>
public sealed class Condition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Condition"/> class.
    /// </summary>
    public Condition(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// 条件名称.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 条件值.
    /// </summary>
    public string Value { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
