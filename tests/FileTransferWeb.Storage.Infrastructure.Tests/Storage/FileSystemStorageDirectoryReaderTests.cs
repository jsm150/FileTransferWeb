using FileTransferWeb.Storage.Infrastructure.Storage;
using Xunit;

namespace FileTransferWeb.Storage.Infrastructure.Tests.Storage;

public class FileSystemStorageDirectoryReaderTests
{
    [Fact(DisplayName = "디렉터리 리더는 폴더 이름만 반환한다")]
    public async Task GetDirectoryNamesAsync_ReturnsOnlyDirectories()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"ftw-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        var firstDirectory = Path.Combine(rootPath, "a");
        var secondDirectory = Path.Combine(rootPath, "b");
        var filePath = Path.Combine(rootPath, "file.txt");

        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);
        await File.WriteAllTextAsync(filePath, "sample");

        try
        {
            var reader = new FileSystemStorageDirectoryReader();

            var result = await reader.GetDirectoryNamesAsync(rootPath, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Contains("a", result);
            Assert.Contains("b", result);
            Assert.DoesNotContain("file.txt", result);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }
}
