using FFmpegInteropX;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using Windows.Media.Playback;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// FFmpeg 播放器视图模型.
/// </summary>
public sealed partial class FFmpegPlayerViewModel : PlayerViewModelBase
{
    public FFmpegPlayerViewModel(
        ISettingsService settingsService,
        AppViewModel appViewModel,
        ILogger<PlayerViewModelBase> logger,
        DispatcherQueue dispatcherQueue
    )
        : base(settingsService, appViewModel, logger, dispatcherQueue) { }

    private MediaPlayerElement? _element;
    private FFmpegMediaSource? _ffmpegSource;
    private FFmpegMediaSource? _preloadedSource;
    private string? _preloadedUrl;
    private readonly SemaphoreSlim _preloadLock = new(1, 1);
    private bool _isDisposed;
    private bool _hasEnded;

    /// <summary>
    /// 后台预加载下一集 FFmpeg 媒体源 (提前进行网络探测与解密初始化，实现 0 秒无缝秒播).
    /// </summary>
    public async Task PreloadSourceAsync(string videoUrl, string? decryptionKey, string? referer)
    {
        if (string.IsNullOrEmpty(videoUrl) || videoUrl == _preloadedUrl || videoUrl == _videoUrl)
        {
            return;
        }

        await _preloadLock.WaitAsync();
        try
        {
            if (videoUrl == _preloadedUrl)
            {
                return;
            }

            DisposePreloadedSource();

            MediaSourceConfig config = CreateMediaSourceConfig(decryptionKey, referer);
            FFmpegMediaSource source = await FFmpegMediaSource.CreateFromUriAsync(videoUrl, config);
            _preloadedSource = source;
            _preloadedUrl = videoUrl;
            _logger.LogInformation("下一集 FFmpegMediaSource 后台预加载就绪.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预加载下一集 FFmpegMediaSource 失败.");
            DisposePreloadedSource();
        }
        finally
        {
            _preloadLock.Release();
        }
    }

    /// <summary>
    /// 检查并消费已预载的媒体源.
    /// </summary>
    public bool TryConsumePreloadedSource(string videoUrl, out FFmpegMediaSource? source)
    {
        _preloadLock.Wait();
        try
        {
            if (_preloadedSource is not null && _preloadedUrl == videoUrl)
            {
                source = _preloadedSource;
                _preloadedSource = null;
                _preloadedUrl = null;
                return true;
            }

            source = null;
            return false;
        }
        finally
        {
            _preloadLock.Release();
        }
    }

    /// <summary>
    /// 释放预载的媒体源.
    /// </summary>
    public void DisposePreloadedSource()
    {
        if (_preloadedSource is not null)
        {
            try
            {
                _preloadedSource.PlaybackSession = null;
                _preloadedSource.Dispose();
            }
            catch { }
            _preloadedSource = null;
            _preloadedUrl = null;
        }
    }

    /// <summary>
    /// CENC 解密密钥.
    /// </summary>
    public override string? DecryptionKey { get; set; }

    /// <summary>
    /// 媒体播放器.
    /// </summary>
    public MediaPlayer? Player { get; private set; }

    /// <summary>
    /// 初始化.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    public async Task InitializeAsync(MediaPlayerElement element)
    {
        _element = element;
        IsPlayerInitializing = true;
        if (Player is not null)
        {
            await CloseAsync();
        }

        IsPlayerInitializing = false;
        _isInitialized = true;
        RaiseInitializedEvent();
        await TryLoadPlayDataAsync();
    }

    private MediaPlayer CreatePlayer()
    {
        _isDisposed = false;
        MediaPlayer player = new();
        player.CommandManager.IsEnabled = false;
        player.MediaOpened += OnMediaPlayerOpened;
        player.CurrentStateChanged += OnMediaPlayerStateChanged;
        player.MediaFailed += OnMediaPlayerFailed;
        player.MediaEnded += OnMediaPlayerEnded;
        player.Volume = _settings.PlayerVolume / 100.0;

        return player;
    }

    private void OnMediaPlayerFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        if (
            args.ExtendedErrorCode?.HResult == -1072873851
            || args.Error == MediaPlayerError.Unknown
        )
        {
            // 不处理 Shutdown 造成的错误.
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!IsMediaLoaded())
            {
                return;
            }

            string message = string.Empty;
            switch (args.Error)
            {
                case MediaPlayerError.Aborted:
                    message = Resources.Aborted;
                    break;
                case MediaPlayerError.NetworkError:
                    message = Resources.NetworkError;
                    break;
                case MediaPlayerError.DecodingError:
                    message = Resources.DecodingError;
                    break;
                case MediaPlayerError.SourceNotSupported:
                    message = Resources.SourceNotSupported;
                    break;
                default:
                    break;
            }

