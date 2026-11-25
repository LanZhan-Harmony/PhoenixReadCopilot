using CommunityToolkit.Mvvm.DependencyInjection;
using Danmaku.Core;
using Danmaku.Core.Enums;
using Danmaku.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Danmaku;

/// <summary>
/// 视频弹幕面板.
/// </summary>
internal sealed partial class VideoDanmakuPanel : UserControl
{
    public DanmakuViewModel? ViewModel
    {
        get => (DanmakuViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DanmakuViewModel),
        typeof(VideoDanmakuPanel),
        new PropertyMetadata(null, (d, e) =>
            ((VideoDanmakuPanel)d).OnViewModelChanged(
                (DanmakuViewModel?)e.OldValue,
                (DanmakuViewModel?)e.NewValue
            )
        )
    );

    private readonly ILogger<DanmakuRenderer> _danmakuLogger = Ioc.Default.GetRequiredService<
        ILogger<DanmakuRenderer>
    >();
    private List<DanmakuItem> _cachedDanmakus = [];
    private int _lastProgress;
    private DanmakuRenderer? _danmakuRenderer;

    public VideoDanmakuPanel()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (ViewModel.CurrentDanmakus.Count > 0)
        {
            _cachedDanmakus =
            [
                .. _cachedDanmakus.Concat(ViewModel.CurrentDanmakus).DistinctBy(x => x.Id),
            ];
        }

        EnsureRendererCreated();
        if (_danmakuRenderer is not null && _cachedDanmakus.Count > 0)
        {
            _danmakuRenderer.AddDanmakuList(_cachedDanmakus);
            _danmakuRenderer.UpdateTime(Convert.ToUInt32(_lastProgress * 1000));
        }

        if (ViewModel is { IsPaused: false, IsShowDanmaku: true })
        {
            _danmakuRenderer?.Resume();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.ListAdded -= OnDanmakuListAddedAsync;
            ViewModel.RequestClearDanmaku -= OnRequestClearDanmaku;
            ViewModel.ProgressChanged -= OnProgressChanged;
            ViewModel.PauseDanmaku -= OnPauseDanmaku;
            ViewModel.ResumeDanmaku -= OnResumeDanmaku;
            ViewModel.RequestAddSingleDanmaku -= OnRequestAddSingleDanmaku;
            ViewModel.RequestResetStyle -= OnRequestResetStyle;
            ViewModel.ExtraSpeedChanged -= OnExtraSpeedChanged;
            ViewModel.RequestRedrawDanmaku -= OnRedrawDanmaku;
            ViewModel.RequestShowFrozenFrame -= OnRequestShowFrozenFrame;
        }

