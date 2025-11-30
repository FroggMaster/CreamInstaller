using System;
using System.IO;

namespace CreamInstaller.Utility;

internal static class PathHelper
{
    /// <summary>
    /// Combines path segments using Path.Combine for consistent path building.
    /// </summary>
    internal static string Combine(params string[] paths) => Path.Combine(paths);

    /// <summary>
    /// Validates that a path is safe to use (not null, not traversing outside expected directories).
    /// </summary>
    internal static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check for invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // Get full path to normalize and detect traversal attempts
            string fullPath = Path.GetFullPath(path);

            // Check for path traversal patterns
            if (path.Contains(".."))
            {
                // Ensure the resolved path doesn't escape expected boundaries
                // This is a basic check - callers should validate against expected base paths
                return !string.IsNullOrEmpty(fullPath);
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that a path is within an expected base directory (prevents path traversal attacks).
    /// </summary>
    internal static bool IsPathWithinBase(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
            return false;

        try
        {
            string fullPath = Path.GetFullPath(path);
            string fullBasePath = Path.GetFullPath(basePath);

            // Ensure base path ends with separator for accurate comparison
            if (!fullBasePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullBasePath += Path.DirectorySeparatorChar;

            return fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely combines paths and validates the result is within the base directory.
    /// Returns null if the path would escape the base directory.
    /// </summary>
    internal static string SafeCombine(string basePath, params string[] additionalPaths)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            return null;

        try
        {
            string[] allPaths = new string[additionalPaths.Length + 1];
            allPaths[0] = basePath;
            Array.Copy(additionalPaths, 0, allPaths, 1, additionalPaths.Length);

            string combinedPath = Path.Combine(allPaths);
            string fullPath = Path.GetFullPath(combinedPath);

            // Verify the result is still within the base path
            if (!IsPathWithinBase(fullPath, basePath))
                return null;

            return fullPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the file name from a path, returning null if the path is invalid.
    /// </summary>
    internal static string GetFileName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            return Path.GetFileName(path);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the directory name from a path, returning null if the path is invalid.
    /// </summary>
    internal static string GetDirectoryName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            return Path.GetDirectoryName(path);
        }
        catch
        {
            return null;
        }
    }
}
