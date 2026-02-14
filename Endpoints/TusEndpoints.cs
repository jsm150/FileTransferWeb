using System.Net;
using System.Text;
using FileTransferWeb.Domain.Shared;
using FileTransferWeb.Transfer.Application.Features.TransferBatches.RegisterCompletedTusUpload;
using FileTransferWeb.Transfer.Application.Features.ValidateTusUploadCreate;
using FileTransferWeb.Storage.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Infrastructure.Tus;
using MediatR;
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

                                if (!TryGetRequiredMetadata(context.Metadata, "batchId", out var batchIdText))
                                {
                                    context.FailRequest(HttpStatusCode.BadRequest, "batchId 메타데이터가 필요합니다.");
                                    return;
                                }

                                if (!Guid.TryParse(batchIdText, out _))
                                {
                                    context.FailRequest(HttpStatusCode.BadRequest, "batchId 메타데이터 형식이 올바르지 않습니다.");
                                    return;
                                }

                                var sender = context.HttpContext.RequestServices.GetRequiredService<ISender>();

                                try
                                {
                                    await sender.Send(
                                        new ValidateTusUploadCreateCommand(targetPath, fileName),
                                        context.CancellationToken);
                                }
                                catch (Exception ex) when (ex is DomainException
                                                           or TransferDomainException
                                                           or StorageDomainException)
                                {
                                    context.FailRequest(HttpStatusCode.BadRequest, ex.Message);
                                    return;
                                }
                            },
                            OnFileCompleteAsync = async context =>
                            {
                                if (string.IsNullOrWhiteSpace(context.FileId))
                                {
                                    throw new InvalidOperationException("업로드 식별자를 확인할 수 없습니다.");
                                }

                                var storeFactory = context.HttpContext.RequestServices.GetRequiredService<FileSystemTusStoreFactory>();
                                var store = storeFactory.CreateStore();
                                var tusFile = await store.GetFileAsync(context.FileId, context.CancellationToken)
                                              ?? throw new InvalidOperationException("업로드 파일 정보를 찾을 수 없습니다.");

                                var metadata = await tusFile.GetMetadataAsync(context.CancellationToken);
                                if (!TryGetRequiredMetadata(metadata, "batchId", out var batchIdText))
                                {
                                    throw new InvalidOperationException("batchId 메타데이터가 필요합니다.");
                                }

                                if (!Guid.TryParse(batchIdText, out var batchId))
                                {
                                    throw new InvalidOperationException("batchId 메타데이터 형식이 올바르지 않습니다.");
                                }

                                var sender = context.HttpContext.RequestServices.GetRequiredService<ISender>();
                                await sender.Send(
                                    new RegisterCompletedTusUploadCommand(batchId, context.FileId),
                                    context.CancellationToken);
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
