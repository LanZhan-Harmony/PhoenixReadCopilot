using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Player;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 下载按钮 (直接对接 IDownloadService).
/// </summary>
public sealed partial class DownloadButton : DownloadButtonBase
{
    private readonly IDownloadService _downloadService =
        Ioc.Default.GetRequiredService<IDownloadService>();
    private readonly INotificationService _notificationService =
        Ioc.Default.GetRequiredService<INotificationService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadButton"/> class.
    /// </summary>
    public DownloadButton()
    {
        InitializeComponent();
    }

    private void InitializeDownloadFlyout()
    {
        DownloadFlyout.Items.Clear();
        if (ViewModel is null)
        {
            return;
        }

        if (ViewModel.Formats is { Count: > 0 })
        {
            MenuFlyoutSeparator? formatHeader =
                MenuFlyoutItemHeader.LoadContent() as MenuFlyoutSeparator;
            if (formatHeader is not null)
            {
                formatHeader.Tag = Strings.Resources.Quality;
                DownloadFlyout.Items.Add(formatHeader);
            }

            foreach (PlayerFormatItemViewModel formatItem in ViewModel.Formats)
            {
                PlayerFormatInformation info = formatItem.Data;
                MenuFlyoutItem menuItem = new()
                {
                    Text = info.Description,
                    MinWidth = ActualWidth,
                };
                menuItem.Click += async (_, _) => await DownloadCurrentEpisodeAsync(info);
                DownloadFlyout.Items.Add(menuItem);
            }
        }

        if (ViewModel.Episodes is { Count: > 0 })
        {
            DownloadFlyout.Items.Add(new MenuFlyoutSeparator());
            MenuFlyoutSeparator? partHeader =
                MenuFlyoutItemHeader.LoadContent() as MenuFlyoutSeparator;
            if (partHeader is not null)
            {
                partHeader.Tag = Strings.Resources.BatchDownload;
                DownloadFlyout.Items.Add(partHeader);
            }

            MenuFlyoutItem allPartsItem = new()
            {
                MinWidth = ActualWidth,
                Text = Strings.Resources.DownloadAllParts,
            };
            allPartsItem.Click += async (_, _) => await BatchDownloadAllPartsAsync();

            MenuFlyoutItem selectPartsItem = new()
            {
                MinWidth = ActualWidth,
                Text = Strings.Resources.SelectPartsDownload,
            };
            selectPartsItem.Click += (_, _) => ShowPartsSelectionTip();

            DownloadFlyout.Items.Add(allPartsItem);
            DownloadFlyout.Items.Add(selectPartsItem);
        }

        DownloadFlyout.Items.Add(new MenuFlyoutSeparator());
        MenuFlyoutSeparator? extraHeader =
            MenuFlyoutItemHeader.LoadContent() as MenuFlyoutSeparator;
        if (extraHeader is not null)
        {
            extraHeader.Tag = Strings.Resources.Extra;
            DownloadFlyout.Items.Add(extraHeader);
        }

        MenuFlyoutItem downloadCoverItem = new()
        {
            MinWidth = ActualWidth,
            Text = Strings.Resources.DownloadCover,
        };
        downloadCoverItem.Click += async (_, _) => await DownloadCoverAsync();

        MenuFlyoutItem downloadDanmakuItem = new()
        {
            MinWidth = ActualWidth,
            Text = Strings.Resources.DownloadDanmaku,
        };
        downloadDanmakuItem.Click += async (_, _) => await DownloadDanmakuAsync();

        DownloadFlyout.Items.Add(downloadCoverItem);
        DownloadFlyout.Items.Add(downloadDanmakuItem);
    }

    private async Task DownloadCurrentEpisodeAsync(PlayerFormatInformation? format)
    {
        if (ViewModel is null)
        {
            return;
        }

        Episode? ep = ViewModel.CurrentEpisode ?? ViewModel.Episodes?.FirstOrDefault();
        if (ep is null)
        {
            _notificationService.ShowSystemNotification(
                title: $"下载提示 {ViewModel.DramaTitle}".Trim(),
                message: "未能获取当前分集信息"
            );
            return;
        }

        string epTitle = string.IsNullOrWhiteSpace(ep.Title) ? $"第{ep.Number}集" : ep.Title;
        await _downloadService.DownloadSingleEpisodeAsync(
            dramaId: ViewModel.DramaId,
            videoId: ep.VideoId,
            episodeNumber: ep.Number,
            episodeTitle: epTitle,
            dramaTitle: ViewModel.DramaTitle,
            coverUrl: ViewModel.CoverUrl,
            preferFormat: format
        );
    }

