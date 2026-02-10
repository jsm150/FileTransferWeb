using FileTransferWeb.Storage.Application.Features.ListDirectories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FileTransferWeb.Storage.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ListDirectoriesQuery).Assembly));

        return services;
    }
}
