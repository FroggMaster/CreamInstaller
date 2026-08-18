using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

partial class SettingsForm
{
    private IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SettingsToolTip = new ToolTip();
        appearanceGroup = new GroupBox();
        darkModeCheckBox = new CheckBox();
        gameManagementGroup = new GroupBox();
        blockedGamesCheckBox = new CheckBox();
        sortByNameCheckBox = new CheckBox();
        smokeApiGroup = new GroupBox();
        defaultAppStatusLabel = new Label();
        defaultAppStatusComboBox = new ComboBox();
        maintenanceGroup = new GroupBox();
        clearCacheButton = new Button();
        reconfigureSteamCMDButton = new Button();
        saveButton = new Button();
        cancelButton = new Button();
        openLogDirButton = new Button();
        appearanceGroup.SuspendLayout();
        gameManagementGroup.SuspendLayout();
        smokeApiGroup.SuspendLayout();
        maintenanceGroup.SuspendLayout();
        SuspendLayout();
        // 
        // settingsToolTip
        // 
        SettingsToolTip.AutoPopDelay = 8000;
        SettingsToolTip.InitialDelay = 500;
        SettingsToolTip.ReshowDelay = 100;
        // 
        // appearanceGroup
        // 
        appearanceGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        appearanceGroup.Controls.Add(darkModeCheckBox);
        appearanceGroup.Location = new Point(12, 12);
        appearanceGroup.Name = "appearanceGroup";
        appearanceGroup.Size = new Size(376, 50);
        appearanceGroup.TabIndex = 0;
        appearanceGroup.TabStop = false;
        appearanceGroup.Text = "Appearance";
        // 
        // darkModeCheckBox
        // 
        darkModeCheckBox.AutoSize = false;
        darkModeCheckBox.FlatStyle = FlatStyle.System;
        darkModeCheckBox.Location = new Point(12, 20);
        darkModeCheckBox.Name = "darkModeCheckBox";
        darkModeCheckBox.Size = new Size(160, 22);
        darkModeCheckBox.TabIndex = 0;
        darkModeCheckBox.Text = "Enable dark theme";
        darkModeCheckBox.UseVisualStyleBackColor = true;
        SettingsToolTip.SetToolTip(darkModeCheckBox, "Switches the application between light and dark color themes.");
        // 
        // gameManagementGroup
        // 
        gameManagementGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        gameManagementGroup.Controls.Add(blockedGamesCheckBox);
        gameManagementGroup.Controls.Add(sortByNameCheckBox);
        gameManagementGroup.Location = new Point(12, 72);
        gameManagementGroup.Name = "gameManagementGroup";
        gameManagementGroup.Size = new Size(376, 76);
        gameManagementGroup.TabIndex = 1;
        gameManagementGroup.TabStop = false;
        gameManagementGroup.Text = "Game Management";
        // 
        // blockedGamesCheckBox
        // 
        blockedGamesCheckBox.AutoSize = false;
        blockedGamesCheckBox.FlatStyle = FlatStyle.System;
        blockedGamesCheckBox.Location = new Point(12, 22);
        blockedGamesCheckBox.Name = "blockedGamesCheckBox";
        blockedGamesCheckBox.Size = new Size(260, 22);
        blockedGamesCheckBox.TabIndex = 0;
        blockedGamesCheckBox.Text = "Block games with known anti-cheat";
        blockedGamesCheckBox.UseVisualStyleBackColor = true;
        SettingsToolTip.SetToolTip(blockedGamesCheckBox, "Prevents the program from displaying or modifying games protected by anti-cheat software (e.g. Easy Anti-Cheat, BattlEye). Disable at your own risk.");
        // 
        // sortByNameCheckBox
        // 
        sortByNameCheckBox.AutoSize = false;
        sortByNameCheckBox.FlatStyle = FlatStyle.System;
        sortByNameCheckBox.Location = new Point(12, 48);
        sortByNameCheckBox.Name = "sortByNameCheckBox";
        sortByNameCheckBox.Size = new Size(200, 22);
        sortByNameCheckBox.TabIndex = 1;
        sortByNameCheckBox.Text = "Sort game list by name";
        sortByNameCheckBox.UseVisualStyleBackColor = true;
        SettingsToolTip.SetToolTip(sortByNameCheckBox, "When enabled, games in the main list are sorted alphabetically by name. When disabled, games appear in their default platform order.");
        // 
        // smokeApiGroup
        // 
        smokeApiGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        smokeApiGroup.Controls.Add(defaultAppStatusLabel);
        smokeApiGroup.Controls.Add(defaultAppStatusComboBox);
        smokeApiGroup.Location = new Point(12, 158);
        smokeApiGroup.Name = "smokeApiGroup";
        smokeApiGroup.Size = new Size(376, 55);
        smokeApiGroup.TabIndex = 2;
        smokeApiGroup.TabStop = false;
        smokeApiGroup.Text = "SmokeAPI";
        // 
        // defaultAppStatusLabel
        // 
        defaultAppStatusLabel.AutoSize = true;
        defaultAppStatusLabel.Location = new Point(12, 24);
        defaultAppStatusLabel.Name = "defaultAppStatusLabel";
        defaultAppStatusLabel.Size = new Size(160, 15);
        defaultAppStatusLabel.TabIndex = 0;
        defaultAppStatusLabel.Text = "Default app status for new DLC";
        // 
        // defaultAppStatusComboBox
        // 
        defaultAppStatusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        defaultAppStatusComboBox.Items.AddRange(new object[] {
            "unlocked",
            "locked",
            "original"});
        defaultAppStatusComboBox.Location = new Point(268, 21);
        defaultAppStatusComboBox.Name = "defaultAppStatusComboBox";
        defaultAppStatusComboBox.Size = new Size(95, 23);
        defaultAppStatusComboBox.TabIndex = 1;
        SettingsToolTip.SetToolTip(defaultAppStatusComboBox, "Sets the default_app_status in SmokeAPI.config.json. \"unlocked\" enables all DLC by default, \"locked\" disables them, \"original\" leaves them as-is.");
        // 
        // maintenanceGroup
        // 
        maintenanceGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        maintenanceGroup.Controls.Add(clearCacheButton);
        maintenanceGroup.Controls.Add(reconfigureSteamCMDButton);
        maintenanceGroup.Controls.Add(openLogDirButton);
        maintenanceGroup.Location = new Point(12, 223);
        maintenanceGroup.Name = "maintenanceGroup";
        maintenanceGroup.Size = new Size(376, 85);
        maintenanceGroup.TabIndex = 3;
        maintenanceGroup.TabStop = false;
        maintenanceGroup.Text = "Maintenance";
        // 
        // clearCacheButton
        // 
        clearCacheButton.AutoSize = true;
        clearCacheButton.Location = new Point(12, 20);
        clearCacheButton.Name = "clearCacheButton";
        clearCacheButton.Size = new Size(175, 25);
        clearCacheButton.TabIndex = 0;
        clearCacheButton.Text = "Clear Cached Data";
        clearCacheButton.UseVisualStyleBackColor = true;
        clearCacheButton.Click += OnClearCacheClick;
        SettingsToolTip.SetToolTip(clearCacheButton, "Deletes all cached game data, forcing a fresh scan on the next launch. Settings are preserved.");
        // 
        // reconfigureSteamCMDButton
        // 
        reconfigureSteamCMDButton.AutoSize = true;
        reconfigureSteamCMDButton.Location = new Point(195, 20);
        reconfigureSteamCMDButton.Name = "reconfigureSteamCMDButton";
        reconfigureSteamCMDButton.Size = new Size(175, 25);
        reconfigureSteamCMDButton.TabIndex = 1;
        reconfigureSteamCMDButton.Text = "Reconfigure SteamCMD";
        reconfigureSteamCMDButton.UseVisualStyleBackColor = true;
        reconfigureSteamCMDButton.Click += OnReconfigureSteamCMDClick;
        SettingsToolTip.SetToolTip(reconfigureSteamCMDButton, "Removes the existing SteamCMD installation. It will be re-downloaded automatically on the next scan.");
        // 
        // openLogDirButton
        // 
        openLogDirButton.AutoSize = true;
        openLogDirButton.Location = new Point(12, 50);
        openLogDirButton.Name = "openLogDirButton";
        openLogDirButton.Size = new Size(175, 25);
        openLogDirButton.TabIndex = 2;
        openLogDirButton.Text = "Open Log Directory";
        openLogDirButton.UseVisualStyleBackColor = true;
        openLogDirButton.Click += OnOpenLogDirClick;
        SettingsToolTip.SetToolTip(openLogDirButton, "Opens the logs directory in File Explorer.");
        // 
        // saveButton
        // 
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.AutoSize = true;
        saveButton.Location = new Point(232, 320);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(75, 25);
        saveButton.TabIndex = 4;
        saveButton.Text = "Save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSaveClick;
        // 
        // cancelButton
        // 
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.AutoSize = true;
        cancelButton.Location = new Point(313, 320);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 25);
        cancelButton.TabIndex = 5;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnCancelClick;
        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(400, 355);
        Controls.Add(cancelButton);
        Controls.Add(saveButton);
        Controls.Add(maintenanceGroup);
        Controls.Add(smokeApiGroup);
        Controls.Add(gameManagementGroup);
        Controls.Add(appearanceGroup);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        appearanceGroup.ResumeLayout(false);
        gameManagementGroup.ResumeLayout(false);
        smokeApiGroup.ResumeLayout(false);
        smokeApiGroup.PerformLayout();
        maintenanceGroup.ResumeLayout(false);
        maintenanceGroup.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private GroupBox appearanceGroup;
    private GroupBox gameManagementGroup;
    private GroupBox smokeApiGroup;
    private GroupBox maintenanceGroup;
    private CheckBox darkModeCheckBox;
    private CheckBox blockedGamesCheckBox;
    private CheckBox sortByNameCheckBox;
    private Label defaultAppStatusLabel;
    private ComboBox defaultAppStatusComboBox;
    private Button clearCacheButton;
    private Button reconfigureSteamCMDButton;
    private Button saveButton;
    private Button cancelButton;
    private Button openLogDirButton;
    private ToolTip SettingsToolTip;
}
