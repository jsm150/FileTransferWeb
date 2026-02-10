using Microsoft.Extensions.Options;

namespace FileTransferWeb.Infrastructure.Storage;

public sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UploadRoot))
        {
            return ValidateOptionsResult.Fail("Storage:UploadRoot 설정이 필요합니다.");
        }

        if (!Path.IsPathFullyQualified(options.UploadRoot))
        {
            return ValidateOptionsResult.Fail("업로드 루트 경로는 절대 경로여야 합니다.");
        }

        return ValidateOptionsResult.Success;
    }
}
