using FileTransferWeb.Transfer.Domain.Ports;
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
        services.AddOptions<TransferTusOptions>()
            .Bind(configuration.GetSection(TransferTusOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<FileSystemTusStoreFactory>();

        services.AddSingleton<ITransferCompletedUploadReader, TusCompletedUploadReader>();
        services.AddSingleton<ITransferUploadCreateValidator, TransferUploadCreateValidator>();
        services.AddSingleton<ITransferTargetFileNameReader, FileSystemTransferTargetFileNameReader>();
        services.AddSingleton<ITransferCompletedFileMover, FileSystemTransferCompletedFileMover>();

        return services;
    }
}
