using FileTransferWeb.Transfer.Domain.Ports;
using FileTransferWeb.Transfer.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FileTransferWeb.Transfer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransferInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITransferTargetFileNameReader, FileSystemTransferTargetFileNameReader>();
        services.AddSingleton<ITransferFileStore, FileSystemTransferFileStore>();

        return services;
    }
}
