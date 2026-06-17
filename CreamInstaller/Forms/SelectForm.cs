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

internal sealed partial class SelectForm : CustomForm
{
    private const string HelpButtonListPrefix = "\n    •  ";

    private static SelectForm current;
    private static readonly object currentLock = new();

    private readonly ConcurrentDictionary<string, string> remainingDLCs = new();

    private readonly ConcurrentDictionary<string, string> remainingGames = new();

    private bool initialLoad = true;

    private List<(Platform platform, string id, string name)> programsToScan;

    private SelectForm()
    {
        InitializeComponent();
        selectionTreeView.TreeViewNodeSorter = sortCheckBox.Checked ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;
        Text = Program.ApplicationName;
    }

    internal static SelectForm Current
    {
        get
        {
            lock (currentLock)
            {
                if (current is null || current.Disposing || current.IsDisposed)
                {
                    current = new SelectForm();
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
        if (!uninstallAll && programsToScan is { Count: > 0 })
        {
            string platforms = string.Join(", ", programsToScan.Select(p => p.platform.ToString()).Distinct());
            ProgramData.Log($"[Scan] User selected {programsToScan.Count} game(s) for scanning on {platforms}");
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
            totalLibraryScanSeconds += steamLibTimer.Elapsed.TotalSeconds;
            ProgramData.Log($"[Steam] Scanned library: {steamGames.Count} games in {steamLibTimer.Elapsed.TotalSeconds:F1}s");
            totalGamesDetected += steamGames.Count;
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
                        ProgramData.Log($"[Steam] Skipping blocked game: {name} ({appId}) — {blockReason}");
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
                        ProgramData.Log($"[Steam] {name} ({appId}): no steam_api.dll or steam_api64.dll found — forced proxying will be used");
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
                    CmdAppData cmdAppData = await WithTimeout(SteamCMD.GetAppInfo(appId, branch, buildId), 16000);
                    if (storeAppData is null && cmdAppData is null)
                    {
                        ProgramData.Log($"[Steam] Skipping {name} ({appId}): no store data from Steam Store or SteamCMD — unable to determine DLCs");
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
                                    CmdAppData dlcCmdAppData = await WithTimeout(SteamCMD.GetAppInfo(dlcAppId), 16000);
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
                                    if (!string.IsNullOrWhiteSpace(fullGameName))
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
                        ProgramData.Log($"[Steam] Skipping {name} ({appId}): no DLC entries found in store data");
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
                        ProgramData.Log($"[Steam] Skipping {name} ({appId}): no DLCs remained after processing");
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
                ProgramData.Log($"[Steam] Will process {steamToProcess} selected game(s) for DLC scan ({steamBlocked} blocked, {steamNotSelected} not in current selection)");
        }

        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Epic))
        {
            Stopwatch epicLibTimer = Stopwatch.StartNew();
            List<Manifest> epicGames = await EpicLibrary.GetGames();
            epicLibTimer.Stop();
            totalLibraryScanSeconds += epicLibTimer.Elapsed.TotalSeconds;
            ProgramData.Log($"[Epic] Scanned library: {epicGames.Count} games in {epicLibTimer.Elapsed.TotalSeconds:F1}s");
            totalGamesDetected += epicGames.Count;
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
                        ProgramData.Log($"[Epic] Skipping blocked game: {name} ({@namespace}) — {blockReason}");
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
                        ProgramData.Log($"[Epic] Skipping {name} ({@namespace}): no EOSSDK-Win32-Shipping.dll or EOSSDK-Win64-Shipping.dll found. Game directory may be incomplete");
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
                        ProgramData.Log($"[Epic] Skipping {name} ({@namespace}): no catalog/DLC entries found");
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
                ProgramData.Log($"[Epic] Will process {epicToProcess} selected game(s) for DLC scan ({epicBlocked} blocked, {epicNotSelected} not in current selection)");
        }

        if (uninstallAll || programsToScan.Any(c => c.platform is Platform.Ubisoft))
        {
            Stopwatch ubiLibTimer = Stopwatch.StartNew();
            List<(string gameId, string name, string gameDirectory)> ubisoftGames = await UbisoftLibrary.GetGames();
            ubiLibTimer.Stop();
            totalLibraryScanSeconds += ubiLibTimer.Elapsed.TotalSeconds;
            ProgramData.Log($"[Ubisoft] Scanned library: {ubisoftGames.Count} games in {ubiLibTimer.Elapsed.TotalSeconds:F1}s");
            totalGamesDetected += ubisoftGames.Count;
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
                        ProgramData.Log($"[Ubisoft] Skipping blocked game: {name} ({gameId}) — {blockReason}");
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
                        ProgramData.Log($"[Ubisoft] Skipping {name} ({gameId}): no uplay_r1_loader.dll or uplay_r1_loader64.dll found");
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
                ProgramData.Log($"[Ubisoft] Will process {ubiToProcess} selected game(s) ({ubiBlocked} blocked, {ubiNotSelected} not in current selection)");
        }

        if (!uninstallAll)
            ProgramData.Log($"[Scan] Total games detected across all libraries: {totalGamesDetected}");
        Stopwatch gameDlcTimer = Stopwatch.StartNew();
        foreach (Task task in appTasks)
        {
            if (Program.Canceled)
                return;
            await task;
        }
        gameDlcTimer.Stop();

        gameQueriesDone.TrySetResult();

        scanTimer.Stop();
        ProgramData.Log($"[Scan] Library scan total: {totalLibraryScanSeconds:F1}s across all platforms");
        ProgramData.Log($"[Scan] Game and DLC data gathering: {gameDlcTimer.Elapsed.TotalSeconds:F1}s");
        ProgramData.Log($"[Scan] Scan completed in {scanTimer.Elapsed.TotalSeconds:F1}s");
    }

    private async void OnLoad(bool forceScan = false, bool forceProvideChoices = false)
    {
        try
        {
            Program.Canceled = false;
            blockedGamesCheckBox.Enabled = false;
            blockProtectedHelpButton.Enabled = false;
            useSmokeAPICheckBox.Enabled = false;
            useSmokeAPIHelpButton.Enabled = false;
            cancelButton.Enabled = true;
            scanButton.Enabled = false;
            noneFoundLabel.Visible = false;
            allCheckBox.Enabled = false;
            proxyAllCheckBox.Enabled = false;
            installButton.Enabled = false;
            uninstallButton.Enabled = installButton.Enabled;
            selectionTreeView.Enabled = false;
            saveButton.Enabled = false;
            loadButton.Enabled = false;
            resetButton.Enabled = false;
            progressLabel.Text = "Waiting for user to select which programs/games to scan . . .";
            ShowProgressBar();
            await ProgramData.Setup(this);
            ProgramData.ClearLog();
            ProgramData.Log($"[Scan] CreamInstaller {Program.Version} — scan started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        bool scan = forceScan;
        // On initial launch, if the user has games with installed DLC unlockers, don't re-display the scan window.
        bool skipScanDialog = initialLoad && programsToScan is null && ProgramData.ReadInstalledGames() is { Count: > 0 };
        initialLoad = false;
        if (skipScanDialog)
        {
            ProgramData.Log("[Scan] Found previously installed DLC unlockers; skipping scan window on initial launch");
            progressLabel.Text = "Loading previously installed DLC unlockers from last session...";
        }
        if (!scan && (programsToScan is null || programsToScan.Count < 1 || forceProvideChoices) && !skipScanDialog)
        {
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
            if (gameChoices.Count > 0)
            {
                using SelectDialogForm form = new(this);
                DialogResult selectResult = form.QueryUser("Choose which programs and/or games to scan:", gameChoices,
                    out List<(Platform platform, string id, string name)> choices);
                if (selectResult == DialogResult.Abort)
                {
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
                        progressLabel.Text = $"Quickly gathering games for uninstallation . . . {p}%";
                        progressBar.Value = p;
                    };
                    progressLabel.Text = "Quickly gathering games for uninstallation . . . ";
                    foreach (Selection selection in Selection.All.Keys)
                        selection.TreeNode.Remove();
                    await GetApplicablePrograms(iProgress, true);
                    if (!Program.Canceled)
                        OnUninstall(null, null);
                    Selection.All.Clear();
                    programsToScan = null;
                }
                else
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

        OnLoadSelections(null, null);
        await LoadSavedInstalledGames();
        HideProgressBar();
        selectionTreeView.Enabled = !Selection.All.IsEmpty;
        allCheckBox.Enabled = selectionTreeView.Enabled;
        proxyAllCheckBox.Enabled = selectionTreeView.Enabled;
        noneFoundLabel.Visible = !selectionTreeView.Enabled;
        installButton.Enabled = Selection.AllEnabled.Any();
        uninstallButton.Enabled = installButton.Enabled;
        saveButton.Enabled = CanSaveSelections();
        loadButton.Enabled = CanLoadSelections();
        resetButton.Enabled = CanResetSelections();
        cancelButton.Enabled = false;
        scanButton.Enabled = true;
        blockedGamesCheckBox.Enabled = true;
        blockProtectedHelpButton.Enabled = true;
        useSmokeAPICheckBox.Enabled = true;
        useSmokeAPIHelpButton.Enabled = true;
        }
        catch (Exception ex)
        {
            ProgramData.LogError("SelectForm OnLoad failed", ex);
            // Show error and clean up
            ex.HandleException(this);
            HideProgressBar();
            cancelButton.Enabled = false;
            scanButton.Enabled = true;
            blockedGamesCheckBox.Enabled = true;
            blockProtectedHelpButton.Enabled = true;
            useSmokeAPICheckBox.Enabled = true;
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
        if (sender is "OnLoadSelections" or "OnResetSelections")
            return;
        saveButton.Enabled = CanSaveSelections();
        resetButton.Enabled = CanResetSelections();
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
                    _ = items.Add(new ContextMenuItem("Refresh Queries", "Command Prompt", (_, _) =>
                    {
                        appInfoVDF.DeleteFile();
                        appInfoCmdJSON.DeleteFile();
                        appInfoJSON.DeleteFile();
                        cooldown.DeleteFile();
                        selection?.Remove();
                        if (dlc is not null)
                            dlc.Selection = null;
                        OnLoad(true);
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

    private void OnCancel(object sender, EventArgs e)
    {
        progressLabel.Text = "Cancelling . . . ";
        Program.Cleanup();
    }

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

    private void OnProxyAllCheckBoxChanged(object sender, EventArgs e)
    {
        bool shouldEnable = Selection.All.Keys.Any(selection => !selection.UseProxy);
        foreach (Selection selection in Selection.All.Keys)
            selection.UseProxy = shouldEnable;
        selectionTreeView.Invalidate();
        proxyAllCheckBox.CheckedChanged -= OnProxyAllCheckBoxChanged;
        proxyAllCheckBox.Checked = shouldEnable;
        proxyAllCheckBox.CheckedChanged += OnProxyAllCheckBoxChanged;
        resetButton.Enabled = CanResetSelections();
        saveButton.Enabled = CanSaveSelections();
    }

    private bool AreSelectionsDefault()
        => EnumerateTreeNodes(selectionTreeView.Nodes).All(node
            => node.Parent is null || node.Tag is not Platform and not DLCType ||
               (node.Text == "Unknown" ? !node.Checked : node.Checked));

    private static bool AreProxySelectionsDefault() => Selection.All.Keys.All(selection => !selection.UseProxy);

    private static bool AreExtraProtectionSelectionsDefault() => Selection.All.Keys.All(selection => !selection.UseExtraProtection);

    private bool CanSaveDlc() =>
        installButton.Enabled && (ProgramData.ReadDlcChoices().Any() || !AreSelectionsDefault());

    private static bool CanSaveProxy() =>
        ProgramData.ReadProxyChoices().Any() || !AreProxySelectionsDefault();

    private static bool CanSaveExtraProtection() =>
        ProgramData.ReadExtraProtectionChoices().Any() || !AreExtraProtectionSelectionsDefault();

    private bool CanSaveSelections() => CanSaveDlc() || CanSaveProxy() || CanSaveExtraProtection();

    private void OnSaveSelections(object sender, EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> dlcChoices = ProgramData.ReadDlcChoices().ToList();
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            _ = dlcChoices.RemoveAll(n =>
                n.platform == dlc.Selection.Platform && n.gameId == dlc.Selection.Id && n.dlcId == dlc.Id);
            if (dlc.Name == "Unknown" ? dlc.Enabled : !dlc.Enabled)
                dlcChoices.Add((dlc.Selection.Platform, dlc.Selection.Id, dlc.Id));
        }

        ProgramData.WriteDlcChoices(dlcChoices);

        List<(Platform platform, string id, string proxy, bool enabled)> proxyChoices =
            ProgramData.ReadProxyChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
        {
            _ = proxyChoices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
            if (selection.UseProxy)
                proxyChoices.Add((selection.Platform, selection.Id,
                    selection.Proxy == Selection.DefaultProxy ? null : selection.Proxy,
                    selection.UseProxy));
        }

        ProgramData.WriteProxyChoices(proxyChoices);

        List<(Platform platform, string id)> extraProtectionChoices =
            ProgramData.ReadExtraProtectionChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
        {
            _ = extraProtectionChoices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
            if (selection.UseExtraProtection)
                extraProtectionChoices.Add((selection.Platform, selection.Id));
        }

        ProgramData.WriteExtraProtectionChoices(extraProtectionChoices);

        loadButton.Enabled = CanLoadSelections();
        saveButton.Enabled = CanSaveSelections();
    }

    private static bool CanLoadDlc() => ProgramData.ReadDlcChoices().Any();

    private static bool CanLoadProxy() => ProgramData.ReadProxyChoices().Any();

    private static bool CanLoadExtraProtection() => ProgramData.ReadExtraProtectionChoices().Any();

    private static bool CanLoadSelections() => CanLoadDlc() || CanLoadProxy() || CanLoadExtraProtection();

    private void OnLoadSelections(object sender, EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> dlcChoices = ProgramData.ReadDlcChoices().ToList();
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            dlc.Enabled = dlcChoices.Any(c =>
                c.platform == dlc.Selection?.Platform && c.gameId == dlc.Selection?.Id && c.dlcId == dlc.Id)
                ? dlc.Name == "Unknown"
                : dlc.Name != "Unknown";
            OnTreeViewNodeCheckedChanged("OnLoadSelections", new(dlc.TreeNode, TreeViewAction.ByMouse));
        }

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
        loadButton.Enabled = CanLoadSelections();

        // Detect installed unlockers and proxy DLLs from disk for all selections
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

        // Persist any selections with a detected unlocker to installed.json, preserving existing
        // proxy/extrapolation data from the saved record so detection does not overwrite prior install state
        foreach (Selection selection in Selection.All.Keys)
        {
            if (selection.InstalledUnlocker != InstalledUnlocker.None)
            {
                InstalledGameRecord existing = installedRecords.FirstOrDefault(r =>
                    r.Platform == selection.Platform && r.Id == selection.Id);
                ProgramData.UpsertInstalledGame(new InstalledGameRecord
                {
                    Platform = selection.Platform,
                    Id = selection.Id,
                    Name = selection.Name,
                    RootDirectory = selection.RootDirectory,
                    Unlocker = selection.InstalledUnlocker,
                    UseProxy = existing?.UseProxy ?? false,
                    ProxyDllName = existing?.UseProxy == true ? existing.ProxyDllName : null,
                    UseExtraProtection = existing?.UseExtraProtection ?? false,
                    Dlc = selection.DLC.Select(dlc => new InstalledDlcRecord
                    {
                        DlcType = dlc.Type.ToString(),
                        Id = dlc.Id,
                        Name = dlc.Name
                    }).ToList()
                });
            }
        }

        OnProxyChanged();
    }

    private bool CanResetDlc() => !AreSelectionsDefault();

    private static bool CanResetProxy() => !AreProxySelectionsDefault();

    private static bool CanResetExtraProtection() => !AreExtraProtectionSelectionsDefault();

    private bool CanResetSelections() => CanResetDlc() || CanResetProxy() || CanResetExtraProtection();

    private void OnResetSelections(object sender, EventArgs e)
    {
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            dlc.Enabled = dlc.Name != "Unknown";
            OnTreeViewNodeCheckedChanged("OnResetSelections", new(dlc.TreeNode, TreeViewAction.ByMouse));
        }

        foreach (Selection selection in Selection.All.Keys)
        {
            selection.UseProxy = false;
            selection.Proxy = null;
            selection.UseExtraProtection = false;
        }

        OnProxyChanged();
    }

    internal void InvalidateGameList() => selectionTreeView.Invalidate();

    internal void OnProxyChanged()
    {
        selectionTreeView.Invalidate();
        saveButton.Enabled = CanSaveSelections();
        resetButton.Enabled = CanResetSelections();
        proxyAllCheckBox.CheckedChanged -= OnProxyAllCheckBoxChanged;
        proxyAllCheckBox.Checked = Selection.All.Keys.Count != 0 && Selection.All.Keys.All(selection => selection.UseProxy);
        proxyAllCheckBox.CheckedChanged += OnProxyAllCheckBoxChanged;
    }

    internal void OnExtraProtectionChanged()
    {
        selectionTreeView.Invalidate();
        saveButton.Enabled = CanSaveSelections();
        resetButton.Enabled = CanResetSelections();
    }

    private void OnBlockProtectedGamesCheckBoxChanged(object sender, EventArgs e)
    {
        Program.BlockProtectedGames = blockedGamesCheckBox.Checked;
        OnLoad(forceProvideChoices: true);
    }

    private void OnBlockProtectedGamesHelpButtonClicked(object sender, EventArgs e)
    {
        StringBuilder blockedGames = new();
        foreach (string name in Program.ProtectedGames)
            _ = blockedGames.Append(HelpButtonListPrefix + name);
        StringBuilder blockedDirectories = new();
        foreach (string path in Program.ProtectedGameDirectories)
            _ = blockedDirectories.Append(HelpButtonListPrefix + path);
        StringBuilder blockedDirectoryExceptions = new();
        foreach (string name in Program.ProtectedGameDirectoryExceptions)
            _ = blockedDirectoryExceptions.Append(HelpButtonListPrefix + name);
        using DialogForm form = new(this);
        _ = form.Show(SystemIcons.Information,
            "Blocks the program from caching and displaying games protected by anti-cheats."
            + "\nYou disable this option and install DLC unlockers to protected games at your own risk!" +
            "\n\nBlocked games: "
            + (string.IsNullOrWhiteSpace(blockedGames.ToString()) ? "(none)" : blockedGames) +
            "\n\nBlocked game sub-directories: "
            + (string.IsNullOrWhiteSpace(blockedDirectories.ToString()) ? "(none)" : blockedDirectories) +
            "\n\nBlocked game sub-directory exceptions: "
            + (string.IsNullOrWhiteSpace(blockedDirectoryExceptions.ToString())
                ? "(none)"
                : blockedDirectoryExceptions),
            customFormText: "Block Protected Games");
    }
    private void OnUseSmokeAPICheckBoxChanged(object sender, EventArgs e)
    {
        Program.UseSmokeAPI = useSmokeAPICheckBox.Checked;
        selectionTreeView.Invalidate();
        saveButton.Enabled = CanSaveSelections();
        resetButton.Enabled = CanResetSelections();
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

    private void OnSortCheckBoxChanged(object sender, EventArgs e)
        => selectionTreeView.TreeViewNodeSorter =
            sortCheckBox.Checked ? PlatformIdComparer.NodeText : PlatformIdComparer.NodeName;

    private void programsGroupBox_Enter(object sender, EventArgs e)
    {

    }

    private void OnDarkModeCheckBoxChanged(object sender, EventArgs e)
    {
        Program.DarkModeEnabled = darkModeCheckBox.Checked;
        ThemeManager.ApplyToAllOpenForms();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ThemeManager.Apply(this);
        if (darkModeCheckBox is not null)
            darkModeCheckBox.Checked = Program.DarkModeEnabled;
    }
}