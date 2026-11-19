using CommunityToolkit.Mvvm.ComponentModel;
using Danmaku.Core.Enums;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using Windows.UI;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;
using ZLogger;
using DanmakuCoreItem = Danmaku.Core.Models.DanmakuItem;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 弹幕视图模型.
/// </summary>
public sealed partial class DanmakuViewModel : ViewModelBase
{
    private readonly IDanmakuService _danmakuService;
    private readonly AppViewModel _appViewModel;
    private readonly ILogger<DanmakuViewModel> _logger;
    private readonly IFontToolkit _fontToolkit;
    private readonly AppSettings _settings;

    private string _dramaId = string.Empty;
    private string _videoId = string.Empty;
    private string _loadedVideoId = string.Empty;
    private int _position;
    private int _duration;
    private CancellationTokenSource? _loadCancellationTokenSource;

    /// <summary>
    /// 当前分集弹幕是否已成功加载.
    /// </summary>
    public bool HasLoaded => !string.IsNullOrEmpty(_loadedVideoId) && _loadedVideoId == _videoId;

    /// <summary>
    /// 当前已加载的弹幕列表缓存.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<DanmakuCoreItem> CurrentDanmakus { get; set; } = [];

    [ObservableProperty]
    public partial bool IsShowDanmaku { get; set; }

    [ObservableProperty]
    public partial bool CanShowDanmaku { get; set; }

    [ObservableProperty]
    public partial double DanmakuOpacity { get; set; }

    [ObservableProperty]
    public partial double DanmakuFontSize { get; set; }

    [ObservableProperty]
    public partial string DanmakuFontFamily { get; set; }

    [ObservableProperty]
    public partial double DanmakuArea { get; set; }

    [ObservableProperty]
    public partial double DanmakuSpeed { get; set; }