            UpdateState(PlayerState.Failed);
            _logger.LogError(
                $"播放失败: {args.Error} | {args.ErrorMessage} | {args.ExtendedErrorCode}"
            );
        });
    }

    private void OnMediaPlayerEnded(MediaPlayer sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!IsMediaLoaded() || _hasEnded)
            {
                return;
            }

            try
            {
                sender.Pause();
            }
            catch { }

            _hasEnded = true;
            ReachEnd();
        });
    }

    private void OnMediaPlayerStateChanged(MediaPlayer sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed || Player is null)
            {
                return;
            }

            PlayerState state = PlayerState.None;
            try
            {
                MediaPlaybackSession? session = sender.PlaybackSession;
                if (session is null)
                {
                    return;
                }

                state = session.PlaybackState switch
                {
                    MediaPlaybackState.Opening => PlayerState.Opening,
                    MediaPlaybackState.Buffering => PlayerState.Buffering,
                    MediaPlaybackState.Playing => PlayerState.Playing,
                    MediaPlaybackState.Paused => PlayerState.Paused,
                    _ => PlayerState.None,
                };
            }
            catch
            {
                return;
            }

            UpdateState(state);
        });
    }

    private void OnMediaPlayerOpened(MediaPlayer sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _hasEnded = false;
            MediaPlaybackSession? session = null;
            try
            {
                session = sender.PlaybackSession;
            }
            catch
            {
                return;
            }

            if (session is null)
            {
                return;
            }

            try
            {
                _ffmpegSource?.PlaybackSession = session;
            }
            catch { }

            try
            {
                session.PlaybackRate = _settings.PlayerSpeed;
                _speedAction?.Invoke(session.PlaybackRate);
            }
            catch { }

            double duration = 0;
            try
            {
                if (session.PlaybackState != MediaPlaybackState.None)
                {
                    duration = session.NaturalDuration.TotalSeconds;
                }
            }
            catch
            {
                // 忽略底层 COMException
            }
            if (duration <= 0 && _ffmpegSource?.Duration.TotalSeconds > 0)
            {
                duration = _ffmpegSource.Duration.TotalSeconds;
            }

            if (duration > 0)
            {
                UpdatePosition(Position, Convert.ToInt32(duration));
            }

            if (Position > 0)
            {
                try
                {
                    session.Position = TimeSpan.FromSeconds(Position);
                }
                catch { }
            }

            try
            {
                session.PositionChanged -= OnMediaPositionChanged;
                session.PositionChanged += OnMediaPositionChanged;
                session.NaturalDurationChanged -= OnMediaNaturalDurationChanged;
                session.NaturalDurationChanged += OnMediaNaturalDurationChanged;
            }
            catch { }
        });
    }

    private void OnMediaNaturalDurationChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (_isDisposed || Player is null)
                {
                    return;
                }

                double duration = 0;
                try
                {
                    if (sender.PlaybackState == MediaPlaybackState.None)
                    {
                        return;
                    }

                    duration = sender.NaturalDuration.TotalSeconds;
                }
                catch
                {
                    return;
                }
                if (duration <= 0 && _ffmpegSource?.Duration.TotalSeconds > 0)
                {
                    duration = _ffmpegSource.Duration.TotalSeconds;
                }

                if (duration > 0)
                {
                    UpdatePosition(Position, Convert.ToInt32(duration));
                }
            }
            catch { }
        });
    }

    private void OnMediaPositionChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (_isDisposed || Player is null)
                {
                    return;
                }

                double duration = 0;
                double position = 0;
                try
                {
                    if (sender.PlaybackState == MediaPlaybackState.None)
                    {
                        return;
                    }

                    duration = sender.NaturalDuration.TotalSeconds;
                    position = sender.Position.TotalSeconds;
                }
                catch
                {
                    return;
                }
                if (duration <= 0 && _ffmpegSource?.Duration.TotalSeconds > 0)
                {
                    duration = _ffmpegSource.Duration.TotalSeconds;
                }

                if (duration <= 0 && Duration > 0)
                {
                    duration = Duration;
                }

                if (duration > 0 && position >= duration)
                {
                    if (!_hasEnded)
                    {
                        try
                        {
                            Player?.Pause();
                        }
                        catch { }

                        _hasEnded = true;
                        UpdatePosition(Convert.ToInt32(duration), Convert.ToInt32(duration));
                        ReachEnd();
                    }
                    return;
                }

                UpdatePosition(
                    Convert.ToInt32(position),
                    duration > 0 ? Convert.ToInt32(duration) : 0
                );
            }
            catch { }
        });
    }
}
