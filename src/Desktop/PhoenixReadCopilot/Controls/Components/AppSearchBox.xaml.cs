using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PhoenixReadCopilot.Models.Search;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 应用搜索框.
/// </summary>
public sealed partial class AppSearchBox : AppSearchBoxBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppSearchBox"/> class.
    /// </summary>
    public AppSearchBox()
    {
        InitializeComponent();
    }

    protected override void OnControlUnloaded() => HotSearchView.ItemsSource = null;

    private void OnKeywordChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.ReloadSuggestionsCommand.Execute(null);
        }
    }

    private void OnBoxGettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        if (!Box.IsSuggestionListOpen)
        {
            ViewModel.Keyword = " ";
            ViewModel.Keyword = string.Empty;
        }
    }

    private void OnHotSearchFlyoutOpened(object sender, object e) =>
        ViewModel.LoadHotSearchCommand.Execute(null);

    private void OnHotSearchItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        HotSearchItem? context = args.InvokedItem as HotSearchItem;
        if (context is not null)
        {
            ViewModel.Keyword = context.Keyword;
            SearchSuggestItemViewModel vm = new(
                new SearchSuggestItem(context.Keyword, context.Keyword)
            );
            ViewModel.SearchCommand.Execute(vm);
        }

        HotSearchFlyout.Hide();
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SearchSuggestItemViewModel item)
        {
            ViewModel.SearchCommand.Execute(item);
        }
        else if (!string.IsNullOrEmpty(args.QueryText))
        {
            SearchSuggestItemViewModel vm = new(
                new SearchSuggestItem(args.QueryText, args.QueryText)
            );
            ViewModel.SearchCommand.Execute(vm);
        }

        ViewModel.TryCancelSuggestCommand.Execute(null);
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, ViewModel.Suggestion.Clear);
    }
}

/// <summary>
/// 应用搜索框的基类.
/// </summary>
public abstract class AppSearchBoxBase : LayoutUserControlBase<SearchBoxViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppSearchBoxBase"/> class.
    /// </summary>
    protected AppSearchBoxBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<SearchBoxViewModel>();
    }
}
