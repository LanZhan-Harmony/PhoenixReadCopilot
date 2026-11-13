using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户项视图模型.
/// </summary>
public sealed partial class UserItemViewModel
{
    private static readonly ILogger<UserItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<UserItemViewModel>
    >();

    [ObservableProperty]
    public partial bool IsFollowed { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 用户描述.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 用户头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 用户 Id.
    /// </summary>
    public string Id { get; init; }
}
