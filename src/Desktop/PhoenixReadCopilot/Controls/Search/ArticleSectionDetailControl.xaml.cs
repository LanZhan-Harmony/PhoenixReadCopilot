using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Search;

/// <summary>
/// 文章搜索分区详情控件.
/// </summary>
public sealed partial class ArticleSectionDetailControl : ArticleSectionDetailControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleSectionDetailControl"/> class.
    /// </summary>
    public ArticleSectionDetailControl() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.ListUpdated += OnListUpdatedAsync;
        ArticleScrollView.ViewChanged += OnViewChanged;
        ArticleScrollView.SizeChanged += OnScrollViewSizeChanged;

        CheckArticleCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ArticleRepeater.ItemsSource = null;
        ViewModel.ListUpdated -= OnListUpdatedAsync;
        ArticleScrollView.ViewChanged -= OnViewChanged;
        ArticleScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private async void OnListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckArticleCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        WinUIKernelShareExtensions.IsCardAnimationEnabled = !args.IsIntermediate;
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                ArticleScrollView.ExtentHeight
                    - ArticleScrollView.ViewportHeight
                    - ArticleScrollView.VerticalOffset
                <= 240
            )
            {
                ViewModel.LoadItemsCommand.Execute(null);
            }
        });
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckArticleCount();
        }
    }

    private void CheckArticleCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (ArticleScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                ViewModel.LoadItemsCommand.Execute(null);
            }
        });
    }
}

/// <summary>
/// 文章搜索分区详情控件基类.
/// </summary>
public abstract class ArticleSectionDetailControlBase
    : LayoutUserControlBase<ArticleSearchSectionDetailViewModel> { }
