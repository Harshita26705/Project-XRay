namespace XRay.Api.Services.BackgroundJobs;

/// <summary>
/// Dequeues analysis work items and runs each on a fresh DI scope, since the HTTP request that
/// enqueued the work (and its scoped DbContext) is long gone by the time this executes.
/// </summary>
public class AnalysisJobWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceProvider _rootProvider;
    private readonly ILogger<AnalysisJobWorker> _logger;

    public AnalysisJobWorker(IBackgroundTaskQueue queue, IServiceProvider rootProvider, ILogger<AnalysisJobWorker> logger)
    {
        _queue = queue;
        _rootProvider = rootProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, Task> workItem;
            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            using var scope = _rootProvider.CreateScope();
            try
            {
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background analysis work item failed.");
            }
        }
    }
}
