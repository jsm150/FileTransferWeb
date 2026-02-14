using FileTransferWeb.Transfer.Domain.Ports;
using FileTransferWeb.Transfer.Infrastructure.Batch;
using FileTransferWeb.Transfer.Infrastructure.Storage;
using FileTransferWeb.Transfer.Infrastructure.Tus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileTransferWeb.Transfer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransferInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<TransferTusOptions>()
            .Bind(configuration.GetSection(TransferTusOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<TransferBatchMemoryOptions>()
            .Bind(configuration.GetSection(TransferBatchMemoryOptions.SectionName))
            .Validate(
                options => options.SlidingTtlMinutes >= 0,
                "Transfer:Batch:SlidingTtlMinutes는 0 이상이어야 합니다.")
            .Validate(
                options => options.CleanupIntervalSeconds > 0,
                "Transfer:Batch:CleanupIntervalSeconds는 1 이상이어야 합니다.")
            .ValidateOnStart();

        services.AddSingleton<FileSystemTusStoreFactory>();

        services.AddSingleton<InMemoryTransferBatchRepository>();
        services.AddSingleton<ITransferBatchRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<InMemoryTransferBatchRepository>());
        services.AddSingleton<ITransferBatchExpirationCleaner>(serviceProvider =>
            serviceProvider.GetRequiredService<InMemoryTransferBatchRepository>());
        services.AddHostedService<TransferBatchExpirationCleanupHostedService>();
        services.AddSingleton<ITransferCompletedUploadReader, TusCompletedUploadReader>();
        services.AddSingleton<ITransferUploadCreateValidator, TransferUploadCreateValidator>();
        services.AddSingleton<ITransferTargetFileNameReader, FileSystemTransferTargetFileNameReader>();
        services.AddSingleton<ITransferCompletedFileMover, FileSystemTransferCompletedFileMover>();

        return services;
    }
}
