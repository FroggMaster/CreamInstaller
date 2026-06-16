using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Platforms.Epic;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Platforms.Ubisoft;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal sealed partial class TestGameForm : CustomForm
{
    private static readonly string TestGamesRoot =
        Path.Combine(ProgramData.DirectoryPath, "TestGames");

    private static readonly List<string> CreatedDirectories = [];

    // Cached Epic search results from the last search: (namespace, name)
    private readonly List<(string ns, string name)> epicSearchResults = [];

    // Cached Ubisoft search results from the last search: (id, name)
    private readonly List<(string id, string name)> ubisoftSearchResults = [];

    private bool IsEpicMode => epicRadioButton.Checked;

    private bool IsUbisoftMode => ubisoftRadioButton.Checked;

    internal TestGameForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        appIdTextBox.Leave += OnAppIdLeave;
        appIdTextBox.KeyDown += OnAppIdKeyDown;
        gameNameTextBox.KeyDown += OnGameNameKeyDown;
        UpdatePlatformMode();
    }

    private void UpdatePlatformMode()
    {
        bool epic = IsEpicMode;
        bool ubisoft = IsUbisoftMode;

        // App ID row: only Steam needs it; Epic and Ubisoft use name search
        appIdLabel.Visible = !epic && !ubisoft;
        appIdTextBox.Visible = !epic && !ubisoft;
        appIdLabel.Text = "App ID:";
        appIdTextBox.PlaceholderText = "e.g. 480";
        NativeMethods.RefreshCueBanner(appIdTextBox);

        // Search button: Epic and Ubisoft — shrink the game name box to make room
        bool showSearch = epic || ubisoft;
        epicSearchButton.Visible = epic;
        ubisoftSearchButton.Visible = ubisoft;
        gameNameTextBox.Size = new System.Drawing.Size(showSearch ? 354 : 443, 23);

        // Placeholder text — call RefreshCueBanner to flush the Win32 cue so only one text shows
        gameNameTextBox.PlaceholderText = showSearch
            ? "Enter a game name and press Enter to search"
            : "e.g. Spacewar";
        NativeMethods.RefreshCueBanner(gameNameTextBox);

        // Clear game name and results when switching mode
        gameNameTextBox.Clear();
        epicResultsListBox.Visible = false;
        ubisoftResultsListBox.Visible = false;

        if (!epic)
            epicSearchResults.Clear();

        if (!ubisoft)
            ubisoftSearchResults.Clear();

        SetStatus(showSearch
            ? "Enter a game name and press Enter to search."
            : "Enter the App ID and press Enter to search.");
    }

    private void OnPlatformChanged(object sender, EventArgs e) => UpdatePlatformMode();

    // ── Steam: auto-detect name from AppID ──────────────────────────────────

    private async void OnAppIdLeave(object sender, EventArgs e)
    {
        if (IsEpicMode || IsUbisoftMode)
            return;
        string appId = appIdTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(appId) || !int.TryParse(appId, out _))
            return;
        if (!string.IsNullOrWhiteSpace(gameNameTextBox.Text))
            return;

        SetStatus("Looking up game name . . .");
        generateButton.Enabled = false;

        string name = await Task.Run(async () =>
        {
            // Use an isolated client with neutral UA so Steam's store API doesn't reject the request.
            using System.Net.Http.HttpClient client = HttpClientManager.CreateIsolatedClient();
            string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=basic";
            try
            {
                string json = await client.GetStringAsync(url);
                Newtonsoft.Json.Linq.JObject root = Newtonsoft.Json.Linq.JObject.Parse(json);
                string title = root[appId]?["data"]?["name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(title))
                    return title;
            }
            catch (Exception ex) { ProgramData.LogWarning($"[TestGame] Store name lookup failed for AppID {appId}: {ex.Message}"); /* fall through to SteamCMD */ }

            CmdAppData cmdData = await SteamCMD.GetAppInfo(appId);
            return cmdData?.Common?.Name;
        });

        generateButton.Enabled = true;

        if (name is not null)
        {
            gameNameTextBox.Text = name;
            SetStatus($"✓ Game name detected: {name}");
        }
        else
        {
            SetStatus("Could not auto-detect name; enter it manually.");
        }
    }

    // ── Epic: search by name ─────────────────────────────────────────────────

    private async void OnEpicSearch(object sender, EventArgs e)
    {
        string keyword = gameNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            SetStatus("Enter a game name to search.");
            return;
        }

        SetStatus("Searching Epic store . . .");
        epicSearchButton.Enabled = false;
        generateButton.Enabled = false;
        epicResultsListBox.Items.Clear();
        epicResultsListBox.Visible = false;
        epicSearchResults.Clear();

        List<(string ns, string name)> results = await EpicStore.QuerySearch(keyword);

        epicSearchButton.Enabled = true;
        generateButton.Enabled = true;

        if (results.Count == 0)
        {
            SetStatus("No results found. Try a different name.");
            return;
        }

        epicSearchResults.AddRange(results);
        foreach ((string _, string name) in results)
            epicResultsListBox.Items.Add(name);

        epicResultsListBox.Visible = true;
        SetStatus($"Found {results.Count} result(s). Select one to use it.");
    }

    private void OnEpicResultSelected(object sender, EventArgs e)
    {
        int idx = epicResultsListBox.SelectedIndex;
        if (idx < 0 || idx >= epicSearchResults.Count)
            return;
        gameNameTextBox.Text = epicSearchResults[idx].name;
        SetStatus($"✓ Selected: {epicSearchResults[idx].name}");
    }

    // ── Ubisoft: search by name ──────────────────────────────────────────────

    private async void OnUbisoftSearch(object sender, EventArgs e)
    {
        string keyword = gameNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            SetStatus("Enter a game name to search.");
            return;
        }

        SetStatus("Searching Ubisoft store . . .");
        ubisoftSearchButton.Enabled = false;
        generateButton.Enabled = false;
        ubisoftResultsListBox.Items.Clear();
        ubisoftResultsListBox.Visible = false;
        ubisoftSearchResults.Clear();

        List<(string id, string name)> results = await UbisoftStore.QuerySearch(keyword);

        ubisoftSearchButton.Enabled = true;
        generateButton.Enabled = true;

        if (results.Count == 0)
        {
            SetStatus("No results found. Try a different name.");
            return;
        }

        ubisoftSearchResults.AddRange(results);
        foreach ((string _, string name) in results)
            ubisoftResultsListBox.Items.Add(name);

        ubisoftResultsListBox.Visible = true;
        SetStatus($"Found {results.Count} result(s). Select one to use it.");
    }

    private void OnUbisoftResultSelected(object sender, EventArgs e)
    {
        int idx = ubisoftResultsListBox.SelectedIndex;
        if (idx < 0 || idx >= ubisoftSearchResults.Count)
            return;
        gameNameTextBox.Text = ubisoftSearchResults[idx].name;
        SetStatus($"✓ Selected: {ubisoftSearchResults[idx].name}");
    }

    // ── Enter key handlers ───────────────────────────────────────────────────

    private void OnAppIdKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !IsEpicMode && !IsUbisoftMode)
        {
            e.SuppressKeyPress = true;
            OnAppIdLeave(sender, e);
        }
    }

    private void OnGameNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;
        e.SuppressKeyPress = true;
        if (IsEpicMode)
            OnEpicSearch(sender, e);
        else if (IsUbisoftMode)
            OnUbisoftSearch(sender, e);
        else
            OnAppIdLeave(sender, e);
    }

    // ── Generate ────────────────────────────────────────────────────────────

    private void OnGenerate(object sender, EventArgs e)
    {
        if (IsEpicMode)
            GenerateEpic();
        else if (IsUbisoftMode)
            GenerateUbisoft();
        else
            GenerateSteam();
    }

    private void GenerateSteam()
    {
        string appId = appIdTextBox.Text.Trim();
        string gameName = gameNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(appId) || !int.TryParse(appId, out _))
        {
            SetStatus("App ID must be a valid integer.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gameName))
        {
            SetStatus("Game Name cannot be empty.");
            return;
        }

        if (SteamLibrary.TestGames.Any(g => g.appId == appId))
        {
            SetStatus($"A test game with App ID {appId} already exists.");
            return;
        }

        try
        {
            string gameDir = Path.Combine(TestGamesRoot, $"steam_{appId}_{SanitizeName(gameName)}");
            Directory.CreateDirectory(gameDir);

            string dllPath = Path.Combine(gameDir, "steam_api64.dll");
            WriteDllStub(dllPath);

            CreatedDirectories.Add(gameDir);
            SteamLibrary.TestGames.Add((appId, gameName, "public", 1, gameDir));
            ProgramData.Log($"[TestGame] Steam: {gameName} ({appId}) at {gameDir}");
            SetStatus($"✓ Steam test game '{gameName}' ({appId}) generated. Press Rescan.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
    }

    private void GenerateEpic()
    {
        string gameName = gameNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(gameName))
        {
            SetStatus("Game Name cannot be empty. Search for a game first.");
            return;
        }

        // Use the selected search result namespace if available, otherwise derive a stub
        string catalogNamespace;
        int idx = epicResultsListBox.SelectedIndex;
        if (idx >= 0 && idx < epicSearchResults.Count)
        {
            catalogNamespace = epicSearchResults[idx].ns;
            gameName = epicSearchResults[idx].name;
        }
        else
        {
            catalogNamespace = $"test_{SanitizeName(gameName).ToLowerInvariant()}";
        }

        if (EpicLibrary.TestManifests.Any(m => m.CatalogNamespace == catalogNamespace))
        {
            SetStatus("An Epic test game with that namespace already exists.");
            return;
        }

        try
        {
            string gameDir = Path.Combine(TestGamesRoot, $"epic_{SanitizeName(gameName)}");
            Directory.CreateDirectory(gameDir);

            // Stub DLL so Epic DLL-directory scanning finds the game
            string dllPath = Path.Combine(gameDir, "EOSSDK-Win64-Shipping.dll");
            WriteDllStub(dllPath);

            CreatedDirectories.Add(gameDir);

            EpicLibrary.TestManifests.Add(new Manifest
            {
                DisplayName = gameName,
                CatalogNamespace = catalogNamespace,
                InstallLocation = gameDir
            });

            ProgramData.Log($"[TestGame] Epic: {gameName} ({catalogNamespace}) at {gameDir}");
            SetStatus($"✓ Epic test game '{gameName}' generated. Press Rescan.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
    }

    private void GenerateUbisoft()
    {
        string gameName = gameNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(gameName))
        {
            SetStatus("Game Name cannot be empty. Search for a game first.");
            return;
        }

        // Use the selected search result ID if available, otherwise derive a stub
        string gameId;
        int selIdx = ubisoftResultsListBox.SelectedIndex;
        if (selIdx >= 0 && selIdx < ubisoftSearchResults.Count)
        {
            gameId = ubisoftSearchResults[selIdx].id;
            gameName = ubisoftSearchResults[selIdx].name;
        }
        else
        {
            gameId = $"test_{SanitizeName(gameName).ToLowerInvariant()}";
        }

        if (UbisoftLibrary.TestGames.Any(g => g.gameId == gameId))
        {
            SetStatus("An Ubisoft test game with that ID already exists.");
            return;
        }

        try
        {
            string gameDir = Path.Combine(TestGamesRoot, $"ubisoft_{SanitizeName(gameId)}_{SanitizeName(gameName)}");
            Directory.CreateDirectory(gameDir);

            // Write stub DLLs for both Uplay R1 and R2 unlocker detection
            WriteDllStub(Path.Combine(gameDir, "uplay_r1_loader.dll"));
            WriteDllStub(Path.Combine(gameDir, "uplay_r1_loader64.dll"));
            WriteDllStub(Path.Combine(gameDir, "upc_r2_loader.dll"));
            WriteDllStub(Path.Combine(gameDir, "upc_r2_loader64.dll"));

            CreatedDirectories.Add(gameDir);
            UbisoftLibrary.TestGames.Add((gameId, gameName, gameDir));
            ProgramData.Log($"[TestGame] Ubisoft: {gameName} ({gameId}) at {gameDir}");
            SetStatus($"✓ Ubisoft test game '{gameName}' ({gameId}) generated. Press Rescan.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
    }

    // ── Clear / Close ────────────────────────────────────────────────────────

    private void OnClearAll(object sender, EventArgs e)
    {
        SteamLibrary.TestGames.Clear();
        EpicLibrary.TestManifests.Clear();
        UbisoftLibrary.TestGames.Clear();
        foreach (string dir in CreatedDirectories)
            try { Directory.Delete(dir, true); } catch (Exception ex) { ProgramData.LogWarning($"[TestGame] Cleanup deletion failed for {dir}: {ex.Message}"); }
        CreatedDirectories.Clear();
        if (Directory.Exists(TestGamesRoot))
            try { Directory.Delete(TestGamesRoot, true); } catch (Exception ex) { ProgramData.LogWarning($"[TestGame] Cleanup failed to delete TestGames root: {ex.Message}"); }
        // Remove any installed.json records for test games (e.g. if an unlocker was installed to a test game)
        List<InstalledGameRecord> installedRecords = ProgramData.ReadInstalledGames();
        int removed = installedRecords.RemoveAll(r => r.RootDirectory?.StartsWith(TestGamesRoot, StringComparison.OrdinalIgnoreCase) == true);
        if (removed > 0)
        {
            ProgramData.WriteInstalledGames(installedRecords);
            ProgramData.Log($"[TestGame] Removed {removed} stale installed-game record(s) from test games.");
        }
        // Remove any Selection entries under the TestGames root so the main game list updates immediately
        foreach (Selection selection in Selection.All.Keys.ToHashSet().Where(s =>
            s.RootDirectory?.StartsWith(TestGamesRoot, StringComparison.OrdinalIgnoreCase) == true))
            selection.Remove();
        epicSearchResults.Clear();
        epicResultsListBox.Items.Clear();
        epicResultsListBox.Visible = false;
        ubisoftSearchResults.Clear();
        ubisoftResultsListBox.Items.Clear();
        ubisoftResultsListBox.Visible = false;
        SetStatus("All test games cleared. Press Rescan in the main window.");
    }

    private void OnClose(object sender, EventArgs e) => Close();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = message.StartsWith('✓') ? System.Drawing.Color.Green
            : System.Drawing.Color.FromArgb(212, 212, 212);
    }

    private static string SanitizeName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static void WriteDllStub(string path)
    {
        byte[] mzStub =
        [
            0x4D, 0x5A,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];
        File.WriteAllBytes(path, mzStub);
    }
}
