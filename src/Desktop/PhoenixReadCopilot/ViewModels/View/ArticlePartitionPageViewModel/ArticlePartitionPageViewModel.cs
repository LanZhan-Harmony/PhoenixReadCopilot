using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Article;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 专栏文章分区页面视图模型.
/// </summary>
public sealed partial class ArticlePartitionPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticlePartitionPageViewModel"/> class.
    /// </summary>
    public ArticlePartitionPageViewModel(
        IArticleDiscoveryService discoveryService,
        ILogger<ArticlePartitionPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _service = discoveryService;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(ArticlePartitionPage);

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Partitions.Count > 0)
        {
            return;
        }

        IsPartitionLoading = true;
        Partition hotPartition = new("-", Resources.HotArticle);
        Partitions.Add(new PartitionViewModel(hotPartition));
        IReadOnlyList<Partition> partitions = await _service.GetPartitionsAsync();
        if (partitions != null)
        {
            foreach (Partition item in partitions)
            {
                Partitions.Add(new PartitionViewModel(item));
            }
        }

        IsPartitionLoading = false;
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                string lastSelectedPartitionId = string.IsNullOrEmpty(
                    _settings.ArticlePartitionPageLastSelectedPartitionId
                )
                    ? Partitions.First().Data.Id
                    : _settings.ArticlePartitionPageLastSelectedPartitionId;
                PartitionViewModel partition =
                    Partitions.FirstOrDefault(p => p.Data.Id == lastSelectedPartitionId)
                    ?? Partitions.First();
                SelectPartition(partition);
                PartitionInitialized?.Invoke(this, EventArgs.Empty);
            }
        );
    }

    [RelayCommand]
    private void SelectPartition(PartitionViewModel partition)
    {
        if (partition is null || partition.Data.Equals(SelectedPartition?.Data))
        {
            return;
        }

        if (
            _partitionCache.TryGetValue(partition.Data, out ArticlePartitionDetailViewModel? detail)
        )
        {
            SelectedPartition = detail;
        }
        else
        {
            ArticlePartitionDetailViewModel vm = new(
                partition,
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<ArticlePartitionDetailViewModel>()
            );
            _partitionCache.Add(partition.Data, vm);
            SelectedPartition = vm;
            vm.InitializeCommand.Execute(null);
        }

        _settings.ArticlePartitionPageLastSelectedPartitionId = partition.Data.Id;
    }
}
