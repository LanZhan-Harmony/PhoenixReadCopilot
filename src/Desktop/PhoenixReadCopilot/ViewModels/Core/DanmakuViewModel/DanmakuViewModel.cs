using CommunityToolkit.Mvvm.Input;
using Danmaku.Core.Enums;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Toolkits;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;
using ZLogger;
using DanmakuCoreItem = Danmaku.Core.Models.DanmakuItem;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 弹幕视图模型.
/// </summary>
public sealed partial class DanmakuViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DanmakuViewModel"/> class.
    /// </summary>
    public DanmakuViewModel(
        PhoenixReadApiClient apiClient,
        AppViewModel appViewModel,
        ILogger<DanmakuViewModel> logger,
        ISettingsService settingsService,
        IFontToolkit fontToolkit
    )
    {
        _apiClient = apiClient;
        _appViewModel = appViewModel;
        _logger = logger;
        _settings = settingsService.Settings;
        _fontToolkit = fontToolkit;
        Locations = [DanmakuLocation.Scroll, DanmakuLocation.Top, DanmakuLocation.Bottom];
        Colors =
        [
            Microsoft.UI.Colors.White,
            Microsoft.UI.Colors.Red,
            Microsoft.UI.Colors.Orange,
            Microsoft.UI.Colors.Khaki,
            Microsoft.UI.Colors.Yellow,
            Microsoft.UI.Colors.GreenYellow,
            Microsoft.UI.Colors.Green,
            Microsoft.UI.Colors.Blue,
            Microsoft.UI.Colors.Purple,
            Microsoft.UI.Colors.LightBlue,
        ];
    }

    /// <summary>
    /// 重置数据.
    /// </summary>
    public void ResetData(string seriesId, string videoId)
    {
        ClearAll();
        _seriesId = seriesId;
        _videoId = videoId;
        _loadedVideoId = string.Empty;
        ResetData();
    }

    /// <summary>
    /// 重置数据.
    /// </summary>
    public void ResetData()
    {
        ReloadFontsCommand.Execute(null);
        ResetOptions();
    }

    /// <summary>
    /// 更新进度.
    /// </summary>
    public void UpdatePosition(int position)
    {
        _position = position;
        ProgressChanged?.Invoke(this, position);
    }

    /// <summary>
    /// 暂停.
    /// </summary>
    public void Pause()
    {
        IsPaused = true;
        PauseDanmaku?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 恢复.
    /// </summary>
    public void Resume()
    {
        IsPaused = false;
        ResumeDanmaku?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 重新绘制.
    /// </summary>
    public void Redraw()
    {
        RequestRedrawDanmaku?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 清除数据.
    /// </summary>
    public void ClearAll()
    {
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _loadCancellationTokenSource = null;
        _loadedVideoId = string.Empty;
        CurrentDanmakus = [];
        _duration = 0;
        _seriesId = string.Empty;
        _videoId = string.Empty;
        ClearDanmaku();
    }

    /// <summary>
    /// 清理弹幕.
    /// </summary>
    public void ClearDanmaku() => RequestClearDanmaku?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// 重置样式.
    /// </summary>
    public void ResetStyle() => RequestResetStyle?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// 是否为空.
    /// </summary>
    /// <returns>结果.</returns>
    public bool IsEmpty() => string.IsNullOrEmpty(_seriesId) || string.IsNullOrEmpty(_videoId);

    [RelayCommand]
    private async Task LoadDanmakusAsync(int duration)
    {
        if (
            IsLoading
            || string.IsNullOrEmpty(_seriesId)
            || string.IsNullOrEmpty(_videoId)
            || (_loadedVideoId == _videoId && !string.IsNullOrEmpty(_loadedVideoId))
        )
        {
            return;
        }

        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _loadCancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _loadCancellationTokenSource.Token;

        IsLoading = true;
        _duration = duration;
        var currentLoadingVideoId = _videoId;
        var currentLoadingSeriesId = _seriesId;

        try
        {
            long durationMs = duration > 0 ? duration * 1000L : 3600000L;
            DanmakuPage page = await _apiClient.GetDanmakuAsync(
                currentLoadingSeriesId,
                currentLoadingVideoId,
                0,
                durationMs,
                token
            );

            if (token.IsCancellationRequested)
            {
                return;
            }

            _loadedVideoId = currentLoadingVideoId;

            if (page.Items is { Count: > 0 })
            {
                List<DanmakuCoreItem> danmakus =
                [
                    .. page.Items.Select(item => new DanmakuCoreItem
                    {
                        Id = item.Id,
                        Text = item.Text,
                        StartMs = (uint)Math.Max(0, item.TimeMs),
                        Mode = DanmakuMode.Rolling,
                        TextColor = Microsoft.UI.Colors.White,
                        BaseFontSize = 24,
                        HasOutline = true,
                    }),
                ];

                CurrentDanmakus = danmakus;
                CanShowDanmaku = true;
                _logger.ZLogInformation(
                    $"[Danmaku] 成功获取弹幕: 剧集={currentLoadingSeriesId}, 分集={currentLoadingVideoId}, 弹幕条数={danmakus.Count}"
                );
                foreach (DanmakuCoreItem? sample in danmakus.Take(3))
                {
                    _logger.ZLogInformation(
                        $"[Danmaku Sample] Id={sample.Id}, Text='{sample.Text}', StartMs={sample.StartMs}ms ({sample.StartMs / 1000.0:F1}s)"
                    );
                }
                ListAdded?.Invoke(this, danmakus);
            }
            else
            {
                CurrentDanmakus = [];
                _logger.ZLogInformation(
                    $"[Danmaku] 请求完成，分集 {currentLoadingVideoId} 弹幕数量为 0."
                );
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消（如快速切集、分集重置），静默处理不记日志
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"加载短剧 {currentLoadingSeriesId} | {currentLoadingVideoId} 的弹幕失败."
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SendDanmakuAsync(string text)
    {
        if (
            string.IsNullOrWhiteSpace(text)
            || string.IsNullOrEmpty(_seriesId)
            || string.IsNullOrEmpty(_videoId)
        )
        {
            return;
        }

        try
        {
            PhoenixOperationResult result = await _apiClient.SendDanmakuAsync(
                _seriesId,
                _videoId,
                text.Trim()
            );

            if (result.Succeeded)
            {
                RequestAddSingleDanmaku?.Invoke(this, text.Trim());
                _appViewModel.ShowTipCommand.Execute(("弹幕发送成功", InfoType.Success));
            }
            else
            {
                _appViewModel.ShowTipCommand.Execute(
                    (result.Message ?? "弹幕发送失败", InfoType.Warning)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"弹幕发送失败.");
            _appViewModel.ShowTipCommand.Execute(("弹幕发送出现异常", InfoType.Error));
        }
    }

    [RelayCommand]
    private void AddDanmaku(string text)
    {
        CanShowDanmaku = true;
        RequestAddSingleDanmaku?.Invoke(this, text);
    }

    private void ResetOptions()
    {
        IsShowDanmaku = _settings.IsShowDanmaku;
        DanmakuOpacity = _settings.DanmakuOpacity;
        DanmakuFontSize = _settings.DanmakuFontSize;
        DanmakuArea = _settings.DanmakuArea;
        DanmakuSpeed = _settings.DanmakuSpeed;
        DanmakuFontFamily = _settings.DanmakuFontFamily;
        IsDanmakuBold = _settings.IsDanmakuBold;
        IsDanmakuLimit = true;
        Location = _settings.DanmakuLocation;
        Color = AppToolkit.HexToColor(
            string.IsNullOrEmpty(_settings.DanmakuColor)
                ? Microsoft.UI.Colors.White.ToString()
                : _settings.DanmakuColor
        );
        IsStandardSize = _settings.IsDanmakuStandardSize;
        ResetStyle();
    }

    [RelayCommand]
    private async Task ReloadFontsAsync()
    {
        IReadOnlyList<string> fonts = await _fontToolkit.GetFontsAsync();
        Fonts = [.. fonts];
        string localFont = _settings.DanmakuFontFamily;
        if (!Fonts.Contains(localFont))
        {
            DanmakuFontFamily = "Segoe UI";
        }
    }

    partial void OnIsShowDanmakuChanged(bool value)
    {
        if (value)
        {
            Redraw();
        }

        _settings.IsShowDanmaku = value;
    }

    partial void OnDanmakuOpacityChanged(double value) => _settings.DanmakuOpacity = value;

    partial void OnDanmakuFontSizeChanged(double value) => _settings.DanmakuFontSize = value;

    partial void OnIsDanmakuBoldChanged(bool value) => _settings.IsDanmakuBold = value;

    partial void OnDanmakuAreaChanged(double value) => _settings.DanmakuArea = value;

    partial void OnDanmakuSpeedChanged(double value) => _settings.DanmakuSpeed = value;

    partial void OnDanmakuFontFamilyChanged(string value) => _settings.DanmakuFontFamily = value;

    partial void OnIsStandardSizeChanged(bool value) => _settings.IsDanmakuStandardSize = value;

    partial void OnLocationChanged(DanmakuLocation value) => _settings.DanmakuLocation = value;

    partial void OnColorChanged(Windows.UI.Color value) =>
        _settings.DanmakuColor = value.ToString();

    partial void OnExtraSpeedChanged(double value) =>
        ExtraSpeedChanged?.Invoke(this, EventArgs.Empty);
}
