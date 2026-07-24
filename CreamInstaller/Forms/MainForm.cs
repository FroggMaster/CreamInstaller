using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Platforms.Epic;
using CreamInstaller.Platforms.Epic.Heroic;
using CreamInstaller.Platforms.Paradox;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Platforms.Ubisoft;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Forms;

internal sealed partial class MainForm : CustomForm
{
    private const string HelpButtonListPrefix = "\n    •  ";

    private static MainForm current;
    private static readonly object currentLock = new();

    private readonly ConcurrentDictionary<string, string> remainingDLCs = new();

    private readonly ConcurrentDictionary<string, string> remainingGames = new();

    private bool initialLoad = true;

    private List<(Platform platform, string id, string name)> programsToScan;

    private static readonly List<Task> configApiTasks = [];

    private const int SteamCmdTimeoutMs = 16000;
    private const string DlcRefreshLogPrefix = "[DLCRefresh] ";

    private MainForm()
    {
        InitializeComponent();
        selectionTreeView.TreeViewNodeSorter = Program.SortByName ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;
        Text = Program.ApplicationName;
    }

    internal void UpdateSortOrder(bool sortByName)
        => selectionTreeView.TreeViewNodeSorter = sortByName
            ? PlatformIdComparer.NodeText
            : PlatformIdComparer.NodeName;

    internal static MainForm Current
    {
        get
        {
            lock (currentLock)
            {
                if (current is null || current.Disposing || current.IsDisposed)
                {
                    current = new MainForm();
                }
                return current;
            }
        }
    }

    private static void UpdateRemaining(Label label, ConcurrentDictionary<string, string> list, string descriptor)
        => label.Text = list.IsEmpty
            ? ""
            : $"Remaining {descriptor} ({list.Count}): " + string.Join(", ", list.Values).Replace("&", "&&");

    private void UpdateRemainingGames() => UpdateRemaining(progressLabelGames, remainingGames, "games");

