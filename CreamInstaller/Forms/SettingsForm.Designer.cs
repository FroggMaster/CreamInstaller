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
        SuspendLayout();
        // 
        // darkModeCheckBox
        // 
        darkModeCheckBox.AutoSize = true;
        darkModeCheckBox.Location = new Point(20, 20);
        darkModeCheckBox.Name = "darkModeCheckBox";
        darkModeCheckBox.Size = new Size(120, 19);
        darkModeCheckBox.TabIndex = 0;
        darkModeCheckBox.Text = "Enable Dark Mode";
        darkModeCheckBox.UseVisualStyleBackColor = true;
        // 
        // blockedGamesCheckBox
        // 
        blockedGamesCheckBox.AutoSize = true;
        blockedGamesCheckBox.Location = new Point(20, 50);
        blockedGamesCheckBox.Name = "blockedGamesCheckBox";
        blockedGamesCheckBox.Size = new Size(148, 19);
        blockedGamesCheckBox.TabIndex = 1;
        blockedGamesCheckBox.Text = "Block Protected Games";
        blockedGamesCheckBox.UseVisualStyleBackColor = true;
        // 
        // sortByNameCheckBox
        // 
        sortByNameCheckBox.AutoSize = true;
        sortByNameCheckBox.Location = new Point(20, 80);
        sortByNameCheckBox.Name = "sortByNameCheckBox";
        sortByNameCheckBox.Size = new Size(160, 19);
        sortByNameCheckBox.TabIndex = 2;
        sortByNameCheckBox.Text = "Sort game list by name";
        sortByNameCheckBox.UseVisualStyleBackColor = true;
        // 
        // saveButton
        // 
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.AutoSize = true;
        saveButton.Location = new Point(198, 115);
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
        cancelButton.Location = new Point(279, 115);
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
        ClientSize = new Size(370, 155);
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
}
