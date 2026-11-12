namespace PhoenixReadCopilot.Core.Models.Filter;

/// <summary>
/// 筛选维度行.
/// </summary>
public sealed class Filter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Filter"/> class.
    /// </summary>
    public Filter(string key, string name, IReadOnlyList<Condition> conditions)
    {
        Key = key;
        Name = name;
        Conditions = conditions;
    }

    /// <summary>
    /// 维度标识键.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// 维度显示名称.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 可选条件列表.
    /// </summary>
    public IReadOnlyList<Condition> Conditions { get; init; }
}
