using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频操作区域.
/// </summary>
public sealed partial class VideoOperationControl : VideoPlayerPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoOperationControl"/> class.
    /// </summary>
    public VideoOperationControl() => InitializeComponent();

    private void OnLoopSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || LoopComboBox.SelectedItem is not VideoLoopType loopType)
        {
            return;
        }

        if (ViewModel.CurrentLoop != loopType)
        {
            ViewModel.CurrentLoop = loopType;
        }
    }
}
