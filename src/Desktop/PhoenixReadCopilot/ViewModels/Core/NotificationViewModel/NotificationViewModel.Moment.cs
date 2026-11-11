using System.Security;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Pages.Overlay;
using Richasy.BiliKernel.Models.Media;
using Richasy.BiliKernel.Models.Moment;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 通知视图模型.
/// </summary>
public sealed partial class NotificationViewModel
{
    [RelayCommand]
    private async Task TrySendVideoMomentNotificationsAsync()
    {
        bool isVideoNotifyEnabled = Settings.IsVideoMomentNotificationEnabled;
        if (!isVideoNotifyEnabled)
        {
            return;
        }

        try
        {
            MomentView? lastMoments = await _momentService.GetVideoMomentsAsync(
                null,
                null,
                new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token
            );
            if (lastMoments is null || lastMoments.Moments.Count == 0)
            {
                return;
            }

            LoadLastTenMomentIds();

            // 只取最新的三个动态.
            List<MomentInformation> newMoments =
            [
                .. lastMoments.Moments.Take(3),
            ];
            foreach (MomentInformation? moment in newMoments)
            {
                if (_momentIds.Contains(moment.Id))
                {
                    continue;
                }

                string? title = string.Empty;
                string cover = string.Empty;
                string avatar = string.Empty;
                string? publisher = string.Empty;
                string? pageType = string.Empty;
                string arguments = string.Empty;
                if (moment.Data is VideoInformation vinfo)
                {
                    title = vinfo.Identifier.Title;
                    cover = vinfo.Identifier.Cover.Uri.ToString();
                    pageType = typeof(VideoPlayerPage).FullName;
                    arguments = JsonAotSerializer.Serialize(vinfo.Identifier);
                }
                else if (moment.Data is EpisodeInformation einfo)
                {
                    title = einfo.Identifier.Title;
                    cover = einfo.Identifier.Cover.Uri.ToString();
                    pageType = typeof(PgcPlayerPage).FullName;
                    bool hasEpid = einfo.Identifier.Id != "0";
                    if (hasEpid)
                    {
                        MediaIdentifier identifier = new(
                            "ep_" + einfo.Identifier.Id,
                            null,
                            null
                        );
                        arguments = JsonAotSerializer.Serialize(identifier);
                    }
                    else
                    {
                        arguments = string.Empty;
                    }
                }

                avatar = moment.User.Avatar.Uri.ToString();
                publisher = moment.User.Name;
                AppNotification notification = new AppNotificationBuilder()
                    .AddArgument("page", pageType)
                    .AddArgument("args", arguments)
                    .AddText(title)
                    .AddText(publisher)
                    .SetAppLogoOverride(new Uri(avatar), AppNotificationImageCrop.Circle)
                    .SetHeroImage(new Uri(cover))
                    .BuildNotification();

                try
                {
                    AppNotificationManager.Default.Show(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送通知失败.");
                }
                await Task.Delay(1000);
            }

            // 尝试提取最新的10个动态id，如果不足10个，则从 _momentIds 中补充.
            List<string> newMomentIds =
            [
                .. newMoments.Select(p => p.Id),
            ];
            List<string> momentIds =
            [
                .. newMomentIds,
                .. _momentIds.Where(p => !newMomentIds.Contains(p)).Take(10 - newMomentIds.Count),
            ];

            SaveLastTenMomentIds(momentIds);

            List<MomentInformation> lastFiveMoments =
            [
                .. lastMoments.Moments.Take(5),
            ];
            UpdateMomentTiles(lastFiveMoments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试发送视频动态通知时发生错误.");
        }
    }

    /// <summary>
    /// 这里会读取保存在本地的最近十个动态Id.
    /// 如果新的动态Id不在这十个Id中,则会触发通知.
    /// </summary>
    private void LoadLastTenMomentIds()
    {
        string lastTenMomentIds = Settings.LastTenMomentIds;
        if (string.IsNullOrEmpty(lastTenMomentIds))
        {
            _momentIds = [];
            return;
        }

        _momentIds =
        [
            .. lastTenMomentIds.Split(','),
        ];
    }

    private void SaveLastTenMomentIds(List<string> momentIds)
    {
        _momentIds = momentIds;
        string lastTenMomentIds = string.Join(',', momentIds);
        Settings.LastTenMomentIds = lastTenMomentIds;
    }

    private void UpdateMomentTiles(List<MomentInformation> moments)
    {
        if (!_isTileSupport)
        {
            return;
        }

        foreach (MomentInformation d in moments)
        {
            string? title = string.Empty;
            string cover = string.Empty;
            if (d.Data is VideoInformation videoInfo)
            {
                title = videoInfo.Identifier.Title;
                cover = videoInfo.Identifier.Cover.SourceUri.ToString();
            }
            else if (d.Data is EpisodeInformation episodeInfo)
            {
                title = episodeInfo.Identifier.Title;
                cover = episodeInfo.Identifier.Cover.SourceUri.ToString();
            }

            Uri avatar = d.User.Avatar.Uri;
            string? publisher = d.User.Name;

            string xmlContent = $"""
                                 <?xml version="1.0" encoding="utf-8"?>
                                 <tile>
                                     <visual>
                                         <binding template="TileMedium" branding="name">
                                             <image src="{cover}" placement="background" hint-overlay="60" />
                                             <image src="{avatar}" placement="peek" hint-crop="circle" />
                                             <text hint-style="caption" hint-wrap="true">{SecurityElement.Escape(
                                                 title
                                             )}</text>
                                         </binding>
                                         <binding template="TileWide" branding="nameAndLogo">
                                             <image src="{cover}" placement="background" hint-overlay="60" />
                                             <text hint-maxLines="2" hint-style="base" hint-wrap="true">{SecurityElement.Escape(
                                                 title
                                             )}</text>
                                             <text hint-style="caption">{SecurityElement.Escape(publisher)}</text>
                                         </binding>
                                         <binding template="TileLarge" branding="nameAndLogo">
                                             <image src="{cover}" placement="background" hint-overlay="60" />
                                             <text hint-style="subtitle" hint-wrap="true">{SecurityElement.Escape(
                                                 title
                                             )}</text>
                                             <text hint-style="base"></text>
                                             <text hint-style="base">{SecurityElement.Escape(publisher)}</text>
                                         </binding>
                                     </visual>
                                 </tile>
                                 """;
            XmlDocument xml = new();
            xml.LoadXml(xmlContent);
            TileUpdateManager
                .CreateTileUpdaterForApplication()
                .Update(new TileNotification(xml) { Tag = d.Id });
        }
    }
}
