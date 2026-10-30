using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.Toolkits;
using Richasy.BiliKernel.Models.Media;
using Windows.Media.Playback;

namespace PhoenixReadCopilot.UI.ViewModels.Core;

/// <summary>
/// FFmpeg 播放器视图模型.
/// </summary>
public sealed partial class FFmpegPlayerViewModel : PlayerViewModelBase
{
    private MediaPlayerElement? _element;
    private DashSegmentInformation? _videoSegment;
    private DashSegmentInformation? _audioSegment;
    private int _customDuration;
    private FFmpegInteropX.FFmpegMediaSource? _ffmpegSource;
    private System.Net.Sockets.TcpListener? _manifestListener;
    private CancellationTokenSource? _manifestServerCts;
    private bool _isDisposed;

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
        DashSegmentInformation? video,
        DashSegmentInformation? audio,
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
        player.Volume = SettingsToolkit.ReadLocalSetting(SettingNames.PlayerVolume, 100) / 100.0;

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
            if (!IsMediaLoaded())
            {
                return;
            }

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

            var state = sender.PlaybackSession.PlaybackState switch
            {
                MediaPlaybackState.Opening => PlayerState.Opening,
                MediaPlaybackState.Buffering => PlayerState.Buffering,
                MediaPlaybackState.Playing => PlayerState.Playing,
                MediaPlaybackState.Paused => PlayerState.Paused,
                _ => PlayerState.None,
            };

            UpdateState(state);
        });
    }

    private void OnMediaPlayerOpened(MediaPlayer sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var session = sender.PlaybackSession;
            if (session is null)
            {
                return;
            }

            if (_ffmpegSource is not null)
            {
                _ffmpegSource.PlaybackSession = session;
            }

            sender.PlaybackSession.PlaybackRate = SettingsToolkit.ReadLocalSetting(
                SettingNames.PlayerSpeed,
                1d
            );
            _speedAction?.Invoke(sender.PlaybackSession.PlaybackRate);

            var duration = session.NaturalDuration.TotalSeconds;
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
            var duration = sender.NaturalDuration.TotalSeconds;
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

            var duration = sender.NaturalDuration.TotalSeconds;
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

            var position = sender.Position.TotalSeconds;
            if (duration > 0 && position > duration)
            {
                return;
            }

            UpdatePosition(
                Convert.ToInt32(position),
                duration > 0 ? Convert.ToInt32(duration) : default
            );
        });
    }
}
