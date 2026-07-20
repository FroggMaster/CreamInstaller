using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

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
        saveButton = new Button();
        cancelButton = new Button();
        appearanceGroup.SuspendLayout();
        gameManagementGroup.SuspendLayout();
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
        darkModeCheckBox.Text = "Enable Dark Mode";
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
        blockedGamesCheckBox.Size = new Size(190, 22);
        blockedGamesCheckBox.TabIndex = 0;
        blockedGamesCheckBox.Text = "Block Protected Games";
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
        // saveButton
        // 
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.AutoSize = true;
        saveButton.Location = new Point(232, 160);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(75, 25);
        saveButton.TabIndex = 2;
        saveButton.Text = "Save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSaveClick;
        // 
        // cancelButton
        // 
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.AutoSize = true;
        cancelButton.Location = new Point(313, 160);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 25);
        cancelButton.TabIndex = 3;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnCancelClick;
        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(400, 197);
        Controls.Add(cancelButton);
        Controls.Add(saveButton);
        Controls.Add(gameManagementGroup);
        Controls.Add(appearanceGroup);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        appearanceGroup.ResumeLayout(false);
        gameManagementGroup.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private GroupBox appearanceGroup;
    private GroupBox gameManagementGroup;
    private CheckBox darkModeCheckBox;
    private CheckBox blockedGamesCheckBox;
    private CheckBox sortByNameCheckBox;
    private Button saveButton;
    private Button cancelButton;
    private ToolTip SettingsToolTip;
}
