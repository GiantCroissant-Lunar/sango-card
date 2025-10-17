using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SangoCard.Build.Tool.Core.Utilities;

/// <summary>
/// Helper for Git repository operations.
/// </summary>
public class GitHelper
{
    private readonly ILogger<GitHelper> _logger;
    private string? _cachedGitRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHelper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public GitHelper(ILogger<GitHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects the git repository root directory.
    /// </summary>
    /// <param name="startPath">Optional starting path. If null, uses current directory.</param>
    /// <returns>Absolute path to git repository root.</returns>
    /// <exception cref="InvalidOperationException">Thrown when git root cannot be found.</exception>
    public string DetectGitRoot(string? startPath = null)
    {
        // Return cached value if available
        if (_cachedGitRoot != null)
        {
            _logger.LogDebug("Using cached git root: {GitRoot}", _cachedGitRoot);
            return _cachedGitRoot;
        }

        var searchPath = startPath ?? Directory.GetCurrentDirectory();
        _logger.LogDebug("Detecting git root from: {SearchPath}", searchPath);

        // Ensure absolute path
        searchPath = Path.GetFullPath(searchPath);

        // Method 1: Look for .git directory
        var gitRoot = FindGitDirectoryUpwards(searchPath);
        if (gitRoot != null)
        {
            _cachedGitRoot = gitRoot;
            _logger.LogInformation("Git root detected: {GitRoot}", gitRoot);
            return gitRoot;
        }

        // Method 2: Use git command
        gitRoot = DetectGitRootViaCommand(searchPath);
        if (gitRoot != null)
        {
            _cachedGitRoot = gitRoot;
            _logger.LogInformation("Git root detected via git command: {GitRoot}", gitRoot);
            return gitRoot;
        }

        var error = $"Git repository root not found. Started search from: {searchPath}";
        _logger.LogError(error);
        throw new InvalidOperationException(error);
    }

    /// <summary>
    /// Checks if a directory is within a git repository.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if path is within a git repository.</returns>
    public bool IsInGitRepository(string path)
    {
        try
        {
            DetectGitRoot(path);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Clears the cached git root. Useful for testing.
    /// </summary>
    public void ClearCache()
    {
        _cachedGitRoot = null;
        _logger.LogDebug("Git root cache cleared");
    }

    private string? FindGitDirectoryUpwards(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);

        while (currentDir != null)
        {
            var gitDir = Path.Combine(currentDir.FullName, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
            {
                _logger.LogDebug("Found .git at: {GitDir}", gitDir);
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    private string? DetectGitRootViaCommand(string workingDirectory)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start git process");
                return null;
            }

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();

                // Git on Windows may return Unix-style paths, normalize them
                if (OperatingSystem.IsWindows() && output.StartsWith('/'))
                {
                    // Convert /c/path/to/repo to C:\path\to\repo
                    output = ConvertUnixPathToWindows(output);
                }

                if (Directory.Exists(output))
                {
                    return Path.GetFullPath(output);
                }
            }
            else
            {
                var error = process.StandardError.ReadToEnd();
                _logger.LogDebug("Git command failed: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect git root via command");
        }

        return null;
    }

    private string ConvertUnixPathToWindows(string unixPath)
    {
        // Convert /c/path/to/repo to C:\path\to\repo
        if (unixPath.Length >= 3 && unixPath[0] == '/' && unixPath[2] == '/')
        {
            var driveLetter = char.ToUpper(unixPath[1]);
            var restOfPath = unixPath.Substring(3).Replace('/', '\\');
            return $"{driveLetter}:\\{restOfPath}";
        }

        return unixPath.Replace('/', '\\');
    }
}
