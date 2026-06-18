using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CreamInstaller.Platforms.Epic.Heroic;
using CreamInstaller.Utility;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Epic;

internal static class EpicLibrary
{
    private static string epicManifestsPath;

    internal static string EpicManifestsPath
    {
        get
        {
            epicManifestsPath ??=
                Registry.GetValue(@"HKEY_CURRENT_USER\Software\Epic Games\EOS", "ModSdkMetadataDir", null) as string;
            epicManifestsPath ??=
                Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher", "AppDataPath",
                    null) as string;
            if (epicManifestsPath is not null && epicManifestsPath.EndsWith(@"\Data", StringComparison.Ordinal))
                epicManifestsPath += @"\Manifests";
            return epicManifestsPath.ResolvePath();
        }
    }

    internal static readonly List<Manifest> TestManifests = [];

    internal static async Task<List<Manifest>> GetGames()
        => await Task.Run(async () =>
        {
            Stopwatch timer = Stopwatch.StartNew();
            List<Manifest> games = new();

            foreach (Manifest test in TestManifests)
                if (games.All(g => g.CatalogNamespace != test.CatalogNamespace))
                    games.Add(test);

            string manifests = EpicManifestsPath;
            ProgramData.Log($"[Epic] Manifests directory: {manifests ?? "(not found)"}");
            if (manifests.DirectoryExists())
            {
                ProgramData.Log($"[Epic] Scanning manifests: {manifests}");
                foreach (string item in manifests.EnumerateDirectory("*.item"))
                {
                    if (Program.Canceled)
                        return games;
                    string json = item.ReadFile();
                    try
                    {
                        Manifest manifest = JsonConvert.DeserializeObject<Manifest>(json);
                        if (manifest is not null && (manifest.InstallLocation = manifest.InstallLocation.ResolvePath())
                                                 is not null
                                                 && games.All(g => g.CatalogNamespace != manifest.CatalogNamespace))
                        {
                            games.Add(manifest);
                            ProgramData.Log($"[Epic] Detected game: {manifest.DisplayName} ({manifest.CatalogNamespace}) | Dir: {manifest.InstallLocation}");
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (Program.Canceled)
                return games;
            int beforeHeroic = games.Count;
            await HeroicLibrary.GetGames(games);
            int heroicCount = games.Count - beforeHeroic;
            if (heroicCount > 0)
                ProgramData.Log($"[Epic] Found {heroicCount} game(s) from Heroic Games Launcher");
            if (TestManifests.Count > 0)
                ProgramData.Log($"[Epic] Injected {TestManifests.Count} test game(s).");
            timer.Stop();
            ProgramData.Log($"[Epic] Total games detected: {games.Count} in {(timer.Elapsed.TotalSeconds >= 60 ? $"{timer.Elapsed.TotalSeconds / 60:F1} minutes" : $"{timer.Elapsed.TotalSeconds:F1}s")}");
            return games;
        });
}