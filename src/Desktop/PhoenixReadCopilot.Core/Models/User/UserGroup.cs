namespace PhoenixReadCopilot.Core.Models.User;

/// <summary>
/// 用户分组信息.
/// </summary>
public sealed class UserGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserGroup"/> class.
    /// </summary>
    public UserGroup(string id, string name, int totalCount = 0)
    {
        Id = id;
        Name = name;
        TotalCount = totalCount;
    }

    /// <summary>
    /// 分组标识.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 分组名称.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 该组用户总数.
    /// </summary>
    public int TotalCount { get; init; }
}
