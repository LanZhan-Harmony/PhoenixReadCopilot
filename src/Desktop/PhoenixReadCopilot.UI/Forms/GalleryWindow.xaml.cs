using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Core;
using Richasy.BiliKernel.Models.Appearance;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Forms;

/// <summary>
/// 图片浏览窗口.
/// </summary>
public sealed partial class GalleryWindow : WinUIEx.WindowEx, ITipWindow
{
    private bool _isActivated;

    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryWindow"/> class.
    /// </summary>
    public GalleryWindow(BiliImage image, List<BiliImage> list = default)
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        MainFrame.Tag = this;
        Activated += OnActivated;
        Closed += OnClosed;
        Title = Strings.Resources.ImageGallery;
        MinWidth = 500;
        MinHeight = 500;
        SetTitleBar(TitleBar);

        MainFrame.Navigate(typeof(GalleryPage), (image, list));
        this.Get<AppViewModel>().Windows.Add(this);

        MoveAndResize();
    }

    /// <inheritdoc/>
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Error)
    {
        var popup = new TipPopup() { Text = text };
        TipContainer.Visibility = Visibility.Visible;
        TipContainer.Children.Add(popup);
        await popup.ShowAsync(type);
        TipContainer.Children.Remove(popup);
        TipContainer.Visibility = Visibility.Collapsed;
    }

    private static PointInt32 GetSavedWindowPosition()
    {
        var left = Settings.GalleryWindowPositionLeft;
        var top = Settings.GalleryWindowPositionTop;
        return new PointInt32(left, top);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            this.Get<AppViewModel>().ActivatedWindow = this;
        }

        if (!_isActivated)
        {
            var isMaximized = Settings.IsGalleryWindowMaximized;
            if (isMaximized)
            {
                (AppWindow.Presenter as OverlappedPresenter).Maximize();
            }

            var localTheme = Settings.AppTheme;
            this.Get<AppViewModel>().ChangeThemeCommand.Execute(localTheme);
        }

        _isActivated = true;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        this.Get<AppViewModel>().Windows.Remove(this);
        SaveCurrentWindowStats();
    }

    private RectInt32 GetRenderRect(RectInt32 workArea)
    {
        var scaleFactor = this.GetDpiForWindow() / 96d;
        var previousWidth = Settings.GalleryWindowWidth;
        var previousHeight = Settings.GalleryWindowHeight;
        var width = Convert.ToInt32(previousWidth * scaleFactor);
        var height = Convert.ToInt32(previousHeight * scaleFactor);

        // Ensure the window is not larger than the work area.
        if (height > workArea.Height - 20)
        {
            height = workArea.Height - 20;
        }

        var lastPoint = GetSavedWindowPosition();
        var isZeroPoint = lastPoint.X == 0 && lastPoint.Y == 0;
        var isValidPosition = lastPoint.X >= workArea.X && lastPoint.Y >= workArea.Y;
        var left = isZeroPoint || !isValidPosition ? (workArea.Width - width) / 2d : lastPoint.X;
        var top = isZeroPoint || !isValidPosition ? (workArea.Height - height) / 2d : lastPoint.Y;
        return new RectInt32(Convert.ToInt32(left), Convert.ToInt32(top), width, height);
    }

    private void MoveAndResize()
    {
        var lastPoint = GetSavedWindowPosition();
        var displayArea =
            DisplayArea.GetFromPoint(lastPoint, DisplayAreaFallback.Primary) ?? DisplayArea.Primary;

        var rect = GetRenderRect(displayArea.WorkArea);
        AppWindow.MoveAndResize(rect);
    }

    private void SaveCurrentWindowStats()
    {
        var left = AppWindow.Position.X;
        var top = AppWindow.Position.Y;
        var isMaximized = PInvoke.IsZoomed(new HWND(this.GetWindowHandle()));
        Settings.IsGalleryWindowMaximized = (bool)isMaximized;

        if (!isMaximized)
        {
            Settings.GalleryWindowPositionLeft = left;
            Settings.GalleryWindowPositionTop = top;
            Settings.GalleryWindowHeight = Height < 500 ? 500d : Height
            ;
            Settings.GalleryWindowWidth = Width;
        }
    }
}
