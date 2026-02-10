using FileTransferWeb.Storage.Infrastructure.Storage;
using Xunit;

namespace FileTransferWeb.Storage.Infrastructure.Tests.Storage;

public class StorageOptionsValidatorTests
{
    private readonly StorageOptionsValidator _validator = new();

    [Fact(DisplayName = "UploadRoot가 비어 있으면 검증에 실패한다")]
    public void Validate_Fails_WhenUploadRootIsMissing()
    {
        var options = new StorageOptions
        {
            UploadRoot = "   "
        };

        var result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
    }

    [Fact(DisplayName = "UploadRoot가 상대 경로이면 검증에 실패한다")]
    public void Validate_Fails_WhenUploadRootIsRelativePath()
    {
        var options = new StorageOptions
        {
            UploadRoot = "relative/path"
        };

        var result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
    }

    [Fact(DisplayName = "UploadRoot가 절대 경로이면 검증에 성공한다")]
    public void Validate_Succeeds_WhenUploadRootIsAbsolutePath()
    {
        var options = new StorageOptions
        {
            UploadRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "file-transfer-web-tests"))
        };

        var result = _validator.Validate(name: null, options);

        Assert.True(result.Succeeded);
    }
}
