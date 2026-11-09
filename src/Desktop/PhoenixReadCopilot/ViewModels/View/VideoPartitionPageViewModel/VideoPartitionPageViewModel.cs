using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.ViewModels.View;

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
        ILogger<VideoPartitionPageViewModel> logger,
        ISettingsService settingsService,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _service = discoveryService;
        _logger = logger;
        _loggerFactory = loggerFactory;
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
        IReadOnlyList<Partition> partitions = await _service.GetVideoPartitionsAsync();
        if (partitions != null)
        {
            foreach (Partition item in partitions)
            {
                Partitions.Add(new PartitionViewModel(item));
            }
        }

        IsPartitionLoading = false;
        await Task.Delay(200);
        string lastSelectedPartitionId = string.IsNullOrEmpty(
            _settings.VideoPartitionPageLastSelectedPartitionId
        )
            ? Partitions.First().Data.Id
            : _settings.VideoPartitionPageLastSelectedPartitionId;
        PartitionViewModel partition =
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

        if (_partitionCache.TryGetValue(partition.Data, out VideoPartitionDetailViewModel? detail))
        {
            SelectedPartition = detail;
        }
        else
        {
            VideoPartitionDetailViewModel vm = new(
                partition,
                _service,
                _loggerFactory.CreateLogger<VideoPartitionDetailViewModel>()
            );
            _partitionCache.Add(partition.Data, vm);
            SelectedPartition = vm;
            vm.InitializeCommand.Execute(null);
        }

        _settings.VideoPartitionPageLastSelectedPartitionId = partition.Data.Id;
    }
}
