using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Microsoft.Win32;

namespace CreamInstaller.Platforms.Ubisoft;

internal static class UbisoftLibrary
{
    internal static readonly List<(string gameId, string name, string gameDirectory)> TestGames = [];

    private static RegistryKey installsKey;

    private static RegistryKey InstallsKey
    {
        get
        {
            installsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs");
            return installsKey;
        }
    }

    internal static async Task<List<(string gameId, string name, string gameDirectory)>> GetGames()
        => await Task.Run(() =>
        {
            Stopwatch timer = Stopwatch.StartNew();
            List<(string gameId, string name, string gameDirectory)> games = new();
            RegistryKey installsKey = InstallsKey;
            if (installsKey is not null)
            {
                ProgramData.Log.Info($"[Ubisoft] Scanning registry: HKLM\\SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs", LogDestination.Scan);
                foreach (string gameId in installsKey.GetSubKeyNames())
                {
                    RegistryKey installKey = installsKey.OpenSubKey(gameId);
                    string installDir = installKey?.GetValue("InstallDir")?.ToString()?.ResolvePath();
                    if (installDir is not null && games.All(g => g.gameId != gameId))
                    {
                        games.Add((gameId, new DirectoryInfo(installDir).Name, installDir));
                        ProgramData.Log.Info($"[Ubisoft] Detected game: {new DirectoryInfo(installDir).Name} ({gameId}) | Dir: {installDir}", LogDestination.Scan);
                    }
                }
            }
            else
            {
                ProgramData.Log.Info($"[Ubisoft] Registry key not found: HKLM\\SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs", LogDestination.Scan);
            }

            foreach ((string gameId, string name, string gameDirectory) testGame in
                     TestGames.Where(t => games.All(g => g.gameId != t.gameId)))
                games.Add(testGame);
            if (TestGames.Count > 0)
                ProgramData.Log.Info($"[Ubisoft] Injected {TestGames.Count} test game(s).", LogDestination.Scan);
            timer.Stop();
            ProgramData.Log.Info($"[Ubisoft] Total games detected: {games.Count} in {(timer.Elapsed.TotalSeconds >= 60 ? $"{timer.Elapsed.TotalSeconds / 60:F1} minutes" : $"{timer.Elapsed.TotalSeconds:F1}s")}", LogDestination.Scan);
            return games;
        });
}