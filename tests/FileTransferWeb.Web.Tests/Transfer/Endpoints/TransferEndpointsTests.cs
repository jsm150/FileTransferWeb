using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FileTransferWeb.Web.Tests.Transfer.Endpoints;

public class TransferEndpointsTests
{
    [Fact(DisplayName = "유효한 파일 업로드 요청이면 저장 결과를 반환한다")]
    public async Task UploadFiles_ReturnsOk_WhenRequestIsValid()
    {
        using var tempDirectory = new TemporaryDirectory();
        await using var factory = new TransferApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        using var form = BuildUploadForm(
            targetPath: "images",
            ("photo.txt", "hello-transfer"));

        var response = await client.PostAsync("/api/transfer/uploads", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("images", root.GetProperty("targetPath").GetString());

        var summary = root.GetProperty("summary");
        Assert.Equal(1, summary.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, summary.GetProperty("successCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("failedCount").GetInt32());

        var savedFiles = root.GetProperty("savedFiles").EnumerateArray().ToArray();
        Assert.Single(savedFiles);
        Assert.Equal("photo.txt", savedFiles[0].GetProperty("originalFileName").GetString());

        var storedRelativePath = savedFiles[0].GetProperty("relativePath").GetString();
        Assert.Equal("images/photo.txt", storedRelativePath);
        Assert.True(File.Exists(Path.Combine(tempDirectory.DirectoryPath, "images", "photo.txt")));
    }

    [Fact(DisplayName = "루트 밖 경로 업로드 요청이면 400 ProblemDetails를 반환한다")]
    public async Task UploadFiles_ReturnsBadRequest_WhenPathTraversalIsRequested()
    {
        using var tempDirectory = new TemporaryDirectory();
        await using var factory = new TransferApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        using var form = BuildUploadForm(
            targetPath: "../outside",
            ("photo.txt", "hello-transfer"));

        var response = await client.PostAsync("/api/transfer/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = json.RootElement;

        Assert.Equal("잘못된 디렉터리 경로 요청입니다.", problem.GetProperty("title").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
    }

    [Fact(DisplayName = "업로드 파일이 비어 있으면 400 ProblemDetails를 반환한다")]
    public async Task UploadFiles_ReturnsBadRequest_WhenFilesAreMissing()
    {
        using var tempDirectory = new TemporaryDirectory();
        await using var factory = new TransferApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        using var form = BuildUploadForm(targetPath: "images");

        var response = await client.PostAsync("/api/transfer/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = json.RootElement;

        Assert.Equal("업로드 요청이 올바르지 않습니다.", problem.GetProperty("title").GetString());
        Assert.Equal("업로드할 파일이 없습니다.", problem.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "동일 파일명을 여러 개 올리면 중복 파일명을 자동 변경한다")]
    public async Task UploadFiles_RenamesDuplicateNames_WhenSameFileNameIsUploaded()
    {
        using var tempDirectory = new TemporaryDirectory();
        await using var factory = new TransferApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        using var form = BuildUploadForm(
            targetPath: "docs",
            ("report.txt", "first"),
            ("report.txt", "second"));

        var response = await client.PostAsync("/api/transfer/uploads", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        var savedFiles = root.GetProperty("savedFiles").EnumerateArray().ToArray();
        Assert.Equal(2, savedFiles.Length);

        Assert.Equal("report.txt", savedFiles[0].GetProperty("storedFileName").GetString());
        Assert.Equal("report (1).txt", savedFiles[1].GetProperty("storedFileName").GetString());

        Assert.True(File.Exists(Path.Combine(tempDirectory.DirectoryPath, "docs", "report.txt")));
        Assert.True(File.Exists(Path.Combine(tempDirectory.DirectoryPath, "docs", "report (1).txt")));
    }

    private static MultipartFormDataContent BuildUploadForm(
        string? targetPath,
        params (string FileName, string Content)[] files)
    {
        var form = new MultipartFormDataContent();

        if (targetPath is not null)
        {
            form.Add(new StringContent(targetPath), "targetPath");
        }

        foreach (var file in files)
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(file.Content));
            form.Add(fileContent, "files", file.FileName);
        }

        return form;
    }

    private sealed class TransferApiFactory(string uploadRoot) : WebApplicationFactory<Program>
    {
        private readonly string _uploadRoot = uploadRoot;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Storage:UploadRoot"] = _uploadRoot
                    });
            });
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                $"ftw-transfer-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
