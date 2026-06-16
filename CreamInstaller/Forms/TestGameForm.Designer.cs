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
        ubisoftRadioButton = new RadioButton();
        appIdLabel = new Label();
        appIdTextBox = new TextBox();
        gameNameLabel = new Label();
        gameNameTextBox = new TextBox();
        epicSearchButton = new Button();
        ubisoftSearchButton = new Button();
        epicResultsListBox = new ListBox();
        ubisoftResultsListBox = new ListBox();
        generateButton = new Button();
        clearButton = new Button();
        closeButton = new Button();
        statusLabel = new Label();
        platformGroupBox.SuspendLayout();
        SuspendLayout();

        // ── Platform group box ── y=8, h=44
        platformGroupBox.Location = new System.Drawing.Point(12, 8);
        platformGroupBox.Size = new System.Drawing.Size(536, 44);
        platformGroupBox.TabStop = false;
        platformGroupBox.Text = "Platform";
        platformGroupBox.Controls.Add(steamRadioButton);
        platformGroupBox.Controls.Add(epicRadioButton);
        platformGroupBox.Controls.Add(ubisoftRadioButton);

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

        ubisoftRadioButton.AutoSize = true;
        ubisoftRadioButton.Location = new System.Drawing.Point(140, 17);
        ubisoftRadioButton.Text = "Ubisoft";
        ubisoftRadioButton.CheckedChanged += OnPlatformChanged;

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

        // ── Ubisoft search button ── shares same position as Epic button (mutually exclusive)
        ubisoftSearchButton.Location = new System.Drawing.Point(468, 97);
        ubisoftSearchButton.Size = new System.Drawing.Size(80, 23);
        ubisoftSearchButton.Text = "Search";
        ubisoftSearchButton.Visible = false;
        ubisoftSearchButton.Click += OnUbisoftSearch;

        // ── Epic results list ── y=130, same slot as DLC group
        epicResultsListBox.Location = new System.Drawing.Point(12, 130);
        epicResultsListBox.Size = new System.Drawing.Size(536, 80);
        epicResultsListBox.Visible = false;
        epicResultsListBox.SelectedIndexChanged += OnEpicResultSelected;

        // ── Ubisoft results list ── shares same position as Epic results (mutually exclusive)
        ubisoftResultsListBox.Location = new System.Drawing.Point(12, 130);
        ubisoftResultsListBox.Size = new System.Drawing.Size(536, 80);
        ubisoftResultsListBox.Visible = false;
        ubisoftResultsListBox.SelectedIndexChanged += OnUbisoftResultSelected;

        // ── Action buttons ── y=220 (was y=270 with DLC section)
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
        Controls.Add(ubisoftSearchButton);
        Controls.Add(epicResultsListBox);
        Controls.Add(ubisoftResultsListBox);
        Controls.Add(generateButton);
        Controls.Add(clearButton);
        Controls.Add(closeButton);
        Controls.Add(statusLabel);

        platformGroupBox.ResumeLayout(false);
        platformGroupBox.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private GroupBox platformGroupBox;
    private RadioButton steamRadioButton;
    private RadioButton epicRadioButton;
    private RadioButton ubisoftRadioButton;
    private Button ubisoftSearchButton;
    private ListBox ubisoftResultsListBox;
    private Label appIdLabel;
    private TextBox appIdTextBox;
    private Label gameNameLabel;
    private TextBox gameNameTextBox;
    private Button epicSearchButton;
    private ListBox epicResultsListBox;
    private Button generateButton;
    private Button clearButton;
    private Button closeButton;
    private Label statusLabel;
}
