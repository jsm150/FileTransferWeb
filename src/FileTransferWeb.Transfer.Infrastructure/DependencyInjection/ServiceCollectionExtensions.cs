using Microsoft.Extensions.DependencyInjection;

namespace FileTransferWeb.Transfer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransferInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
