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

public class TransferBatchEndpointsTests
{
    [Fact(DisplayName = "배치 생성 요청 시 배치 식별자를 반환한다")]
    public async Task CreateBatch_ReturnsBatchInfo()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transfer/batches",
            new
            {
                targetPath = "images",
                expectedFileCount = 2
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;
        Assert.NotEqual(Guid.Empty, root.GetProperty("batchId").GetGuid());
        Assert.Equal("images", root.GetProperty("targetPath").GetString());
        Assert.Equal(2, root.GetProperty("expectedFileCount").GetInt32());
    }

    [Fact(DisplayName = "배치 상태 조회 시 수집중 상태를 반환한다")]
    public async Task GetBatchStatus_ReturnsCollectingStatus()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var batchId = await CreateBatchAsync(client, "images", 2);
        var response = await client.GetAsync($"/api/transfer/batches/{batchId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;
        Assert.Equal(batchId, root.GetProperty("batchId").GetGuid());
        Assert.Equal(0, root.GetProperty("status").GetInt32());
        Assert.Equal(0, root.GetProperty("completedUploadCount").GetInt32());
        Assert.Equal(0, root.GetProperty("files").GetArrayLength());
    }

    [Fact(DisplayName = "배치 완료 호출 시 성공 조건에 따라 Completed 상태를 반환한다")]
    public async Task CompleteBatch_ReturnsCompletedStatus_WhenAllExpectedUploadsArrived()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var batchId = await CreateBatchAsync(client, "images/completed", 1);
        _ = await UploadSingleTusFileAsync(
            client,
            batchId,
            "images/completed",
            "photo.txt",
            Encoding.UTF8.GetBytes("completed-content"));

