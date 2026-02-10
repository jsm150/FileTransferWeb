using FileTransferWeb.Domain.Shared;

namespace FileTransferWeb.Storage.Domain.Policies;

public sealed class StoragePathPolicy
{
    public string FullCurrentPath { get; }
    public string CurrentRelativePath { get; }
    public string? ParentRelativePath { get; }

    public StoragePathPolicy(string uploadRoot, string? requestedRelativePath)
    {
        ValidateUploadRoot(uploadRoot);

        var fullRootPath = Path.GetFullPath(uploadRoot);
        var normalizedRelativePath = NormalizeRelativePath(requestedRelativePath);

        if (Path.IsPathRooted(normalizedRelativePath))
        {
            throw new DomainRuleViolationException("상대 경로만 허용됩니다.");
        }

        var combinedPath = Path.Combine(fullRootPath, normalizedRelativePath);
        FullCurrentPath = Path.GetFullPath(combinedPath);

        if (IsOutsideRoot(fullRootPath, FullCurrentPath))
        {
            throw new DomainRuleViolationException("업로드 루트 경로 밖으로 접근할 수 없습니다.");
        }

        var relativePath = Path.GetRelativePath(fullRootPath, FullCurrentPath);
        if (string.Equals(relativePath, ".", StringComparison.Ordinal))
        {
            CurrentRelativePath = string.Empty;
        }
        else
        {
            CurrentRelativePath = relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        ParentRelativePath = BuildParentRelativePath(CurrentRelativePath);
    }

    private static string NormalizeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var normalizedPath = relativePath.Trim();
        normalizedPath = normalizedPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalizedPath))
        {
            throw new DomainRuleViolationException("상대 경로만 허용됩니다.");
        }

        normalizedPath = normalizedPath.TrimStart(Path.DirectorySeparatorChar);

        if (string.Equals(normalizedPath, ".", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return normalizedPath;
    }

    private static void ValidateUploadRoot(string uploadRoot)
    {
        if (string.IsNullOrWhiteSpace(uploadRoot))
        {
            throw new DomainRuleViolationException("업로드 루트 경로가 비어 있습니다.");
        }
    }

    private static string? BuildParentRelativePath(string currentRelativePath)
    {
        if (string.IsNullOrEmpty(currentRelativePath))
        {
            return null;
        }

        var lastSeparatorIndex = currentRelativePath.LastIndexOf('/');
        if (lastSeparatorIndex < 0)
        {
            return string.Empty;
        }

        return currentRelativePath[..lastSeparatorIndex];
    }

    private static bool IsOutsideRoot(string fullRootPath, string fullTargetPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(fullTargetPath, fullRootPath, comparison))
        {
            return false;
        }

        var rootPrefix = EnsureTrailingDirectorySeparator(fullRootPath);
        return !fullTargetPath.StartsWith(rootPrefix, comparison);
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }
}
