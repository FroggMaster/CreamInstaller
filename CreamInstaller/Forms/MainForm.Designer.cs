using CreamInstaller.Components;
using CreamInstaller.Resources;
using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms
{
    partial class MainForm
    {
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components is not null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            installButton = new Button();
            programsGroupBox = new GroupBox();
            noneFoundLabel = new Label();
            useSmokeAPILayoutPanel = new FlowLayoutPanel();
            useSmokeApiToggle = new ToggleSwitch();
            useSmokeApiLabel = new Label();
            useSmokeAPIHelpButton = new Button();
            allCheckBoxLayoutPanel = new FlowLayoutPanel();
            allCheckBox = new CheckBox();
            progressBar = new ProgressBar();
            progressLabel = new Label();
            scanButton = new Button();
            uninstallAllButton = new Button();
            uninstallButton = new Button();
            progressLabelGames = new Label();
            progressLabelDLCs = new Label();
            saveFlowPanel = new FlowLayoutPanel();
            settingsButton = new Button();
            selectionTreeView = new CustomTreeView();
            topOptionsTable = new TableLayoutPanel();
            programsGroupBox.SuspendLayout();
            useSmokeAPILayoutPanel.SuspendLayout();
            allCheckBoxLayoutPanel.SuspendLayout();
            saveFlowPanel.SuspendLayout();
            SuspendLayout();
            // 
            // installButton
            // 
            installButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            installButton.AutoSize = true;
            installButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            installButton.Enabled = false;
            installButton.Location = new System.Drawing.Point(541, 382);
            installButton.Name = "installButton";
            installButton.Padding = new Padding(3, 0, 3, 0);
            installButton.Size = new System.Drawing.Size(127, 25);
            installButton.TabIndex = 10000;
            installButton.Text = "Generate and Install";
            installButton.UseVisualStyleBackColor = true;
            installButton.Click += OnInstall;
            // 
            // programsGroupBox
            // 
            programsGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            programsGroupBox.Controls.Add(selectionTreeView);
            programsGroupBox.Location = new System.Drawing.Point(12, 43);
            programsGroupBox.Name = "programsGroupBox";
            programsGroupBox.Size = new System.Drawing.Size(656, 252);
            programsGroupBox.TabIndex = 1000;
            programsGroupBox.TabStop = false;
            programsGroupBox.Text = "Programs && Games";
            programsGroupBox.Enter += programsGroupBox_Enter;
            // 
            // noneFoundLabel
            // 
            noneFoundLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            noneFoundLabel.Location = new System.Drawing.Point(12, 321);
            noneFoundLabel.Name = "noneFoundLabel";
            noneFoundLabel.Size = new System.Drawing.Size(656, 18);
            noneFoundLabel.TabIndex = 1003;
            noneFoundLabel.Text = "No applicable programs and/or games found.";
            noneFoundLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            noneFoundLabel.Visible = false;
            // 
            // useSmokeAPILayoutPanel
            // 
            useSmokeAPILayoutPanel.AutoSize = true;
            useSmokeAPILayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            useSmokeAPILayoutPanel.Controls.Add(useSmokeApiToggle);
            useSmokeAPILayoutPanel.Controls.Add(useSmokeApiLabel);
            useSmokeAPILayoutPanel.Controls.Add(useSmokeAPIHelpButton);
            useSmokeAPILayoutPanel.Margin = new Padding(0);
            useSmokeAPILayoutPanel.Name = "useSmokeAPILayoutPanel";
            useSmokeAPILayoutPanel.Size = new System.Drawing.Size(150, 22);
            useSmokeAPILayoutPanel.TabIndex = 1006;
            useSmokeAPILayoutPanel.WrapContents = false;
            // 
            // useSmokeApiToggle
            // 
            useSmokeApiToggle.Location = new System.Drawing.Point(0, 0);
            useSmokeApiToggle.Name = "useSmokeApiToggle";
            useSmokeApiToggle.Size = new System.Drawing.Size(44, 22);
            useSmokeApiToggle.TabIndex = 1;
            useSmokeApiToggle.CheckedChanged += OnUseSmokeApiToggleChanged;
            // 
            // useSmokeApiLabel
            // 
            useSmokeApiLabel.AutoSize = true;
            useSmokeApiLabel.Location = new System.Drawing.Point(47, 2);
            useSmokeApiLabel.Margin = new Padding(3, 2, 0, 0);
            useSmokeApiLabel.Name = "useSmokeApiLabel";
            useSmokeApiLabel.Size = new System.Drawing.Size(70, 15);
            useSmokeApiLabel.TabIndex = 3;
            useSmokeApiLabel.Text = "SmokeAPI";
            useSmokeApiLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // useSmokeAPIHelpButton
            // 
            useSmokeAPIHelpButton.Enabled = false;
            useSmokeAPIHelpButton.Font = new System.Drawing.Font("Segoe UI", 7F);
            useSmokeAPIHelpButton.Location = new System.Drawing.Point(120, 0);
            useSmokeAPIHelpButton.Margin = new Padding(2, 0, 1, 0);
            useSmokeAPIHelpButton.Name = "useSmokeAPIHelpButton";
            useSmokeAPIHelpButton.Size = new System.Drawing.Size(19, 19);
            useSmokeAPIHelpButton.TabIndex = 2;
            useSmokeAPIHelpButton.Text = "?";
            useSmokeAPIHelpButton.UseVisualStyleBackColor = true;
            useSmokeAPIHelpButton.Click += OnUseSmokeAPIHelpButtonClicked;
            // 
            // allCheckBoxLayoutPanel
            // 
            allCheckBoxLayoutPanel.AutoSize = true;
            allCheckBoxLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            allCheckBoxLayoutPanel.Controls.Add(allCheckBox);
            allCheckBoxLayoutPanel.Margin = new Padding(12, 0, 0, 0);
            allCheckBoxLayoutPanel.Name = "allCheckBoxLayoutPanel";
            allCheckBoxLayoutPanel.Size = new System.Drawing.Size(42, 19);
            allCheckBoxLayoutPanel.TabIndex = 1007;
            allCheckBoxLayoutPanel.WrapContents = false;
            // 
            // allCheckBox
            // 
            allCheckBox.AutoSize = true;
            allCheckBox.Checked = true;
            allCheckBox.CheckState = CheckState.Checked;
            allCheckBox.Enabled = false;
            allCheckBox.Location = new System.Drawing.Point(2, 0);
            allCheckBox.Margin = new Padding(2, 0, 0, 0);
            allCheckBox.Name = "allCheckBox";
            allCheckBox.Size = new System.Drawing.Size(40, 19);
            allCheckBox.TabIndex = 4;
            allCheckBox.Text = "All";
            allCheckBox.CheckedChanged += OnAllCheckBoxChanged;
            // 
            // selectionTreeView
            // 
            selectionTreeView.BackColor = System.Drawing.SystemColors.Control;
            selectionTreeView.BorderStyle = BorderStyle.None;
            selectionTreeView.CheckBoxes = true;
            selectionTreeView.Dock = DockStyle.Fill;
            selectionTreeView.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            selectionTreeView.Enabled = false;
            selectionTreeView.FullRowSelect = true;
            selectionTreeView.Location = new System.Drawing.Point(3, 19);
            selectionTreeView.Name = "selectionTreeView";
            selectionTreeView.Size = new System.Drawing.Size(604, 230);
            selectionTreeView.TabIndex = 1001;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new System.Drawing.Point(12, 352);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(656, 23);
            progressBar.TabIndex = 9;
            // 
            // progressLabel
            // 
            progressLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabel.Location = new System.Drawing.Point(12, 302);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new System.Drawing.Size(656, 23);
            progressLabel.TabIndex = 10;
            progressLabel.Text = "Gathering and caching programs . . .";
            progressLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            progressLabel.Visible = false;
            // 
            // scanButton
            // 
            scanButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            scanButton.AutoSize = true;
            scanButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            scanButton.Location = new System.Drawing.Point(450, 382);
            scanButton.Name = "scanButton";
            scanButton.Padding = new Padding(3, 0, 3, 0);
            scanButton.Size = new System.Drawing.Size(85, 25);
            scanButton.TabIndex = 10004;
            scanButton.Text = "Rescan";
            scanButton.UseVisualStyleBackColor = true;
            scanButton.Click += OnScan;
            // 
            // uninstallAllButton
            // 
            uninstallAllButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uninstallAllButton.AutoSize = true;
            uninstallAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            uninstallAllButton.Enabled = false;
            uninstallAllButton.Location = new System.Drawing.Point(12, 382);
            uninstallAllButton.Name = "uninstallAllButton";
            uninstallAllButton.Padding = new Padding(3, 0, 3, 0);
            uninstallAllButton.Size = new System.Drawing.Size(85, 25);
            uninstallAllButton.TabIndex = 10006;
            uninstallAllButton.Text = "Uninstall All";
            uninstallAllButton.UseVisualStyleBackColor = true;
            uninstallAllButton.Click += OnUninstallAll;
            // 
            // uninstallButton
            // 
            uninstallButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            uninstallButton.AutoSize = true;
            uninstallButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            uninstallButton.Enabled = false;
            uninstallButton.Location = new System.Drawing.Point(100, 382);
            uninstallButton.Name = "uninstallButton";
            uninstallButton.Padding = new Padding(3, 0, 3, 0);
            uninstallButton.Size = new System.Drawing.Size(174, 25);
            uninstallButton.TabIndex = 10005;
            uninstallButton.Text = "Uninstall Selected";
            uninstallButton.UseVisualStyleBackColor = true;
            uninstallButton.Click += OnUninstall;
            // 
            // progressLabelGames
            // 
            progressLabelGames.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            progressLabelGames.Location = new System.Drawing.Point(12, 328);
            progressLabelGames.Name = "progressLabelGames";
            progressLabelGames.Size = new System.Drawing.Size(375, 18);
            progressLabelGames.TabIndex = 10007;
            progressLabelGames.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressLabelDLCs
            // 
            progressLabelDLCs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            progressLabelDLCs.Location = new System.Drawing.Point(393, 328);
            progressLabelDLCs.Name = "progressLabelDLCs";
            progressLabelDLCs.Size = new System.Drawing.Size(329, 18);
            progressLabelDLCs.TabIndex = 10008;
            progressLabelDLCs.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // saveFlowPanel
            // 
            saveFlowPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            saveFlowPanel.AutoSize = true;
            saveFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            saveFlowPanel.Location = new System.Drawing.Point(380, 382);
            saveFlowPanel.Name = "saveFlowPanel";
            saveFlowPanel.Size = new System.Drawing.Size(141, 25);
            saveFlowPanel.TabIndex = 10009;
            saveFlowPanel.WrapContents = false;
            // 
            // settingsButton
            // 
            settingsButton.AutoSize = true;
            settingsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            settingsButton.Location = new System.Drawing.Point(0, 0);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new System.Drawing.Size(60, 25);
            settingsButton.TabIndex = 10010;
            settingsButton.Text = "Settings";
            settingsButton.UseVisualStyleBackColor = true;
            settingsButton.Click += OnSettingsButtonClick;
            // 
            // topOptionsTable
            // 
            topOptionsTable.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            topOptionsTable.AutoSize = true;
            topOptionsTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            topOptionsTable.ColumnCount = 4;
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.Location = new System.Drawing.Point(12, 12);
            topOptionsTable.Margin = new Padding(0);
            topOptionsTable.Name = "topOptionsTable";
            topOptionsTable.RowCount = 1;
            topOptionsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topOptionsTable.Size = new System.Drawing.Size(656, 25);
            topOptionsTable.TabIndex = 10009;
            topOptionsTable.Controls.Clear();
            topOptionsTable.Controls.Add(useSmokeAPILayoutPanel, 0, 0);
            topOptionsTable.Controls.Add(settingsButton, 2, 0);
            topOptionsTable.Controls.Add(allCheckBoxLayoutPanel, 3, 0);
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(680, 417);
            Controls.Add(topOptionsTable);
            Controls.Add(saveFlowPanel);
            Controls.Add(progressLabelDLCs);
            Controls.Add(progressLabelGames);
            Controls.Add(progressLabel);
            Controls.Add(progressBar);
            Controls.Add(noneFoundLabel);
            Controls.Add(programsGroupBox);
            Controls.Add(uninstallAllButton);
            Controls.Add(uninstallButton);
            Controls.Add(scanButton);
            Controls.Add(installButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            HelpButton = true;
            Icon = Properties.Resources.Icon;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.Manual;
            Text = "MainForm";
            Load += OnLoad;
            programsGroupBox.ResumeLayout(false);
            useSmokeAPILayoutPanel.ResumeLayout(false);
            useSmokeAPILayoutPanel.PerformLayout();
            allCheckBoxLayoutPanel.ResumeLayout(false);
            allCheckBoxLayoutPanel.PerformLayout();
            saveFlowPanel.ResumeLayout(false);
            saveFlowPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button installButton;
        private GroupBox programsGroupBox;
        private ProgressBar progressBar;
        private Label progressLabel;
        internal CheckBox allCheckBox;
        private Button scanButton;
        private Label noneFoundLabel;
        private CustomTreeView selectionTreeView;
        private ToggleSwitch useSmokeApiToggle;
        private Label useSmokeApiLabel;
        private Button useSmokeAPIHelpButton;
        private FlowLayoutPanel useSmokeAPILayoutPanel;
        private FlowLayoutPanel allCheckBoxLayoutPanel;
        private Button uninstallButton;
        private Button uninstallAllButton;
        private Label progressLabelGames;
        private Label progressLabelDLCs;
        private FlowLayoutPanel saveFlowPanel;
        private Button settingsButton;
        private TableLayoutPanel topOptionsTable;
    }
}
