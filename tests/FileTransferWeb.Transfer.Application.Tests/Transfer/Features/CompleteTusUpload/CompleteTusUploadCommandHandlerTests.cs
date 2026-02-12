using FileTransferWeb.Transfer.Application.Features.CompleteTusUpload;
using FileTransferWeb.Transfer.Domain.Ports;
using Xunit;

namespace FileTransferWeb.Transfer.Application.Tests.Transfer.Features.CompleteTusUpload;

public class CompleteTusUploadCommandHandlerTests
{
    [Fact(DisplayName = "완료 명령 처리 시 기존 파일명을 조회하고 정책 파일명으로 이동을 요청한다")]
    public async Task Handle_ResolvesStoredName_AndMovesCompletedFile()
    {
        var completedUploadReader = new FakeCompletedUploadReader(
            new TransferCompletedUploadInfo("upload-1", "images", "photo.jpg", 123, "image/jpeg"));
        var reader = new FakeTargetFileNameReader(["photo.jpg", "photo (1).jpg"]);
        var mover = new FakeCompletedFileMover();
        var handler = new CompleteTusUploadCommandHandler(completedUploadReader, reader, mover);

        var command = new CompleteTusUploadCommand("upload-1");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, completedUploadReader.CallCount);
        Assert.Equal(1, reader.CallCount);
        Assert.Equal(1, mover.CallCount);
        Assert.Equal("upload-1", mover.LastUploadId);
        Assert.Equal("images", mover.LastTargetPath);
        Assert.Equal("photo (2).jpg", mover.LastStoredFileName);
    }

    [Fact(DisplayName = "이동 중 오류가 발생하면 예외를 그대로 전달한다")]
    public async Task Handle_Throws_WhenMoveFails()
    {
        var completedUploadReader = new FakeCompletedUploadReader(
            new TransferCompletedUploadInfo("upload-2", "images", "photo.jpg", 123, "image/jpeg"));
        var reader = new FakeTargetFileNameReader([]);
        var mover = new FakeCompletedFileMover { ThrowOnMove = true };
        var handler = new CompleteTusUploadCommandHandler(completedUploadReader, reader, mover);

        var command = new CompleteTusUploadCommand("upload-2");

        await Assert.ThrowsAsync<IOException>(() => handler.Handle(command, CancellationToken.None));
    }

    private sealed class FakeCompletedUploadReader(TransferCompletedUploadInfo completedUploadInfo)
        : ITransferCompletedUploadReader
    {
        private readonly TransferCompletedUploadInfo _completedUploadInfo = completedUploadInfo;
        public int CallCount { get; private set; }

        public ValueTask<TransferCompletedUploadInfo> ReadAsync(
            string uploadId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(_completedUploadInfo);
        }
    }

    private sealed class FakeTargetFileNameReader(IReadOnlyList<string> existingFileNames)
        : ITransferTargetFileNameReader
    {
        private readonly IReadOnlyList<string> _existingFileNames = existingFileNames;
        public int CallCount { get; private set; }

        public ValueTask<IReadOnlyList<string>> GetExistingFileNamesAsync(
            string targetRelativePath,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(_existingFileNames);
        }
    }

    private sealed class FakeCompletedFileMover : ITransferCompletedFileMover
    {
        public bool ThrowOnMove { get; set; }
        public int CallCount { get; private set; }
        public string LastUploadId { get; private set; } = string.Empty;
        public string LastTargetPath { get; private set; } = string.Empty;
        public string LastStoredFileName { get; private set; } = string.Empty;

        public ValueTask<string> MoveCompletedFileAsync(
            string uploadId,
            string targetRelativePath,
            string storedFileName,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastUploadId = uploadId;
            LastTargetPath = targetRelativePath;
            LastStoredFileName = storedFileName;

            if (ThrowOnMove)
            {
                throw new IOException("이동 실패");
            }

            return ValueTask.FromResult(
                string.IsNullOrEmpty(targetRelativePath)
                    ? storedFileName
                    : $"{targetRelativePath}/{storedFileName}");
        }
    }
}
