namespace XRay.Api.Services.Repositories;

/// <summary>
/// Reads a repository straight off the local filesystem. Branch/commit detection uses lightweight
/// ".git" plumbing (HEAD, refs/heads, packed-refs) instead of a native Git library dependency —
/// good enough for branch name + HEAD SHA without shelling out to git.exe.
/// </summary>
public class LocalRepositoryProvider : IRepositoryProvider
{
    public static readonly string[] IgnoredSegments =
    {
        ".git", "node_modules", "bin", "obj", "dist", "build", "coverage", ".vscode", ".idea"
    };

    private static readonly string[] AllowedExtensions = { ".cs", ".ts", ".tsx", ".sql" };

    public string ProviderCode => "LOCAL";

    public Task<RepositoryValidationResult> ValidateAsync(RepositoryConnectionInfo connection, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connection.RootPath) || !Directory.Exists(connection.RootPath))
        {
            return Task.FromResult(new RepositoryValidationResult(false, $"Path not found: {connection.RootPath}"));
        }
        return Task.FromResult(new RepositoryValidationResult(true, null));
    }

    public Task<IReadOnlyList<BranchInfo>> ListBranchesAsync(RepositoryConnectionInfo connection, CancellationToken ct = default)
    {
        var root = RequireRoot(connection);
        var gitDir = Path.Combine(root, ".git");
        var currentBranch = ReadCurrentBranchName(gitDir);

        var branches = new Dictionary<string, string>(); // name -> commit sha

        var headsDir = Path.Combine(gitDir, "refs", "heads");
        if (Directory.Exists(headsDir))
        {
            foreach (var file in Directory.EnumerateFiles(headsDir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetRelativePath(headsDir, file).Replace('\\', '/');
                var sha = File.ReadAllText(file).Trim();
                branches[name] = sha;
            }
        }

        var packedRefs = Path.Combine(gitDir, "packed-refs");
        if (File.Exists(packedRefs))
        {
            foreach (var line in File.ReadAllLines(packedRefs))
            {
                if (line.StartsWith('#') || !line.Contains("refs/heads/")) continue;
                var parts = line.Split(' ', 2);
                if (parts.Length != 2) continue;
                var name = parts[1].Replace("refs/heads/", "").Trim();
                if (!branches.ContainsKey(name)) branches[name] = parts[0].Trim();
            }
        }

        if (branches.Count == 0 && currentBranch is not null)
        {
            // Not a git repo (or a fresh one with no commits yet) — still expose the "branch" the
            // user configured so ingestion/analysis has something stable to scope against.
            branches[currentBranch] = "";
        }

        var result = branches.Select(kv => new BranchInfo(kv.Key, kv.Value, kv.Key == currentBranch)).ToList();
        return Task.FromResult<IReadOnlyList<BranchInfo>>(result);
    }

    public async Task<string> GetHeadCommitAsync(RepositoryConnectionInfo connection, string branchName, CancellationToken ct = default)
    {
        var branches = await ListBranchesAsync(connection, ct);
        return branches.FirstOrDefault(b => b.Name == branchName)?.HeadCommitSha ?? "";
    }

    public Task<IReadOnlyList<RepositoryFile>> ListFilesAsync(RepositoryConnectionInfo connection, string branchName, CancellationToken ct = default)
    {
        // Local provider always reads the working tree as checked out on disk; it does not switch
        // branches for the caller (that remains a manual `git checkout` today — see update.md
        // follow-ups for on-demand worktree checkout per branch).
        var root = RequireRoot(connection);
        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => !IgnoredSegments.Any(seg => f.Replace('\\', '/').Contains($"/{seg}/", StringComparison.OrdinalIgnoreCase)))
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => new RepositoryFile(Path.GetRelativePath(root, f).Replace('\\', '/'), new FileInfo(f).Length))
            .ToList();
        return Task.FromResult<IReadOnlyList<RepositoryFile>>(files);
    }

    public async Task<string> GetFileContentAsync(RepositoryConnectionInfo connection, string branchName, string relativePath, CancellationToken ct = default)
    {
        var root = RequireRoot(connection);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved file path escapes the repository root.");
        }
        return await File.ReadAllTextAsync(fullPath, ct);
    }

    private static string RequireRoot(RepositoryConnectionInfo connection) =>
        connection.RootPath ?? throw new InvalidOperationException("Local repository connection requires RootPath.");

    private static string? ReadCurrentBranchName(string gitDir)
    {
        var headFile = Path.Combine(gitDir, "HEAD");
        if (!File.Exists(headFile)) return null;
        var content = File.ReadAllText(headFile).Trim();
        // "ref: refs/heads/main" -> "main"; a detached HEAD (raw SHA) has no branch name.
        return content.StartsWith("ref: refs/heads/", StringComparison.Ordinal)
            ? content["ref: refs/heads/".Length..]
            : null;
    }
}
