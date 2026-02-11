using FileTransferWeb.Transfer.Application.Features.UploadFiles;
using FileTransferWeb.Transfer.Domain;
using FileTransferWeb.Transfer.Domain.Models;
using FileTransferWeb.Transfer.Domain.Ports;
using Xunit;

namespace FileTransferWeb.Transfer.Application.Tests.Transfer.Features.UploadFiles;

public class UploadFilesCommandHandlerTests
{
    [Fact(DisplayName = "핸들러는 파일명 조회를 한 번 수행하고 정책 결과를 저장 포트로 전달한다")]
    public async Task Handle_LoadsNamesOnce_AndPassesPlannedFilesToStore()
    {
        var reader = new FakeTransferTargetFileNameReader(["report.txt", "report (1).txt"]);
        var store = new FakeTransferFileStore();
        var handler = new UploadFilesCommandHandler(reader, store);

        var command = new UploadFilesCommand(
            "docs",
            [
                new UploadFileInput("report.txt", 10, CreateStreamFactory("first")),
                new UploadFileInput("report.txt", 20, CreateStreamFactory("second"))
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reader.CallCount);
        Assert.Equal(1, store.CallCount);
        Assert.Equal(["report (2).txt", "report (3).txt"], store.LastStoredFileNames);
        Assert.Equal(UploadStatus.Completed, result.Status);
        Assert.Equal(2, result.Summary.SuccessCount);
    }

    [Fact(DisplayName = "핸들러는 원본 파일 이름을 정규화한 계획으로 저장을 요청한다")]
    public async Task Handle_NormalizesOriginalFileName_BeforeSaving()
    {
        var reader = new FakeTransferTargetFileNameReader([]);
        var store = new FakeTransferFileStore();
        var handler = new UploadFilesCommandHandler(reader, store);

        var command = new UploadFilesCommand(
            "images",
            [new UploadFileInput("../unsafe/path/photo.png", 10, CreateStreamFactory("file"))]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(store.LastOriginalFileNames);
        Assert.Equal("photo.png", store.LastOriginalFileNames[0]);
        Assert.Single(result.SavedFiles);
        Assert.Equal("photo.png", result.SavedFiles[0].OriginalFileName);
    }

    private static Func<Stream> CreateStreamFactory(string content)
    {
        return () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    }

    private sealed class FakeTransferTargetFileNameReader(IReadOnlyList<string> fileNames)
        : ITransferTargetFileNameReader
    {
        private readonly IReadOnlyList<string> _fileNames = fileNames;

        public int CallCount { get; private set; }

        public ValueTask<IReadOnlyList<string>> GetExistingFileNamesAsync(
            string targetRelativePath,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(_fileNames);
        }
    }

    private sealed class FakeTransferFileStore : ITransferFileStore
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<string> LastStoredFileNames { get; private set; } = [];

        public IReadOnlyList<string> LastOriginalFileNames { get; private set; } = [];

        public ValueTask<IReadOnlyList<UploadFileResult>> SaveFilesAsync(
            string targetRelativePath,
            IReadOnlyList<UploadFilePlanItem> plannedFiles,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastStoredFileNames = plannedFiles.Select(file => file.StoredFileName).ToArray();
            LastOriginalFileNames = plannedFiles.Select(file => file.OriginalFileName).ToArray();

            var results = plannedFiles
                .Select(file => UploadFileResult.Succeeded(
                    file.OriginalFileName,
                    file.StoredFileName,
                    CombineRelativePath(targetRelativePath, file.StoredFileName),
                    file.SizeBytes))
                .ToArray();

            return ValueTask.FromResult<IReadOnlyList<UploadFileResult>>(results);
        }

        private static string CombineRelativePath(string currentRelativePath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(currentRelativePath))
            {
                return fileName;
            }

            return $"{currentRelativePath}/{fileName}";
        }
    }
}
