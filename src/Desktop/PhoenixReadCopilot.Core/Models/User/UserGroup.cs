namespace PhoenixReadCopilot.Core.Models.User;

/// <summary>
/// 用户分组信息.
/// </summary>
public sealed class UserGroup(string id, string name, int totalCount = 0)
{
    /// <summary>
    /// 分组标识.
    /// </summary>
    public string Id { get; init; } = id;

    /// <summary>
    /// 分组名称.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// 该组用户总数.
    /// </summary>
    public int TotalCount { get; init; } = totalCount;
}
