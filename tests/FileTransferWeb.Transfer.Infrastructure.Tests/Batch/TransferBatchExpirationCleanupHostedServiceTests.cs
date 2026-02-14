using System.Collections.Concurrent;
using FileTransferWeb.Transfer.Infrastructure.Batch;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FileTransferWeb.Transfer.Infrastructure.Tests.Batch;

public sealed class TransferBatchExpirationCleanupHostedServiceTests
{
    [Fact(DisplayName = "정리 서비스는 주기 실행 시 만료 정리기를 호출한다")]
    public async Task CleanupService_CallsCleaner_OnInterval()
    {
        var cleaner = new RecordingCleaner();
        var options = Options.Create(
            new TransferBatchMemoryOptions
            {
                SlidingTtlMinutes = 1440,
                CleanupIntervalSeconds = 1
            });
        var service = new TransferBatchExpirationCleanupHostedService(
            cleaner,
            options,
            NullLogger<TransferBatchExpirationCleanupHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await cleaner.WaitUntilCalledAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.True(cleaner.CallCount >= 1);
    }

    [Fact(DisplayName = "정리 서비스는 중단 요청 시 예외 없이 종료된다")]
    public async Task CleanupService_Stops_WhenCancellationRequested()
    {
        var cleaner = new RecordingCleaner();
        var options = Options.Create(
            new TransferBatchMemoryOptions
            {
                SlidingTtlMinutes = 1440,
                CleanupIntervalSeconds = 5
            });
        var service = new TransferBatchExpirationCleanupHostedService(
            cleaner,
            options,
            NullLogger<TransferBatchExpirationCleanupHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    private sealed class RecordingCleaner : ITransferBatchExpirationCleaner
    {
        private readonly ConcurrentQueue<DateTimeOffset> _calls = new();
        private readonly TaskCompletionSource<bool> _called = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount => _calls.Count;

        public ValueTask RemoveExpiredAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _calls.Enqueue(DateTimeOffset.UtcNow);
            _called.TrySetResult(true);
            return ValueTask.CompletedTask;
        }

        public async Task WaitUntilCalledAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await _called.Task.WaitAsync(cts.Token);
        }
    }
}
