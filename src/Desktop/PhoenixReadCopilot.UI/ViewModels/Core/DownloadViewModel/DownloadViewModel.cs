using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.BiliKernel.Models.Media;
using Windows.ApplicationModel.DataTransfer;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Core;

/// <summary>
/// 下载视图模型.
/// </summary>
public sealed partial class DownloadViewModel : ViewModelBase
{
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadViewModel"/> class.
    /// </summary>
    public DownloadViewModel(
        ILogger<DownloadViewModel> logger,
        IBiliTokenResolver tokenResolver,
        IBiliCookiesResolver cookiesResolver,
        AppViewModel appViewModel,
        ISettingsService settingsService
    )
    {
        _logger = logger;
        _tokenResolver = tokenResolver;
        _cookiesResolver = cookiesResolver;
        _appViewModel = appViewModel;
        _settings = settingsService.Settings;
        _bbdownPath = _appViewModel.BBDownPath;
        _ffmpegPath = _appViewModel.FFmpegPath;
        var localDownloadFolder = _settings.DownloadFolder;
        _downloadPath =
            string.IsNullOrEmpty(localDownloadFolder) || !Directory.Exists(localDownloadFolder)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    "Bili Downloads"
                )
                : localDownloadFolder;
        if (!Directory.Exists(_downloadPath))
        {
            Directory.CreateDirectory(_downloadPath);
        }

        _openFolderAfterDownload = _settings.OpenFolderAfterDownload;
    }

    /// <summary>
    /// 初始化基础数据.
    /// </summary>
    public void InitializeMetas(
        string url,
        IReadOnlyCollection<PlayerFormatInformation> formats,
        IReadOnlyCollection<VideoPart>? parts = default,
        int currentPart = 1
    )
    {
        _currentUrl = url;
        _currentPartIndex = currentPart;
        Formats = [.. formats];
        Parts = parts is null ? default : [.. parts];
        MetaInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 初始化基础数据.
    /// </summary>
    public void InitializeMetas(
        string epUrl,
        string ssUrl,
        IReadOnlyCollection<PlayerFormatInformation> formats,
        IReadOnlyCollection<EpisodeInformation>? episodes = default,
        int currentEpisode = 1
    )
    {
        _currentUrl = epUrl;
        _parentUrl = ssUrl;
        _currentPartIndex = currentEpisode;
        Formats = [.. formats];
        Episodes = episodes is null ? default : [.. episodes];
        MetaInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 清除数据.
    /// </summary>
    public void Clear()
    {
        _currentUrl = string.Empty;
        _parentUrl = string.Empty;
        _currentPartIndex = 0;
        Formats = null;
        Parts = null;
        Episodes = null;
    }

    private bool UseExternal() => _settings.UseExternalBBDown;

    /// <summary>
    /// 下载视频.
    /// </summary>
    [RelayCommand]
    private void DownloadWithFormat(PlayerFormatInformation? format = default)
    {
        var basicCommand = GetBasicCommand();
        if (_currentPartIndex > 1 && !_currentUrl.Contains("/ep"))
        {
            basicCommand += $" --select-page {_currentPartIndex}";
        }

        var quality = UseExternal() ? format.Description : format.Quality.ToString();
        var command = $"{basicCommand} -q \"{quality}\" \"{_currentUrl}\"";
        LaunchDownloadProcess(command);
    }

    [RelayCommand]
    private void DownloadCover()
    {
        var command = $"{GetBasicCommand(false)} --cover-only \"{_currentUrl}\"";
        LaunchDownloadProcess(command);
    }

    [RelayCommand]
    private void DownloadDanmaku()
    {
        var command = $"{GetBasicCommand(false)} --danmaku-only \"{_currentUrl}\"";
        LaunchDownloadProcess(command);
    }

    [RelayCommand]
    private void DownloadSubtitle()
    {
        var isAISubtitleFiltered = _settings.FilterAISubtitle;
        var subCommand = isAISubtitleFiltered ? "--sub-only" : "--sub-only --skip-ai False";
        var command = $"{GetBasicCommand(false)} {subCommand} \"{_currentUrl}\"";
        LaunchDownloadProcess(command);
    }

    [RelayCommand]
    private void BatchDownloadAllParts()
    {
        var url = _currentUrl.Contains("/ep") ? _parentUrl : _currentUrl;
        var basicCommand = GetBasicCommand();
        basicCommand += " --select-page ALL";
        var command = $"{basicCommand} \"{url}\"";
        LaunchDownloadProcess(command);
    }

    [RelayCommand]
    private void BatchDownloadSelectedParts(string selection)
    {
        var url = _currentUrl.Contains("/ep") ? _parentUrl : _currentUrl;
        var basicCommand = GetBasicCommand();
        basicCommand += $" --select-page {selection}";
        var command = $"{basicCommand} \"{url}\"";
        LaunchDownloadProcess(command);
    }

    private string GetBasicCommand(bool danmakuEnabled = true)
    {
        var token = _tokenResolver.GetToken().AccessToken;
        var cookie = _cookiesResolver.GetCookieString();
        var preferCodec = _settings.PreferCodec;
        var codec = preferCodec switch
        {
            PreferCodecType.H264 => "avc",
            PreferCodecType.H265 => "hevc",
            _ => "av1",
        };

        var cmd = $"-e {codec} --work-dir \"{_downloadPath}\" -ua \"{VideoUserAgent}\"";
        var copyCommandOnly = _settings.OnlyCopyCommandWhenDownload;
        var withoutCredential = _settings.WithoutCredentialWhenGenDownloadCommand;
        if (!UseExternal())
        {
            cmd += $" -app --ffmpeg-path \"{_ffmpegPath}\"";
        }

        if (!(UseExternal() && withoutCredential))
        {
            cmd += $" --cookie \"{cookie}\" -token \"{token}\"";
        }

        if (danmakuEnabled)
        {
            var alsoDownDanmaku = _settings.DownloadWithDanmaku;
            if (alsoDownDanmaku)
            {
                cmd += " -dd";
            }
        }

        return cmd;
    }

    private void LaunchDownloadProcess(string command)
    {
        var useExternalBBDown = _settings.UseExternalBBDown;
        var copyCommandOnly = _settings.OnlyCopyCommandWhenDownload;
        var fileName = useExternalBBDown ? "BBDown" : _bbdownPath;
        if (useExternalBBDown && copyCommandOnly)
        {
            var cmd = $"{fileName} {command}";
            var dp = new DataPackage();
            dp.SetText(cmd);
            Clipboard.SetContent(dp);
            _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
            return;
        }

        _ = Task.Run(() =>
        {
            var startInfo = new ProcessStartInfo(fileName, command);
            var process = Process.Start(startInfo);
            if (_openFolderAfterDownload)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Process.Start("explorer.exe", _downloadPath);
                }
            }
        });
    }
}
