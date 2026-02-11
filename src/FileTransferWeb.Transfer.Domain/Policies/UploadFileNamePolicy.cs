using FileTransferWeb.Transfer.Domain.Exceptions;

namespace FileTransferWeb.Transfer.Domain.Policies;

public sealed class UploadFileNamePolicy
{
    private static readonly StringComparer NameComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private readonly Dictionary<FileNameKey, UsedNumbers> _usedNumbersByKey;

    public UploadFileNamePolicy(IReadOnlyCollection<string> existingFileNames)
    {
        if (existingFileNames is null)
        {
            throw new TransferDomainException("기존 파일 이름 목록이 비어 있습니다.");
        }

        _usedNumbersByKey = new Dictionary<FileNameKey, UsedNumbers>(FileNameKeyComparer.Instance);

        foreach (var existingFileName in existingFileNames)
        {
            var normalizedName = NormalizeFileName(existingFileName);
            var parsed = ParseNumberedName(normalizedName);
            var key = new FileNameKey(parsed.BaseName, parsed.Extension);

            if (!_usedNumbersByKey.TryGetValue(key, out var usedNumbers))
            {
                usedNumbers = new UsedNumbers();
                _usedNumbersByKey[key] = usedNumbers;
            }

            usedNumbers.MarkUsed(parsed.Number);
        }
    }

    public string ReserveStoredFileName(string rawFileName)
    {
        var normalizedName = NormalizeFileName(rawFileName);
        var parsed = ParseNumberedName(normalizedName);
        var key = new FileNameKey(parsed.BaseName, parsed.Extension);

        if (!_usedNumbersByKey.TryGetValue(key, out var usedNumbers))
        {
            usedNumbers = new UsedNumbers();
            _usedNumbersByKey[key] = usedNumbers;
        }

        var assignedNumber = usedNumbers.ReserveSmallestAvailable();
        return BuildFileName(parsed.BaseName, parsed.Extension, assignedNumber);
    }

    private static string NormalizeFileName(string? fileName)
    {
        var normalizedName = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalizedName))
        {
            return normalizedName;
        }

        return "이름없는파일";
    }

    private static ParsedName ParseNumberedName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var baseNameWithSuffix = Path.GetFileNameWithoutExtension(fileName);

        if (TryParseSuffixNumber(baseNameWithSuffix, out var baseName, out var number))
        {
            return new ParsedName(baseName, extension, number);
        }

        return new ParsedName(baseNameWithSuffix, extension, 0);
    }

    private static bool TryParseSuffixNumber(string baseNameWithSuffix, out string baseName, out int number)
    {
        baseName = baseNameWithSuffix;
        number = 0;

        if (!baseNameWithSuffix.EndsWith(")", StringComparison.Ordinal))
        {
            return false;
        }

        var openParenthesisIndex = baseNameWithSuffix.LastIndexOf(" (", StringComparison.Ordinal);
        if (openParenthesisIndex <= 0)
        {
            return false;
        }

        var numberText = baseNameWithSuffix[(openParenthesisIndex + 2)..^1];
        if (string.IsNullOrWhiteSpace(numberText))
        {
            return false;
        }

        if (!int.TryParse(numberText, out var parsedNumber))
        {
            return false;
        }

        if (parsedNumber < 1)
        {
            return false;
        }

        baseName = baseNameWithSuffix[..openParenthesisIndex];
        number = parsedNumber;
        return true;
    }

    private static string BuildFileName(string baseName, string extension, int number)
    {
        if (number == 0)
        {
            return baseName + extension;
        }

        return $"{baseName} ({number}){extension}";
    }

    private readonly record struct ParsedName(string BaseName, string Extension, int Number);

    private readonly record struct FileNameKey(string BaseName, string Extension);

    private sealed class FileNameKeyComparer : IEqualityComparer<FileNameKey>
    {
        public static FileNameKeyComparer Instance { get; } = new();

        public bool Equals(FileNameKey x, FileNameKey y)
        {
            return NameComparer.Equals(x.BaseName, y.BaseName)
                   && NameComparer.Equals(x.Extension, y.Extension);
        }

        public int GetHashCode(FileNameKey obj)
        {
            return HashCode.Combine(
                NameComparer.GetHashCode(obj.BaseName),
                NameComparer.GetHashCode(obj.Extension));
        }
    }

    private sealed class UsedNumbers
    {
        private readonly HashSet<int> _numbers = [];
        private int _nextAvailableNumber;

        public void MarkUsed(int number)
        {
            if (number < 0)
            {
                throw new TransferDomainException("파일 번호는 0 이상이어야 합니다.");
            }

            _numbers.Add(number);

            if (number == _nextAvailableNumber)
            {
                while (_numbers.Contains(_nextAvailableNumber))
                {
                    _nextAvailableNumber++;
                }
            }
        }

        public int ReserveSmallestAvailable()
        {
            var reservedNumber = _nextAvailableNumber;
            _numbers.Add(reservedNumber);

            while (_numbers.Contains(_nextAvailableNumber))
            {
                _nextAvailableNumber++;
            }

            return reservedNumber;
        }
    }
}
