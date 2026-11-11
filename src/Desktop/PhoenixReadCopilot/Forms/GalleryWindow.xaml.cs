using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Models;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Forms;

/// <summary>
/// 图片浏览窗口.
/// </summary>
public sealed partial class GalleryWindow : WinUIEx.WindowEx, ITipWindow
{
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings _settings;

    private bool _isActivated;

    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryWindow"/> class.
    /// </summary>
    public GalleryWindow(AppImage image, List<AppImage>? list = null)
    {
        _settings = Ioc.Default.GetRequiredService<ISettingsService>().Settings;
        _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        MainFrame.Tag = this;
        Activated += OnActivated;
        Closed += OnClosed;
        Title = Strings.Resources.ImageGallery;
        MinWidth = 500;
        MinHeight = 500;
        SetTitleBar(TitleBar);

        MainFrame.Navigate(typeof(GalleryPage), (image, list ?? [image]));
        _appViewModel.Windows.Add(this);

        MoveAndResize();
    }

    /// <inheritdoc/>
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Error)
    {
        TipPopup popup = new() { Text = text };
        TipContainer.Visibility = Visibility.Visible;
        TipContainer.Children.Add(popup);
        await popup.ShowAsync(type);
        TipContainer.Children.Remove(popup);
        TipContainer.Visibility = Visibility.Collapsed;
    }

    private PointInt32 GetSavedWindowPosition()
    {
        int left = _settings.GalleryWindowPositionLeft;
        int top = _settings.GalleryWindowPositionTop;
        return new PointInt32(left, top);
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _appViewModel.ActivatedWindow = this;
        }

        if (!_isActivated)
        {
            bool isMaximized = _settings.IsGalleryWindowMaximized;
            if (isMaximized)
            {
                (AppWindow.Presenter as OverlappedPresenter).Maximize();
            }

            ElementTheme localTheme = _settings.AppTheme;
            _appViewModel.ChangeThemeCommand.Execute(localTheme);
        }

        _isActivated = true;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _appViewModel.Windows.Remove(this);
        SaveCurrentWindowStats();
    }

    private RectInt32 GetRenderRect(RectInt32 workArea)
    {
        double scaleFactor = this.GetDpiForWindow() / 96d;
        double previousWidth = _settings.GalleryWindowWidth;
        double previousHeight = _settings.GalleryWindowHeight;
        int width = Convert.ToInt32(previousWidth * scaleFactor);
        int height = Convert.ToInt32(previousHeight * scaleFactor);

        // Ensure the window is not larger than the work area.
        if (height > workArea.Height - 20)
        {
            height = workArea.Height - 20;
        }

        PointInt32 lastPoint = GetSavedWindowPosition();
        bool isZeroPoint = lastPoint is
        {
            X: 0,
            Y: 0
        };
        bool isValidPosition = lastPoint.X >= workArea.X && lastPoint.Y >= workArea.Y;
        double left = isZeroPoint || !isValidPosition ? (workArea.Width - width) / 2d : lastPoint.X;
        double top = isZeroPoint || !isValidPosition ? (workArea.Height - height) / 2d : lastPoint.Y;
        return new RectInt32(Convert.ToInt32(left), Convert.ToInt32(top), width, height);
    }

    private void MoveAndResize()
    {
        PointInt32 lastPoint = GetSavedWindowPosition();
        DisplayArea displayArea =
            DisplayArea.GetFromPoint(lastPoint, DisplayAreaFallback.Primary) ?? DisplayArea.Primary;

        RectInt32 rect = GetRenderRect(displayArea.WorkArea);
        AppWindow.MoveAndResize(rect);
    }

    private void SaveCurrentWindowStats()
    {
        int left = AppWindow.Position.X;
        int top = AppWindow.Position.Y;
        BOOL isMaximized = PInvoke.IsZoomed(new HWND(this.GetWindowHandle()));
        _settings.IsGalleryWindowMaximized = isMaximized;

        if (!isMaximized)
        {
            _settings.GalleryWindowPositionLeft = left;
            _settings.GalleryWindowPositionTop = top;
            _settings.GalleryWindowHeight = Height < 500 ? 500d : Height;
            _settings.GalleryWindowWidth = Width;
        }
    }
}
