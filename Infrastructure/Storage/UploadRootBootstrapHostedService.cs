using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FileTransferWeb.Infrastructure.Storage;

public sealed class UploadRootBootstrapHostedService(IOptions<StorageOptions> storageOptions) : IHostedService
{
    private readonly StorageOptions _storageOptions = storageOptions.Value;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var fullUploadRoot = Path.GetFullPath(_storageOptions.UploadRoot);

        try
        {
            Directory.CreateDirectory(fullUploadRoot);

            var writeTestFilePath = Path.Combine(
                fullUploadRoot,
                $".write-test-{Guid.NewGuid():N}.tmp");

            using (File.Create(writeTestFilePath))
            {
            }

            File.Delete(writeTestFilePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"업로드 루트 초기화에 실패했습니다. 경로 '{fullUploadRoot}'의 디렉터리 존재 여부와 쓰기 권한을 확인하세요.",
                ex);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