    private async Task BatchDownloadAllPartsAsync()
    {
        if (
            ViewModel is null
            || string.IsNullOrWhiteSpace(ViewModel.DramaId)
            || ViewModel.Episodes is not { Count: > 0 }
        )
        {
            return;
        }

        await _downloadService.BatchDownloadEpisodesAsync(
            dramaId: ViewModel.DramaId,
            dramaTitle: ViewModel.DramaTitle,
            episodes: ViewModel.Episodes,
            coverUrl: ViewModel.CoverUrl
        );
    }

    private void ShowPartsSelectionTip()
    {
        SelectionBox.Text = string.Empty;
        SelectionTip.IsOpen = true;
    }

    private async void OnSelectionSubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        string text = args.QueryText;
        if (
            string.IsNullOrEmpty(text)
            || ViewModel is null
            || ViewModel.Episodes is not { Count: > 0 }
        )
        {
            return;
        }

        SelectionTip.IsOpen = false;
        SelectionBox.Text = string.Empty;

        List<int> indices = ParseSelection(text, ViewModel.Episodes.Count);
        if (indices.Count == 0)
        {
            _notificationService.ShowSystemNotification(
                title: $"下载提示 {ViewModel.DramaTitle}".Trim(),
                message: "分集选择格式不正确，例如: 1-5 或 1,3,5"
            );
            return;
        }

        List<Episode> selectedEpisodes = [.. ViewModel.Episodes.Where(e => indices.Contains(e.Number))];
        if (selectedEpisodes.Count == 0)
        {
            _notificationService.ShowSystemNotification(
                title: $"下载提示 {ViewModel.DramaTitle}".Trim(),
                message: "未匹配到指定的分集序号"
            );
            return;
        }

        await _downloadService.BatchDownloadEpisodesAsync(
            dramaId: ViewModel.DramaId,
            dramaTitle: ViewModel.DramaTitle,
            episodes: selectedEpisodes,
            coverUrl: ViewModel.CoverUrl
        );
    }

    private static List<int> ParseSelection(string selection, int max)
    {
        HashSet<int> result = [];
        string[] parts = selection.Split([',', ' ', '，'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string p in parts)
        {
            if (p.Contains('-'))
            {
                string[] range = p.Split('-');
                if (
                    range.Length == 2
                    && int.TryParse(range[0], out int start)
                    && int.TryParse(range[1], out int end)
                )
                {
                    for (int i = Math.Max(1, start); i <= Math.Min(max, end); i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else if (int.TryParse(p, out int num) && num >= 1 && num <= max)
            {
                result.Add(num);
            }
        }
        return [.. result.OrderBy(i => i)];
    }

    private async Task DownloadCoverAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        await _downloadService.DownloadCoverAsync(ViewModel.CoverUrl, ViewModel.DramaTitle);
    }

    private async Task DownloadDanmakuAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        Episode? ep = ViewModel.CurrentEpisode ?? ViewModel.Episodes?.FirstOrDefault();
        if (ep is null || string.IsNullOrWhiteSpace(ep.VideoId))
        {
            _notificationService.ShowSystemNotification(
                title: $"弹幕下载提示 {ViewModel.DramaTitle}".Trim(),
                message: "未能获取当前分集信息"
            );
            return;
        }

        await _downloadService.DownloadDanmakuAsync(
            dramaId: ViewModel.DramaId,
            videoId: ep.VideoId,
            episodeNumber: ep.Number,
            dramaTitle: ViewModel.DramaTitle,
            coverUrl: ViewModel.CoverUrl
        );
    }

    private void OnBtnClick(object sender, RoutedEventArgs e)
    {
        InitializeDownloadFlyout();
    }
}

/// <summary>
/// 下载按钮基类.
/// </summary>
public abstract class DownloadButtonBase : VideoPlayerPageControlBase { }
