using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Models.Player;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 下载按钮.
/// </summary>
public sealed partial class DownloadButton : DownloadButtonBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadButton"/> class.
    /// </summary>
    public DownloadButton()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.MetaInitialized -= OnMetaInitialized;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        DownloadViewModel? oldValue,
        DownloadViewModel? newValue
    )
    {
        oldValue?.MetaInitialized -= OnMetaInitialized;

        if (newValue is null)
        {
            return;
        }

        newValue.MetaInitialized += OnMetaInitialized;
        DownloadFlyout.Items.Clear();
    }

    private void OnMetaInitialized(object? sender, EventArgs e) => DownloadFlyout.Items.Clear();

    private void InitializeDownloadFlyout()
    {
        if (ViewModel is null || ViewModel.Formats is null)
        {
            return;
        }

        MenuFlyoutSeparator? formatHeader =
            MenuFlyoutItemHeader.LoadContent() as MenuFlyoutSeparator;
        formatHeader.Tag = Strings.Resources.Quality;
        DownloadFlyout.Items.Add(formatHeader);
        foreach (PlayerFormatInformation item in ViewModel.Formats)
        {
            MenuFlyoutItem menuItem = new()
            {
                Text = item.Description,
                MinWidth = ActualWidth,
                Command = ViewModel.DownloadWithFormatCommand,
                CommandParameter = item,
            };
            DownloadFlyout.Items.Add(menuItem);
        }

        if (ViewModel.Episodes is { Count: > 0 })
        {
            DownloadFlyout.Items.Add(new MenuFlyoutSeparator());
            MenuFlyoutSeparator? partHeader =
                MenuFlyoutItemHeader.LoadContent() as MenuFlyoutSeparator;
            partHeader.Tag = Strings.Resources.BatchDownload;
            MenuFlyoutItem allPartsItem = new()
            {
                MinWidth = ActualWidth,
                Command = ViewModel.BatchDownloadAllPartsCommand,
                Text = Strings.Resources.DownloadAllParts,
            };
            MenuFlyoutItem selectPartsItem = new()
            {
                MinWidth = ActualWidth,
                Text = Strings.Resources.SelectPartsDownload,
            };
            selectPartsItem.Click += (_, _) => ShowPartsSelectionTip();
            DownloadFlyout.Items.Add(partHeader);
            DownloadFlyout.Items.Add(allPartsItem);
            DownloadFlyout.Items.Add(selectPartsItem);
        }

        DownloadFlyout.Items.Add(new MenuFlyoutSeparator());
        MenuFlyoutSeparator? extraHeader =
            MenuFlyoutItemHeader.LoadContent() as MenuFlyoutSeparator;
        extraHeader.Tag = Strings.Resources.Extra;
        DownloadFlyout.Items.Add(extraHeader);
        MenuFlyoutItem downloadCoverItem = new()
        {
            MinWidth = ActualWidth,
            Text = Strings.Resources.DownloadCover,
            Command = ViewModel.DownloadCoverCommand,
        };
        MenuFlyoutItem downloadDanmakuItem = new()
        {
            MinWidth = ActualWidth,
            Text = Strings.Resources.DownloadDanmaku,
            Command = ViewModel.DownloadDanmakuCommand,
        };

        DownloadFlyout.Items.Add(downloadCoverItem);
        DownloadFlyout.Items.Add(downloadDanmakuItem);

        if (ViewModel.HasAvailableSubtitle)
        {
            MenuFlyoutItem downloadSubtitleItem = new()
            {
                MinWidth = ActualWidth,
                Text = Strings.Resources.DownloadSubtitle,
                Command = ViewModel.DownloadSubtitleCommand,
            };

            DownloadFlyout.Items.Add(downloadSubtitleItem);
        }
    }

    private void ShowPartsSelectionTip()
    {
        SelectionBox.Text = string.Empty;
        SelectionTip.IsOpen = true;
    }

    private void OnSelectionSubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        string text = args.QueryText;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SelectionTip.IsOpen = false;
        SelectionBox.Text = string.Empty;
        ViewModel.BatchDownloadSelectedPartsCommand.Execute(text);
    }

    private void OnBtnClick(object sender, RoutedEventArgs e)
    {
        if (DownloadFlyout.Items.Count == 0)
        {
            InitializeDownloadFlyout();
        }
    }
}

/// <summary>
/// 下载按钮基类.
/// </summary>
public abstract class DownloadButtonBase : LayoutUserControlBase<DownloadViewModel> { }