        var response = await client.PostAsync($"/api/transfer/batches/{batchId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;
        Assert.Equal(1, root.GetProperty("status").GetInt32());
        Assert.Equal(1, root.GetProperty("completedUploadCount").GetInt32());

        var statusResponse = await client.GetAsync($"/api/transfer/batches/{batchId}");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        using var statusContent = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        var statusRoot = statusContent.RootElement;
        Assert.Equal(1, statusRoot.GetProperty("status").GetInt32());
        Assert.Equal(1, statusRoot.GetProperty("completedUploadCount").GetInt32());
        Assert.Equal(1, statusRoot.GetProperty("files").GetArrayLength());

        var fileResult = statusRoot.GetProperty("files")[0];
        Assert.Equal("photo.txt", fileResult.GetProperty("originalFileName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(fileResult.GetProperty("storedFileName").GetString()));
        Assert.True(fileResult.GetProperty("failureReason").ValueKind is JsonValueKind.Null);
    }

    [Fact(DisplayName = "배치 완료 호출 시 일부 파일만 도착했으면 PartiallyCompleted 상태를 반환한다")]
    public async Task CompleteBatch_ReturnsPartiallyCompletedStatus_WhenExpectedCountNotMet()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var batchId = await CreateBatchAsync(client, "images/partial", 2);
        _ = await UploadSingleTusFileAsync(
            client,
            batchId,
            "images/partial",
            "photo-ok.txt",
            Encoding.UTF8.GetBytes("partial-content-ok"));

        var failedUploadId = await UploadSingleTusFileAsync(
            client,
            batchId,
            "images/partial",
            "photo-fail.txt",
            Encoding.UTF8.GetBytes("partial-content-fail"));
        DeleteTusUploadArtifacts(roots.TusTempRoot, failedUploadId);

        var response = await client.PostAsync($"/api/transfer/batches/{batchId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var content = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;
        Assert.Equal(2, root.GetProperty("status").GetInt32());
        Assert.Equal(2, root.GetProperty("completedUploadCount").GetInt32());

        var statusResponse = await client.GetAsync($"/api/transfer/batches/{batchId}");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        using var statusContent = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        var statusRoot = statusContent.RootElement;
        Assert.Equal(2, statusRoot.GetProperty("status").GetInt32());
        Assert.Equal(2, statusRoot.GetProperty("completedUploadCount").GetInt32());
        Assert.Equal(2, statusRoot.GetProperty("files").GetArrayLength());

        var files = statusRoot.GetProperty("files").EnumerateArray().ToArray();
        Assert.Single(files, IsSucceededFileResult);
        Assert.Single(files, IsFailedFileResult);
    }

    [Fact(DisplayName = "존재하지 않는 배치 식별자는 400을 반환한다")]
    public async Task GetBatchStatus_ReturnsBadRequest_WhenBatchDoesNotExist()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot);
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/transfer/batches/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "만료된 배치 식별자는 조회와 완료에서 400을 반환한다")]
    public async Task BatchEndpoints_ReturnBadRequest_WhenBatchExpired()
    {
        using var roots = new TemporaryRoots();
        await using var factory = new TransferApiFactory(roots.UploadRoot, roots.TusTempRoot, batchSlidingTtlMinutes: 0);
        using var client = factory.CreateClient();

        var batchId = await CreateBatchAsync(client, "images/expired", 1);

        var statusResponse = await client.GetAsync($"/api/transfer/batches/{batchId}");
        var completeResponse = await client.PostAsync($"/api/transfer/batches/{batchId}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, completeResponse.StatusCode);
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

    private static async Task<string> UploadSingleTusFileAsync(
        HttpClient client,
        Guid batchId,
        string targetPath,
        string fileName,
        byte[] bytes)
    {
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/transfer/uploads");
        createRequest.Headers.Add("Tus-Resumable", "1.0.0");
        createRequest.Headers.Add("Upload-Length", bytes.Length.ToString());
        createRequest.Headers.Add(
            "Upload-Metadata",
            BuildMetadata(
                ("targetPath", targetPath),
                ("fileName", fileName),
                ("batchId", batchId.ToString())));

        var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();

        var uploadUrl = createResponse.Headers.Location!;
        var uploadId = uploadUrl.ToString().TrimEnd('/').Split('/').Last();

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
        patchRequest.Headers.Add("Tus-Resumable", "1.0.0");
        patchRequest.Headers.Add("Upload-Offset", "0");
        patchRequest.Content = new ByteArrayContent(bytes);
        patchRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

        var patchResponse = await client.SendAsync(patchRequest);
        patchResponse.EnsureSuccessStatusCode();

        return uploadId;
    }

    private static bool IsSucceededFileResult(JsonElement fileResult)
    {
        return fileResult.GetProperty("failureReason").ValueKind is JsonValueKind.Null;
    }

    private static bool IsFailedFileResult(JsonElement fileResult)
    {
        var failureReason = fileResult.GetProperty("failureReason");
        return failureReason.ValueKind is JsonValueKind.String
               && !string.IsNullOrWhiteSpace(failureReason.GetString());
    }

    private static void DeleteTusUploadArtifacts(string tusTempRoot, string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId) || !Directory.Exists(tusTempRoot))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(tusTempRoot, "*", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(filePath).Contains(uploadId, StringComparison.Ordinal))
            {
                File.Delete(filePath);
            }
        }

        var directories = Directory.EnumerateDirectories(tusTempRoot, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains(uploadId, StringComparison.Ordinal))
            .OrderByDescending(path => path.Length);

        foreach (var directoryPath in directories)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    private static string BuildMetadata(params (string Key, string Value)[] entries)
    {
        return string.Join(
            ",",
            entries.Select(entry =>
                $"{entry.Key} {Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.Value))}"));
    }

    private sealed class TransferApiFactory(
        string uploadRoot,
        string tusTempRoot,
        int batchSlidingTtlMinutes = 1440) : WebApplicationFactory<Program>
    {
        private readonly string _uploadRoot = uploadRoot;
        private readonly string _tusTempRoot = tusTempRoot;
        private readonly int _batchSlidingTtlMinutes = batchSlidingTtlMinutes;

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
                        ["Transfer:Tus:MaxSizeBytes"] = "10485760",
                        ["Transfer:Batch:SlidingTtlMinutes"] = _batchSlidingTtlMinutes.ToString(),
                        ["Transfer:Batch:CleanupIntervalSeconds"] = "300"
                    });
            });
        }
    }

    private sealed class TemporaryRoots : IDisposable
    {
        public TemporaryRoots()
        {
            var root = Path.Combine(Path.GetTempPath(), $"ftw-batch-tests-{Guid.NewGuid():N}");
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
