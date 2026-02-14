using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FileTransferWeb.Web.Tests.Transfer.Endpoints;

public class TusEndpointsTests
{
    [Fact(DisplayName = "tus 업로드 후 배치 완료 API를 호출하면 최종 파일이 저장된다")]
    public async Task TusUpload_SavesFile_WhenBatchCompleteIsCalled()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var batchId = await CreateBatchAsync(client, "images", 1);
        var fileBytes = Encoding.UTF8.GetBytes("hello-tus");
        var createResponse = await SendCreateRequestAsync(
            client,
            uploadLength: fileBytes.Length,
            targetPath: "images",
            fileName: "photo.txt",
            batchId: batchId);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var uploadUrl = GetUploadUrl(createResponse);

        var headBeforePatch = await SendHeadRequestAsync(client, uploadUrl);
        Assert.True(headBeforePatch.IsSuccessStatusCode);
        Assert.Equal("0", GetRequiredHeaderValue(headBeforePatch, "Upload-Offset"));

        var patchResponse = await SendPatchRequestAsync(client, uploadUrl, fileBytes);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);
        Assert.Equal(fileBytes.Length.ToString(), GetRequiredHeaderValue(patchResponse, "Upload-Offset"));

        var savedFilePath = Path.Combine(roots.UploadRoot, "images", "photo.txt");
        await Task.Delay(200);
        Assert.False(File.Exists(savedFilePath));

        var completeResponse = await client.PostAsync($"/api/transfer/batches/{batchId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        await WaitForFileAsync(savedFilePath, TimeSpan.FromSeconds(3));

        Assert.True(File.Exists(savedFilePath));
        Assert.Equal("hello-tus", await File.ReadAllTextAsync(savedFilePath));
    }

    [Fact(DisplayName = "targetPath 메타데이터가 루트 밖 경로이면 400을 반환한다")]
    public async Task TusUpload_ReturnsBadRequest_WhenTargetPathIsOutsideRoot()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var response = await SendCreateRequestAsync(
            client,
            uploadLength: 10,
            targetPath: "../outside",
            fileName: "photo.txt",
            batchId: await CreateBatchAsync(client, "images", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "batchId 메타데이터가 없으면 400을 반환한다")]
    public async Task TusUpload_ReturnsBadRequest_WhenBatchIdMetadataIsMissing()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfer/uploads");
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Length", "10");
        request.Headers.Add(
            "Upload-Metadata",
            BuildMetadata(("targetPath", "images"), ("fileName", "photo.txt")));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "존재하지 않는 batchId로 업로드 완료 시 400을 반환한다")]
    public async Task TusUpload_ReturnsBadRequest_WhenBatchIdDoesNotExistOnComplete()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var fileBytes = Encoding.UTF8.GetBytes("missing-batch");
        var createResponse = await SendCreateRequestAsync(
            client,
            uploadLength: fileBytes.Length,
            targetPath: "images",
            fileName: "photo.txt",
            batchId: Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var uploadUrl = GetUploadUrl(createResponse);
        var patchResponse = await SendPatchRequestAsync(client, uploadUrl, fileBytes);

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendCreateRequestAsync(
        HttpClient client,
        int uploadLength,
        string targetPath,
        string fileName,
        Guid batchId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfer/uploads");
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Length", uploadLength.ToString());
        request.Headers.Add(
            "Upload-Metadata",
            BuildMetadata(
                ("targetPath", targetPath),
                ("fileName", fileName),
                ("batchId", batchId.ToString())));

        return await client.SendAsync(request);
    }

    private static async Task<Guid> CreateBatchAsync(HttpClient client, string targetPath, int expectedFileCount)
    {
        var response = await client.PostAsJsonAsync(
            "/api/transfer/batches",
            new
            {
                targetPath,
                expectedFileCount
            });
        response.EnsureSuccessStatusCode();

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return content.RootElement.GetProperty("batchId").GetGuid();
    }

    private static async Task<HttpResponseMessage> SendHeadRequestAsync(HttpClient client, Uri uploadUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, uploadUrl);
        request.Headers.Add("Tus-Resumable", "1.0.0");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendPatchRequestAsync(
        HttpClient client,
        Uri uploadUrl,
        byte[] content)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
        request.Headers.Add("Tus-Resumable", "1.0.0");
        request.Headers.Add("Upload-Offset", "0");
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

        return await client.SendAsync(request);
    }

    private static Uri GetUploadUrl(HttpResponseMessage response)
    {
        Assert.NotNull(response.Headers.Location);
        return response.Headers.Location!;
    }

    private static string BuildMetadata(params (string Key, string Value)[] entries)
    {
        return string.Join(
            ",",
            entries.Select(entry =>
                $"{entry.Key} {Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.Value))}"));
    }

    private static string GetRequiredHeaderValue(HttpResponseMessage response, string headerName)
    {
        Assert.True(response.Headers.TryGetValues(headerName, out var values));
        return values.Single();
    }

    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                return;
            }

            await Task.Delay(50);
        }
    }

    private sealed class TransferApiFactory(string uploadRoot, string tusTempRoot) : WebApplicationFactory<Program>
    {
        private readonly string _uploadRoot = uploadRoot;
        private readonly string _tusTempRoot = tusTempRoot;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Storage:UploadRoot"] = _uploadRoot,
                        ["Transfer:Tus:TempRoot"] = _tusTempRoot,
                        ["Transfer:Tus:ExpirationHours"] = "24",
                        ["Transfer:Tus:MaxSizeBytes"] = "10485760"
                    });
            });
        }
    }

    private sealed class TemporaryRoots : IDisposable
    {
        public TemporaryRoots()
        {
            var root = Path.Combine(Path.GetTempPath(), $"ftw-tus-tests-{Guid.NewGuid():N}");
            UploadRoot = Path.Combine(root, "upload-root");
            TusTempRoot = Path.Combine(root, "tus-temp");
            Directory.CreateDirectory(UploadRoot);
            Directory.CreateDirectory(TusTempRoot);
        }

        public string UploadRoot { get; }

        public string TusTempRoot { get; }

        public void Dispose()
        {
            var baseRoot = Path.GetDirectoryName(UploadRoot);
            if (!string.IsNullOrWhiteSpace(baseRoot) && Directory.Exists(baseRoot))
            {
                Directory.Delete(baseRoot, recursive: true);
            }
        }
    }
}
