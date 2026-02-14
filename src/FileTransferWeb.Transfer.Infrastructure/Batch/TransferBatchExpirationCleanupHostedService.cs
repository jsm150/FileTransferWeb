using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileTransferWeb.Transfer.Infrastructure.Batch;

public sealed class TransferBatchExpirationCleanupHostedService(
    ITransferBatchExpirationCleaner expirationCleaner,
    IOptions<TransferBatchMemoryOptions> options,
    ILogger<TransferBatchExpirationCleanupHostedService> logger)
    : BackgroundService
{
    private readonly ITransferBatchExpirationCleaner _expirationCleaner = expirationCleaner;
    private readonly TransferBatchMemoryOptions _options = options.Value;
    private readonly ILogger<TransferBatchExpirationCleanupHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.CleanupIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _expirationCleaner.RemoveExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "배치 만료 정리 작업 중 오류가 발생했습니다.");
            }
        }
    }
}
