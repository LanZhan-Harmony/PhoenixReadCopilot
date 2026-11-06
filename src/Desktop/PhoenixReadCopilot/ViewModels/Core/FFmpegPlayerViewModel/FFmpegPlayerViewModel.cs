using System.Net.Sockets;
using FFmpegInteropX;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
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
    private MediaSegmentInformation? _videoSegment;
    private MediaSegmentInformation? _audioSegment;
    private int _customDuration;
    private FFmpegMediaSource? _ffmpegSource;
    private TcpListener? _manifestListener;
    private CancellationTokenSource? _manifestServerCts;
    private bool _isDisposed;
    private bool _hasEnded;

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

    /// <summary>
    /// 注入播放片段.
    /// </summary>
    public void InjectSegments(
        MediaSegmentInformation? video,
        MediaSegmentInformation? audio,
        int duration = 0
    )
    {
        _videoSegment = video;
        _audioSegment = audio;
        _customDuration = duration;
        if (duration > 0)
        {
            Duration = duration;
        }

        UpdateState(PlayerState.None);
    }

    private MediaPlayer CreatePlayer()
    {
        _isDisposed = false;
        var player = new MediaPlayer();
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

            var message = string.Empty;
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
                var session = sender.PlaybackSession;
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
            Windows.Media.Playback.MediaPlaybackSession? session = null;
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
                if (_ffmpegSource is not null)
                {
                    _ffmpegSource.PlaybackSession = session;
                }
            }
            catch { }

            sender.PlaybackSession.PlaybackRate = _settings.PlayerSpeed;
            _speedAction?.Invoke(sender.PlaybackSession.PlaybackRate);

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

            if (duration <= 0 && _customDuration > 0)
            {
                duration = _customDuration;
            }

            if (duration > 0)
            {
                UpdatePosition(Position, Convert.ToInt32(duration));
            }

            if (Position > 0)
            {
                session.Position = TimeSpan.FromSeconds(Position);
            }

            session.PositionChanged -= OnMediaPositionChanged;
            session.PositionChanged += OnMediaPositionChanged;
            session.NaturalDurationChanged -= OnMediaNaturalDurationChanged;
            session.NaturalDurationChanged += OnMediaNaturalDurationChanged;
        });
    }

    private void OnMediaNaturalDurationChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
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

            if (duration <= 0 && _customDuration > 0)
            {
                duration = _customDuration;
            }

            if (duration > 0)
            {
                UpdatePosition(Position, Convert.ToInt32(duration));
            }
        });
    }

    private void OnMediaPositionChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
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

            if (duration <= 0 && _customDuration > 0)
            {
                duration = _customDuration;
            }

            if (duration <= 0 && Duration > 0)
            {
                duration = Duration;
            }

            if (duration > 0 && position >= duration)
            {
                if (!_hasEnded)
                {
                    _hasEnded = true;
                    UpdatePosition(Convert.ToInt32(duration), Convert.ToInt32(duration));
                    ReachEnd();
                }
                return;
            }

            UpdatePosition(
                Convert.ToInt32(position),
                duration > 0 ? Convert.ToInt32(duration) : default
            );
        });
    }
}