        _danmakuRenderer?.Dispose();
        _danmakuRenderer = null;
    }

    private void OnViewModelChanged(DanmakuViewModel? oldValue, DanmakuViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.ListAdded -= OnDanmakuListAddedAsync;
            oldValue.RequestClearDanmaku -= OnRequestClearDanmaku;
            oldValue.ProgressChanged -= OnProgressChanged;
            oldValue.PauseDanmaku -= OnPauseDanmaku;
            oldValue.ResumeDanmaku -= OnResumeDanmaku;
            oldValue.RequestAddSingleDanmaku -= OnRequestAddSingleDanmaku;
            oldValue.RequestResetStyle -= OnRequestResetStyle;
            oldValue.ExtraSpeedChanged -= OnExtraSpeedChanged;
            oldValue.RequestRedrawDanmaku -= OnRedrawDanmaku;
            oldValue.RequestShowFrozenFrame -= OnRequestShowFrozenFrame;
        }

        if (newValue is null)
        {
            return;
        }

        newValue.ListAdded += OnDanmakuListAddedAsync;
        newValue.RequestClearDanmaku += OnRequestClearDanmaku;
        newValue.ProgressChanged += OnProgressChanged;
        newValue.PauseDanmaku += OnPauseDanmaku;
        newValue.ResumeDanmaku += OnResumeDanmaku;
        newValue.RequestAddSingleDanmaku += OnRequestAddSingleDanmaku;
        newValue.RequestResetStyle += OnRequestResetStyle;
        newValue.ExtraSpeedChanged += OnExtraSpeedChanged;
        newValue.RequestRedrawDanmaku += OnRedrawDanmaku;
        newValue.RequestShowFrozenFrame += OnRequestShowFrozenFrame;

        if (newValue.CurrentDanmakus.Count > 0)
        {
            _cachedDanmakus =
            [
                .. _cachedDanmakus.Concat(newValue.CurrentDanmakus).DistinctBy(x => x.Id),
            ];
            EnsureRendererCreated();
            _danmakuRenderer?.AddDanmakuList(_cachedDanmakus);
            _danmakuRenderer?.UpdateTime(Convert.ToUInt32(_lastProgress * 1000));
        }

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (IsLoaded && ViewModel == newValue)
            {
                ResetDanmakuStyle();
            }
        });
    }

    private void OnDanmakuListAddedAsync(object? sender, IReadOnlyList<DanmakuItem> e)
    {
        _cachedDanmakus = [.. _cachedDanmakus.Concat(e).DistinctBy(x => x.Id)];
        EnsureRendererCreated();
        _danmakuRenderer?.AddDanmakuList(e as List<DanmakuItem> ?? [.. e]);
        _danmakuRenderer?.UpdateTime(Convert.ToUInt32(_lastProgress * 1000));
        if (ViewModel is { IsPaused: false, IsShowDanmaku: true })
        {
            _danmakuRenderer?.Resume();
        }
    }

    private void OnRequestClearDanmaku(object? sender, EventArgs e)
    {
        _cachedDanmakus.Clear();
        _lastProgress = 0;
        _danmakuRenderer?.Clear();
    }

    private void OnRequestAddSingleDanmaku(object? sender, string e)
    {
        if (ViewModel is null)
        {
            return;
        }

        DanmakuItem model = new()
        {
            StartMs = Convert.ToUInt32(_lastProgress * 1000),
            Mode = (DanmakuMode)(int)ViewModel.Location,
            TextColor = ViewModel.Color,
            BaseFontSize = ViewModel.IsStandardSize ? 20 : 24,
            Text = e,
            HasOutline = true,
        };

        _danmakuRenderer?.AddRealtimeDanmaku(model, true);
    }

    private void OnRequestResetStyle(object? sender, EventArgs e) => ResetDanmakuStyle();

    private void OnExtraSpeedChanged(object? sender, EventArgs e) => ResetSpeed();

    private void OnProgressChanged(object? sender, int e)
    {
        _lastProgress = e;
        if (_danmakuRenderer is null)
        {
            EnsureRendererCreated();
        }
        _danmakuRenderer?.UpdateTime(Convert.ToUInt32(e * 1000));
    }

    private void OnResumeDanmaku(object? sender, EventArgs e) => _danmakuRenderer?.Resume();

    private void OnPauseDanmaku(object? sender, EventArgs e) => _danmakuRenderer?.Pause();

    private void OnRedrawDanmaku(object? sender, EventArgs e) => Redraw();

    private void OnRequestShowFrozenFrame(object? sender, (byte[] Pixels, int Width, int Height) e)
    {
        EnsureRendererCreated();
        _danmakuRenderer?.SetFrozenFrame(e.Pixels, e.Width, e.Height);
    }

    private void ResetDanmakuStyle()
    {
        if (_danmakuRenderer is null || ViewModel is null)
        {
            return;
        }

        if (!ViewModel.IsShowDanmaku)
        {
            _danmakuRenderer.Pause();
        }

        double finalSpeed = ViewModel.DanmakuSpeed * 5 * ViewModel.ExtraSpeed;

        DanmakuConfiguration config = DanmakuConfiguration.CreateDefault();
        config.TextOpacity = ViewModel.DanmakuOpacity;
        config.FontSizeScale = ViewModel.DanmakuFontSize;
        config.RollingAreaRatio = (float)ViewModel.DanmakuArea;
        config.FontFamilyName = ViewModel.DanmakuFontFamily ?? "Segoe UI";
        config.IsTextBold = ViewModel.IsDanmakuBold;
        config.OutlineSize = 1;
        config.RollingSpeedBase = (float)finalSpeed;

        _danmakuRenderer.SetDanmakuEnabledType(
            isRollingEnabled: true,
            isTopEnabled: true,
            isBottomEnabled: true
        );
        _danmakuRenderer.UpdateConfiguration(config);
    }

    private void ResetSpeed()
    {
        if (ViewModel is null)
        {
            return;
        }

        double finalSpeed = ViewModel.DanmakuSpeed * 5 * ViewModel.ExtraSpeed;
        _danmakuRenderer?.UpdateConfiguration(
            new DanmakuConfiguration { RollingSpeedBase = (float)finalSpeed }
        );
    }

    private void EnsureRendererCreated()
    {
        if (_danmakuRenderer is not null)
        {
            return;
        }

        if (RootGrid is null || RootGrid.XamlRoot is null)
        {
            return;
        }

        _danmakuRenderer = new DanmakuRenderer(RootGrid, _danmakuLogger);
        ResetDanmakuStyle();
        if (_cachedDanmakus.Count > 0)
        {
            _danmakuRenderer.AddDanmakuList(_cachedDanmakus);
            _danmakuRenderer.UpdateTime(Convert.ToUInt32(_lastProgress * 1000));
        }

        if (ViewModel is { IsPaused: false, IsShowDanmaku: true })
        {
            _danmakuRenderer?.Resume();
        }
    }

    private void Redraw(bool force = false)
    {
        if (ViewModel is null)
        {
            return;
        }

        DispatcherQueue?.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (_danmakuRenderer is null)
            {
                EnsureRendererCreated();
            }
            else
            {
                _danmakuRenderer.Clear();
            }

            if (_cachedDanmakus.Count != 0)
            {
                _danmakuRenderer?.AddDanmakuList(_cachedDanmakus);
            }

            _danmakuRenderer?.UpdateTime(Convert.ToUInt32(_lastProgress * 1000));
            ResetDanmakuStyle();
        });
    }
}
