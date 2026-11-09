using System.Text;
using FFmpegInteropX;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using Windows.Media.Playback;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// FFmpeg 播放器视图模型.
/// </summary>
public sealed partial class FFmpegPlayerViewModel
{
    /// <inheritdoc/>
    protected override Task OnCloseAsync()
    {
        DisposePreloadedSource();

        if (Player is null && _ffmpegSource is null)
        {
            return Task.CompletedTask;
        }

        _isDisposed = true;
        _element?.SetMediaPlayer(null);

        if (Player is not null)
        {
            try
            {
                Player.Pause();
            }
            catch { }

            try
            {
                if (Player.PlaybackSession != null)
                {
                    Player.PlaybackSession.PositionChanged -= OnMediaPositionChanged;
                    Player.PlaybackSession.NaturalDurationChanged -= OnMediaNaturalDurationChanged;
                }
            }
            catch { }

            try
            {
                Player.MediaOpened -= OnMediaPlayerOpened;
                Player.CurrentStateChanged -= OnMediaPlayerStateChanged;
                Player.MediaFailed -= OnMediaPlayerFailed;
                Player.MediaEnded -= OnMediaPlayerEnded;
                Player.Source = null;
                Player.Dispose();
            }
            catch { }
            Player = null;
        }

        if (_ffmpegSource is not null)
        {
            try
            {
                _ffmpegSource.PlaybackSession = null;
                _ffmpegSource.Dispose();
            }
            catch { }
            _ffmpegSource = null;
        }

        GC.Collect(1, GCCollectionMode.Optimized);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override bool IsMediaLoaded() =>
        !_isDisposed
        && Player is not null
        && (_ffmpegSource is not null || Player.PlaybackSession is not null);

    /// <inheritdoc/>
    protected override Task OnPauseAsync()
    {
        if (Player is not null)
        {
            try
            {
                Player.Pause();
            }
            catch { }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnTogglePlayPauseAsync()
    {
        if (!IsMediaLoaded() || Player?.PlaybackSession is null)
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
            Player?.Play();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnSeekAsync(TimeSpan position)
    {
        if (Player is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            if (Player.PlaybackSession?.CanSeek == false && !Player.CanSeek)
            {
                _appViewModel.ShowTipCommand.Execute((Resources.CanNotSeek, InfoType.Warning));
                return Task.CompletedTask;
            }

            _hasEnded = false;
            Player.Position = position;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "执行 Seek 失败.");
        }

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

        CanvasRenderTarget renderTarget = new(
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
        // 1. 无缝秒切：若命中预载源且已有 Player，直接替换 Player.Source，不销毁 Player、不解绑 Element，0 延迟无缝瞬播
        if (
            Player is not null
            && !string.IsNullOrEmpty(_videoUrl)
            && TryConsumePreloadedSource(_videoUrl, out FFmpegMediaSource? preloaded)
        )
        {
            try
            {
                if (_ffmpegSource is not null)
                {
                    try
                    {
                        _ffmpegSource.PlaybackSession = null;
                        _ffmpegSource.Dispose();
                    }
                    catch { }
                    _ffmpegSource = null;
                }

                _ffmpegSource = preloaded;
                if (_ffmpegSource.Duration > TimeSpan.Zero)
                {
                    Duration = Convert.ToInt32(_ffmpegSource.Duration.TotalSeconds);
                }

                MediaPlaybackItem playbackItem = _ffmpegSource.CreateMediaPlaybackItem();
                Player.Source = playbackItem;
                if (_autoPlay)
                {
                    Player.Play();
                }

                _logger.LogInformation("命中下一集后台预载源，实现 0 秒无缝瞬播！");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无缝应用预载源失败，回退常规加载流程.");
            }
        }

        // 2. 常规加载流程（首次播放或未命中预载）
        if (Player is null)
        {
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
                UpdateState(PlayerState.Failed);
            }
        }
        else
        {
            // 复用已有 Player，直接替换媒体源，避免黑屏与重新挂载开销
            if (_ffmpegSource is not null)
            {
                try
                {
                    _ffmpegSource.PlaybackSession = null;
                    _ffmpegSource.Dispose();
                }
                catch { }
                _ffmpegSource = null;
            }

            try
            {
                await LoadMediaSourceAsync();
                if (_autoPlay)
                {
                    Player.Play();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FFmpeg 加载媒体失败.");
                UpdateState(PlayerState.Failed);
            }
        }
    }

    private MediaSourceConfig CreateMediaSourceConfig(
        string? decryptionKey = null,
        string? referer = null
    )
    {
        MediaSourceConfig config = new();
        config.Video.VideoDecoderMode = VideoDecoderMode.Automatic;
        config.General.FastSeek = true;
        config.General.ReadAheadBufferEnabled = true;
        config.General.ReadAheadBufferDuration = TimeSpan.FromSeconds(15);
        config.General.ReadAheadBufferSize = 16 * 1024 * 1024;

        string? refUrl = !string.IsNullOrEmpty(referer) ? referer : CustomReferer;
        StringBuilder headerSb = new();
        headerSb.Append($"Referer: {refUrl}\r\n");

        string headerStr = headerSb.ToString();
        config.FFmpegOptions["headers"] = headerStr;
        config.FFmpegOptions["referer"] = refUrl;
        config.FFmpegOptions["protocol_whitelist"] = "file,http,https,tcp,tls,crypto";
        config.FFmpegOptions["reconnect"] = 1;
        config.FFmpegOptions["reconnect_streamed"] = 1;
        config.FFmpegOptions["reconnect_on_network_error"] = 1;

        string? key = !string.IsNullOrEmpty(decryptionKey) ? decryptionKey : DecryptionKey;
        if (!string.IsNullOrEmpty(key))
        {
            config.FFmpegOptions["decryption_key"] = key;
        }

        return config;
    }

    private async Task LoadMediaSourceAsync()
    {
        MediaSourceConfig config = CreateMediaSourceConfig();

        string? fallbackUrl = string.IsNullOrEmpty(_videoUrl) ? _audioUrl : _videoUrl;
        if (!string.IsNullOrEmpty(fallbackUrl))
        {
            _ffmpegSource = await FFmpegMediaSource.CreateFromUriAsync(fallbackUrl, config);
        }

        if (_ffmpegSource is not null)
        {
            if (_ffmpegSource.Duration > TimeSpan.Zero)
            {
                Duration = Convert.ToInt32(_ffmpegSource.Duration.TotalSeconds);
            }

            MediaPlaybackItem playbackItem = _ffmpegSource.CreateMediaPlaybackItem();
            Player.Source = playbackItem;
        }
    }
}
