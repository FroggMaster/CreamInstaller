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
        darkModeCheckBox = new CheckBox();
        blockedGamesCheckBox = new CheckBox();
        sortByNameCheckBox = new CheckBox();
        saveButton = new Button();
        cancelButton = new Button();
        settingsToolTip = new ToolTip();
        SuspendLayout();
        // 
        // settingsToolTip
        // 
        settingsToolTip.AutoPopDelay = 8000;
        settingsToolTip.InitialDelay = 500;
        settingsToolTip.ReshowDelay = 100;
        // 
        // darkModeCheckBox
        // 
        darkModeCheckBox.AutoSize = false;
        darkModeCheckBox.Location = new Point(36, 20);
        darkModeCheckBox.Name = "darkModeCheckBox";
        darkModeCheckBox.Size = new Size(160, 22);
        darkModeCheckBox.TabIndex = 0;
        darkModeCheckBox.Text = "Enable Dark Mode";
        darkModeCheckBox.FlatStyle = FlatStyle.System;
        darkModeCheckBox.UseVisualStyleBackColor = true;
        settingsToolTip.SetToolTip(darkModeCheckBox, "Switches the application between light and dark color themes. Changes apply immediately.");
        // 
        // blockedGamesCheckBox
        // 
        blockedGamesCheckBox.AutoSize = false;
        blockedGamesCheckBox.Location = new Point(36, 50);
        blockedGamesCheckBox.Name = "blockedGamesCheckBox";
        blockedGamesCheckBox.Size = new Size(190, 22);
        blockedGamesCheckBox.TabIndex = 1;
        blockedGamesCheckBox.Text = "Block Protected Games";
        blockedGamesCheckBox.FlatStyle = FlatStyle.System;
        blockedGamesCheckBox.UseVisualStyleBackColor = true;
        settingsToolTip.SetToolTip(blockedGamesCheckBox, "Prevents the program from displaying or modifying games protected by anti-cheat software (e.g. Easy Anti-Cheat, BattlEye). Disable at your own risk.");
        // 
        // sortByNameCheckBox
        // 
        sortByNameCheckBox.AutoSize = false;
        sortByNameCheckBox.Location = new Point(36, 80);
        sortByNameCheckBox.Name = "sortByNameCheckBox";
        sortByNameCheckBox.Size = new Size(200, 22);
        sortByNameCheckBox.TabIndex = 2;
        sortByNameCheckBox.Text = "Sort game list by name";
        sortByNameCheckBox.FlatStyle = FlatStyle.System;
        sortByNameCheckBox.UseVisualStyleBackColor = true;
        settingsToolTip.SetToolTip(sortByNameCheckBox, "When enabled, games in the main list are sorted alphabetically by name. When disabled, games appear in their default platform order.");
        // 
        // saveButton
        // 
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.AutoSize = true;
        saveButton.Location = new Point(220, 115);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(75, 25);
        saveButton.TabIndex = 3;
        saveButton.Text = "Save";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSaveClick;
        // 
        // cancelButton
        // 
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.AutoSize = true;
        cancelButton.Location = new Point(301, 115);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 25);
        cancelButton.TabIndex = 4;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += OnCancelClick;
        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(400, 155);
        Controls.Add(cancelButton);
        Controls.Add(saveButton);
        Controls.Add(sortByNameCheckBox);
        Controls.Add(blockedGamesCheckBox);
        Controls.Add(darkModeCheckBox);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        ResumeLayout(false);
        PerformLayout();
    }

    private CheckBox darkModeCheckBox;
    private CheckBox blockedGamesCheckBox;
    private CheckBox sortByNameCheckBox;
    private Button saveButton;
    private Button cancelButton;
    private ToolTip settingsToolTip;
}
