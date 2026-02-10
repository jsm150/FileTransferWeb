using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileTransferWeb.Storage.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddHostedService<UploadRootBootstrapHostedService>();

        services.AddSingleton<IStorageRootPathProvider, StorageRootPathProvider>();
        services.AddSingleton<IStorageDirectoryReader, FileSystemStorageDirectoryReader>();

        return services;
    }
}
