using System.Net;
using System.Text;
using FileTransferWeb.Storage.Application.Abstractions;
using FileTransferWeb.Storage.Domain.Policies;
using FileTransferWeb.Transfer.Infrastructure.Tus;
using Microsoft.Extensions.Options;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;

namespace FileTransferWeb.Endpoints;

public static class TusEndpoints
{
    public static IEndpointRouteBuilder MapTusUploads(this IEndpointRouteBuilder app)
    {
        app.MapTus(
            "/api/transfer/uploads",
            async httpContext =>
            {
                var tusOptions = httpContext.RequestServices
                    .GetRequiredService<IOptions<TransferTusOptions>>()
                    .Value;

                var storeFactory = httpContext.RequestServices.GetRequiredService<FileSystemTusStoreFactory>();
                var completionEventPublisher = httpContext.RequestServices.GetRequiredService<TusCompletionEventPublisher>();

                return await Task.FromResult(
                    new DefaultTusConfiguration
                    {
                        Store = storeFactory.CreateStore(),
                        MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
                        MaxAllowedUploadSizeInBytesLong = tusOptions.MaxSizeBytes,
                        Expiration = new AbsoluteExpiration(TimeSpan.FromHours(tusOptions.ExpirationHours)),
                        Events = new Events
                        {
                            OnBeforeCreateAsync = async context =>
                            {
                                if (!TryGetRequiredMetadata(context.Metadata, "targetPath", out var targetPath))
                                {
                                    context.FailRequest(HttpStatusCode.BadRequest, "targetPath 메타데이터가 필요합니다.");
                                    return;
                                }

                                if (!TryGetRequiredMetadata(context.Metadata, "fileName", out var fileName))
                                {
                                    context.FailRequest(HttpStatusCode.BadRequest, "fileName 메타데이터가 필요합니다.");
                                    return;
                                }

                                var storageRootPathProvider = context.HttpContext.RequestServices
                                    .GetRequiredService<IStorageRootPathProvider>();
                                var uploadRoot = storageRootPathProvider.GetUploadRootPath();

                                try
                                {
                                    _ = new StoragePathPolicy(uploadRoot, targetPath);
                                }
                                catch (Exception ex)
                                {
                                    context.FailRequest(HttpStatusCode.BadRequest, ex.Message);
                                    return;
                                }

                                await Task.CompletedTask;
                            },
                            OnFileCompleteAsync = async context =>
                            {
                                await completionEventPublisher.PublishAsync(context);
                            }
                        }
                    });
            });

        return app;
    }

    private static bool TryGetRequiredMetadata(
        IReadOnlyDictionary<string, Metadata> metadata,
        string key,
        out string resolvedValue)
    {
        if (!metadata.TryGetValue(key, out var metadataItem))
        {
            resolvedValue = string.Empty;
            return false;
        }

        var metadataValue = metadataItem.GetString(Encoding.UTF8).Trim();
        if (string.IsNullOrWhiteSpace(metadataValue))
        {
            resolvedValue = string.Empty;
            return false;
        }

        resolvedValue = metadataValue;
        return true;
    }
}
