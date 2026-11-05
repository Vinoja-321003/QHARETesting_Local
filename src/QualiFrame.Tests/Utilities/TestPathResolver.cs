using System;
using System.IO;

namespace QualiFrame.Tests.Utilities;

/// <summary>
/// Resolves test data and artifact paths in a framework-relative way.
/// Compatible with local runs and CI (no hardcoded absolute paths).
/// </summary>
public static class TestPathResolver
{
    private static string? _projectRoot;

    /// <summary>
    /// Gets the project root directory (solution or test project root).
    /// Walks up from current directory until a .sln or QualiFrame.Tests.csproj is found.
    /// </summary>
    public static string GetProjectRoot()
    {
        if (_projectRoot != null)
            return _projectRoot;

        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);

        while (dir != null && dir.Exists)
        {
            var slnFiles = dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
            var testProj = dir.GetFiles("QualiFrame.Tests.csproj", SearchOption.TopDirectoryOnly);
            if (slnFiles.Length > 0 || testProj.Length > 0)
            {
                _projectRoot = dir.FullName;
                return _projectRoot;
            }
            dir = dir.Parent;
        }

        _projectRoot = currentDir;
        return _projectRoot;
    }

    /// <summary>
    /// Resolves a path relative to the test project (e.g. "Data/FileValidation/PDFTestFile.pdf").
    /// Tries: output directory (when Data is copied), then project root + src/QualiFrame.Tests, then project root.
    /// </summary>
    /// <param name="relativePath">Path using forward slashes, e.g. Data/FileValidation/PDFTestFile.pdf</param>
    /// <returns>Absolute path to the file or directory.</returns>
    public static string ResolveTestPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1) Relative to output directory (Data is copied to output)
        var fromOutput = Path.Combine(baseDir, normalized);
        if (File.Exists(fromOutput) || Directory.Exists(fromOutput))
            return Path.GetFullPath(fromOutput);

        // 2) Project root + src/QualiFrame.Tests (when running from solution root)
        var root = GetProjectRoot();
        var fromTestsProject = Path.Combine(root, "src", "QualiFrame.Tests", normalized);
        if (File.Exists(fromTestsProject) || Directory.Exists(fromTestsProject))
            return Path.GetFullPath(fromTestsProject);

        // 3) Project root directly
        var fromRoot = Path.Combine(root, normalized);
        if (File.Exists(fromRoot) || Directory.Exists(fromRoot))
            return Path.GetFullPath(fromRoot);

        return Path.GetFullPath(fromOutput);
    }

    /// <summary>
    /// Resolves a path for writing artifacts (e.g. under Results/artifacts/pdfs).
    /// Creates the directory if needed.
    /// </summary>
    public static string ResolveArtifactPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var root = GetProjectRoot();
        var full = Path.Combine(root, normalized);
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        return Path.GetFullPath(full);
    }
}
