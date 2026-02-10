using FileTransferWeb.Storage.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace FileTransferWeb.Storage.Infrastructure.Tests.Storage;

public class UploadRootBootstrapHostedServiceTests
{
    [Fact(DisplayName = "업로드 루트 경로가 없으면 디렉터리를 생성한다")]
    public async Task StartAsync_CreatesDirectory_WhenUploadRootDoesNotExist()
    {
        var testRoot = Path.Combine(
            Path.GetTempPath(),
            $"ftw-{Guid.NewGuid():N}",
            "uploads");
        var parentDirectory = Directory.GetParent(testRoot)!.FullName;

        try
        {
            var service = CreateService(testRoot);

            await service.StartAsync(CancellationToken.None);

            Assert.True(Directory.Exists(testRoot));
        }
        finally
        {
            if (Directory.Exists(parentDirectory))
            {
                Directory.Delete(parentDirectory, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "업로드 루트가 파일 경로이면 초기화 예외를 발생시킨다")]
    public async Task StartAsync_Throws_WhenUploadRootIsNotDirectoryPath()
    {
        var tempFilePath = Path.GetTempFileName();

        try
        {
            var service = CreateService(tempFilePath);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.StartAsync(CancellationToken.None));
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact(DisplayName = "업로드 루트가 이미 존재하고 쓰기 가능하면 성공한다")]
    public async Task StartAsync_Succeeds_WhenUploadRootAlreadyExistsAndWritable()
    {
        var uploadRoot = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        Directory.CreateDirectory(uploadRoot);

        try
        {
            var service = CreateService(uploadRoot);

            await service.StartAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(uploadRoot))
            {
                Directory.Delete(uploadRoot, recursive: true);
            }
        }
    }

    private static UploadRootBootstrapHostedService CreateService(string uploadRoot)
    {
        var options = Options.Create(new StorageOptions
        {
            UploadRoot = uploadRoot
        });

        return new UploadRootBootstrapHostedService(options);
    }
}
