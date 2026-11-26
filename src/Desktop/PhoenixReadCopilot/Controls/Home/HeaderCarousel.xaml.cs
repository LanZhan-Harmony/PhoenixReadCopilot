using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Foundation;

namespace PhoenixReadCopilot.Controls.Home;

internal sealed partial class HeaderCarousel : UserControl
{
    private const int MaxTileCount = 10;

    private readonly Random _random = new();
    private readonly DispatcherTimer _selectionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(4000),
    };
    private readonly DispatcherTimer _deselectionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(3000),
    };
    private readonly List<int> _numbers = [];
    private readonly Dictionary<string, bool> _luminanceCache = [with(StringComparer.Ordinal)];
    private HeaderTile? _selectedTile;
    private int _currentIndex;
    private CancellationTokenSource? _tileCts;
    private INotifyCollectionChanged? _subscribedCollection;
    private Storyboard? _titleStoryboard;
    private bool _isPaused;

    /// <summary>
    /// 轮播展示的新剧推荐列表.
    /// </summary>
    public IEnumerable<DramaItemViewModel>? ItemsSource
    {
        get => (IEnumerable<DramaItemViewModel>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable<DramaItemViewModel>),
        typeof(HeaderCarousel),
        new PropertyMetadata(null, (d, e) =>
            ((HeaderCarousel)d).OnItemsSourceChanged(
                e.OldValue as IEnumerable<DramaItemViewModel>,
                e.NewValue as IEnumerable<DramaItemViewModel>
            )
        )
    );

    public HeaderCarousel()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged += OnActualThemeChanged;

        if (ItemsSource is INotifyCollectionChanged notify && _subscribedCollection is null)
        {
            notify.CollectionChanged += OnItemsCollectionChanged;
            _subscribedCollection = notify;
        }

        RebuildTiles();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
        DetachCollectionChanged();
        Pause();
        UnsubscribeToEvents();
        _luminanceCache.Clear();
    }

    /// <summary>
    /// 暂停轮播计时器与标题动画，避免在滚动到下方或进入浮层时持续占用 UI 与合成资源.
    /// </summary>
    public void Pause()
    {
        if (_isPaused)
        {
            return;
        }

        _isPaused = true;
        CancelPendingSelection();
        _selectionTimer.Stop();
        _deselectionTimer.Stop();
        _titleStoryboard?.Stop();
    }

    /// <summary>
    /// 恢复轮播计时器.
    /// </summary>
    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        if (IsLoaded && TilePanel.Children.Count > 0)
        {
            _selectionTimer.Start();
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (
            _selectedTile is { ImageUrl: { Length: > 0 } url }
            && _luminanceCache.TryGetValue(url, out bool isLight)
        )
        {
            ApplyTitleForegroundBrush(ResolveContrastBrush(isLight));
            return;
        }

        ApplyTitleForegroundBrush(ResolveThemeFallbackBrush());
    }

    private void OnItemsSourceChanged(
        IEnumerable<DramaItemViewModel>? oldValue,
        IEnumerable<DramaItemViewModel>? newValue
    )
    {
        DetachCollectionChanged();

        if (IsLoaded && newValue is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged += OnItemsCollectionChanged;
            _subscribedCollection = notify;
        }

        RebuildTiles();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildTiles();
    }

    private void DetachCollectionChanged()
    {
        _subscribedCollection?.CollectionChanged -= OnItemsCollectionChanged;
        _subscribedCollection = null;
    }

    private void RebuildTiles()
    {
        CancelPendingSelection();
        UnsubscribeToEvents();
        _selectedTile = null;
        TilePanel.Children.Clear();
        _luminanceCache.Clear();

        if (ItemsSource is null)
        {
            return;
        }

        ApplyTitleForegroundBrush(ResolveThemeFallbackBrush());

        foreach (DramaItemViewModel drama in ItemsSource.Take(MaxTileCount))
        {
            string description = !string.IsNullOrWhiteSpace(drama.Drama.Description)
                ? drama.Drama.Description
                : (!string.IsNullOrWhiteSpace(drama.Subtitle) ? drama.Subtitle : drama.Category);

            HeaderTile tile = new()
            {
                Header = drama.Title,
                Description = description,
                ImageUrl = drama.CoverUrl ?? string.Empty,
                SampleID = drama.MediaId,
                ViewModel = drama,
                TabIndex = 1,
            };
            TilePanel.Children.Add(tile);
        }

        if (IsLoaded && TilePanel.Children.Count > 0)
        {
            ResetAndShuffle();
            SubscribeToEvents();
            if (!_isPaused)
            {
                _ = SelectNextTileAsync();
            }
        }
    }

    private LinearGradientBrush? ResolveContrastBrush(bool isLightBackground)
    {
        string key = isLightBackground ? "DarkTextGradient" : "LightTextGradient";
        return Resources.TryGetValue(key, out object brush) ? brush as LinearGradientBrush : null;
    }

    private LinearGradientBrush? ResolveThemeFallbackBrush()
    {
        string key = ActualTheme == ElementTheme.Dark ? "LightTextGradient" : "DarkTextGradient";
        return Resources.TryGetValue(key, out object brush) ? brush as LinearGradientBrush : null;
    }

    private void ApplyTitleForegroundBrush(LinearGradientBrush? brush)
    {
        if (brush is not null)
        {
            AnimateTitleGradient(brush);
        }
    }

    private void SubscribeToEvents()
    {
        UnsubscribeToEvents();

        _selectionTimer.Tick += SelectionTimer_Tick;
        _deselectionTimer.Tick += DeselectionTimer_Tick;
        if (!_isPaused)
        {
            _selectionTimer.Start();
        }

        foreach (HeaderTile tile in TilePanel.Children.OfType<HeaderTile>())
        {
            tile.PointerEntered += Tile_PointerEntered;
            tile.PointerExited += Tile_PointerExited;
            tile.GotFocus += Tile_GotFocus;
            tile.LostFocus += Tile_LostFocus;
            tile.Click += Tile_Click;
            tile.ImageLoaded += Tile_ImageLoaded;
        }
    }

    private void UnsubscribeToEvents()
    {
        _selectionTimer.Tick -= SelectionTimer_Tick;
        _deselectionTimer.Tick -= DeselectionTimer_Tick;
        _selectionTimer.Stop();
        _deselectionTimer.Stop();

        foreach (HeaderTile tile in TilePanel.Children.OfType<HeaderTile>())
        {
            tile.PointerEntered -= Tile_PointerEntered;
            tile.PointerExited -= Tile_PointerExited;
            tile.GotFocus -= Tile_GotFocus;
            tile.LostFocus -= Tile_LostFocus;
            tile.Click -= Tile_Click;
            tile.ImageLoaded -= Tile_ImageLoaded;
        }
    }

    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HeaderTile { ViewModel: { } drama })
        {
            drama.Play();
        }
    }

    private void Tile_ImageLoaded(object? sender, EventArgs e)
    {
        if (sender is HeaderTile tile && IsLoaded && tile == _selectedTile)
        {
            _ = UpdateTitleForegroundAsync(tile);
        }
    }

    private void SelectionTimer_Tick(object? sender, object e)
    {
        if (_isPaused)
        {
            return;
        }

        _ = SelectNextTileAsync();
    }

    private void CancelPendingSelection()
    {
        _tileCts?.Cancel();
        _tileCts?.Dispose();
        _tileCts = null;
    }

    private async Task SelectNextTileAsync()
    {
        if (_isPaused || !IsLoaded || TilePanel.Children.Count == 0)
        {
            return;
        }

        CancelPendingSelection();
        _tileCts = new CancellationTokenSource();
        CancellationToken ct = _tileCts.Token;

        int index = GetNextUniqueRandom();
        if (index < 0 || index >= TilePanel.Children.Count)
        {
            return;
        }

        if (TilePanel.Children[index] is HeaderTile tile)
        {
            _selectedTile = tile;

            try
            {
                if (
                    RecommendScrollViewer.ViewportWidth > 0
                    && _selectedTile.ActualWidth > 0
                    && TilePanel.Children.Contains(_selectedTile)
                )
                {
                    GeneralTransform transform = _selectedTile.TransformToVisual(TilePanel);
                    Point center = transform.TransformPoint(
                        new Point(_selectedTile.ActualWidth / 2, _selectedTile.ActualHeight / 2)
                    );
                    RecommendScrollViewer.ChangeView(
                        center.X - (RecommendScrollViewer.ViewportWidth / 2),
                        null,
                        null
                    );
                }

                await Task.Delay(500, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                return;
            }

            if (_isPaused || !IsLoaded || ct.IsCancellationRequested || _selectedTile != tile)
            {
                return;
            }

            SetTileVisuals();
            if (!_isPaused)
            {
                _deselectionTimer.Start();
            }
        }
    }

    private void DeselectionTimer_Tick(object? sender, object e)
    {
        _selectedTile?.IsSelected = false;
        _selectedTile = null;

        _deselectionTimer.Stop();
    }

    private void ResetAndShuffle()
    {
        _numbers.Clear();
        for (int i = 0; i < TilePanel.Children.Count; i++)
        {
            _numbers.Add(i);
        }

        for (int i = _numbers.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_numbers[j], _numbers[i]) = (_numbers[i], _numbers[j]);
        }

        _currentIndex = 0;
    }

    private int GetNextUniqueRandom()
    {
        if (_numbers.Count == 0 || _currentIndex >= _numbers.Count)
        {
            ResetAndShuffle();
        }

        return _numbers.Count > 0 ? _numbers[_currentIndex++] : -1;
    }

    private void SetTileVisuals()
    {
        if (_selectedTile is null)
        {
            return;
        }

        _selectedTile.IsSelected = true;

        if (Uri.TryCreate(_selectedTile.ImageUrl, UriKind.Absolute, out Uri? imageUri))
        {
            BackDropImage.ImageUrl = imageUri;
        }

        _ = UpdateTitleForegroundAsync(_selectedTile);
    }

    private async Task UpdateTitleForegroundAsync(HeaderTile tile)
    {
        if (!IsLoaded || tile != _selectedTile)
        {
            return;
        }

        string url = tile.ImageUrl ?? string.Empty;
        if (!string.IsNullOrEmpty(url) && _luminanceCache.TryGetValue(url, out bool cachedIsLight))
        {
            ApplyTitleForegroundBrush(ResolveContrastBrush(cachedIsLight));
            return;
        }

        if (!tile.IsImageLoaded)
        {
            ApplyTitleForegroundBrush(ResolveThemeFallbackBrush());
            return;
        }

        bool? isLight = await tile.IsLightImageAsync();
        if (isLight.HasValue && !string.IsNullOrEmpty(url))
        {
            _luminanceCache[url] = isLight.Value;
        }

        if (!IsLoaded || tile != _selectedTile)
        {
            return;
        }

        LinearGradientBrush? targetBrush = isLight.HasValue
            ? ResolveContrastBrush(isLight.Value)
            : ResolveThemeFallbackBrush();
        ApplyTitleForegroundBrush(targetBrush);
    }

    private void AnimateTitleGradient(LinearGradientBrush brush)
    {
        if (brush.GradientStops.Count == 0 || AnimatedGradientBrush.GradientStops.Count == 0)
        {
            return;
        }

        _titleStoryboard?.Stop();
        Storyboard storyboard = new();
        int count = Math.Min(brush.GradientStops.Count, AnimatedGradientBrush.GradientStops.Count);
        for (int i = 0; i < count; i++)
        {
            ColorAnimation colorAnimation = new()
            {
                To = brush.GradientStops[i].Color,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EnableDependentAnimation = true,
            };
            Storyboard.SetTarget(colorAnimation, AnimatedGradientBrush.GradientStops[i]);
            Storyboard.SetTargetProperty(colorAnimation, "Color");
            storyboard.Children.Add(colorAnimation);
        }

        _titleStoryboard = storyboard;
        storyboard.Begin();
    }

    private void Tile_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CancelPendingSelection();
        ((HeaderTile)sender).IsSelected = false;
        if (!_isPaused)
        {
            _selectionTimer.Start();
        }
    }

    private void Tile_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_isPaused)
        {
            return;
        }

        _selectedTile = (HeaderTile)sender;
        _ = SelectTileAsync();
    }

    private async Task SelectTileAsync()
    {
        if (_isPaused)
        {
            return;
        }

        CancelPendingSelection();
        _tileCts = new CancellationTokenSource();
        CancellationToken ct = _tileCts.Token;
        HeaderTile? targetTile = _selectedTile;

        try
        {
            await Task.Delay(100, ct);
            _selectionTimer.Stop();
            _deselectionTimer.Stop();

            foreach (HeaderTile tile in TilePanel.Children.OfType<HeaderTile>())
            {
                tile.IsSelected = false;
            }

            // Wait for the animation of a potential other tile to finish
            await Task.Delay(360, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (_isPaused || !IsLoaded || ct.IsCancellationRequested || _selectedTile != targetTile)
        {
            return;
        }

        SetTileVisuals();
    }

    private void Tile_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_isPaused)
        {
            return;
        }

        _selectedTile = (HeaderTile)sender;
        _ = SelectTileAsync();
    }

    private void Tile_LostFocus(object sender, RoutedEventArgs e)
    {
        CancelPendingSelection();
        ((HeaderTile)sender).IsSelected = false;
        if (!_isPaused)
        {
            _selectionTimer.Start();
        }
    }
}
