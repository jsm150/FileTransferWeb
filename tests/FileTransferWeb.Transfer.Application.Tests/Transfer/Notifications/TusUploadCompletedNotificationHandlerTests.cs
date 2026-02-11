using FileTransferWeb.Transfer.Application.Notifications;
using FileTransferWeb.Transfer.Domain.Ports;
using Xunit;

namespace FileTransferWeb.Transfer.Application.Tests.Transfer.Notifications;

public class TusUploadCompletedNotificationHandlerTests
{
    [Fact(DisplayName = "완료 이벤트 처리 시 기존 파일명을 조회하고 정책 파일명으로 이동을 요청한다")]
    public async Task Handle_ResolvesStoredName_AndMovesCompletedFile()
    {
        var reader = new FakeTargetFileNameReader(["photo.jpg", "photo (1).jpg"]);
        var mover = new FakeCompletedFileMover();
        var handler = new TusUploadCompletedNotificationHandler(reader, mover);

        var notification = new TusUploadCompletedNotification(
            "upload-1",
            "images",
            "photo.jpg",
            "/tmp/upload-1",
            123,
            "image/jpeg");

        await handler.Handle(notification, CancellationToken.None);

        Assert.Equal(1, reader.CallCount);
        Assert.Equal(1, mover.CallCount);
        Assert.Equal("upload-1", mover.LastUploadId);
        Assert.Equal("images", mover.LastTargetPath);
        Assert.Equal("photo (2).jpg", mover.LastStoredFileName);
    }

    [Fact(DisplayName = "이동 중 오류가 발생하면 예외를 그대로 전달한다")]
    public async Task Handle_Throws_WhenMoveFails()
    {
        var reader = new FakeTargetFileNameReader([]);
        var mover = new FakeCompletedFileMover { ThrowOnMove = true };
        var handler = new TusUploadCompletedNotificationHandler(reader, mover);

        var notification = new TusUploadCompletedNotification(
            "upload-2",
            "images",
            "photo.jpg",
            "/tmp/upload-2",
            123,
            "image/jpeg");

        await Assert.ThrowsAsync<IOException>(() => handler.Handle(notification, CancellationToken.None));
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
