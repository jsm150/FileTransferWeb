using Microsoft.Extensions.Options;
using tusdotnet.Stores;

namespace FileTransferWeb.Transfer.Infrastructure.Tus;

public sealed class FileSystemTusStoreFactory(IOptions<TransferTusOptions> options)
{
    private readonly TransferTusOptions _options = options.Value;
    private readonly object _sync = new();
    private TusDiskStore? _store;

    public TusDiskStore CreateStore()
    {
        if (_store is not null)
        {
            return _store;
        }

        lock (_sync)
        {
            if (_store is not null)
            {
                return _store;
            }

            var tempRoot = ResolveTempRootPath();
            Directory.CreateDirectory(tempRoot);
            _store = new TusDiskStore(tempRoot);
            return _store;
        }
    }

    private string ResolveTempRootPath()
    {
        if (string.IsNullOrWhiteSpace(_options.TempRoot))
        {
            throw new InvalidOperationException("Transfer:Tus:TempRoot 설정이 필요합니다.");
        }

        return Path.GetFullPath(_options.TempRoot);
    }
}
