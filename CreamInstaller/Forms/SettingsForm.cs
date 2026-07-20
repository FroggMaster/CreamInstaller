using System;
using System.Drawing;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal sealed partial class SettingsForm : CustomForm
{
    private bool wasDarkModeEnabled;

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
    }

    private void OnSaveClick(object sender, EventArgs e)
    {
        Program.DarkModeEnabled = darkModeCheckBox.Checked;
        Program.BlockProtectedGames = blockedGamesCheckBox.Checked;
        Program.SortByName = sortByNameCheckBox.Checked;

        ProgramData.SaveSettings(Program.AppSettings);

        if (wasDarkModeEnabled != darkModeCheckBox.Checked)
            ThemeManager.ApplyToAllOpenForms();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancelClick(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
