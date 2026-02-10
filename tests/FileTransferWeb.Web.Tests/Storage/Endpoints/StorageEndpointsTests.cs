using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FileTransferWeb.Web.Tests.Storage.Endpoints;

public class StorageEndpointsTests
{
    [Fact(DisplayName = "relativePath 없이 조회하면 루트 폴더 목록을 반환한다")]
    public async Task GetDirectories_ReturnsRootListing_WhenRelativePathIsMissing()
    {
        using var tempDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.DirectoryPath, "zeta"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.DirectoryPath, "Alpha"));

        await using var factory = new StorageApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/storage/directories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;

        Assert.Equal(string.Empty, root.GetProperty("currentPath").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("parentPath").ValueKind);

        var directories = root.GetProperty("directories").EnumerateArray().ToArray();
        Assert.Equal(2, directories.Length);
        Assert.Equal("Alpha", directories[0].GetProperty("name").GetString());
        Assert.Equal("Alpha", directories[0].GetProperty("relativePath").GetString());
        Assert.Equal("zeta", directories[1].GetProperty("name").GetString());
    }

    [Fact(DisplayName = "하위 경로 조회 시 현재/부모 경로와 자식 경로를 반환한다")]
    public async Task GetDirectories_ReturnsNestedListing_WhenRelativePathIsValid()
    {
        using var tempDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.DirectoryPath, "images", "2026", "trip"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.DirectoryPath, "images", "2026", "Docs"));

        await using var factory = new StorageApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/storage/directories?relativePath=images/2026");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;

        Assert.Equal("images/2026", root.GetProperty("currentPath").GetString());
        Assert.Equal("images", root.GetProperty("parentPath").GetString());

        var directories = root.GetProperty("directories").EnumerateArray().ToArray();
        Assert.Equal("Docs", directories[0].GetProperty("name").GetString());
        Assert.Equal("images/2026/Docs", directories[0].GetProperty("relativePath").GetString());
        Assert.Equal("trip", directories[1].GetProperty("name").GetString());
    }

    [Fact(DisplayName = "루트 밖 경로를 요청하면 400 ProblemDetails를 반환한다")]
    public async Task GetDirectories_ReturnsBadRequest_WhenPathTraversalIsRequested()
    {
        using var tempDirectory = new TemporaryDirectory();

        await using var factory = new StorageApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/storage/directories?relativePath=../outside");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = content.RootElement;

        Assert.Equal("잘못된 디렉터리 경로 요청입니다.", problem.GetProperty("title").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
    }

    [Fact(DisplayName = "존재하지 않는 경로를 요청하면 404 ProblemDetails를 반환한다")]
    public async Task GetDirectories_ReturnsNotFound_WhenDirectoryDoesNotExist()
    {
        using var tempDirectory = new TemporaryDirectory();

        await using var factory = new StorageApiFactory(tempDirectory.DirectoryPath);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/storage/directories?relativePath=not-found/path");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = content.RootElement;

        Assert.Equal("디렉터리를 찾을 수 없습니다.", problem.GetProperty("title").GetString());
        Assert.Equal("요청한 디렉터리가 존재하지 않습니다.", problem.GetProperty("detail").GetString());
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
    }

    private sealed class StorageApiFactory(string uploadRoot) : WebApplicationFactory<Program>
    {
        private readonly string _uploadRoot = uploadRoot;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [ "Storage:UploadRoot" ] = _uploadRoot
                });
            });
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ftw-web-tests-{Guid.NewGuid():N}");
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
