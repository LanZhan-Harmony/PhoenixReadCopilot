using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using Richasy.BiliKernel.Models.Article;
using Windows.Globalization;
using Windows.System;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 文章项视图模型.
/// </summary>
public sealed partial class ArticleItemViewModel : ViewModelBase<ArticleInformation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleItemViewModel"/> class.
    /// </summary>
    public ArticleItemViewModel(
        ArticleInformation data,
        ArticleCardStyle style,
        Action<ArticleItemViewModel>? removeAction = null
    )
        : base(data)
    {
        string primaryLan = ApplicationLanguages.Languages[0];
        Title = data.Identifier.Title.Trim();
        Subtitle = data.GetExtensionIfNotNull<string>(ArticleExtensionDataId.Subtitle);
        Cover = data.Identifier.Cover?.Uri;
        Author = data.Publisher?.Name;
        Avatar = data.Publisher?.Avatar?.Uri;
        PublishRelativeTime = data.PublishDateTime.Humanize(culture: new CultureInfo(primaryLan));
        LikeCount = data.CommunityInformation?.LikeCount;
        Style = style;
        DateTimeOffset? collectTime = data.GetExtensionIfNotNull<DateTimeOffset?>(
            ArticleExtensionDataId.CollectTime
        );
        if (collectTime is not null)
        {
            CollectRelativeTime = collectTime.Value.Humanize();
        }

        _removeAction = removeAction;
    }

    [RelayCommand]
    private void OpenReader()
    {
        NavigationViewModel navVM = ItemServices.Navigation;
        navVM.NavigateToOver(typeof(ArticleReaderPage), Data.Identifier);
    }

    [RelayCommand]
    private void ShowUserSpace() =>
        ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data.Publisher);

    [RelayCommand]
    private async Task OpenInBroswerAsync()
    {
        string url = $"https://www.bilibili.com/read/cv{Data.Identifier.Id}";
        await Launcher.LaunchUriAsync(new Uri(url));
    }

    [RelayCommand]
    private async Task RemoveHistoryAsync()
    {
        try
        {
            await ItemServices.ViewHistory.RemoveArticleHistoryItemAsync(Data);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "移除历史记录时失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToRemoveVideoFromHistory, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(
            Data.Identifier.Id,
            Data.Identifier.Title,
            Data.Identifier.Cover.Uri.ToString(),
            PinContentType.Article
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }
}
