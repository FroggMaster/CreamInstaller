using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms;

partial class TestGameForm
{
    private IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
            components.Dispose();
        base.Dispose(disposing);
    }

    // All coordinates are based on ClientSize = 560 x 330
    // Left margin = 12, right edge of usable area = 548 (560 - 12)
    // Usable width = 536

    private void InitializeComponent()
    {
        platformGroupBox = new GroupBox();
        steamRadioButton = new RadioButton();
        epicRadioButton = new RadioButton();
        appIdLabel = new Label();
        appIdTextBox = new TextBox();
        gameNameLabel = new Label();
        gameNameTextBox = new TextBox();
        epicSearchButton = new Button();
        epicResultsListBox = new ListBox();
        dlcGroupBox = new GroupBox();
        dlcListBox = new ListBox();
        dlcIdLabel = new Label();
        dlcIdTextBox = new TextBox();
        dlcNameLabel = new Label();
        dlcNameTextBox = new TextBox();
        addDlcButton = new Button();
        removeDlcButton = new Button();
        generateButton = new Button();
        clearButton = new Button();
        closeButton = new Button();
        statusLabel = new Label();
        platformGroupBox.SuspendLayout();
        dlcGroupBox.SuspendLayout();
        SuspendLayout();

        // ── Platform group box ── y=8, h=44
        platformGroupBox.Location = new System.Drawing.Point(12, 8);
        platformGroupBox.Size = new System.Drawing.Size(536, 44);
        platformGroupBox.TabStop = false;
        platformGroupBox.Text = "Platform";
        platformGroupBox.Controls.Add(steamRadioButton);
        platformGroupBox.Controls.Add(epicRadioButton);

        steamRadioButton.AutoSize = true;
        steamRadioButton.Checked = true;
        steamRadioButton.Location = new System.Drawing.Point(10, 17);
        steamRadioButton.TabStop = true;
        steamRadioButton.Text = "Steam";
        steamRadioButton.CheckedChanged += OnPlatformChanged;

        epicRadioButton.AutoSize = true;
        epicRadioButton.Location = new System.Drawing.Point(80, 17);
        epicRadioButton.Text = "Epic";
        epicRadioButton.CheckedChanged += OnPlatformChanged;

        // ── App ID row ── y=62
        appIdLabel.AutoSize = true;
        appIdLabel.Location = new System.Drawing.Point(12, 66);
        appIdLabel.Text = "App ID:";

        appIdTextBox.Location = new System.Drawing.Point(105, 63);
        appIdTextBox.Size = new System.Drawing.Size(443, 23);
        appIdTextBox.PlaceholderText = "e.g. 480";

        // ── Game Name row ── y=96
        gameNameLabel.AutoSize = true;
        gameNameLabel.Location = new System.Drawing.Point(12, 100);
        gameNameLabel.Text = "Game Name:";

        // Steam: full width; Epic: leaves room for Search button (75px + 4px gap)
        gameNameTextBox.Location = new System.Drawing.Point(105, 97);
        gameNameTextBox.Size = new System.Drawing.Size(443, 23);

        epicSearchButton.Location = new System.Drawing.Point(468, 97);
        epicSearchButton.Size = new System.Drawing.Size(80, 23);
        epicSearchButton.Text = "Search";
        epicSearchButton.Visible = false;
        epicSearchButton.Click += OnEpicSearch;

        // ── Epic results list ── y=130, same slot as DLC group
        epicResultsListBox.Location = new System.Drawing.Point(12, 130);
        epicResultsListBox.Size = new System.Drawing.Size(536, 80);
        epicResultsListBox.Visible = false;
        epicResultsListBox.SelectedIndexChanged += OnEpicResultSelected;

        // ── DLC group box ── y=130, h=130
        dlcGroupBox.Location = new System.Drawing.Point(12, 130);
        dlcGroupBox.Size = new System.Drawing.Size(536, 130);
        dlcGroupBox.TabStop = false;
        dlcGroupBox.Text = "DLC Entries (Steam only)";
        dlcGroupBox.Controls.Add(dlcListBox);
        dlcGroupBox.Controls.Add(dlcIdLabel);
        dlcGroupBox.Controls.Add(dlcIdTextBox);
        dlcGroupBox.Controls.Add(dlcNameLabel);
        dlcGroupBox.Controls.Add(dlcNameTextBox);
        dlcGroupBox.Controls.Add(addDlcButton);
        dlcGroupBox.Controls.Add(removeDlcButton);

        dlcListBox.Location = new System.Drawing.Point(6, 20);
        dlcListBox.Size = new System.Drawing.Size(524, 60);

        // DLC row inside group box — left-to-right:
        // "DLC ID:" label + 70px box + "DLC Name:" label + 160px box + "Add"(60) + "Remove"(70)
        // Total: ~48 + 70 + ~72 + 160 + 60 + 70 = 480  (fits in 524)
        dlcIdLabel.AutoSize = true;
        dlcIdLabel.Location = new System.Drawing.Point(6, 92);
        dlcIdLabel.Text = "DLC ID:";

        dlcIdTextBox.Location = new System.Drawing.Point(62, 89);
        dlcIdTextBox.Size = new System.Drawing.Size(70, 23);
        dlcIdTextBox.PlaceholderText = "e.g. 12345";

        dlcNameLabel.AutoSize = true;
        dlcNameLabel.Location = new System.Drawing.Point(140, 92);
        dlcNameLabel.Text = "DLC Name:";

        dlcNameTextBox.Location = new System.Drawing.Point(216, 89);
        dlcNameTextBox.Size = new System.Drawing.Size(184, 23);
        dlcNameTextBox.PlaceholderText = "e.g. Test DLC";

        addDlcButton.Location = new System.Drawing.Point(406, 89);
        addDlcButton.Size = new System.Drawing.Size(52, 23);
        addDlcButton.Text = "Add";
        addDlcButton.Click += OnAddDlc;

        removeDlcButton.Location = new System.Drawing.Point(462, 89);
        removeDlcButton.Size = new System.Drawing.Size(62, 23);
        removeDlcButton.Text = "Remove";
        removeDlcButton.Click += OnRemoveDlc;

        // ── Action buttons ── y=270
        generateButton.Location = new System.Drawing.Point(12, 270);
        generateButton.Size = new System.Drawing.Size(150, 26);
        generateButton.Text = "Generate Test Game";
        generateButton.Click += OnGenerate;

        clearButton.Location = new System.Drawing.Point(168, 270);
        clearButton.Size = new System.Drawing.Size(110, 26);
        clearButton.Text = "Clear All Tests";
        clearButton.Click += OnClearAll;

        closeButton.Location = new System.Drawing.Point(284, 270);
        closeButton.Size = new System.Drawing.Size(70, 26);
        closeButton.Text = "Close";
        closeButton.Click += OnClose;

        // ── Status label ── y=302
        statusLabel.Location = new System.Drawing.Point(12, 302);
        statusLabel.Size = new System.Drawing.Size(536, 20);
        statusLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F);

        // ── Form ──
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(560, 328);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Test Game Generator";
        Controls.Add(platformGroupBox);
        Controls.Add(appIdLabel);
        Controls.Add(appIdTextBox);
        Controls.Add(gameNameLabel);
        Controls.Add(gameNameTextBox);
        Controls.Add(epicSearchButton);
        Controls.Add(epicResultsListBox);
        Controls.Add(dlcGroupBox);
        Controls.Add(generateButton);
        Controls.Add(clearButton);
        Controls.Add(closeButton);
        Controls.Add(statusLabel);

        platformGroupBox.ResumeLayout(false);
        platformGroupBox.PerformLayout();
        dlcGroupBox.ResumeLayout(false);
        dlcGroupBox.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private GroupBox platformGroupBox;
    private RadioButton steamRadioButton;
    private RadioButton epicRadioButton;
    private Label appIdLabel;
    private TextBox appIdTextBox;
    private Label gameNameLabel;
    private TextBox gameNameTextBox;
    private Button epicSearchButton;
    private ListBox epicResultsListBox;
    private GroupBox dlcGroupBox;
    private ListBox dlcListBox;
    private Label dlcIdLabel;
    private TextBox dlcIdTextBox;
    private Label dlcNameLabel;
    private TextBox dlcNameTextBox;
    private Button addDlcButton;
    private Button removeDlcButton;
    private Button generateButton;
    private Button clearButton;
    private Button closeButton;
    private Label statusLabel;
}
