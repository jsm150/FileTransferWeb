using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Infrastructure.Batch;
using Microsoft.Extensions.Options;
using Xunit;

namespace FileTransferWeb.Transfer.Infrastructure.Tests.Batch;

public sealed class InMemoryTransferBatchRepositoryTests
{
    [Fact(DisplayName = "저장 후 만료 전에는 배치를 조회할 수 있다")]
    public async Task GetAsync_ReturnsBatch_WhenNotExpired()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 2, 14, 10, 0, 0, TimeSpan.Zero));
        var repository = CreateRepository(slidingTtlMinutes: 10, timeProvider);
        var batch = TransferBatch.Create("images", 1, timeProvider.GetUtcNow().UtcDateTime);

        await repository.SaveAsync(batch, CancellationToken.None);
        var loaded = await repository.GetAsync(batch.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(batch.Id, loaded!.Id);
    }

    [Fact(DisplayName = "TTL이 0이면 저장 직후 조회 시 만료되어 null을 반환한다")]
    public async Task GetAsync_ReturnsNull_WhenTtlIsZero()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 2, 14, 10, 0, 0, TimeSpan.Zero));
        var repository = CreateRepository(slidingTtlMinutes: 0, timeProvider);
        var batch = TransferBatch.Create("images", 1, timeProvider.GetUtcNow().UtcDateTime);

        await repository.SaveAsync(batch, CancellationToken.None);
        var loaded = await repository.GetAsync(batch.Id, CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact(DisplayName = "만료 정리 실행 시 만료된 배치를 물리적으로 제거한다")]
    public async Task RemoveExpiredAsync_RemovesExpiredEntries()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 2, 14, 10, 0, 0, TimeSpan.Zero));
        var repository = CreateRepository(slidingTtlMinutes: 1, timeProvider);
        var batch = TransferBatch.Create("images", 1, timeProvider.GetUtcNow().UtcDateTime);

        await repository.SaveAsync(batch, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        await repository.RemoveExpiredAsync(CancellationToken.None);
        var loaded = await repository.GetAsync(batch.Id, CancellationToken.None);

        Assert.Null(loaded);
    }

    private static InMemoryTransferBatchRepository CreateRepository(int slidingTtlMinutes, TimeProvider timeProvider)
    {
        var options = Options.Create(
            new TransferBatchMemoryOptions
            {
                SlidingTtlMinutes = slidingTtlMinutes,
                CleanupIntervalSeconds = 300
            });
        return new InMemoryTransferBatchRepository(options, timeProvider);
    }

    private sealed class MutableTimeProvider(DateTimeOffset nowUtc) : TimeProvider
    {
        private DateTimeOffset _nowUtc = nowUtc;

        public override DateTimeOffset GetUtcNow()
        {
            return _nowUtc;
        }

        public void Advance(TimeSpan duration)
        {
            _nowUtc = _nowUtc.Add(duration);
        }
    }
}
