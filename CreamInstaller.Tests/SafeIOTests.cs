using CreamInstaller.Utility;

namespace CreamInstaller.Tests;

public class SafeIOTests
{
    [Fact]
    public void MaxRetryAttempts_HasReasonableValue()
    {
        // Ensure retry attempts is a reasonable number (not too high to cause hangs)
        Assert.True(SafeIO.MaxRetryAttempts > 0);
        Assert.True(SafeIO.MaxRetryAttempts <= 20);
    }

    [Fact]
    public void RetryDelayMs_HasReasonableValue()
    {
        // Ensure retry delay is reasonable (not too short or too long)
        Assert.True(SafeIO.RetryDelayMs >= 100);
        Assert.True(SafeIO.RetryDelayMs <= 5000);
    }

    [Fact]
    public void DirectoryExists_NonExistentDirectory_ReturnsFalse()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.False(nonExistentPath.DirectoryExists());
    }

    [Fact]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        string tempPath = Path.GetTempPath();
        Assert.True(tempPath.DirectoryExists());
    }

    [Fact]
    public void FileExists_NonExistentFile_ReturnsFalse()
    {
        string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        Assert.False(nonExistentFile.FileExists());
    }

    [Fact]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            Assert.True(tempFile.FileExists());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadFile_NonExistentFile_ReturnsNull()
    {
        string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        Assert.Null(nonExistentFile.ReadFile());
    }

    [Fact]
    public void ReadFile_ExistingFile_ReturnsContent()
    {
        string tempFile = Path.GetTempFileName();
        string expectedContent = "Test content " + Guid.NewGuid();
        try
        {
            File.WriteAllText(tempFile, expectedContent);
            string? result = tempFile.ReadFile();
            Assert.Equal(expectedContent, result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void EnumerateDirectory_NonExistentDirectory_ReturnsEmpty()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = nonExistentPath.EnumerateDirectory("*.txt");
        Assert.Empty(result);
    }

    [Fact]
    public void EnumerateSubdirectories_NonExistentDirectory_ReturnsEmpty()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = nonExistentPath.EnumerateSubdirectories("*");
        Assert.Empty(result);
    }
}