    private void AddToRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            remainingGames[gameName] = gameName;
            UpdateRemainingGames();
        });
    }

    private void RemoveFromRemainingGames(string gameName)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            _ = remainingGames.Remove(gameName, out _);
            UpdateRemainingGames();
        });
    }

    private void UpdateRemainingDLCs() => UpdateRemaining(progressLabelDLCs, remainingDLCs, "DLCs");

    private void AddToRemainingDLCs(string dlcId)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            remainingDLCs[dlcId] = dlcId;
            UpdateRemainingDLCs();
        });
    }

    private void RemoveFromRemainingDLCs(string dlcId)
    {
        if (Program.Canceled)
            return;
        Invoke(delegate
        {
            if (Program.Canceled)
                return;
            _ = remainingDLCs.Remove(dlcId, out _);
            UpdateRemainingDLCs();
        });
    }
    private static async Task<T> WithTimeout<T>(Task<T> task, int millisecondsTimeout)
    {
        if (await Task.WhenAny(task, Task.Delay(millisecondsTimeout)) == task)
            return await task;
        return default;
    }

    private static async Task<string> ResolveSteamDlcName(string dlcId, string parentGameName = null, string parentGameId = null)
    {
        StoreAppData dlcStore = await SteamStore.QueryStoreAPI(dlcId, isDlc: true, attempts: 0, parentGameName, parentGameId);
        if (dlcStore?.Name is not null)
            return dlcStore.Name;
        CmdAppData dlcCmd = await SteamCMD.GetAppInfo(dlcId);
        return dlcCmd?.Common?.Name ?? "Unknown";
    }
    private async Task GetApplicablePrograms(IProgress<int> progress, bool uninstallAll = false)
    {
        if (!uninstallAll && (programsToScan is null || programsToScan.Count < 1))
            return;
        int totalGameCount = 0;
        int completeGameCount = 0;

        void AddToRemainingGames(string gameName)
        {
            this.AddToRemainingGames(gameName);
            progress.Report(-Interlocked.Increment(ref totalGameCount));
            progress.Report(completeGameCount);
        }

        void RemoveFromRemainingGames(string gameName)
        {
            this.RemoveFromRemainingGames(gameName);
            progress.Report(Interlocked.Increment(ref completeGameCount));
        }

        if (Program.Canceled)
            return;
        remainingGames.Clear(); // for display purposes only, otherwise ignorable
        remainingDLCs.Clear(); // for display purposes only, otherwise ignorable
        Stopwatch scanTimer = Stopwatch.StartNew();
        double totalLibraryScanSeconds = 0;
        int totalGamesDetected = 0;
        int steamCount = 0, epicCount = 0, ubisoftCount = 0;
        double steamSeconds = 0, epicSeconds = 0, ubiSeconds = 0;
        if (!uninstallAll && programsToScan is { Count: > 0 })
        {
            string platforms = string.Join(", ", programsToScan.Select(p => p.platform.ToString()).Distinct());
            ProgramData.Log.Info($"[Scan] User selected {programsToScan.Count} game(s) for scanning on {platforms}", LogDestination.Scan);
        }
        List<Task> appTasks = new();
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Paradox))
        {
            AddToRemainingGames("Paradox Launcher");
            HashSet<string> dllDirectories =
                await ParadoxLauncher.InstallPath.GetDllDirectoriesFromGameDirectory(Platform.Paradox);
            if (dllDirectories is not null)
            {
                Selection selection = Selection.GetOrCreate(Platform.Paradox, "PL", "Paradox Launcher",
                    ParadoxLauncher.InstallPath, dllDirectories,
                    await ParadoxLauncher.InstallPath.GetExecutableDirectories(validFunc: path =>
                        !Path.GetFileName(path).Contains("bootstrapper")));
                if (uninstallAll)
                    selection.Enabled = true;
                else if (selection.TreeNode.TreeView is null)
                    _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                RemoveFromRemainingGames("Paradox Launcher");
            }
        }

        int steamGamesToCheck;
        TaskCompletionSource gameQueriesDone = new();
        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Steam))
        {
            Stopwatch steamLibTimer = Stopwatch.StartNew();
            List<(string appId, string name, string branch, int buildId, string gameDirectory)> steamGames =
                await SteamLibrary.GetGames();
            steamLibTimer.Stop();
            steamCount = steamGames.Count;
            steamSeconds = steamLibTimer.Elapsed.TotalSeconds;
            totalLibraryScanSeconds += steamSeconds;
            ProgramData.Log.Info($"[Steam] Scanned library: {steamCount} games in {steamSeconds:F3}s", LogDestination.Scan);
            totalGamesDetected += steamCount;
            int steamToProcess = 0, steamBlocked = 0, steamNotSelected = 0;
            steamGamesToCheck = steamGames.Count;
            foreach ((string appId, string name, string branch, int buildId, string gameDirectory) in steamGames)
            {
                if (Program.Canceled)
                    return;
                if (!uninstallAll)
                {
                    var blockReason = Program.GetGameBlockedReason(name, gameDirectory);
                    if (blockReason is not null)
                    {
                        steamBlocked++;
                        ProgramData.Log.Info($"[Steam] Skipping blocked game: {name} ({appId}) — {blockReason}", LogDestination.Scan);
                        _ = Interlocked.Decrement(ref steamGamesToCheck);
                        continue;
                    }
                    if (!programsToScan.Any(c => c.platform is Platform.Steam && c.id == appId))
                    {
                        steamNotSelected++;
                        _ = Interlocked.Decrement(ref steamGamesToCheck);
                        continue;
                    }
                }
                steamToProcess++;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories =
                        await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Steam);
                    bool steamApiDllMissing = dllDirectories is null;
                    if (steamApiDllMissing)
                    {
                        dllDirectories = [];
                        ProgramData.Log.Info($"[Steam] {name} ({appId}): no steam_api.dll or steam_api64.dll found — forced proxying will be used", LogDestination.Scan);
                        if (uninstallAll)
                        {
                            _ = Interlocked.Decrement(ref steamGamesToCheck);
                            RemoveFromRemainingGames(name);
                            return;
                        }
                    }

                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.GetOrCreate(Platform.Steam, appId, name, gameDirectory,
                            dllDirectories,
                            await gameDirectory.GetExecutableDirectories(true));
                        bareSelection.Enabled = true;
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    StoreAppData storeAppData = await SteamStore.QueryStoreAPI(appId);
                    _ = Interlocked.Decrement(ref steamGamesToCheck);
                    if (Volatile.Read(ref steamGamesToCheck) == 0)
                        gameQueriesDone.TrySetResult();
                    CmdAppData cmdAppData = await WithTimeout(SteamCMD.GetAppInfo(appId, branch, buildId), SteamCmdTimeoutMs);
                    if (storeAppData is null && cmdAppData is null)
                    {
                        ProgramData.Log.Info($"[Steam] Skipping {name} ({appId}): no store data from Steam Store or SteamCMD — unable to determine DLCs", LogDestination.Scan);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    ConcurrentDictionary<SelectionDLC, byte> dlc = new();
                    List<Task> dlcTasks = [];
                    HashSet<string> dlcIds = [];
                    if (storeAppData is not null)
                        foreach (string dlcId in await SteamStore.ParseDlcAppIds(storeAppData))
                            _ = dlcIds.Add(dlcId);
                    if (cmdAppData is not null)
                        foreach (string dlcId in await SteamCMD.ParseDlcAppIds(cmdAppData))
                            _ = dlcIds.Add(dlcId);
                    if (dlcIds.Count > 0)
                        foreach (string dlcAppId in dlcIds)
                        {
                            if (Program.Canceled)
                                return;
                            AddToRemainingDLCs(dlcAppId);
                            Task task = Task.Run(async () =>
                            {
                                if (Program.Canceled)
                                    return;
                                while (!Program.Canceled)
                                {
                                    Task completed = await Task.WhenAny(gameQueriesDone.Task, Task.Delay(250));
                                    if (completed == gameQueriesDone.Task)
                                        break;
                                }
                                if (Program.Canceled)
                                    return;
                                string fullGameAppId = null;
                                string dlcName = null;
                                string dlcIcon = null;
                                bool onSteamStore = false;
                                StoreAppData dlcStoreAppData = await SteamStore.QueryStoreAPI(dlcAppId, true, 0, name, appId);
                                if (dlcStoreAppData is not null)
                                {
                                    dlcName = dlcStoreAppData.Name;
                                    dlcIcon = dlcStoreAppData.HeaderImage;
                                    onSteamStore = true;
                                    fullGameAppId = dlcStoreAppData.FullGame?.AppId;
                                }
                                else
                                {
                                    CmdAppData dlcCmdAppData = await SteamCMD.GetAppInfo(dlcAppId);
                                    if (dlcCmdAppData is not null)
                                    {
                                        dlcName = dlcCmdAppData.Common?.Name;
                                        string dlcIconStaticId = dlcCmdAppData.Common?.Icon;
                                        dlcIconStaticId ??= dlcCmdAppData.Common?.LogoSmall;
                                        dlcIconStaticId ??= dlcCmdAppData.Common?.Logo;
                                        if (dlcIconStaticId is not null)
                                            dlcIcon = IconGrabber.SteamAppImagesPath +
                                                      @$"\{dlcAppId}\{dlcIconStaticId}.jpg";
                                        fullGameAppId = dlcCmdAppData.Common?.Parent;
                                    }
                                }

                                if (fullGameAppId != null && fullGameAppId != appId)
                                {
                                    string fullGameName = null;
                                    string fullGameIcon = null;
                                    bool fullGameOnSteamStore = false;
                                    StoreAppData fullGameStoreAppData =
                                        await SteamStore.QueryStoreAPI(fullGameAppId, true, 0, null, null);
                                    if (fullGameStoreAppData is not null)
                                    {
                                        fullGameName = fullGameStoreAppData.Name;
                                        fullGameIcon = fullGameStoreAppData.HeaderImage;
                                        fullGameOnSteamStore = true;
                                    }
                                    else
                                    {
                                        CmdAppData fullGameAppInfo = await SteamCMD.GetAppInfo(fullGameAppId);
                                        if (fullGameAppInfo is not null)
                                        {
                                            fullGameName = fullGameAppInfo.Common?.Name;
                                            string fullGameIconStaticId = fullGameAppInfo.Common?.Icon;
                                            fullGameIconStaticId ??= fullGameAppInfo.Common?.LogoSmall;
                                            fullGameIconStaticId ??= fullGameAppInfo.Common?.Logo;
                                            if (fullGameIconStaticId is not null)
                                                dlcIcon = IconGrabber.SteamAppImagesPath +
                                                          @$"\{fullGameAppId}\{fullGameIconStaticId}.jpg";
                                        }
                                    }

                                    if (Program.Canceled)
                                        return;
                                    if (!string.IsNullOrWhiteSpace(fullGameName) && fullGameAppId != dlcAppId && !Selection.FromId(Platform.Steam, appId)?.DLCById.ContainsKey(fullGameAppId) == true)
                                    {
                                        SelectionDLC fullGameDlc = SelectionDLC.GetOrCreate(
                                            fullGameOnSteamStore ? DLCType.Steam : DLCType.SteamHidden, appId,
                                            fullGameAppId, fullGameName);
                                        fullGameDlc.Icon = fullGameIcon;
                                        _ = dlc.TryAdd(fullGameDlc, default);
                                    }
                                }

                                if (Program.Canceled)
                                    return;
                                if (string.IsNullOrWhiteSpace(dlcName))
                                    dlcName = "Unknown";
                                SelectionDLC _dlc = SelectionDLC.GetOrCreate(
                                    onSteamStore ? DLCType.Steam : DLCType.SteamHidden, appId, dlcAppId, dlcName);
                                _dlc.Icon = dlcIcon;
                                _ = dlc.TryAdd(_dlc, default);
                                RemoveFromRemainingDLCs(dlcAppId);
                            });
                            dlcTasks.Add(task);
                        }
                    else
                    {
                        ProgramData.Log.Info($"[Steam] Skipping {name} ({appId}): no DLC entries found in store data", LogDestination.Scan);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled)
                            return;
                        await task;
                    }

                    gameQueriesDone.TrySetResult();
                    if (dlc.IsEmpty)
                    {
                        ProgramData.Log.Info($"[Steam] Skipping {name} ({appId}): no DLCs remained after processing", LogDestination.Scan);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    Selection selection = Selection.GetOrCreate(Platform.Steam, appId, storeAppData?.Name ?? name,
                        gameDirectory, dllDirectories,
                        await gameDirectory.GetExecutableDirectories(true));
                    selection.SteamApiDllMissing = steamApiDllMissing;
                    if (steamApiDllMissing)
                    {
                        selection.UseProxy = true;
                        bool has64 = selection.ExecutableDirectories.Any(d => d.binaryType == BinaryType.BIT64);
                        bool has32 = selection.ExecutableDirectories.Any(d => d.binaryType == BinaryType.BIT32);
                        string dllName = (has64, has32) switch
                        {
                            (true, true) => "steam_api.dll / steam_api64.dll",
                            (true, false) => "steam_api64.dll",
                            _ => "steam_api.dll"
                        };
                        selection.TreeNode.ToolTipText = dllName + " was not detected in the game directory. Only proxy installation is available.";
                    }
                    selection.Product = "https://store.steampowered.com/app/" + appId;
                    selection.Icon = IconGrabber.SteamAppImagesPath + @$"\{appId}\{cmdAppData?.Common?.Icon}.jpg";
                    selection.SubIcon = storeAppData?.HeaderImage ?? IconGrabber.SteamAppImagesPath
                        + @$"\{appId}\{cmdAppData?.Common?.ClientIcon}.ico";
                    selection.Publisher = storeAppData?.Publishers?.FirstOrDefault() ?? cmdAppData?.Extended?.Publisher;
                    selection.Website = storeAppData?.Website;
                    if (Program.Canceled)
                        return;
                    Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        if (selection.TreeNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                        foreach ((SelectionDLC dlc, _) in dlc)
                        {
                            if (Program.Canceled)
                                return;
                            dlc.Selection = selection;
                        }
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
            if (!uninstallAll)
                ProgramData.Log.Info($"[Steam] Will process {steamToProcess} selected game(s) for DLC scan ({steamBlocked} blocked, {steamNotSelected} not in current selection)", LogDestination.Scan);
        }

        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Epic))
        {
            Stopwatch epicLibTimer = Stopwatch.StartNew();
            List<Manifest> epicGames = await EpicLibrary.GetGames();
            epicLibTimer.Stop();
            epicCount = epicGames.Count;
            epicSeconds = epicLibTimer.Elapsed.TotalSeconds;
            totalLibraryScanSeconds += epicSeconds;
            ProgramData.Log.Info($"[Epic] Scanned library: {epicCount} games in {epicSeconds:F3}s", LogDestination.Scan);
            totalGamesDetected += epicCount;
            int epicToProcess = 0, epicBlocked = 0, epicNotSelected = 0;
            foreach (Manifest manifest in epicGames)
            {
                string @namespace = manifest.CatalogNamespace;
                string name = manifest.DisplayName;
                string directory = manifest.InstallLocation;
                if (Program.Canceled)
                    return;
                if (!uninstallAll)
                {
                    var blockReason = Program.GetGameBlockedReason(name, directory);
                    if (blockReason is not null)
                    {
                        epicBlocked++;
                        ProgramData.Log.Info($"[Epic] Skipping blocked game: {name} ({@namespace}) — {blockReason}", LogDestination.Scan);
                        continue;
                    }
                    if (!programsToScan.Any(c => c.platform is Platform.Epic && c.id == @namespace))
                    {
                        epicNotSelected++;
                        continue;
                    }
                }
                epicToProcess++;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories = await directory.GetDllDirectoriesFromGameDirectory(Platform.Epic);
                    if (dllDirectories is null)
                    {
                        ProgramData.Log.Info($"[Epic] Skipping {name} ({@namespace}): no EOSSDK-Win32-Shipping.dll or EOSSDK-Win64-Shipping.dll found. Game directory may be incomplete", LogDestination.Scan);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.GetOrCreate(Platform.Epic, @namespace, name, directory,
                            dllDirectories,
                            await directory.GetExecutableDirectories(true));
                        bareSelection.Enabled = true;
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    List<Task> dlcTasks = new();
                    ConcurrentDictionary<SelectionDLC, byte> catalogItems = new();
                    List<(string id, string name, string product, string icon, string developer)> catalogIds =
                        await EpicStore.QueryCatalog(@namespace);
                    if (catalogIds.Count > 0)
                        foreach ((string id, string name, string product, string icon, string developer) in catalogIds)
                        {
                            if (Program.Canceled)
                                return;
                            AddToRemainingDLCs(id);
                            Task task = Task.Run(() =>
                            {
                                if (Program.Canceled)
                                    return;
                                SelectionDLC catalogItem = SelectionDLC.GetOrCreate(DLCType.Epic, @namespace, id, name);
                                catalogItem.Icon = icon;
                                catalogItem.Product = product;
                                catalogItem.Publisher = developer;
                                _ = catalogItems.TryAdd(catalogItem, default);
                                RemoveFromRemainingDLCs(id);
                            });
                            dlcTasks.Add(task);
                        }

                    if (Program.Canceled)
                        return;
                    foreach (Task task in dlcTasks)
                    {
                        if (Program.Canceled)
                            return;
                        await task;
                    }

                    if (catalogItems.IsEmpty)
                    {
                        ProgramData.Log.Info($"[Epic] Skipping {name} ({@namespace}): no catalog/DLC entries found", LogDestination.Scan);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    Selection selection = Selection.GetOrCreate(Platform.Epic, @namespace, name, directory,
                        dllDirectories,
                        await directory.GetExecutableDirectories(true));
                    foreach ((SelectionDLC dlc, _) in catalogItems.Where(dlc => dlc.Key.Name == selection.Name))
                    {
                        if (Program.Canceled)
                            return;
                        selection.Product = "https://www.epicgames.com/store/product/" + dlc.Product;
                        selection.Icon = dlc.Icon;
                        selection.Publisher = dlc.Publisher;
                    }

                    if (Program.Canceled)
                        return;
                    Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        if (selection.TreeNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                        if (catalogItems.IsEmpty)
                            return;
                        foreach ((SelectionDLC dlc, _) in catalogItems)
                        {
                            if (Program.Canceled)
                                return;
                            dlc.Selection = selection;
                        }
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
            if (!uninstallAll)
                ProgramData.Log.Info($"[Epic] Will process {epicToProcess} selected game(s) for DLC scan ({epicBlocked} blocked, {epicNotSelected} not in current selection)", LogDestination.Scan);
        }

        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Ubisoft))
        {
            Stopwatch ubiLibTimer = Stopwatch.StartNew();
            List<(string gameId, string name, string gameDirectory)> ubisoftGames = await UbisoftLibrary.GetGames();
            ubiLibTimer.Stop();
            ubisoftCount = ubisoftGames.Count;
            ubiSeconds = ubiLibTimer.Elapsed.TotalSeconds;
            totalLibraryScanSeconds += ubiSeconds;
            ProgramData.Log.Info($"[Ubisoft] Scanned library: {ubisoftCount} games in {ubiSeconds:F3}s", LogDestination.Scan);
            totalGamesDetected += ubisoftCount;
            int ubiToProcess = 0, ubiBlocked = 0, ubiNotSelected = 0;
            foreach ((string gameId, string name, string gameDirectory) in ubisoftGames)
            {
                if (Program.Canceled)
                    return;
                if (!uninstallAll)
                {
                    var blockReason = Program.GetGameBlockedReason(name, gameDirectory);
                    if (blockReason is not null)
                    {
                        ubiBlocked++;
                        ProgramData.Log.Info($"[Ubisoft] Skipping blocked game: {name} ({gameId}) — {blockReason}", LogDestination.Scan);
                        continue;
                    }
                    if (!programsToScan.Any(c => c.platform is Platform.Ubisoft && c.id == gameId))
                    {
                        ubiNotSelected++;
                        continue;
                    }
                }
                ubiToProcess++;
                AddToRemainingGames(name);
                Task task = Task.Run(async () =>
                {
                    if (Program.Canceled)
                        return;
                    HashSet<string> dllDirectories =
                        await gameDirectory.GetDllDirectoriesFromGameDirectory(Platform.Ubisoft);
                    if (dllDirectories is null)
                    {
                        ProgramData.Log.Info($"[Ubisoft] Skipping {name} ({gameId}): no uplay_r1_loader.dll or uplay_r1_loader64.dll found", LogDestination.Scan);
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (uninstallAll)
                    {
                        Selection bareSelection = Selection.GetOrCreate(Platform.Ubisoft, gameId, name, gameDirectory,
                            dllDirectories,
                            await gameDirectory.GetExecutableDirectories(true));
                        bareSelection.Enabled = true;
                        RemoveFromRemainingGames(name);
                        return;
                    }

                    if (Program.Canceled)
                        return;
                    Selection selection = Selection.GetOrCreate(Platform.Ubisoft, gameId, name, gameDirectory,
                        dllDirectories,
                        await gameDirectory.GetExecutableDirectories(true));
                    selection.Icon = IconGrabber.GetDomainFaviconUrl("store.ubi.com");
                    Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        if (selection.TreeNode.TreeView is null)
                            _ = selectionTreeView.Nodes.Add(selection.TreeNode);
                    });
                    if (Program.Canceled)
                        return;
                    RemoveFromRemainingGames(name);
                });
                appTasks.Add(task);
            }
            if (!uninstallAll)
                ProgramData.Log.Info($"[Ubisoft] Will process {ubiToProcess} selected game(s) ({ubiBlocked} blocked, {ubiNotSelected} not in current selection)", LogDestination.Scan);
        }

        Stopwatch gameDlcTimer = Stopwatch.StartNew();
        await Task.WhenAll(appTasks);
        gameDlcTimer.Stop();

        gameQueriesDone.TrySetResult();

        scanTimer.Stop();
        if (!uninstallAll)
        {
            if (steamCount > 0)
                ProgramData.Log.Info($"[Steam] Total games detected: {steamCount} in {(steamSeconds >= 60 ? $"{steamSeconds / 60:F1} minutes" : $"{steamSeconds:F3}s")}", LogDestination.Scan);
            if (epicCount > 0)
                ProgramData.Log.Info($"[Epic] Total games detected: {epicCount} in {(epicSeconds >= 60 ? $"{epicSeconds / 60:F1} minutes" : $"{epicSeconds:F3}s")}", LogDestination.Scan);
            if (ubisoftCount > 0)
                ProgramData.Log.Info($"[Ubisoft] Total games detected: {ubisoftCount} in {(ubiSeconds >= 60 ? $"{ubiSeconds / 60:F1} minutes" : $"{ubiSeconds:F3}s")}", LogDestination.Scan);
        }
        ProgramData.Log.Info($"[Scan] Game and DLC data gathering: {gameDlcTimer.Elapsed.TotalSeconds:F3}s", LogDestination.Scan);
        ProgramData.Log.Info($"[Scan] Scan completed in {scanTimer.Elapsed.TotalSeconds:F3}s", LogDestination.Scan);
    }

    private async void OnLoad(bool forceScan = false, bool forceProvideChoices = false)
    {
        try
        {
            Program.Canceled = false;
            useSmokeApiToggle.Enabled = false;
            useSmokeAPIHelpButton.Enabled = false;
            scanButton.Enabled = false;
            noneFoundLabel.Visible = false;
            allCheckBox.Enabled = false;
            installButton.Enabled = false;
            uninstallButton.Enabled = installButton.Enabled;
            selectionTreeView.Enabled = false;
            progressLabel.Text = "Waiting for user to select which programs/games to scan . . .";
            ShowProgressBar();
            await ProgramData.Setup(this);
            ProgramData.ClearLog();
            ProgramData.Log.Info($"[Scan] CreamInstaller {Program.Version} — scan started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogDestination.Scan);
        bool scan = forceScan;
        // On initial launch, if the user has games with installed DLC unlockers, don't re-display the scan window.
        bool skipScanDialog = initialLoad && programsToScan is null && ProgramData.ReadInstalledGames() is { Count: > 0 };
        initialLoad = false;
        if (skipScanDialog)
        {
            ProgramData.Log.Info("[Scan] Found previously installed DLC unlockers; skipping scan window on initial launch", LogDestination.Scan);
            progressLabel.Text = "Loading previously installed DLC unlockers from last session...";
        }
        if (!scan && (programsToScan is null || programsToScan.Count < 1 || forceProvideChoices) && !skipScanDialog)
        {
            Stopwatch selectionTimer = Stopwatch.StartNew();
            List<(Platform platform, string id, string name, bool alreadySelected)> gameChoices = new();
            if (ParadoxLauncher.InstallPath.DirectoryExists())
                gameChoices.Add((Platform.Paradox, "PL", "Paradox Launcher",
                    programsToScan is not null &&
                    programsToScan.Any(p => p.platform is Platform.Paradox && p.id == "PL")));
            if (SteamLibrary.InstallPath.DirectoryExists())
                foreach ((string appId, string name, string _, int _, string _) in
                         (await SteamLibrary.GetGames()).Where(g
                             => !Program.IsGameBlocked(g.name, g.gameDirectory)))
                    gameChoices.Add((Platform.Steam, appId, name,
                        programsToScan is not null &&
                        programsToScan.Any(p => p.platform is Platform.Steam && p.id == appId)));
            if (EpicLibrary.EpicManifestsPath.DirectoryExists() || HeroicLibrary.HeroicLibraryPath.DirectoryExists())
                gameChoices.AddRange((await EpicLibrary.GetGames())
                    .Where(m => !Program.IsGameBlocked(m.DisplayName, m.InstallLocation)).Select(manifest
                        => (Platform.Epic, manifest.CatalogNamespace, manifest.DisplayName,
                            programsToScan is not null && programsToScan.Any(p =>
                                p.platform is Platform.Epic && p.id == manifest.CatalogNamespace))));
            foreach ((string gameId, string name, string _) in (await UbisoftLibrary.GetGames()).Where(g =>
                         !Program.IsGameBlocked(g.name, g.gameDirectory)))
                gameChoices.Add((Platform.Ubisoft, gameId, name,
                    programsToScan is not null &&
                    programsToScan.Any(p => p.platform is Platform.Ubisoft && p.id == gameId)));
            selectionTimer.Stop();
            ProgramData.Log.Info($"[Total] Total time spent detecting games and libraries: {(selectionTimer.Elapsed.TotalSeconds >= 60 ? $"{selectionTimer.Elapsed.TotalSeconds / 60:F1} minutes" : $"{selectionTimer.Elapsed.TotalSeconds:F3}s")}", LogDestination.Scan);
            if (gameChoices.Count > 0)
            {
                using ScanDialog form = new(this);
                DialogResult selectResult = form.QueryUser("Choose which programs and/or games to scan:", gameChoices,
                    out List<(Platform platform, string id, string name)> choices);
                scan = selectResult == DialogResult.OK && choices is not null && choices.Count > 0;

                const string retry = "\n\nPress the \"Rescan\" button to re-choose.";
                if (scan)
                {
                    programsToScan = choices;
                    noneFoundLabel.Text = "None of the chosen programs nor games were applicable!" + retry;
                }
                else
                    noneFoundLabel.Text = "You didn't choose any programs nor games!" + retry;
            }
            else
                noneFoundLabel.Text = "No applicable programs nor games were found on your computer!";
        }

        if (scan)
        {
            bool setup = true;
            int maxProgress = 0;
            int curProgress = 0;
            Progress<int> progress = new();
            IProgress<int> iProgress = progress;
            progress.ProgressChanged += (_, _progress) =>
            {
                if (Program.Canceled)
                    return;
                if (_progress < 0 || _progress > maxProgress)
                    maxProgress = -_progress;
                else
                    curProgress = _progress;
                int p = Math.Max(Math.Min((int)((float)curProgress / maxProgress * 100), 100), 0);
                progressLabel.Text =
                    setup
                        ? $"Setting up SteamCMD . . . {p}%"
                        : $"Gathering and caching your applicable games and their DLCs . . . {p}%";
                progressBar.Value = p;
            };
            if (SteamLibrary.InstallPath.DirectoryExists() && programsToScan is not null &&
                programsToScan.Any(c => c.platform is Platform.Steam))
            {
                progressLabel.Text = "Setting up SteamCMD . . . ";
                if (!await SteamCMD.Setup(iProgress))
                {
                    HideProgressBar();
                    OnLoad(forceScan, true);
                    return;
                }
            }

            setup = false;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . ";
            Selection.ValidateAll(programsToScan);
            foreach (Selection selection in Selection.All.Keys)
                selection.TreeNode.Remove();
            await GetApplicablePrograms(iProgress);
            await SteamCMD.Cleanup();
        }

        LoadSelections();
        await DrainConfigApiTasks();
        await LoadSavedInstalledGames();
        SyncInstallerConfigs();
        await DrainConfigApiTasks();
        if (!scan && Selection.All.Keys.Any(s => s.InstalledUnlocker != InstalledUnlocker.None))
            RefreshNewDLCsForInstalledGames();
        HideProgressBar();
            selectionTreeView.Enabled = !Selection.All.IsEmpty;
            allCheckBox.Enabled = selectionTreeView.Enabled;
            noneFoundLabel.Visible = !selectionTreeView.Enabled;
            installButton.Enabled = Selection.AllEnabled.Any();
            uninstallButton.Enabled = installButton.Enabled;
            scanButton.Enabled = true;
            useSmokeApiToggle.Enabled = true;
            useSmokeAPIHelpButton.Enabled = true;
        }
        catch (Exception ex)
        {
            ProgramData.Log.Error("MainForm OnLoad failed", ex);
            // Show error and clean up
            ex.HandleException(this);
            HideProgressBar();
            scanButton.Enabled = true;
            useSmokeApiToggle.Enabled = true;
            useSmokeAPIHelpButton.Enabled = true;
        }
    }

    private void OnTreeViewNodeCheckedChanged(object sender, TreeViewEventArgs e)
    {
        if (e.Action == TreeViewAction.Unknown)
            return;
        TreeNode node = e.Node;
        if (node is null)
            return;
        SyncNodeAncestors(node);
        SyncNodeDescendants(node);
        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = EnumerateTreeNodes(selectionTreeView.Nodes)
            .All(node => node.Text == "Unknown" || node.Checked);
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
        installButton.Enabled = Selection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
    }

    private static void SyncNodeAncestors(TreeNode node)
    {
        TreeNode parentNode = node.Parent;
        if (parentNode is null)
            return;
        parentNode.Checked = parentNode.Nodes.Cast<TreeNode>().Any(childNode => childNode.Checked);
        SyncNodeAncestors(parentNode);
    }

    private static void SyncNodeDescendants(TreeNode node)
    {
        foreach (TreeNode childNode in node.Nodes)
        {
            if (childNode.Text == "Unknown")
                continue;
            childNode.Checked = node.Checked;
            SyncNodeDescendants(childNode);
        }
    }

    private static IEnumerable<TreeNode> EnumerateTreeNodes(TreeNodeCollection nodeCollection)
    {
        foreach (TreeNode rootNode in nodeCollection)
        {
            yield return rootNode;
            foreach (TreeNode childNode in EnumerateTreeNodes(rootNode.Nodes))
                yield return childNode;
        }
    }

    private void ShowProgressBar()
    {
        progressBar.Value = 0;
        progressLabelGames.Text = "Loading . . . ";
        progressLabel.Visible = true;
        progressLabelGames.Text = "";
        progressLabelGames.Visible = true;
        progressLabelDLCs.Text = "";
        progressLabelDLCs.Visible = true;
        progressBar.Visible = true;
        programsGroupBox.Size = programsGroupBox.Size with
        {
            Height = programsGroupBox.Size.Height - progressLabel.Size.Height - progressLabelGames.Size.Height -
                     progressLabelDLCs.Size.Height - progressBar.Size.Height - 6
        };
    }

    private void HideProgressBar()
    {
        progressBar.Value = 100;
        progressLabel.Visible = false;
        progressLabelGames.Visible = false;
        progressLabelDLCs.Visible = false;
        progressBar.Visible = false;
        programsGroupBox.Size = programsGroupBox.Size with
        {
            Height = programsGroupBox.Size.Height + progressLabel.Size.Height + progressLabelGames.Size.Height +
                     progressLabelDLCs.Size.Height + progressBar.Size.Height + 6
        };
    }

    internal void OnNodeRightClick(TreeNode node, Point location)
        => Invoke(() =>
        {
            ContextMenuStrip contextMenuStrip = new();
            ThemeManager.ApplyContextMenu(contextMenuStrip);
            ToolStripItemCollection items = contextMenuStrip.Items;
            string id = node.Name;
            Platform platform = (Platform)node.Tag;
            Selection selection = Selection.FromId(platform, id);
            SelectionDLC dlc = null;
            if (selection is null)
                dlc = SelectionDLC.FromId((DLCType)node.Tag, node.Parent?.Name, id);
            Selection dlcParentSelection = null;
            if (dlc is not null)
                dlcParentSelection = dlc.Selection;
            if (selection is null && dlcParentSelection is null)
                return;
            ContextMenuItem header = id == "PL"
                ? new(node.Text, "Paradox Launcher")
                : selection is not null
                    ? new(node.Text, (id, selection.Icon))
                    : new(node.Text, (id, dlc.Icon), (id, dlcParentSelection.Icon));
            _ = items.Add(header);
            string appInfoVDF = $@"{SteamCMD.AppInfoPath}\{id}.vdf";
            string appInfoCmdJSON = $@"{SteamCMD.AppInfoPath}\{id}.cmd.json";
            string appInfoJSON = $@"{SteamCMD.AppInfoPath}\{id}.json";
            string cooldown = $@"{ProgramData.CooldownPath}\{id}.txt";
            if (appInfoVDF.FileExists() || appInfoCmdJSON.FileExists() || appInfoJSON.FileExists())
            {
                List<ContextMenuItem> queries = [];
                if (appInfoJSON.FileExists())
                {
                    string platformString = selection is null || selection.Platform is Platform.Steam
                        ? "Steam Store "
                        : selection.Platform is Platform.Epic
                            ? "Epic GraphQL "
                            : "";
                    queries.Add(new($"Open {platformString}Query", "Notepad",
                        (_, _) => Diagnostics.OpenFileInNotepad(appInfoJSON)));
                }

                if (appInfoCmdJSON.FileExists())
                    queries.Add(new("Open SteamCMD.net Query", "Notepad",
                        (_, _) => Diagnostics.OpenFileInNotepad(appInfoCmdJSON)));

                if (appInfoVDF.FileExists())
                    queries.Add(new("Open SteamCMD Query", "Notepad",
                        (_, _) => Diagnostics.OpenFileInNotepad(appInfoVDF)));

                if (queries.Count > 0)
                {
                    _ = items.Add(new ToolStripSeparator());
                    foreach (ContextMenuItem query in queries)
                        _ = items.Add(query);
                    bool isGameNode = selection is not null;
                    _ = items.Add(new ContextMenuItem(isGameNode ? "Refresh Game Data" : "Refresh DLC Data", "Command Prompt", async (_, _) =>
                    {
                        appInfoVDF.DeleteFile();
                        appInfoCmdJSON.DeleteFile();
                        appInfoJSON.DeleteFile();
                        cooldown.DeleteFile();
                        if (isGameNode)
                            await RefreshSingleGameData(selection);
                        else
                            await RefreshSingleDlcData(dlc);
                    }));
                }
            }

            if (selection is not null)
            {
                if (id == "PL")
                {
                    _ = items.Add(new ToolStripSeparator());

                    async void EventHandler(object sender, EventArgs e)
                    {
                        _ = await ParadoxLauncher.Repair(this, selection);
                        Program.Canceled = false;
                    }

                    _ = items.Add(new ContextMenuItem("Repair", "Command Prompt", EventHandler));
                }

                _ = items.Add(new ToolStripSeparator());
                _ = items.Add(new ContextMenuItem("Open Root Directory", "File Explorer",
                    (_, _) => Diagnostics.OpenDirectoryInFileExplorer(selection.RootDirectory)));
                int executables = 0;
                foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories)
                    _ = items.Add(new ContextMenuItem(
                        $"Open Executable Directory #{++executables} ({(binaryType == BinaryType.BIT32 ? "32" : "64")}-bit)",
                        "File Explorer", (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                HashSet<string> directories = selection.DllDirectories;
                int steam = 0, epic = 0, r1 = 0, r2 = 0;
                if (selection.Platform is Platform.Steam or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64,
                            out string api64_o, out string old_config,
                            out string config, out string old_log, out string log, out string cache);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() ||
                            old_config.FileExists()
                            || config.FileExists() || old_log.FileExists() || log.FileExists() || cache.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Steamworks Directory #{++steam}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }

                if (selection.Platform is Platform.Epic or Platform.Paradox)
                    foreach (string directory in directories)
                    {
                        directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64,
                            out string api64_o, out string old_config, out string config,
                            out string old_log, out string log);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() ||
                            config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open EOS Directory #{++epic}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }

                if (selection.Platform is Platform.Ubisoft)
                    foreach (string directory in directories)
                    {
                        directory.GetUplayR1Components(out string api32, out string api32_o, out string api64,
                            out string api64_o, out string config,
                            out string log);
                        if (api32.FileExists() || api32_o.FileExists() || api64.FileExists() || api64_o.FileExists() ||
                            config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Uplay R1 Directory #{++r1}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                        directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32,
                            out api32_o, out api64, out api64_o, out config,
                            out log);
                        if (old_api32.FileExists() || old_api64.FileExists() || api32.FileExists() ||
                            api32_o.FileExists() || api64.FileExists()
                            || api64_o.FileExists() || config.FileExists() || log.FileExists())
                            _ = items.Add(new ContextMenuItem($"Open Uplay R2 Directory #{++r2}", "File Explorer",
                                (_, _) => Diagnostics.OpenDirectoryInFileExplorer(directory)));
                    }
            }

            if (id != "PL")
            {
                if (selection?.Platform is Platform.Steam || dlcParentSelection?.Platform is Platform.Steam)
                {
                    _ = items.Add(new ToolStripSeparator());
                    _ = items.Add(new ContextMenuItem("Open SteamDB", "SteamDB",
                        (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamdb.info/app/" + id)));
                }

                if (selection is not null)
                    switch (selection.Platform)
                    {
                        case Platform.Steam:
                            _ = items.Add(new ContextMenuItem("Open Steam Store", "Steam Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Product)));
                            _ = items.Add(new ContextMenuItem("Open Steam Community", ("Sub_" + id, selection.SubIcon),
                                "Steam Community",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://steamcommunity.com/app/" +
                                                                               id)));
                            break;
                        case Platform.Epic:
                            _ = items.Add(new ToolStripSeparator());
                            _ = items.Add(new ContextMenuItem("Open ScreamDB", "ScreamDB",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser("https://scream-db.web.app/offers/" +
                                                                               id)));
                            _ = items.Add(new ContextMenuItem("Open Epic Games Store", "Epic Games",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Product)));
                            break;
                        case Platform.Ubisoft:
                            _ = items.Add(new ToolStripSeparator());
                            _ = items.Add(new ContextMenuItem("Open Ubisoft Store", "Ubisoft Store",
                                (_, _) => Diagnostics.OpenUrlInInternetBrowser(
                                    "https://store.ubi.com/us/" +
                                    selection.Name.Replace(" ", "-").ToLowerInvariant())));
                            break;
                    }
            }

            if (selection?.Website is not null)
                _ = items.Add(new ContextMenuItem("Open Official Website",
                    ("Web_" + id, IconGrabber.GetDomainFaviconUrl(selection.Website)),
                    (_, _) => Diagnostics.OpenUrlInInternetBrowser(selection.Website)));
            contextMenuStrip.Show(selectionTreeView, location);
            contextMenuStrip.Refresh();
        });

    private async Task LoadSavedInstalledGames()
    {
        List<InstalledGameRecord> saved = ProgramData.ReadInstalledGames();
        if (saved.Count == 0)
            return;

        List<InstalledGameRecord> toRemove = [];
        foreach (InstalledGameRecord record in saved)
        {
            // Already in the list from this scan; ensure unlocker, proxy, and extra protection are set
            Selection existing = Selection.FromId(record.Platform, record.Id);
            if (existing is not null)
            {
                if (existing.InstalledUnlocker == InstalledUnlocker.None)
                    existing.InstalledUnlocker = record.Unlocker;
                if (record.UseProxy)
                {
                    existing.UseProxy = true;
                    existing.Proxy = record.ProxyDllName;
                }
                if (record.UseExtraProtection)
                    existing.UseExtraProtection = true;
                continue;
            }

            // Root directory no longer exists mark for removal
            if (!record.RootDirectory.DirectoryExists())
            {
                toRemove.Add(record);
                continue;
            }

            // Reconstruct a minimal Selection from the saved record
            HashSet<string> dllDirectories =
                await record.RootDirectory.GetDllDirectoriesFromGameDirectory(record.Platform);
            if (dllDirectories is null || dllDirectories.Count == 0)
            {
                toRemove.Add(record);
                continue;
            }

            List<(string directory, BinaryType binaryType)> executableDirectories =
                await record.RootDirectory.GetExecutableDirectories(true);

            Selection selection = Selection.FromId(record.Platform, record.Id) ?? Selection.GetOrCreate(record.Platform, record.Id, record.Name,
                record.RootDirectory, dllDirectories, executableDirectories);
            selection.InstalledUnlocker = selection.DetectInstalledUnlocker();
            if (selection.InstalledUnlocker == InstalledUnlocker.None)
                selection.InstalledUnlocker = record.Unlocker;
            if (selection.InstalledUnlocker != InstalledUnlocker.None)
            {
                string detectedProxy = selection.DetectInstalledProxy();
                if (detectedProxy is not null)
                {
                    selection.UseProxy = true;
                    selection.Proxy = detectedProxy;
                }
                else
                {
                    selection.UseProxy = record.UseProxy;
                    selection.Proxy = record.ProxyDllName;
                }
            }
            selection.UseExtraProtection = record.UseExtraProtection;

            Invoke(delegate
            {
                if (selection.TreeNode.TreeView is null)
                    _ = selectionTreeView.Nodes.Add(selection.TreeNode);

                // Restore DLC children from saved record
                if (record.Dlc != null && record.Dlc.Count > 0)
                {
                    foreach (InstalledDlcRecord dlcRecord in record.Dlc)
                    {
                        if (!Enum.TryParse(dlcRecord.DlcType, out DLCType dlcType))
                            continue;
                        SelectionDLC dlc = SelectionDLC.GetOrCreate(dlcType, record.Id, dlcRecord.Id, dlcRecord.Name);
                        dlc.Selection = selection;
                        dlc.Enabled = dlcRecord.Enabled;
                    }
                }
            });
        }

        // Clean up records for games that are gone
        if (toRemove.Count > 0)
        {
            List<InstalledGameRecord> updated = saved.Except(toRemove).ToList();
            ProgramData.WriteInstalledGames(updated);
        }
    }

    /// <summary>Fires a one-time async API query for a config-only DLC; only creates the entry if the API confirms it exists.</summary>
    private static void FireConfigDlcApiQuery(MainForm form, Selection selection, string dlcId)
    {
        Task task = Task.Run(async () =>
        {
            try
            {
                string apiName = await ResolveSteamDlcName(dlcId, selection.Name, selection.Id);
                if (apiName == "Unknown")
                    apiName = null;
                if (!string.IsNullOrEmpty(apiName))
                {
                    if (form is null || form.Disposing || form.IsDisposed)
                        return;
                    form.Invoke(delegate
                    {
                        if (Program.Canceled)
                            return;
                        SelectionDLC dlc = SelectionDLC.GetOrCreate(DLCType.SteamHidden, selection.Id, dlcId, apiName);
                        dlc.Selection = selection;
                        dlc.Enabled = true; // explicitly present in CreamAPI config
                    });
                }
            }
            catch
            {
                // Don't create the DLC if the API query fails
            }
        });
        lock (configApiTasks)
            configApiTasks.Add(task);
    }

    /// <summary>Waits for all in-flight config API queries to complete so the background refresh has a complete picture of known DLCs.</summary>
    private static async Task DrainConfigApiTasks()
    {
        Task[] tasks;
        lock (configApiTasks)
        {
            tasks = [.. configApiTasks];
            configApiTasks.Clear();
        }
        if (tasks.Length > 0)
            await Task.WhenAll(tasks);
    }

    /// <summary>Fires background tasks per installed game to check for new DLCs from store APIs that were released since the last install or scan. Any newly discovered DLCs are added to the tree in a disabled (unchecked) state, since they are not yet configured in the unlocker config files.</summary>
    private static void RefreshNewDLCsForInstalledGames()
    {
        _ = Task.Run(async () =>
        {
            List<Task> refreshTasks = [];
            foreach (Selection selection in Selection.All.Keys)
            {
                if (Program.Canceled)
                    return;
                if (selection.InstalledUnlocker == InstalledUnlocker.None)
                    continue;

                HashSet<string> savedDlcIds = selection.DLC.Select(d => d.Id).ToHashSet();
                Task task = Task.Run(async () =>
                {
                    Stopwatch timer = Stopwatch.StartNew();
                    try
                    {
                        ProgramData.Log.Info($"{DlcRefreshLogPrefix}Checking for new DLCs on {selection.Platform} game \"{selection.Name}\" ({selection.Id}) ...", LogDestination.Scan);
                        foreach (SelectionDLC dlc in selection.DLC)
                            dlc.IsNew = false;
                        HashSet<string> currentDlcIds = [];
                        List<(string id, string name)> newDlcList = [];
                        List<string> discoveredMessages = [];

                        if (selection.Platform == Platform.Steam)
                        {
                            StoreAppData storeAppData = await SteamStore.QueryStoreAPI(selection.Id);
                            if (storeAppData is not null)
                                foreach (string dlcId in await SteamStore.ParseDlcAppIds(storeAppData))
                                    _ = currentDlcIds.Add(dlcId);

                            CmdAppData cmdAppData = await WithTimeout(SteamCMD.GetAppInfo(selection.Id), SteamCmdTimeoutMs);
                            if (cmdAppData is not null)
                                foreach (string dlcId in await SteamCMD.ParseDlcAppIds(cmdAppData))
                                    _ = currentDlcIds.Add(dlcId);

                            foreach (string dlcId in currentDlcIds)
                            {
                                if (savedDlcIds.Contains(dlcId))
                                    continue;
                                string dlcName = await ResolveSteamDlcName(dlcId, selection.Name, selection.Id);
                                newDlcList.Add((dlcId, dlcName));
                                discoveredMessages.Add($"{DlcRefreshLogPrefix}New DLC discovered for \"{selection.Name}\" ({selection.Id}): \"{dlcName}\" ({dlcId})");
                            }
                        }
                        else if (selection.Platform == Platform.Epic)
                        {
                            List<(string id, string name, string product, string icon, string developer)> catalog =
                                await EpicStore.QueryCatalog(selection.Id);
                            foreach (var (id, name, _, _, _) in catalog)
                            {
                                _ = currentDlcIds.Add(id);
                                if (!savedDlcIds.Contains(id))
                                {
                                    newDlcList.Add((id, name ?? "Unknown"));
                                    discoveredMessages.Add($"{DlcRefreshLogPrefix}New DLC discovered for \"{selection.Name}\" ({selection.Id}): \"{name ?? "Unknown"}\" ({id})");
                                }
                            }
                        }

                        if (newDlcList.Count > 0)
                        {
                            MainForm form = MainForm.Current;
                            if (form is null || form.Disposing || form.IsDisposed)
                                return;
                            form.Invoke(delegate
                            {
                                if (Program.Canceled)
                                    return;
                                foreach (string msg in discoveredMessages)
                                    ProgramData.Log.Info(msg, LogDestination.Scan);
                                foreach ((string id, string name) in newDlcList)
                                {
                                    DLCType dlcType = selection.Platform switch
                                    {
                                        Platform.Steam => DLCType.Steam,
                                        Platform.Epic => DLCType.Epic,
                                        _ => DLCType.None
                                    };
                                    SelectionDLC dlc = SelectionDLC.GetOrCreate(dlcType, selection.Id, id, name);
                                    dlc.Selection = selection;
                                    dlc.Enabled = selection.InstalledUnlocker == InstalledUnlocker.SmokeAPI;
                                    dlc.IsNew = true;
                                }
                                string state = selection.InstalledUnlocker == InstalledUnlocker.SmokeAPI ? "enabled" : "disabled";
                                ProgramData.Log.Info($"{DlcRefreshLogPrefix}Added {newDlcList.Count} new {state} DLC(s) to the tree for \"{selection.Name}\" ({selection.Id}) in {timer.Elapsed.TotalSeconds:F3}s", LogDestination.Scan);
                            });
                        }
                        else
                            ProgramData.Log.Info($"{DlcRefreshLogPrefix}No new DLCs found for \"{selection.Name}\" ({selection.Id}) — {currentDlcIds.Count} total DLCs known in {timer.Elapsed.TotalSeconds:F3}s", LogDestination.Scan);
                    }
                    catch (Exception e)
                    {
                        ProgramData.Log.Info($"{DlcRefreshLogPrefix}Failed to refresh DLCs for \"{selection.Name}\" ({selection.Id}) after {timer.Elapsed.TotalSeconds:F3}s: {e.Message}", LogDestination.Scan);
                    }
                });
                refreshTasks.Add(task);
            }
            Stopwatch timer = Stopwatch.StartNew();
            await Task.WhenAll(refreshTasks);
            timer.Stop();
            ProgramData.Log.Info($"{DlcRefreshLogPrefix}Background DLC refresh completed for {refreshTasks.Count} installed game(s) in {timer.Elapsed.TotalSeconds:F3}s", LogDestination.Scan);

            // Persist all selections with unlockers so newly discovered DLCs survive restart
            PersistInstalledGames();
        });
    }

    /// <summary>Persists all selections with a detected unlocker to installed.json, preserving existing proxy/extra-protection data so detection does not overwrite prior install state.</summary>
    private static void PersistInstalledGames()
    {
        List<InstalledGameRecord> installedRecords = ProgramData.ReadInstalledGames();
        foreach (Selection selection in Selection.All.Keys)
        {
            if (selection.InstalledUnlocker != InstalledUnlocker.None)
            {
                InstalledGameRecord existing = installedRecords.FirstOrDefault(r =>
                    r.Platform == selection.Platform && r.Id == selection.Id);
                ProgramData.UpsertInstalledGame(selection.ToInstalledGameRecord(existing));
            }
        }
    }

    /// <summary>Re-queries store/SteamCMD data for a single game and adds any newly-discovered DLCs to the tree. Does not remove existing DLCs.</summary>
    private static async Task RefreshSingleGameData(Selection selection)
    {
        if (selection.Platform == Platform.Steam)
        {
            StoreAppData storeAppData = await SteamStore.QueryStoreAPI(selection.Id);
            CmdAppData cmdAppData = await SteamCMD.GetAppInfo(selection.Id);
            HashSet<string> currentDlcIds = [];
            if (storeAppData is not null)
                foreach (string dlcId in await SteamStore.ParseDlcAppIds(storeAppData))
                    _ = currentDlcIds.Add(dlcId);
            if (cmdAppData is not null)
                foreach (string dlcId in await SteamCMD.ParseDlcAppIds(cmdAppData))
                    _ = currentDlcIds.Add(dlcId);
            HashSet<string> existingIds = selection.DLC.Select(d => d.Id).ToHashSet();
            foreach (string dlcId in currentDlcIds)
            {
                if (existingIds.Contains(dlcId))
                    continue;
                string dlcName = await ResolveSteamDlcName(dlcId, selection.Name, selection.Id);
                SelectionDLC dlc = SelectionDLC.GetOrCreate(DLCType.Steam, selection.Id, dlcId, dlcName);
                dlc.Selection = selection;
            }
        }
        else if (selection.Platform == Platform.Epic)
        {
            List<(string id, string name, string product, string icon, string developer)> catalog =
                await EpicStore.QueryCatalog(selection.Id);
            HashSet<string> existingIds = selection.DLC.Select(d => d.Id).ToHashSet();
            foreach ((string id, string name, string product, string icon, string developer) in catalog)
            {
                if (existingIds.Contains(id))
                    continue;
                SelectionDLC dlc = SelectionDLC.GetOrCreate(DLCType.Epic, selection.Id, id, name);
                dlc.Product = product;
                dlc.Icon = icon;
                dlc.Publisher = developer;
                dlc.Selection = selection;
            }
        }
    }

    /// <summary>Re-queries the name for a single DLC and updates it in the tree.</summary>
    private static async Task RefreshSingleDlcData(SelectionDLC dlc)
    {
        if (dlc.Type is DLCType.Steam or DLCType.SteamHidden)
        {
            string name = await ResolveSteamDlcName(dlc.Id, dlc.Selection?.Name, dlc.Selection?.Id);
            if (name != "Unknown")
                dlc.Name = name;
        }
    }

    private void OnLoad(object sender, EventArgs _)
    {
        bool retry = true;
        while (retry)
        {
            try
            {
                HideProgressBar();
                selectionTreeView.AfterCheck += OnTreeViewNodeCheckedChanged;
                OnLoad(forceProvideChoices: true);
                retry = false;
            }
            catch (Exception e)
            {
                retry = e.HandleException(this);
                if (!retry)
                    Close();
            }
        }
    }

    private void OnAccept(bool uninstall = false)
    {
        if (Selection.All.IsEmpty || !uninstall && ParadoxLauncher.DlcDialog(this))
            return;
        Hide();
        InstallForm form = new(uninstall);
        form.InheritLocation(this);
        form.FormClosing += (_, _) =>
        {
            if (form.Reselecting)
            {
                InheritLocation(form);
                Show();
#if DEBUG
                DebugForm.Current.Attach(this);
#endif
                OnLoad();
            }
            else
                Close();
        };
        form.Show();
        Hide();
#if DEBUG
        DebugForm.Current.Attach(form);
#endif
    }

    private void OnInstall(object sender, EventArgs e) => OnAccept();

    private void OnUninstall(object sender, EventArgs e) => OnAccept(true);

    private void OnScan(object sender, EventArgs e) => OnLoad(forceProvideChoices: true);

    private void OnAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldEnable = Selection.All.Keys.Any(s => !s.Enabled);
        foreach (Selection selection in Selection.All.Keys.Where(s => s.Enabled != shouldEnable))
        {
            selection.Enabled = shouldEnable;
            OnTreeViewNodeCheckedChanged("OnAllCheckBoxChanged", new(selection.TreeNode, TreeViewAction.ByMouse));
        }

        allCheckBox.CheckedChanged -= OnAllCheckBoxChanged;
        allCheckBox.Checked = shouldEnable;
        allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
    }

    private void LoadSelections()
    {
        List<(Platform platform, string id, string proxy, bool enabled)> proxyChoices =
            ProgramData.ReadProxyChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
            if (proxyChoices.Any(c => c.platform == selection.Platform && c.id == selection.Id))
            {
                (Platform platform, string id, string proxy, bool enabled)
                    choice = proxyChoices.First(c => c.platform == selection.Platform && c.id == selection.Id);
                (Platform platform, string id, string proxy, bool enabled) = choice;
                string currentProxy = proxy;
                if (proxy is not null && proxy.Contains('.')) // convert pre-v4.1.0.0 choices
                    proxy.GetProxyInfoFromIdentifier(out currentProxy, out _);
                if (proxy != currentProxy && proxyChoices.Remove(choice)) // convert pre-v4.1.0.0 choices
                    proxyChoices.Add((platform, id, currentProxy, enabled));
                if (currentProxy is null or Selection.DefaultProxy && !enabled)
                    _ = proxyChoices.RemoveAll(c => c.platform == platform && c.id == id);
                else
                {
                    selection.UseProxy = enabled;
                    selection.Proxy = currentProxy == Selection.DefaultProxy ? currentProxy : proxy;
                }
            }
            else if (!selection.SteamApiDllMissing)
            {
                selection.UseProxy = false;
                selection.Proxy = null;
            }

        ProgramData.WriteProxyChoices(proxyChoices);

        List<(Platform platform, string id)> extraProtectionChoices =
            ProgramData.ReadExtraProtectionChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
            selection.UseExtraProtection = extraProtectionChoices.Any(c =>
                c.platform == selection.Platform && c.id == selection.Id);

        ProgramData.WriteExtraProtectionChoices(extraProtectionChoices);

        SyncInstallerConfigs();
        PersistInstalledGames();

        OnProxyChanged();
    }

    internal void InvalidateGameList() => selectionTreeView.Invalidate();

    internal void OnProxyChanged()
    {
        selectionTreeView.Invalidate();
    }

    /// <summary>
    /// Detect installed unlockers and proxy DLLs, read SmokeAPI/CreamAPI configs, fire API queries for config DLCs,
    /// and merge with persisted installed game records. Must run after selections are populated and unlocker detection
    /// is meaningful (i.e., after <see cref="LoadSavedInstalledGames"/> on the non-scan path).
    /// </summary>
    private void SyncInstallerConfigs()
    {
        // Detect installed unlockers, proxy DLLs, and read config files — grouped per-game
        foreach (Selection selection in Selection.All.Keys)
        {
            selection.InstalledUnlocker = selection.DetectInstalledUnlocker();
            if (selection.InstalledUnlocker != InstalledUnlocker.None)
            {
                string detectedProxy = selection.DetectInstalledProxy();
                if (detectedProxy is not null)
                {
                    selection.UseProxy = true;
                    selection.Proxy = detectedProxy;
                }
                if (selection.InstalledUnlocker == InstalledUnlocker.SmokeAPI)
                {
                    foreach (string directory in selection.DllDirectories)
                    {
                        HashSet<string> allDlcIds = selection.DLC.Select(d => d.Id).ToHashSet();
                        var (enabledIds, disabledIds) = SmokeAPI.ReadConfigDlcIds(directory, allDlcIds);
                        if (enabledIds is not null) // config was found and read
                        {
                            foreach (SelectionDLC dlc in selection.DLC)
                            {
                                if (enabledIds.Contains(dlc.Id))
                                    dlc.Enabled = true;
                                else if (disabledIds.Contains(dlc.Id))
                                    dlc.Enabled = false;
                                else
                                    dlc.Enabled = true; // not in config — SmokeAPI auto-unlocks by default
                            }
                            break;
                        }
                    }
                }
                else if (selection.InstalledUnlocker == InstalledUnlocker.CreamAPI)
                {
                    foreach (string directory in selection.DllDirectories)
                    {
                        List<(string id, string name)> configDlcs = CreamAPI.ReadConfigDlcs(directory);
                        if (configDlcs is not null)
                        {
                            HashSet<string> configDlcIds = configDlcs.Select(e => e.id).ToHashSet();
                            MainForm form = MainForm.Current;
                            // Sync enabled state for already-known DLCs from the CreamAPI config
                            foreach (SelectionDLC dlc in selection.DLC)
                                dlc.Enabled = configDlcIds.Contains(dlc.Id);
                            // Fire async API queries for config DLCs not already known;
                            // DLC entries are only created if the API confirms they exist
                            foreach ((string id, string name) in configDlcs)
                            {
                                if (!selection.DLC.Any(d => d.Id == id))
                                {
                                    if (form is not null && !form.Disposing && !form.IsDisposed && selection.Platform is Platform.Steam)
                                        FireConfigDlcApiQuery(form, selection, id);
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        // Merge with persisted installed game records for any saved games not yet having a detected unlocker
        List<InstalledGameRecord> installedRecords = ProgramData.ReadInstalledGames();
        foreach (InstalledGameRecord record in installedRecords)
        {
            Selection selection = Selection.FromId(record.Platform, record.Id);
            if (selection is null)
                continue;
            if (selection.InstalledUnlocker == InstalledUnlocker.None && record.Unlocker != InstalledUnlocker.None)
                selection.InstalledUnlocker = record.Unlocker;
        }
    }

    internal void OnExtraProtectionChanged()
    {
        selectionTreeView.Invalidate();
    }

    private void OnUseSmokeApiToggleChanged(object sender, EventArgs e)
    {
        Program.UseSmokeAPI = useSmokeApiToggle.Checked;
        useSmokeApiLabel.Text = useSmokeApiToggle.Checked ? "Selected Unlocker: SmokeAPI" : "Selected Unlocker: CreamAPI";
        ProgramData.SaveSettings(Program.AppSettings);
        selectionTreeView.Invalidate();
    }

    private void OnUseSmokeAPIHelpButtonClicked(object sender, EventArgs e)
    {
        using DialogForm form = new(this);
        _ = form.Show(SystemIcons.Information,
            "[Experimental] WARNING: This may still be unstable.\n" +
            "This setting restores the use of SmokeAPI.\n" +
            "If some games don't launch with SmokeAPI enabled, try disabling this setting then Generate and Install again.",
            customFormText: "Use SmokeAPI");
    }

    private void programsGroupBox_Enter(object sender, EventArgs e) { }

    private void OnSettingsButtonClick(object sender, EventArgs e)
        => SettingsForm.Show(this);

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ThemeManager.Apply(this);
        if (useSmokeApiToggle is not null)
        {
            useSmokeApiToggle.Checked = Program.UseSmokeAPI;
            useSmokeApiLabel.Text = Program.UseSmokeAPI ? "Selected Unlocker: SmokeAPI" : "Selected Unlocker: CreamAPI";
        }
    }
}