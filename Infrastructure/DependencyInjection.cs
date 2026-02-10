using FileTransferWeb.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileTransferWeb.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageBootstrap(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddHostedService<UploadRootBootstrapHostedService>();

        return services;
    }
}
