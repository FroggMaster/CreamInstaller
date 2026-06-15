using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Forms;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Utility;

namespace CreamInstaller;

internal static class Program
{
    internal static readonly string Name = Application.CompanyName;
    private static readonly string Description = Application.ProductName;

    internal static readonly string Version = Application.ProductVersion[
        ..(Application.ProductVersion.IndexOf('+') is var index && index != -1
            ? index
            : Application.ProductVersion.Length)];

    internal const string RepositoryOwner = "FroggMaster";
    internal static readonly string RepositoryName = Name;
    internal static readonly string RepositoryPackage = Name + ".zip";
    internal static readonly string RepositoryExecutable = Name + ".exe";
#if DEBUG
    internal static readonly string ApplicationName = Name + " v" + Version + "-debug: " + Description;
    internal static readonly string ApplicationNameShort = Name + " v" + Version + "-debug";
#else
    internal static readonly string ApplicationName = Name + " v" + Version + ": " + Description;
    internal static readonly string ApplicationNameShort = Name + " v" + Version;
#endif

    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    internal static readonly string CurrentProcessFilePath = CurrentProcess.MainModule?.FileName;
    internal static readonly int CurrentProcessId = CurrentProcess.Id;

    // Setting is now toggleable. Huzzah! 
    internal static bool UseSmokeAPI = true;

    internal static bool BlockProtectedGames = true;
    internal static readonly string[] ProtectedGames = ["PAYDAY 2"];
    internal static readonly string[] ProtectedGameDirectories = [@"\EasyAntiCheat", @"\BattlEye"];
    internal static readonly string[] ProtectedGameDirectoryExceptions = [];

    // Dark mode enabled by default
    internal static bool DarkModeEnabled = true;

    internal static bool IsGameBlocked(string name, string directory = null)
        => GetGameBlockedReason(name, directory) is not null;

    internal static string? GetGameBlockedReason(string name, string directory = null)
    {
        if (!BlockProtectedGames) return null;
        if (ProtectedGames.Contains(name)) return "on protected games list";
        if (directory is null) return null;
        if (ProtectedGameDirectoryExceptions.Contains(name)) return null;
        string? foundAntiCheat = ProtectedGameDirectories.FirstOrDefault(path => (directory + path).DirectoryExists());
        return foundAntiCheat is not null
            ? $"{foundAntiCheat[1..]} directory found"
            : null;
    }

    [STAThread]
    private static void Main()
    {
        using Mutex mutex = new(true, Name, out bool createdNew);
        if (createdNew)
        {
            _ = Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += OnApplicationExit;
            Application.ThreadException += (_, e) => e.Exception.HandleFatalException();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException +=
                (_, e) => (e.ExceptionObject as Exception)?.HandleFatalException();
            bool retry = true;
            while (retry)
            {
                try
                {
                    HttpClientManager.Setup();
                    using UpdateForm form = new();
#if DEBUG
                    DebugForm.Current.Attach(form);
#endif
                    // Apply initial theme (dark by default)
                    Utility.ThemeManager.Apply(form);
                    Application.Run(form);
                    retry = false;
                }
                catch (Exception e)
                {
                    retry = e.HandleException();
                    if (!retry)
                    {
                        Application.Exit();
                        return;
                    }
                }
            }
        }

        mutex.Close();
    }

    internal static bool Canceled;

    /// <summary>
    /// Initiates application cleanup asynchronously. Use this when you can await the result.
    /// </summary>
    /// <param name="cancel">Whether to set the Canceled flag</param>
    /// <returns>Task that completes when cleanup is finished</returns>
    internal static async Task CleanupAsync(bool cancel = true)
    {
        if (cancel)
            Canceled = true;
        await SteamCMD.Cleanup();
    }

    /// <summary>
    /// Synchronous cleanup wrapper for event handlers and other synchronous contexts.
    /// Initiates cleanup without blocking but does not wait for completion.
    /// </summary>
    /// <param name="cancel">Whether to set the Canceled flag</param>
    internal static void Cleanup(bool cancel = true)
    {
        if (cancel)
            Canceled = true;

        // Fire and forget - don't block synchronous callers
        // Any exceptions will be logged but won't crash the app
        _ = Task.Run(async () =>
        {
            try
            {
                await SteamCMD.Cleanup();
            }
            catch (Exception ex)
            {
                ProgramData.LogWarning($"Cleanup failed: {ex.Message}");
            }
        });
    }

    private static void OnApplicationExit(object s, EventArgs e)
    {
        Canceled = true;

        // For application exit, we should try to wait briefly for cleanup
        try
        {
            Task cleanupTask = SteamCMD.Cleanup();
            // Wait up to 5 seconds for graceful cleanup
            if (!cleanupTask.Wait(TimeSpan.FromSeconds(5)))
            {
                ProgramData.LogWarning("Cleanup timed out during application exit");
            }
        }
        catch (Exception ex)
        {
            ProgramData.LogWarning($"Cleanup exception during exit: {ex.Message}");
        }
        finally
        {
            HttpClientManager.Dispose();
        }
    }
}