    [ObservableProperty]
    public partial double ExtraSpeed { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsDanmakuLimit { get; set; }

    [ObservableProperty]
    public partial bool IsDanmakuBold { get; set; }

    [ObservableProperty]
    public partial bool IsStandardSize { get; set; }

    [ObservableProperty]
    public partial DanmakuLocation Location { get; set; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial List<DanmakuLocation> Locations { get; set; }

    [ObservableProperty]
    public partial List<Color> Colors { get; set; }

    [ObservableProperty]
    public partial List<string> Fonts { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    /// <summary>
    /// 当弹幕列表添加时触发的事件.
    /// </summary>
    public event EventHandler<IReadOnlyList<DanmakuCoreItem>>? ListAdded;

    /// <summary>
    /// 速度加成改变时触发的事件.
    /// </summary>
    public event EventHandler? ExtraSpeedChanged;

    /// <summary>
    /// 当请求清除弹幕时触发的事件.
    /// </summary>
    public event EventHandler? RequestClearDanmaku;

    /// <summary>
    /// 当请求重绘弹幕时触发的事件.
    /// </summary>
    public event EventHandler? RequestRedrawDanmaku;

    /// <summary>
    /// 当进度更新时触发的事件.
    /// </summary>
    public event EventHandler<int>? ProgressChanged;

    /// <summary>
    /// 暂停弹幕.
    /// </summary>
    public event EventHandler? PauseDanmaku;

    /// <summary>
    /// 恢复弹幕.
    /// </summary>
    public event EventHandler? ResumeDanmaku;

    /// <summary>
    /// 请求添加单条弹幕.
    /// </summary>
    public event EventHandler<string>? RequestAddSingleDanmaku;

    /// <summary>
    /// 请求重置样式.
    /// </summary>
    public event EventHandler? RequestResetStyle;

    /// <summary>
    /// 请求展示冻结帧以防切集闪黑.
    /// </summary>
    public event EventHandler<(byte[] Pixels, int Width, int Height)>? RequestShowFrozenFrame;

    /// <summary>
    /// Initializes a new instance of the <see cref="DanmakuViewModel"/> class.
    /// </summary>
    public DanmakuViewModel(
        IDanmakuService danmakuService,
        AppViewModel appViewModel,
        ILogger<DanmakuViewModel> logger,
        ISettingsService settingsService,
        IFontToolkit fontToolkit
    )
    {
        _danmakuService = danmakuService;
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
    public void ResetData(string dramaId, string videoId)
    {
        ClearAll();
        _dramaId = dramaId;
        _videoId = videoId;
        _loadedVideoId = string.Empty;
        ResetData();
    }

    /// <summary>
    /// 重置数据.
    /// </summary>
    public void ResetData()
    {
        _ = ReloadFontsAsync();
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
    /// 展示切集冻结帧（用于防闪黑过渡）.
    /// </summary>
    public void ShowFrozenFrame(byte[] pixels, int width, int height)
    {
        RequestShowFrozenFrame?.Invoke(this, (pixels, width, height));
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
        _dramaId = string.Empty;
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
    public bool IsEmpty() => string.IsNullOrEmpty(_dramaId) || string.IsNullOrEmpty(_videoId);

    public async Task LoadDanmakusAsync(int duration)
    {
        if (
            IsLoading
            || string.IsNullOrEmpty(_dramaId)
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
        var currentLoadingDramaId = _dramaId;

        try
        {
            long durationMs = duration > 0 ? duration * 1000L : 3600000L;
            DanmakuPage page = await _danmakuService.GetDanmakuAsync(
                currentLoadingDramaId,
                currentLoadingVideoId,
                0,
                durationMs,
                cancellationToken: token
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
                        StartMs = (uint)Math.Max(0, item.OffsetMs),
                        Mode = DanmakuMode.Rolling,
                        TextColor = Microsoft.UI.Colors.White,
                        BaseFontSize = 24,
                        HasOutline = true,
                    }),
                ];

                CurrentDanmakus = danmakus;
                CanShowDanmaku = true;
                _logger.ZLogInformation(
                    $"[Danmaku] 成功获取弹幕: 剧集={currentLoadingDramaId}, 分集={currentLoadingVideoId}, 弹幕条数={danmakus.Count}"
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
                $"加载短剧 {currentLoadingDramaId} | {currentLoadingVideoId} 的弹幕失败."
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SendDanmakuAsync(string text)
    {
        if (
            string.IsNullOrWhiteSpace(text)
            || string.IsNullOrEmpty(_dramaId)
            || string.IsNullOrEmpty(_videoId)
        )
        {
            return;
        }

        try
        {
            PhoenixOperationResult result = await _danmakuService.SendDanmakuAsync(
                _dramaId,
                _videoId,
                text.Trim()
            );

            if (result.Succeeded)
            {
                RequestAddSingleDanmaku?.Invoke(this, text.Trim());
                _appViewModel.ShowTip("弹幕发送成功", InfoType.Success);
            }
            else
            {
                _appViewModel.ShowTip(result.Message ?? "弹幕发送失败", InfoType.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"弹幕发送失败.");
            _appViewModel.ShowTip("弹幕发送出现异常", InfoType.Error);
        }
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
        Color = Helpers.ColorHelper.HexToColor(
            string.IsNullOrEmpty(_settings.DanmakuColor)
                ? Microsoft.UI.Colors.White.ToString()
                : _settings.DanmakuColor
        );
        IsStandardSize = _settings.IsDanmakuStandardSize;
        ResetStyle();
    }

    /// <summary>
    /// 重新加载系统字体列表.
    /// </summary>
    public async Task ReloadFontsAsync()
    {
        IReadOnlyList<string> fonts = await _fontToolkit.GetFontsAsync();
        Fonts = [.. fonts];
        string localFont = _settings.DanmakuFontFamily;
        if (!Fonts.Contains(localFont))
        {
            DanmakuFontFamily = "Segoe UI";
        }
    }

    /// <summary>
    /// 重新加载字体 (同步触发).
    /// </summary>
    public void ReloadFonts() => _ = ReloadFontsAsync();

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
