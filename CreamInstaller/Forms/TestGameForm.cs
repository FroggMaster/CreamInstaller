using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Platforms.Epic;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal sealed partial class TestGameForm : CustomForm
{
    private static readonly string TestGamesRoot =
        Path.Combine(ProgramData.DirectoryPath, "TestGames");

    private static readonly List<string> CreatedDirectories = [];

    // Steam DLC entries per-form: (dlcId, dlcName)
    private readonly List<(string id, string name)> dlcEntries = [];

    // Cached Epic search results from the last search: (namespace, name)
    private readonly List<(string ns, string name)> epicSearchResults = [];

    private bool IsEpicMode => epicRadioButton.Checked;

    internal TestGameForm(IWin32Window owner) : base(owner)
    {
        InitializeComponent();
        appIdTextBox.Leave += OnAppIdLeave;
        RefreshDlcList();
        UpdatePlatformMode();
    }

    private void UpdatePlatformMode()
    {
        bool epic = IsEpicMode;

        // App ID row: Steam only
        appIdLabel.Visible = !epic;
        appIdTextBox.Visible = !epic;

        // Search button: Epic only — shrink the game name box to make room
        epicSearchButton.Visible = epic;
        gameNameTextBox.Size = new System.Drawing.Size(epic ? 354 : 443, 23);

        // Placeholder text — call RefreshCueBanner to flush the Win32 cue so only one text shows
        gameNameTextBox.PlaceholderText = epic ? "Enter game name and click Search" : "e.g. Spacewar";
        NativeMethods.RefreshCueBanner(gameNameTextBox);

        // DLC group and Epic results share the same vertical slot
        dlcGroupBox.Visible = !epic;
        epicResultsListBox.Visible = false; // hidden until search runs

        if (!epic)
            epicSearchResults.Clear();

        SetStatus(epic
            ? "Enter a game name and click Search to find it on the Epic store."
            : "Enter the App ID, then tab out to auto-detect the game name.");
    }

    private void OnPlatformChanged(object sender, EventArgs e) => UpdatePlatformMode();

    // ── Steam: auto-detect name from AppID ──────────────────────────────────

    private async void OnAppIdLeave(object sender, EventArgs e)
    {
        if (IsEpicMode)
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
            catch { /* fall through to SteamCMD */ }

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
            SetStatus("Could not auto-detect name — enter it manually.");
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

    // ── DLC (Steam) ──────────────────────────────────────────────────────────

    private void OnAddDlc(object sender, EventArgs e)
    {
        string dlcId = dlcIdTextBox.Text.Trim();
        string dlcName = dlcNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(dlcId) || !int.TryParse(dlcId, out _))
        {
            SetStatus("DLC ID must be a valid integer.");
            return;
        }

        if (string.IsNullOrWhiteSpace(dlcName))
        {
            SetStatus("DLC Name cannot be empty.");
            return;
        }

        if (dlcEntries.Any(d => d.id == dlcId))
        {
            SetStatus($"DLC ID {dlcId} is already in the list.");
            return;
        }

        dlcEntries.Add((dlcId, dlcName));
        RefreshDlcList();
        dlcIdTextBox.Clear();
        dlcNameTextBox.Clear();
        SetStatus($"Added DLC: {dlcId} = {dlcName}");
    }

    private void OnRemoveDlc(object sender, EventArgs e)
    {
        if (dlcListBox.SelectedIndex < 0)
            return;
        dlcEntries.RemoveAt(dlcListBox.SelectedIndex);
        RefreshDlcList();
        SetStatus("Removed selected DLC entry.");
    }

    private void OnDlcListBoxSelectionChanged(object sender, EventArgs e) { }

    private void RefreshDlcList()
    {
        dlcListBox.Items.Clear();
        foreach ((string id, string name) in dlcEntries)
            dlcListBox.Items.Add($"{id} = {name}");
    }

    // ── Generate ────────────────────────────────────────────────────────────

    private void OnGenerate(object sender, EventArgs e)
    {
        if (IsEpicMode)
            GenerateEpic();
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
            WriteSteamApiStub(dllPath);

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
            WriteSteamApiStub(dllPath);

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

    // ── Clear / Close ────────────────────────────────────────────────────────

    private void OnClearAll(object sender, EventArgs e)
    {
        SteamLibrary.TestGames.Clear();
        EpicLibrary.TestManifests.Clear();
        foreach (string dir in CreatedDirectories)
            try { Directory.Delete(dir, true); } catch { /* best-effort */ }
        CreatedDirectories.Clear();
        dlcEntries.Clear();
        RefreshDlcList();
        epicSearchResults.Clear();
        epicResultsListBox.Items.Clear();
        epicResultsListBox.Visible = false;
        SetStatus("All test games cleared. Press Rescan in the main window.");
    }

    private void OnClose(object sender, EventArgs e) => Close();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = message.StartsWith("✓", StringComparison.Ordinal)
            ? System.Drawing.Color.Green
            : System.Drawing.Color.FromArgb(212, 212, 212);
    }

    private static string SanitizeName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static void WriteSteamApiStub(string path)
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
