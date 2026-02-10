using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FileTransferWeb.Transfer.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransferApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TransferApplicationAssemblyMarker).Assembly));

        return services;
    }
}
