using System.Text;
using FileTransferWeb.Transfer.Application.Notifications;
using MediatR;
using Microsoft.Extensions.Options;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace FileTransferWeb.Transfer.Infrastructure.Tus;

public sealed class TusCompletionEventPublisher(
    IPublisher publisher,
    IOptions<TransferTusOptions> options)
{
    private readonly IPublisher _publisher = publisher;
    private readonly TransferTusOptions _options = options.Value;

    public async Task PublishAsync(FileCompleteContext context)
    {
        var uploadId = context.FileId;
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new InvalidOperationException("업로드 식별자를 확인할 수 없습니다.");
        }

        var uploadedFile = await context.GetFileAsync();
        if (uploadedFile is null)
        {
            throw new InvalidOperationException("업로드 파일 정보를 찾을 수 없습니다.");
        }

        var metadata = await uploadedFile.GetMetadataAsync(context.CancellationToken);
        var targetPath = ReadRequiredMetadata(metadata, "targetPath");
        var originalFileName = ReadRequiredMetadata(metadata, "fileName");
        var contentType = ReadOptionalMetadata(metadata, "contentType");

        long fileSizeBytes;
        await using (var contentStream = await uploadedFile.GetContentAsync(context.CancellationToken))
        {
            fileSizeBytes = contentStream.CanSeek ? contentStream.Length : 0;
        }

        var tempFilePath = Path.Combine(ResolveTempRootPath(), uploadId);

        await _publisher.Publish(
            new TusUploadCompletedNotification(
                uploadId,
                targetPath,
                originalFileName,
                tempFilePath,
                fileSizeBytes,
                contentType),
            context.CancellationToken);
    }

    private string ResolveTempRootPath()
    {
        if (string.IsNullOrWhiteSpace(_options.TempRoot))
        {
            throw new InvalidOperationException("Transfer:Tus:TempRoot 설정이 필요합니다.");
        }

        return Path.GetFullPath(_options.TempRoot);
    }

    private static string ReadRequiredMetadata(
        IReadOnlyDictionary<string, Metadata> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"{key} 메타데이터가 필요합니다.");
        }

        var text = value.GetString(Encoding.UTF8).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"{key} 메타데이터가 비어 있습니다.");
        }

        return text;
    }

    private static string? ReadOptionalMetadata(
        IReadOnlyDictionary<string, Metadata> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            return null;
        }

        var text = value.GetString(Encoding.UTF8).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
