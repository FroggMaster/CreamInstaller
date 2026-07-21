using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal sealed partial class SettingsForm : CustomForm
{
    private bool wasDarkModeEnabled;
    private bool wasSortByName;

    private SettingsForm()
    {
        InitializeComponent();
        Text = "Settings";
    }

    internal static void Show(Form owner)
    {
        using SettingsForm form = new();
        form.Owner = owner;
        form.StartPosition = FormStartPosition.CenterParent;
        form.LoadSettings();
        form.ShowDialog(owner);
    }

    private void LoadSettings()
    {
        darkModeCheckBox.Checked = Program.DarkModeEnabled;
        blockedGamesCheckBox.Checked = Program.BlockProtectedGames;
        sortByNameCheckBox.Checked = Program.SortByName;
        wasDarkModeEnabled = Program.DarkModeEnabled;
        wasSortByName = Program.SortByName;
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
        Program.DarkModeEnabled = darkModeCheckBox.Checked;
        Program.BlockProtectedGames = blockedGamesCheckBox.Checked;
        Program.SortByName = sortByNameCheckBox.Checked;

        ProgramData.SaveSettings(Program.AppSettings);

        if (wasDarkModeEnabled != darkModeCheckBox.Checked)
        {
            ThemeManager.ApplyToAllOpenForms();
            if (DebugForm.IsOpen)
                ThemeManager.Apply(DebugForm.Current);
        }

        if (wasSortByName != sortByNameCheckBox.Checked)
            MainForm.Current?.UpdateSortOrder(sortByNameCheckBox.Checked);

        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void OnClearCacheClick(object sender, EventArgs e)
    {
        using DialogForm confirm = new(this);
        if (confirm.Show(SystemIcons.Warning,
                "This will delete all cached game data, installed game records, and proxy configurations.\n\nYour settings will be preserved. A fresh scan will be required on the next launch.",
                acceptButtonText: "Clear Cache", cancelButtonText: "Cancel", customFormText: "Clear Cached Data") != DialogResult.OK)
            return;

        string cachePath = ProgramData.DirectoryPath + @"\Cache";
        if (Directory.Exists(cachePath))
        {
            foreach (string file in Directory.GetFiles(cachePath))
            {
                if (Path.GetFileName(file).Equals("settings.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                try { File.Delete(file); } catch { /* skip locked files */ }
            }
            foreach (string dir in Directory.GetDirectories(cachePath))
            {
                try { Directory.Delete(dir, true); } catch { /* skip locked dirs */ }
            }
        }

        ProgramData.SaveSettings(Program.AppSettings);
    }

    private async void OnReconfigureSteamCMDClick(object sender, EventArgs e)
    {
        using DialogForm confirm = new(this);
        if (confirm.Show(SystemIcons.Warning,
                "This will delete and re-download the SteamCMD installation.\n\nCached app data will be preserved.",
                acceptButtonText: "Reconfigure", cancelButtonText: "Cancel", customFormText: "Reconfigure SteamCMD") != DialogResult.OK)
            return;

        reconfigureSteamCMDButton.Enabled = false;
        reconfigureSteamCMDButton.Text = "Reconfiguring...";

        string steamCmdPath = ProgramData.DirectoryPath + @"\SteamCMD";
        await Task.Run(() =>
        {
            try { Directory.Delete(steamCmdPath, true); } catch { /* directory may not exist */ }
        });

        Progress<int> progress = new();
        await SteamCMD.Setup(progress);

        Color originalColor = reconfigureSteamCMDButton.ForeColor;
        reconfigureSteamCMDButton.Text = "Complete";
        reconfigureSteamCMDButton.ForeColor = Color.Green;
        await Task.Delay(2000);
        reconfigureSteamCMDButton.Text = "Reconfigure SteamCMD";
        reconfigureSteamCMDButton.ForeColor = originalColor;
        reconfigureSteamCMDButton.Enabled = true;
    }
}
