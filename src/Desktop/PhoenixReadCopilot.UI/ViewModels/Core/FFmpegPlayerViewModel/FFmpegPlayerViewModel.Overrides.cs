using System.Net;
using System.Net.Sockets;
using System.Security;
using FFmpegInteropX;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.WinUIKernel.Share.Base;
using Windows.Media.Playback;
using Windows.Storage;

namespace PhoenixReadCopilot.UI.ViewModels.Core;

/// <summary>
/// FFmpeg 播放器视图模型.
/// </summary>
public sealed partial class FFmpegPlayerViewModel
{
    /// <inheritdoc/>
    protected override Task OnCloseAsync()
    {
        StopManifestServer();

        if (Player is null && _ffmpegSource is null)
        {
            return Task.CompletedTask;
        }

        _isDisposed = true;
        _element?.SetMediaPlayer(default);
        if (Player?.PlaybackSession != null)
        {
            Player.PlaybackSession.PositionChanged -= OnMediaPositionChanged;
        }

        if (Player is not null)
        {
            Player.MediaOpened -= OnMediaPlayerOpened;
            Player.CurrentStateChanged -= OnMediaPlayerStateChanged;
            Player.MediaFailed -= OnMediaPlayerFailed;
            Player.MediaEnded -= OnMediaPlayerEnded;
            Player.Dispose();
            Player = null;
        }

        if (_ffmpegSource is not null)
        {
            _ffmpegSource.Dispose();
            _ffmpegSource = null;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override bool IsMediaLoaded() =>
        !_isDisposed
        && (
            Player?.PlaybackSession?.NaturalDuration.Ticks > 0 || _ffmpegSource?.Duration.Ticks > 0
        );

    /// <inheritdoc/>
    protected override Task OnTogglePlayPauseAsync()
    {
        if (!IsMediaLoaded())
        {
            return Task.CompletedTask;
        }

        if (Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            Player.Pause();
        }
        else
        {
            Player.Play();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task ForcePlayAsync()
    {
        if (IsMediaLoaded())
        {
            Player.Play();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnSeekAsync(TimeSpan position)
    {
        if (!Player?.CanSeek ?? false)
        {
            this.Get<AppViewModel>()
                .ShowTipCommand.Execute((Resources.CanNotSeek, InfoType.Warning));
            return Task.CompletedTask;
        }

        Player.Position = position;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override void OnSetVolume(int value)
    {
        if (Player is null)
        {
            return;
        }

        Player.Volume = value / 100.0;
    }

    /// <inheritdoc/>
    protected override void OnSetSpeed(double speed)
    {
        if (Player is null)
        {
            return;
        }

        Player.PlaybackRate = speed;
        _speedAction?.Invoke(speed);
    }

    /// <inheritdoc/>
    protected override async Task OnTakeScreenshotAsync(string path)
    {
        if (Player?.PlaybackSession is null)
        {
            return;
        }

        var renderTarget = new CanvasRenderTarget(
            CanvasDevice.GetSharedDevice(),
            Player.PlaybackSession.NaturalVideoWidth,
            Player.PlaybackSession.NaturalVideoHeight,
            96
        );
        Player.CopyFrameToVideoSurface(renderTarget);
        await renderTarget.SaveAsync(path, CanvasBitmapFileFormat.Png);
    }

    /// <inheritdoc/>
    protected override async Task OnLoadPlayDataAsync()
    {
        if (Player is not null)
        {
            await OnCloseAsync();
        }

        Player = CreatePlayer();
        Player.AutoPlay = _autoPlay;

        try
        {
            await LoadMediaSourceAsync();
            _element?.SetMediaPlayer(Player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg 加载媒体失败.");
            UpdateState(Models.Constants.PlayerState.Failed);
        }
    }

    private MediaSourceConfig CreateMediaSourceConfig()
    {
        var config = new MediaSourceConfig();
        config.Video.VideoDecoderMode = VideoDecoderMode.Automatic;
        config.General.FastSeek = true;

        var cookies = this.Get<IBiliCookiesResolver>().GetCookieString();
        var headerSb = new StringBuilder();
        headerSb.Append($"User-Agent: {VideoUserAgent}\r\n");
        headerSb.Append($"Referer: {VideoReferer}\r\n");
        if (!string.IsNullOrEmpty(cookies))
        {
            headerSb.Append($"Cookie: {cookies}\r\n");
        }

        var headerStr = headerSb.ToString();
        config.FFmpegOptions["headers"] = headerStr;
        config.FFmpegOptions["user_agent"] = VideoUserAgent;
        config.FFmpegOptions["referer"] = VideoReferer;
        config.FFmpegOptions["protocol_whitelist"] = "file,http,https,tcp,tls,crypto";
        config.FFmpegOptions["reconnect"] = 1;
        config.FFmpegOptions["reconnect_streamed"] = 1;
        config.FFmpegOptions["reconnect_on_network_error"] = 1;

        return config;
    }

    private async Task LoadMediaSourceAsync()
    {
        var config = CreateMediaSourceConfig();

        var onlyVideo = !string.IsNullOrEmpty(_videoUrl) && string.IsNullOrEmpty(_audioUrl);
        var onlyAudio = string.IsNullOrEmpty(_videoUrl) && !string.IsNullOrEmpty(_audioUrl);

        if (_videoSegment is not null || _audioSegment is not null)
        {
            var mpdFilePath =
                onlyVideo ? "ms-appx:///Assets/DashVideoWithoutAudioTemplate.xml"
                : onlyAudio ? "ms-appx:///Assets/DashAudioWithoutVideoTemplate.xml"
                : "ms-appx:///Assets/DashVideoTemplate.xml";
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(mpdFilePath));
            var mpdStr = await FileIO.ReadTextAsync(file);

            var videoStr = string.Empty;
            if (_videoSegment is not null && !string.IsNullOrEmpty(_videoUrl))
            {
                videoStr = $"""
                    <Representation bandwidth="{_videoSegment.Bandwidth}" codecs="{_videoSegment.Codecs}" height="{_videoSegment.Height}" mimeType="{_videoSegment.MimeType}" id="{_videoSegment.Id}" width="{_videoSegment.Width}" startWithSap="1">
                        <BaseURL>{SecurityElement.Escape(_videoUrl)}</BaseURL>
                        <SegmentBase indexRange="{_videoSegment.IndexRange}">
                            <Initialization range="{_videoSegment.Initialization}" />
                        </SegmentBase>
                    </Representation>
                    """;
            }

            var audioStr = string.Empty;
            if (_audioSegment is not null && !string.IsNullOrEmpty(_audioUrl))
            {
                audioStr = $"""
                    <Representation bandwidth="{_audioSegment.Bandwidth}" codecs="{_audioSegment.Codecs}" mimeType="{_audioSegment.MimeType}" id="{_audioSegment.Id}">
                        <BaseURL>{SecurityElement.Escape(_audioUrl)}</BaseURL>
                        <SegmentBase indexRange="{_audioSegment.IndexRange}">
                            <Initialization range="{_audioSegment.Initialization}" />
                        </SegmentBase>
                    </Representation>
                    """;
            }

            videoStr = videoStr.Trim();
            audioStr = audioStr.Trim();
            mpdStr = mpdStr
                .Replace("{video}", videoStr)
                .Replace("{audio}", audioStr)
                .Replace("{bufferTime}", "PT4S");

            var port = StartManifestServer(mpdStr);
            var manifestUrl = $"http://127.0.0.1:{port}/manifest.mpd";

            try
            {
                _ffmpegSource = await FFmpegMediaSource.CreateFromUriAsync(manifestUrl, config);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FFmpeg 打开 DASH MPD 失败，回退到直接加载流 URL.");
                var fallbackUrl = string.IsNullOrEmpty(_videoUrl) ? _audioUrl : _videoUrl;
                if (!string.IsNullOrEmpty(fallbackUrl))
                {
                    _ffmpegSource = await FFmpegMediaSource.CreateFromUriAsync(fallbackUrl, config);
                }
                else
                {
                    throw;
                }
            }
        }
        else
        {
            var fallbackUrl = string.IsNullOrEmpty(_videoUrl) ? _audioUrl : _videoUrl;
            if (!string.IsNullOrEmpty(fallbackUrl))
            {
                _ffmpegSource = await FFmpegMediaSource.CreateFromUriAsync(fallbackUrl, config);
            }
        }

        if (_ffmpegSource is not null)
        {
            var playbackItem = _ffmpegSource.CreateMediaPlaybackItem();
            Player.Source = playbackItem;
        }
    }

    private int StartManifestServer(string mpdContent)
    {
        StopManifestServer();

        _manifestServerCts = new CancellationTokenSource();
        var ct = _manifestServerCts.Token;

        _manifestListener = new TcpListener(IPAddress.Loopback, 0);
        _manifestListener.Start();
        var port = ((IPEndPoint)_manifestListener.LocalEndpoint).Port;

        var mpdBytes = Encoding.UTF8.GetBytes(mpdContent);
        var listener = _manifestListener;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        var client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                        _ = Task.Run(
                            async () =>
                            {
                                try
                                {
                                    using (client)
                                    using (var networkStream = client.GetStream())
                                    {
                                        var buffer = new byte[2048];
                                        var read = await networkStream
                                            .ReadAsync(buffer, ct)
                                            .ConfigureAwait(false);
                                        if (read <= 0)
                                        {
                                            return;
                                        }

                                        var header =
                                            $"HTTP/1.1 200 OK\r\nContent-Type: application/dash+xml\r\nContent-Length: {mpdBytes.Length}\r\nConnection: close\r\n\r\n";
                                        var headerBytes = Encoding.UTF8.GetBytes(header);
                                        await networkStream
                                            .WriteAsync(headerBytes, ct)
                                            .ConfigureAwait(false);
                                        await networkStream
                                            .WriteAsync(mpdBytes, ct)
                                            .ConfigureAwait(false);
                                        await networkStream.FlushAsync(ct).ConfigureAwait(false);
                                    }
                                }
                                catch
                                {
                                    // 忽略单个连接读写异常.
                                }
                            },
                            ct
                        );
                    }
                }
                catch
                {
                    // 监听器已停止或取消.
                }
            },
            ct
        );

        return port;
    }

    private void StopManifestServer()
    {
        if (_manifestServerCts is not null)
        {
            try
            {
                _manifestServerCts.Cancel();
                _manifestServerCts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "取消清单服务令牌失败.");
            }

            _manifestServerCts = null;
        }

        if (_manifestListener is not null)
        {
            try
            {
                _manifestListener.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "停止清单监听服务失败.");
            }

            _manifestListener = null;
        }
    }
}
