using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace CreamInstaller.Platforms.Steam;

internal static class SteamLibrary
{
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
        => await Task.Run(async () =>
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
            HashSet<string> gameLibraryDirectories = await GetLibraryDirectories();
            ProgramData.Log($"[Steam] Found {gameLibraryDirectories.Count} library folder(s).");
            foreach (string libraryDirectory in gameLibraryDirectories)
            {
                if (Program.Canceled)
                    return games;
                ProgramData.Log($"[Steam] Scanning library: {libraryDirectory}");
                foreach ((string appId, string name, string branch, int buildId, string gameDirectory) game in (await
                             GetGamesFromLibraryDirectory(
                                 libraryDirectory)).Where(game => games.All(_game => _game.appId != game.appId)))
                    games.Add(game);
            }

            ProgramData.Log($"[Steam] Total games detected: {games.Count}");
            return games;
        });

    private static async Task<List<(string appId, string name, string branch, int buildId, string gameDirectory)>>
        GetGamesFromLibraryDirectory(string libraryDirectory)
        => await Task.Run(() =>
        {
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> games = new();
            if (Program.Canceled || !libraryDirectory.DirectoryExists())
            {
                ProgramData.Log($"[Steam] Skipping library (not found or canceled): {libraryDirectory}");
                return games;
            }

            foreach (string file in libraryDirectory.EnumerateDirectory("*.acf"))
            {
                if (Program.Canceled)
                    return games;
                if (!ValveDataFile.TryDeserialize(file.ReadFile(), out VProperty result))
                {
                    ProgramData.Log($"[Steam] Failed to deserialize ACF: {file}");
                    continue;
                }

                string appId = result.Value.GetChild("appid")?.ToString();
                string installdir = result.Value.GetChild("installdir")?.ToString();
                string name = result.Value.GetChild("name")?.ToString();
                string buildId = result.Value.GetChild("buildid")?.ToString();
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(installdir) ||
                    string.IsNullOrWhiteSpace(name)
                    || string.IsNullOrWhiteSpace(buildId))
                {
                    ProgramData.Log($"[Steam] Skipping ACF with missing fields: {file}");
                    continue;
                }

                string rawGameDirectory = libraryDirectory + @"\common\" + installdir;
                string gameDirectory = rawGameDirectory.ResolvePath();
                if (gameDirectory is null)
                {
                    ProgramData.Log($"[Steam] Game directory not found (drive may be slow or disconnected): {rawGameDirectory} | App: {name} ({appId})");
                    continue;
                }

                if (!int.TryParse(appId, out int _) || !int.TryParse(buildId, out int buildIdInt) ||
                    games.Any(g => g.appId == appId))
                    continue;

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

                ProgramData.Log($"[Steam] Detected game: {name} ({appId}) | Branch: {branch} | Dir: {gameDirectory}");
                games.Add((appId, name, branch, buildIdInt, gameDirectory));
            }

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
                ProgramData.Log($"[Steam] Steam install path not found or inaccessible: {steamInstallPath ?? "(null)"}");
                return libraryDirectories;
            }

            string libraryFolder = steamInstallPath + @"\steamapps";
            if (!libraryFolder.DirectoryExists())
            {
                ProgramData.Log($"[Steam] Default steamapps folder not found: {libraryFolder}");
                return libraryDirectories;
            }

            _ = libraryDirectories.Add(libraryFolder);
            ProgramData.Log($"[Steam] Default library folder: {libraryFolder}");

            string libraryFolders = libraryFolder + @"\libraryfolders.vdf";
            if (!libraryFolders.FileExists() ||
                !ValveDataFile.TryDeserialize(libraryFolders.ReadFile(), out VProperty result))
            {
                ProgramData.Log($"[Steam] libraryfolders.vdf not found or failed to parse: {libraryFolders}");
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
                    ProgramData.Log($"[Steam] External library not accessible (drive may be disconnected or letter changed): {steamappsPath}");
                    continue;
                }

                if (libraryDirectories.Add(resolvedPath))
                    ProgramData.Log($"[Steam] Additional library folder found: {resolvedPath}");
            }

            return libraryDirectories;
        });
}