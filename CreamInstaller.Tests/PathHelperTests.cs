using CreamInstaller.Utility;

namespace CreamInstaller.Tests;

public class PathHelperTests
{
    [Fact]
    public void IsValidPath_NullPath_ReturnsFalse()
    {
        Assert.False(PathHelper.IsValidPath(null!));
    }

    [Fact]
    public void IsValidPath_EmptyPath_ReturnsFalse()
    {
        Assert.False(PathHelper.IsValidPath(""));
    }

    [Fact]
    public void IsValidPath_WhitespacePath_ReturnsFalse()
    {
        Assert.False(PathHelper.IsValidPath("   "));
    }

    [Fact]
    public void IsValidPath_ValidPath_ReturnsTrue()
    {
        Assert.True(PathHelper.IsValidPath(@"C:\Users\Test"));
    }

    [Fact]
    public void IsValidPath_PathWithInvalidChars_ReturnsFalse()
    {
        // Null character is invalid in paths
        Assert.False(PathHelper.IsValidPath("C:\\Test\0Path"));
    }

    [Fact]
    public void IsPathWithinBase_NullPath_ReturnsFalse()
    {
        Assert.False(PathHelper.IsPathWithinBase(null!, @"C:\Base"));
    }

    [Fact]
    public void IsPathWithinBase_NullBase_ReturnsFalse()
    {
        Assert.False(PathHelper.IsPathWithinBase(@"C:\Test", null!));
    }

    [Fact]
    public void IsPathWithinBase_PathWithinBase_ReturnsTrue()
    {
        Assert.True(PathHelper.IsPathWithinBase(@"C:\Base\SubDir\File.txt", @"C:\Base"));
    }

    [Fact]
    public void IsPathWithinBase_PathOutsideBase_ReturnsFalse()
    {
        Assert.False(PathHelper.IsPathWithinBase(@"C:\Other\File.txt", @"C:\Base"));
    }

    [Fact]
    public void IsPathWithinBase_PathTraversalAttempt_ReturnsFalse()
    {
        Assert.False(PathHelper.IsPathWithinBase(@"C:\Base\..\Other\File.txt", @"C:\Base"));
    }

    [Fact]
    public void SafeCombine_NullBase_ReturnsNull()
    {
        Assert.Null(PathHelper.SafeCombine(null!, "subdir"));
    }

    [Fact]
    public void SafeCombine_ValidPaths_ReturnsCombinedPath()
    {
        string? result = PathHelper.SafeCombine(@"C:\Base", "SubDir", "File.txt");
        Assert.NotNull(result);
        Assert.Contains("SubDir", result);
        Assert.Contains("File.txt", result);
    }

    [Fact]
    public void SafeCombine_TraversalAttempt_ReturnsNull()
    {
        // Attempting to escape the base path should return null
        string? result = PathHelper.SafeCombine(@"C:\Base", "..", "Other");
        Assert.Null(result);
    }

    [Fact]
    public void GetFileName_ValidPath_ReturnsFileName()
    {
        Assert.Equal("file.txt", PathHelper.GetFileName(@"C:\Dir\file.txt"));
    }

    [Fact]
    public void GetFileName_NullPath_ReturnsNull()
    {
        Assert.Null(PathHelper.GetFileName(null!));
    }

    [Fact]
    public void GetDirectoryName_ValidPath_ReturnsDirectory()
    {
        string? result = PathHelper.GetDirectoryName(@"C:\Dir\file.txt");
        Assert.NotNull(result);
        Assert.EndsWith("Dir", result);
    }

    [Fact]
    public void GetDirectoryName_NullPath_ReturnsNull()
    {
        Assert.Null(PathHelper.GetDirectoryName(null!));
    }

    [Fact]
    public void Combine_MultiplePaths_ReturnsCombinedPath()
    {
        string result = PathHelper.Combine("C:", "Users", "Test", "file.txt");
        Assert.Contains("Users", result);
        Assert.Contains("Test", result);
        Assert.Contains("file.txt", result);
    }
}
