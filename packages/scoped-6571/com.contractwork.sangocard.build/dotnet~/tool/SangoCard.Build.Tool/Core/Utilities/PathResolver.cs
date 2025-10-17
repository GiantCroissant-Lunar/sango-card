using Microsoft.Extensions.Logging;

namespace SangoCard.Build.Tool.Core.Utilities;

/// <summary>
/// Resolves file paths relative to the git repository root.
/// Implements R-PATH-010: All paths must be resolved relative to git repository root.
/// </summary>
public class PathResolver
{
    private readonly GitHelper _gitHelper;
    private readonly ILogger<PathResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathResolver"/> class.
    /// </summary>
    /// <param name="gitHelper">Git helper instance.</param>
    /// <param name="logger">Logger instance.</param>
    public PathResolver(GitHelper gitHelper, ILogger<PathResolver> logger)
    {
        _gitHelper = gitHelper;
        _logger = logger;
    }

    /// <summary>
    /// Gets the git repository root path.
    /// </summary>
    public string GitRoot => _gitHelper.DetectGitRoot();

    /// <summary>
    /// Resolves a relative path to an absolute path based on git repository root.
    /// </summary>
    /// <param name="relativePath">Relative path from git root (e.g., "build/preparation/configs/config.json").</param>
    /// <returns>Absolute path.</returns>
    /// <exception cref="ArgumentException">Thrown when path is invalid.</exception>
    public string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path cannot be null or whitespace", nameof(relativePath));
        }

        // Normalize path separators
        relativePath = NormalizePath(relativePath);

        // If already absolute, validate it's within git root
        if (Path.IsPathRooted(relativePath))
        {
            var fullPath = Path.GetFullPath(relativePath);

            if (!IsWithinGitRoot(fullPath))
            {
                _logger.LogWarning(
                    "Absolute path {Path} is outside git root {GitRoot}. This may cause issues.",
                    fullPath,
                    GitRoot
                );
            }

            _logger.LogDebug("Path is already absolute: {Path}", fullPath);
            return fullPath;
        }

        // Resolve relative to git root
        var gitRoot = GitRoot;
        var resolvedPath = Path.GetFullPath(Path.Combine(gitRoot, relativePath));

        _logger.LogDebug(
            "Resolved path: {RelativePath} -> {AbsolutePath} (git root: {GitRoot})",
            relativePath,
            resolvedPath,
            gitRoot
        );

        return resolvedPath;
    }

    /// <summary>
    /// Makes an absolute path relative to git repository root.
    /// </summary>
    /// <param name="absolutePath">Absolute path.</param>
    /// <returns>Relative path from git root.</returns>
    /// <exception cref="ArgumentException">Thrown when path is invalid or outside git root.</exception>
    public string MakeRelative(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            throw new ArgumentException("Path cannot be null or whitespace", nameof(absolutePath));
        }

        // Ensure absolute path
        absolutePath = Path.GetFullPath(absolutePath);

        var gitRoot = GitRoot;

        // Check if path is within git root
        if (!IsWithinGitRoot(absolutePath))
        {
            throw new ArgumentException(
                $"Path '{absolutePath}' is outside git repository root '{gitRoot}'",
                nameof(absolutePath)
            );
        }

        // Make relative
        var relativePath = Path.GetRelativePath(gitRoot, absolutePath);

        // Normalize to forward slashes for consistency
        relativePath = relativePath.Replace('\\', '/');

        _logger.LogDebug(
            "Made path relative: {AbsolutePath} -> {RelativePath} (git root: {GitRoot})",
            absolutePath,
            relativePath,
            gitRoot
        );

        return relativePath;
    }

    /// <summary>
    /// Checks if a path exists.
    /// </summary>
    /// <param name="relativePath">Relative path from git root.</param>
    /// <returns>True if path exists (file or directory).</returns>
    public bool Exists(string relativePath)
    {
        var absolutePath = Resolve(relativePath);
        return File.Exists(absolutePath) || Directory.Exists(absolutePath);
    }

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="relativePath">Relative path from git root.</param>
    /// <returns>True if file exists.</returns>
    public bool FileExists(string relativePath)
    {
        var absolutePath = Resolve(relativePath);
        return File.Exists(absolutePath);
    }

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="relativePath">Relative path from git root.</param>
    /// <returns>True if directory exists.</returns>
    public bool DirectoryExists(string relativePath)
    {
        var absolutePath = Resolve(relativePath);
        return Directory.Exists(absolutePath);
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="relativePath">Relative path from git root.</param>
    /// <returns>Absolute path to the directory.</returns>
    public string EnsureDirectory(string relativePath)
    {
        var absolutePath = Resolve(relativePath);

        if (!Directory.Exists(absolutePath))
        {
            _logger.LogDebug("Creating directory: {Path}", absolutePath);
            Directory.CreateDirectory(absolutePath);
        }

        return absolutePath;
    }

    /// <summary>
    /// Validates that a path is within the git repository.
    /// </summary>
    /// <param name="relativePath">Relative path from git root.</param>
    /// <exception cref="ArgumentException">Thrown when path is outside git root.</exception>
    public void ValidateWithinGitRoot(string relativePath)
    {
        var absolutePath = Resolve(relativePath);

        if (!IsWithinGitRoot(absolutePath))
        {
            throw new ArgumentException(
                $"Path '{relativePath}' resolves to '{absolutePath}' which is outside git repository root '{GitRoot}'",
                nameof(relativePath)
            );
        }
    }

    /// <summary>
    /// Checks if an absolute path is within the git repository root.
    /// </summary>
    /// <param name="absolutePath">Absolute path to check.</param>
    /// <returns>True if path is within git root.</returns>
    private bool IsWithinGitRoot(string absolutePath)
    {
        var gitRoot = GitRoot;

        // Normalize paths for comparison
        absolutePath = Path.GetFullPath(absolutePath);
        gitRoot = Path.GetFullPath(gitRoot);

        // Ensure trailing separator for proper prefix check
        if (!gitRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            gitRoot += Path.DirectorySeparatorChar;
        }

        return absolutePath.StartsWith(gitRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes path separators to the current platform.
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized path.</returns>
    private string NormalizePath(string path)
    {
        // Replace forward slashes with platform-specific separator
        return path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar);
    }
}
