using FileTransferWeb.Transfer.Application.Features.ValidateTusUploadCreate;
using FileTransferWeb.Transfer.Domain.Exceptions;
using FileTransferWeb.Transfer.Domain.Ports;
using Xunit;

namespace FileTransferWeb.Transfer.Application.Tests.Transfer.Features.ValidateTusUploadCreate;

public class ValidateTusUploadCreateCommandHandlerTests
{
    [Fact(DisplayName = "업로드 생성 검증 명령이 들어오면 validator를 1회 호출한다")]
    public async Task Handle_CallsValidatorOnce()
    {
        var validator = new FakeValidator();
        var handler = new ValidateTusUploadCreateCommandHandler(validator);
        var command = new ValidateTusUploadCreateCommand("images", "photo.jpg");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, validator.CallCount);
        Assert.Equal("images", validator.LastTargetPath);
        Assert.Equal("photo.jpg", validator.LastFileName);
    }

    [Fact(DisplayName = "validator가 예외를 던지면 예외를 그대로 전파한다")]
    public async Task Handle_Throws_WhenValidatorThrows()
    {
        var validator = new FakeValidator
        {
            ExceptionToThrow = new TransferDomainException("파일 이름 메타데이터가 비어 있습니다.")
        };
        var handler = new ValidateTusUploadCreateCommandHandler(validator);
        var command = new ValidateTusUploadCreateCommand("images", "");

        await Assert.ThrowsAsync<TransferDomainException>(() => handler.Handle(command, CancellationToken.None));
    }

    private sealed class FakeValidator : ITransferUploadCreateValidator
    {
        public int CallCount { get; private set; }
        public string LastTargetPath { get; private set; } = string.Empty;
        public string LastFileName { get; private set; } = string.Empty;
        public Exception? ExceptionToThrow { get; set; }

        public ValueTask ValidateAsync(
            string targetPath,
            string fileName,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastTargetPath = targetPath;
            LastFileName = fileName;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return ValueTask.CompletedTask;
        }
    }
}
