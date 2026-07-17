using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace CreamInstaller.Platforms.Steam;

internal static class SteamLibrary
{
    internal static readonly List<(string appId, string name, string branch, int buildId, string gameDirectory)>
        TestGames = [];

    private static string installPath;

    internal static string InstallPath
    {
        get
        {
            installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            installPath ??=
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            return installPath.ResolvePath();
        }
    }

    internal static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>>
        GetGames()
    {
        Stopwatch timer = Stopwatch.StartNew();
        List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
        HashSet<string> seenAppIds = new();
        HashSet<string> gameLibraryDirectories = await GetLibraryDirectories();
        ProgramData.Log.Info($"[Steam] Found {gameLibraryDirectories.Count} library folder(s).", LogDestination.Scan);
        foreach (string libraryDirectory in gameLibraryDirectories)
        {
            if (Program.Canceled)
                return games;
            ProgramData.Log.Info($"[Steam] Scanning library: {libraryDirectory}", LogDestination.Scan);
            foreach ((string appId, string name, string branch, int buildId, string gameDirectory) game in
                     await GetGamesFromLibraryDirectory(libraryDirectory))
            {
                if (seenAppIds.Add(game.appId))
                    games.Add(game);
            }
        }

        foreach ((string appId, string name, string branch, int buildId, string gameDirectory) testGame in
                 TestGames.Where(t => !seenAppIds.Contains(t.appId)))
            games.Add(testGame);
        if (TestGames.Count > 0)
            ProgramData.Log.Info($"[Steam] Injected {TestGames.Count} test game(s).", LogDestination.Scan);
        timer.Stop();
        ProgramData.Log.Info($"[Steam] Total games detected: {games.Count} in {(timer.Elapsed.TotalSeconds >= 60 ? $"{timer.Elapsed.TotalSeconds / 60:F1} minutes" : $"{timer.Elapsed.TotalSeconds:F1}s")}", LogDestination.Scan);
        return games;
    }

    private static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>>
        GetGamesFromLibraryDirectory(string libraryDirectory)
        => await Task.Run(() =>
        {
            if (Program.Canceled || !libraryDirectory.DirectoryExists())
            {
                ProgramData.Log.Info($"[Steam] Skipping library (not found or canceled): {libraryDirectory}", LogDestination.Scan);
                return [];
            }

            ConcurrentDictionary<string, (string name, string branch, int buildId, string gameDirectory)> gamesDict = new();
            Parallel.ForEach(libraryDirectory.EnumerateDirectory("*.acf"), (file, state) =>
            {
                if (Program.Canceled)
                {
                    state.Stop();
                    return;
                }
                if (!ValveDataFile.TryDeserialize(file.ReadFile(), out VProperty result))
                {
                    ProgramData.Log.Info($"[Steam] Failed to deserialize ACF: {file}", LogDestination.Scan);
                    return;
                }

                string appId = result.Value.GetChild("appid")?.ToString();
                string installdir = result.Value.GetChild("installdir")?.ToString();
                string name = result.Value.GetChild("name")?.ToString();
                string buildId = result.Value.GetChild("buildid")?.ToString();
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(installdir) ||
                    string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(buildId))
                {
                    ProgramData.Log.Info($"[Steam] Skipping ACF with missing fields: {file}", LogDestination.Scan);
                    return;
                }

                string rawGameDirectory = libraryDirectory + @"\common\" + installdir;
                string gameDirectory = rawGameDirectory.ResolvePath();
                if (gameDirectory is null)
                {
                    ProgramData.Log.Info($"[Steam] Game directory not found (drive may be slow or disconnected): {rawGameDirectory} | App: {name} ({appId})", LogDestination.Scan);
                    return;
                }

                if (!int.TryParse(appId, out int _) || !int.TryParse(buildId, out int buildIdInt))
                    return;

                VToken userConfig = result.Value.GetChild("UserConfig");
                string branch = userConfig?.GetChild("BetaKey")?.ToString();
                branch ??= userConfig?.GetChild("betakey")?.ToString();
                if (branch is null)
                {
                    VToken mountedConfig = result.Value.GetChild("MountedConfig");
                    branch = mountedConfig?.GetChild("BetaKey")?.ToString();
                    branch ??= mountedConfig?.GetChild("betakey")?.ToString();
                }

                if (string.IsNullOrWhiteSpace(branch))
                    branch = "public";

                if (gamesDict.TryAdd(appId, (name, branch, buildIdInt, gameDirectory)))
                    ProgramData.Log.Info($"[Steam] Detected game: {name} ({appId}) | Branch: {branch} | Dir: {gameDirectory}", LogDestination.Scan);
            });

            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new(gamesDict.Count);
            foreach (KeyValuePair<string, (string name, string branch, int buildId, string gameDirectory)> kv in gamesDict)
                games.Add((kv.Key, kv.Value.name, kv.Value.branch, kv.Value.buildId, kv.Value.gameDirectory));
            return games;
        });

    private static async Task<HashSet<string>> GetLibraryDirectories()
        => await Task.Run(() =>
        {
            HashSet<string> libraryDirectories = new();
            if (Program.Canceled)
                return libraryDirectories;
            string steamInstallPath = InstallPath;
            if (steamInstallPath == null || !steamInstallPath.DirectoryExists())
            {
                ProgramData.Log.Info($"[Steam] Steam install path not found or inaccessible: {steamInstallPath ?? "(null)"}", LogDestination.Scan);
                return libraryDirectories;
            }

            string libraryFolder = steamInstallPath + @"\steamapps";
            if (!libraryFolder.DirectoryExists())
            {
                ProgramData.Log.Info($"[Steam] Default steamapps folder not found: {libraryFolder}", LogDestination.Scan);
                return libraryDirectories;
            }

            _ = libraryDirectories.Add(libraryFolder);
            ProgramData.Log.Info($"[Steam] Default library folder: {libraryFolder}", LogDestination.Scan);

            string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
            if (!libraryFolders.FileExists() ||
                !ValveDataFile.TryDeserialize(libraryFolders.ReadFile(), out VProperty result))
            {
                ProgramData.Log.Info($"[Steam] libraryfolders.vdf not found or failed to parse: {libraryFolders}", LogDestination.Scan);
                return libraryDirectories;
            }

            foreach (VToken vToken in result.Value.Where(p =>
                         p is VProperty property && int.TryParse(property.Key, out int _)))
            {
                VProperty property = (VProperty)vToken;
                string rawPath = property.Value.GetChild("path")?.ToString();
                if (string.IsNullOrWhiteSpace(rawPath))
                    continue;

                // Normalize the path from VDF (may use forward slashes or wrong casing)
                string normalizedPath = Path.GetFullPath(rawPath);
                string steamappsPath = normalizedPath + @"\steamapps";
                string resolvedPath = steamappsPath.ResolvePath();

                if (resolvedPath is null)
                {
                    ProgramData.Log.Info($"[Steam] External library not accessible (drive may be disconnected or letter changed): {steamappsPath}", LogDestination.Scan);
                    continue;
                }

                if (libraryDirectories.Add(resolvedPath))
                    ProgramData.Log.Info($"[Steam] Additional library folder found: {resolvedPath}", LogDestination.Scan);
            }

            return libraryDirectories;
        });
}