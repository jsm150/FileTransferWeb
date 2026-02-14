using System.Text;
using FileTransferWeb.Transfer.Domain.Ports;
using tusdotnet.Models;

namespace FileTransferWeb.Transfer.Infrastructure.Tus;

public sealed class TusCompletedUploadReader(FileSystemTusStoreFactory tusStoreFactory)
    : ITransferCompletedUploadReader
{
    private readonly FileSystemTusStoreFactory _tusStoreFactory = tusStoreFactory;

    public async ValueTask<TransferCompletedUploadInfo> ReadAsync(
        string uploadId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new InvalidOperationException("업로드 식별자를 확인할 수 없습니다.");
        }

        var store = _tusStoreFactory.CreateStore();
        var tusFile = await store.GetFileAsync(uploadId, cancellationToken) 
            ?? throw new InvalidOperationException("업로드 파일 정보를 찾을 수 없습니다.");

        var metadata = await tusFile.GetMetadataAsync(cancellationToken);
        var batchIdText = ReadRequiredMetadata(metadata, "batchId");
        if (!Guid.TryParse(batchIdText, out var batchId))
        {
            throw new InvalidOperationException("batchId 메타데이터 형식이 올바르지 않습니다.");
        }

        var targetPath = ReadRequiredMetadata(metadata, "targetPath");
        var originalFileName = ReadRequiredMetadata(metadata, "fileName");
        var contentType = ReadOptionalMetadata(metadata, "contentType");

        long fileSizeBytes;
        await using (var contentStream = await tusFile.GetContentAsync(cancellationToken))
        {
            fileSizeBytes = contentStream.CanSeek ? contentStream.Length : 0;
        }

        return new TransferCompletedUploadInfo(
            uploadId,
            batchId,
            targetPath,
            originalFileName,
            fileSizeBytes,
            contentType);
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
