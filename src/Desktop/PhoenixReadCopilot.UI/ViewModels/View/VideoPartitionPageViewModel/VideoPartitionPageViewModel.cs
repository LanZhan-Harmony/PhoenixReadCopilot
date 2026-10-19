using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Items;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Richasy.BiliKernel.Bili.Media;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 视频分区页面视图模型.
/// </summary>
public sealed partial class VideoPartitionPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPartitionPageViewModel"/> class.
    /// </summary>
    public VideoPartitionPageViewModel(
        IVideoDiscoveryService discoveryService,
        ILogger<VideoPartitionPageViewModel> logger
    )
    {
        _service = discoveryService;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(VideoPartitionPage);

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Partitions.Count > 0)
        {
            return;
        }

        IsPartitionLoading = true;
        var partitions = await _service.GetVideoPartitionsAsync();
        if (partitions != null)
        {
            foreach (var item in partitions)
            {
                Partitions.Add(new PartitionViewModel(item));
            }
        }

        IsPartitionLoading = false;
        await Task.Delay(200);
        var lastSelectedPartitionId = SettingsToolkit.ReadLocalSetting(
            SettingNames.VideoPartitionPageLastSelectedPartitionId,
            Partitions.First().Data.Id
        );
        var partition =
            Partitions.FirstOrDefault(p => p.Data.Id == lastSelectedPartitionId)
            ?? Partitions.First();
        SelectPartition(partition);
        PartitionInitialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectPartition(PartitionViewModel partition)
    {
        if (partition is null || partition.Data.Equals(SelectedPartition?.Data))
        {
            return;
        }

        if (_partitionCache.TryGetValue(partition.Data, out var detail))
        {
            SelectedPartition = detail;
        }
        else
        {
            var vm = new VideoPartitionDetailViewModel(partition, _service);
            _partitionCache.Add(partition.Data, vm);
            SelectedPartition = vm;
            vm.InitializeCommand.Execute(default);
        }

        SettingsToolkit.WriteLocalSetting(
            SettingNames.VideoPartitionPageLastSelectedPartitionId,
            partition.Data.Id
        );
    }
}
