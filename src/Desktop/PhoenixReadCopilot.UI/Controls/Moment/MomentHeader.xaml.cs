using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.ViewModels.Items;
using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Moment;

/// <summary>
/// 动态头部.
/// </summary>
public sealed partial class MomentHeader : MomentHeaderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentHeader"/> class.
    /// </summary>
    public MomentHeader() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        Selector.SelectionChanged += OnSelectorChanged;
        ViewModel.Initialized += OnViewModelInitialized;
        InitializeChildPartitions();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.Initialized -= OnViewModelInitialized;
        }

        Selector.SelectionChanged -= OnSelectorChanged;
    }

    private static string GetSectionName(MomentSectionType type) =>
        type switch
        {
            MomentSectionType.Video => Strings.Resources.Video,
            MomentSectionType.Comprehensive => Strings.Resources.Comprehensive,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

    private void OnViewModelInitialized(object? sender, EventArgs e) => InitializeChildPartitions();

    private void OnSelectorChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is null)
        {
            return;
        }

        var item = sender.SelectedItem.Tag as IMomentSectionDetailViewModel;
        if (item is not null && item != ViewModel.SelectedSection)
        {
            ViewModel.SelectSectionCommand.Execute(item);
        }
    }

    private void InitializeChildPartitions()
    {
        Selector.Items.Clear();
        if (ViewModel.Sections is not null)
        {
            foreach (var item in ViewModel.Sections)
            {
                Selector.Items.Add(
                    new SelectorBarItem()
                    {
                        Text = GetSectionName(item.SectionType),
                        Tag = item
                    }
                );
            }
        }

        Selector.SelectedItem = Selector.Items.FirstOrDefault(p =>
            p.Tag == ViewModel.SelectedSection
        );
    }
}

/// <summary>
/// 动态头部基类.
/// </summary>
public abstract class MomentHeaderBase : LayoutUserControlBase<MomentPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentHeaderBase"/> class.
    /// </summary>
    protected MomentHeaderBase() => ViewModel = this.Get<MomentPageViewModel>();
}
