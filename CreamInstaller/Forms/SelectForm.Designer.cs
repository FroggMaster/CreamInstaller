using CreamInstaller.Components;
using CreamInstaller.Resources;
using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms
{
    partial class SelectForm
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
            cancelButton = new Button();
            programsGroupBox = new GroupBox();
            proxyFlowPanel = new FlowLayoutPanel();
            proxyAllCheckBox = new CheckBox();
            noneFoundLabel = new Label();
            blockedGamesFlowPanel = new FlowLayoutPanel();
            blockedGamesCheckBox = new CheckBox();
            blockProtectedHelpButton = new Button();
            useSmokeAPILayoutPanel = new FlowLayoutPanel();
            useSmokeAPICheckBox = new CheckBox();
            useSmokeAPIHelpButton = new Button();
            darkModeFlowPanel = new FlowLayoutPanel();
            darkModeCheckBox = new CheckBox();
            allCheckBoxLayoutPanel = new FlowLayoutPanel();
            allCheckBox = new CheckBox();
            progressBar = new ProgressBar();
            progressLabel = new Label();
            scanButton = new Button();
            uninstallButton = new Button();
            progressLabelGames = new Label();
            progressLabelDLCs = new Label();
            sortCheckBox = new CheckBox();
            saveButton = new Button();
            loadButton = new Button();
            resetButton = new Button();
            saveFlowPanel = new FlowLayoutPanel();
            selectionTreeView = new CustomTreeView();
            topOptionsTable = new TableLayoutPanel();
            statusStrip = new StatusStrip();
            statusLabelGames = new ToolStripStatusLabel();
            statusLabelSelected = new ToolStripStatusLabel();
            statusLabelDLCs = new ToolStripStatusLabel();
            statusLabelOperation = new ToolStripStatusLabel();
            programsGroupBox.SuspendLayout();
            proxyFlowPanel.SuspendLayout();
            blockedGamesFlowPanel.SuspendLayout();
            useSmokeAPILayoutPanel.SuspendLayout();
            darkModeFlowPanel.SuspendLayout();
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
            installButton.Location = new System.Drawing.Point(540, 436);
            installButton.Name = "installButton";
            installButton.Padding = new Padding(8, 2, 8, 2);
            installButton.Size = new System.Drawing.Size(127, 29);
            installButton.TabIndex = 10000;
            installButton.Text = "Generate and Install";
            installButton.UseVisualStyleBackColor = false;
            installButton.Click += OnInstall;
            installButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            //
            // cancelButton
            //
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cancelButton.AutoSize = true;
            cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cancelButton.Location = new System.Drawing.Point(16, 436);
            cancelButton.Name = "cancelButton";
            cancelButton.Padding = new Padding(6, 2, 6, 2);
            cancelButton.Size = new System.Drawing.Size(65, 29);
            cancelButton.TabIndex = 10004;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += OnCancel;
            //
            // programsGroupBox
            //
            programsGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            programsGroupBox.Controls.Add(noneFoundLabel);
            programsGroupBox.Controls.Add(selectionTreeView);
            programsGroupBox.Location = new System.Drawing.Point(16, 52);
            programsGroupBox.Margin = new Padding(4);
            programsGroupBox.Name = "programsGroupBox";
            programsGroupBox.Padding = new Padding(8, 12, 8, 8);
            programsGroupBox.Size = new System.Drawing.Size(668, 300);
            programsGroupBox.TabIndex = 8;
            programsGroupBox.TabStop = false;
            programsGroupBox.Text = "Programs / Games";
            // 
            // proxyFlowPanel
            // 
            proxyFlowPanel.AutoSize = true;
            proxyFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            proxyFlowPanel.Controls.Add(proxyAllCheckBox);
            proxyFlowPanel.Margin = new Padding(0);
            proxyFlowPanel.Name = "proxyFlowPanel";
            proxyFlowPanel.Size = new System.Drawing.Size(75, 19);
            proxyFlowPanel.TabIndex = 10005;
            proxyFlowPanel.WrapContents = false;
            // 
            // proxyAllCheckBox
            // 
            proxyAllCheckBox.AutoSize = true;
            proxyAllCheckBox.Enabled = false;
            proxyAllCheckBox.Location = new System.Drawing.Point(2, 0);
            proxyAllCheckBox.Margin = new Padding(2, 0, 0, 0);
            proxyAllCheckBox.Name = "proxyAllCheckBox";
            proxyAllCheckBox.Size = new System.Drawing.Size(73, 19);
            proxyAllCheckBox.TabIndex = 4;
            proxyAllCheckBox.Text = "Proxy All";
            proxyAllCheckBox.CheckedChanged += OnProxyAllCheckBoxChanged;
            // 
            // noneFoundLabel
            // 
            noneFoundLabel.Dock = DockStyle.Fill;
            noneFoundLabel.Location = new System.Drawing.Point(3, 19);
            noneFoundLabel.Name = "noneFoundLabel";
            noneFoundLabel.Size = new System.Drawing.Size(604, 230);
            noneFoundLabel.TabIndex = 1002;
            noneFoundLabel.Text = "No applicable programs nor games were found on your computer!";
            noneFoundLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            noneFoundLabel.Visible = false;
            // 
            // blockedGamesFlowPanel
            // 
            blockedGamesFlowPanel.AutoSize = true;
            blockedGamesFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            blockedGamesFlowPanel.Controls.Add(blockedGamesCheckBox);
            blockedGamesFlowPanel.Controls.Add(blockProtectedHelpButton);
            blockedGamesFlowPanel.Margin = new Padding(0);
            blockedGamesFlowPanel.Name = "blockedGamesFlowPanel";
            blockedGamesFlowPanel.Size = new System.Drawing.Size(170, 19);
            blockedGamesFlowPanel.TabIndex = 1005;
            blockedGamesFlowPanel.WrapContents = false;
            // 
            // blockedGamesCheckBox
            // 
            blockedGamesCheckBox.AutoSize = true;
            blockedGamesCheckBox.Checked = true;
            blockedGamesCheckBox.CheckState = CheckState.Checked;
            blockedGamesCheckBox.Enabled = false;
            blockedGamesCheckBox.Location = new System.Drawing.Point(2, 0);
            blockedGamesCheckBox.Margin = new Padding(2, 0, 0, 0);
            blockedGamesCheckBox.Name = "blockedGamesCheckBox";
            blockedGamesCheckBox.Size = new System.Drawing.Size(148, 19);
            blockedGamesCheckBox.TabIndex = 1;
            blockedGamesCheckBox.Text = "Block Protected Games";
            blockedGamesCheckBox.UseVisualStyleBackColor = true;
            blockedGamesCheckBox.CheckedChanged += OnBlockProtectedGamesCheckBoxChanged;
            // 
            // blockProtectedHelpButton
            // 
            blockProtectedHelpButton.Enabled = false;
            blockProtectedHelpButton.Font = new System.Drawing.Font("Segoe UI", 7F);
            blockProtectedHelpButton.Location = new System.Drawing.Point(150, 0);
            blockProtectedHelpButton.Margin = new Padding(0, 0, 1, 0);
            blockProtectedHelpButton.Name = "blockProtectedHelpButton";
            blockProtectedHelpButton.Size = new System.Drawing.Size(19, 19);
            blockProtectedHelpButton.TabIndex = 2;
            blockProtectedHelpButton.Text = "?";
            blockProtectedHelpButton.UseVisualStyleBackColor = true;
            blockProtectedHelpButton.Click += OnBlockProtectedGamesHelpButtonClicked;
            // 
            // useSmokeAPILayoutPanel
            // 
            useSmokeAPILayoutPanel.AutoSize = true;
            useSmokeAPILayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            useSmokeAPILayoutPanel.Controls.Add(useSmokeAPICheckBox);
            useSmokeAPILayoutPanel.Controls.Add(useSmokeAPIHelpButton);
            useSmokeAPILayoutPanel.Margin = new Padding(12, 0, 0, 0);
            useSmokeAPILayoutPanel.Name = "useSmokeAPILayoutPanel";
            useSmokeAPILayoutPanel.Size = new System.Drawing.Size(124, 19);
            useSmokeAPILayoutPanel.TabIndex = 1006;
            useSmokeAPILayoutPanel.WrapContents = false;
            // 
            // useSmokeAPICheckBox
            // 
            useSmokeAPICheckBox.AutoSize = true;
            useSmokeAPICheckBox.Checked = true;
            useSmokeAPICheckBox.CheckState = CheckState.Checked;
            useSmokeAPICheckBox.Enabled = false;
            useSmokeAPICheckBox.Location = new System.Drawing.Point(2, 0);
            useSmokeAPICheckBox.Margin = new Padding(2, 0, 0, 0);
            useSmokeAPICheckBox.Name = "useSmokeAPICheckBox";
            useSmokeAPICheckBox.Size = new System.Drawing.Size(102, 19);
            useSmokeAPICheckBox.TabIndex = 1;
            useSmokeAPICheckBox.Text = "Use SmokeAPI";
            useSmokeAPICheckBox.UseVisualStyleBackColor = true;
            useSmokeAPICheckBox.CheckedChanged += OnUseSmokeAPICheckBoxChanged;
            // 
            // useSmokeAPIHelpButton
            // 
            useSmokeAPIHelpButton.Enabled = false;
            useSmokeAPIHelpButton.Font = new System.Drawing.Font("Segoe UI", 7F);
            useSmokeAPIHelpButton.Location = new System.Drawing.Point(104, 0);
            useSmokeAPIHelpButton.Margin = new Padding(0, 0, 1, 0);
            useSmokeAPIHelpButton.Name = "useSmokeAPIHelpButton";
            useSmokeAPIHelpButton.Size = new System.Drawing.Size(19, 19);
            useSmokeAPIHelpButton.TabIndex = 2;
            useSmokeAPIHelpButton.Text = "?";
            useSmokeAPIHelpButton.UseVisualStyleBackColor = true;
            useSmokeAPIHelpButton.Click += OnUseSmokeAPIHelpButtonClicked;
            // 
            // darkModeFlowPanel
            // 
            darkModeFlowPanel.AutoSize = true;
            darkModeFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            darkModeFlowPanel.Margin = new Padding(12, 0, 0, 0);
            darkModeFlowPanel.Name = "darkModeFlowPanel";
            darkModeFlowPanel.Size = new System.Drawing.Size(98, 19);
            darkModeFlowPanel.TabIndex = 10011;
            darkModeFlowPanel.WrapContents = false;
            // 
            // darkModeCheckBox
            // 
            darkModeCheckBox.AutoSize = true;
            darkModeCheckBox.Enabled = true;
            darkModeCheckBox.Location = new System.Drawing.Point(2, 0);
            darkModeCheckBox.Margin = new Padding(2, 0, 0, 0);
            darkModeCheckBox.Name = "darkModeCheckBox";
            darkModeCheckBox.Size = new System.Drawing.Size(96, 19);
            darkModeCheckBox.TabIndex = 1;
            darkModeCheckBox.Text = "Enable Dark Mode";
            darkModeCheckBox.UseVisualStyleBackColor = true;
            darkModeCheckBox.CheckedChanged += OnDarkModeCheckBoxChanged;
            darkModeFlowPanel.Controls.Add(darkModeCheckBox);
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
            progressBar.Location = new System.Drawing.Point(16, 405);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(668, 23);
            progressBar.TabIndex = 9;
            //
            // progressLabel
            //
            progressLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabel.Location = new System.Drawing.Point(16, 360);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new System.Drawing.Size(530, 15);
            progressLabel.TabIndex = 10;
            progressLabel.Text = "Gathering and caching your applicable games and their DLCs . . . 0%";
            //
            // scanButton
            //
            scanButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            scanButton.AutoSize = true;
            scanButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            scanButton.Enabled = false;
            scanButton.Location = new System.Drawing.Point(190, 436);
            scanButton.Name = "scanButton";
            scanButton.Padding = new Padding(6, 2, 6, 2);
            scanButton.Size = new System.Drawing.Size(66, 29);
            scanButton.TabIndex = 10002;
            scanButton.Text = "Rescan";
            scanButton.UseVisualStyleBackColor = true;
            scanButton.Click += OnScan;
            //
            // uninstallButton
            //
            uninstallButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            uninstallButton.AutoSize = true;
            uninstallButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            uninstallButton.Enabled = false;
            uninstallButton.Location = new System.Drawing.Point(450, 436);
            uninstallButton.Name = "uninstallButton";
            uninstallButton.Padding = new Padding(6, 2, 6, 2);
            uninstallButton.Size = new System.Drawing.Size(75, 29);
            uninstallButton.TabIndex = 10001;
            uninstallButton.Text = "Uninstall";
            uninstallButton.UseVisualStyleBackColor = true;
            uninstallButton.Click += OnUninstall;
            //
            // progressLabelGames
            //
            progressLabelGames.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabelGames.Font = new System.Drawing.Font("Segoe UI", 7F);
            progressLabelGames.Location = new System.Drawing.Point(16, 377);
            progressLabelGames.Name = "progressLabelGames";
            progressLabelGames.Size = new System.Drawing.Size(668, 12);
            progressLabelGames.TabIndex = 11;
            progressLabelGames.Text = "Remaining games (2): Game 1, Game 2";
            //
            // progressLabelDLCs
            //
            progressLabelDLCs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressLabelDLCs.Font = new System.Drawing.Font("Segoe UI", 7F);
            progressLabelDLCs.Location = new System.Drawing.Point(16, 391);
            progressLabelDLCs.Name = "progressLabelDLCs";
            progressLabelDLCs.Size = new System.Drawing.Size(668, 12);
            progressLabelDLCs.TabIndex = 12;
            progressLabelDLCs.Text = "Remaining DLC (2): 123456, 654321";
            //
            // sortCheckBox
            //
            sortCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            sortCheckBox.AutoSize = true;
            sortCheckBox.Location = new System.Drawing.Point(90, 441);
            sortCheckBox.Margin = new Padding(3, 0, 0, 0);
            sortCheckBox.Name = "sortCheckBox";
            sortCheckBox.Size = new System.Drawing.Size(98, 19);
            sortCheckBox.TabIndex = 10003;
            sortCheckBox.Text = "Sort By Name";
            sortCheckBox.CheckedChanged += OnSortCheckBoxChanged;
            // 
            // saveButton
            // 
            saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            saveButton.AutoSize = true;
            saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            saveButton.Enabled = false;
            saveButton.Location = new System.Drawing.Point(51, 0);
            saveButton.Margin = new Padding(6, 0, 6, 0);
            saveButton.Name = "saveButton";
            saveButton.Size = new System.Drawing.Size(41, 25);
            saveButton.TabIndex = 10006;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += OnSaveSelections;
            // 
            // loadButton
            // 
            loadButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            loadButton.AutoSize = true;
            loadButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            loadButton.Enabled = false;
            loadButton.Location = new System.Drawing.Point(98, 0);
            loadButton.Margin = new Padding(0);
            loadButton.Name = "loadButton";
            loadButton.Size = new System.Drawing.Size(43, 25);
            loadButton.TabIndex = 10005;
            loadButton.Text = "Load";
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += OnLoadSelections;
            // 
            // resetButton
            // 
            resetButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            resetButton.AutoSize = true;
            resetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            resetButton.Enabled = false;
            resetButton.Location = new System.Drawing.Point(0, 0);
            resetButton.Margin = new Padding(0);
            resetButton.Name = "resetButton";
            resetButton.Size = new System.Drawing.Size(45, 25);
            resetButton.TabIndex = 10007;
            resetButton.Text = "Reset";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Click += OnResetSelections;
            //
            // saveFlowPanel
            //
            saveFlowPanel.Anchor = AnchorStyles.Bottom;
            saveFlowPanel.AutoSize = true;
            saveFlowPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            saveFlowPanel.Controls.Add(resetButton);
            saveFlowPanel.Controls.Add(saveButton);
            saveFlowPanel.Controls.Add(loadButton);
            saveFlowPanel.Location = new System.Drawing.Point(280, 436);
            saveFlowPanel.Name = "saveFlowPanel";
            saveFlowPanel.Size = new System.Drawing.Size(141, 29);
            saveFlowPanel.TabIndex = 10008;
            saveFlowPanel.WrapContents = false;
            //
            // topOptionsTable
            //
            topOptionsTable.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            topOptionsTable.AutoSize = true;
            topOptionsTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            topOptionsTable.ColumnCount = 6;
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // spacer
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topOptionsTable.Location = new System.Drawing.Point(16, 16);
            topOptionsTable.Margin = new Padding(0);
            topOptionsTable.Name = "topOptionsTable";
            topOptionsTable.RowCount = 1;
            topOptionsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            topOptionsTable.Size = new System.Drawing.Size(668, 28);
            topOptionsTable.TabIndex = 10009;
            topOptionsTable.Controls.Clear();
            topOptionsTable.Controls.Add(blockedGamesFlowPanel, 0, 0);
            topOptionsTable.Controls.Add(useSmokeAPILayoutPanel, 1, 0);
            topOptionsTable.Controls.Add(darkModeFlowPanel, 2, 0);
            topOptionsTable.Controls.Add(proxyFlowPanel, 4, 0);
            topOptionsTable.Controls.Add(allCheckBoxLayoutPanel, 5, 0);
            //
            // statusStrip
            //
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabelGames, statusLabelSelected, statusLabelDLCs, statusLabelOperation });
            statusStrip.Location = new System.Drawing.Point(0, 458);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new System.Drawing.Size(700, 22);
            statusStrip.SizingGrip = true;
            statusStrip.TabIndex = 10010;
            //
            // statusLabelGames
            //
            statusLabelGames.Name = "statusLabelGames";
            statusLabelGames.Size = new System.Drawing.Size(60, 17);
            statusLabelGames.Text = "Games: 0";
            //
            // statusLabelSelected
            //
            statusLabelSelected.Name = "statusLabelSelected";
            statusLabelSelected.Size = new System.Drawing.Size(65, 17);
            statusLabelSelected.Text = "Selected: 0";
            statusLabelSelected.BorderSides = ToolStripStatusLabelBorderSides.Left;
            statusLabelSelected.Margin = new Padding(8, 3, 0, 2);
            //
            // statusLabelDLCs
            //
            statusLabelDLCs.Name = "statusLabelDLCs";
            statusLabelDLCs.Size = new System.Drawing.Size(50, 17);
            statusLabelDLCs.Text = "DLCs: 0";
            statusLabelDLCs.BorderSides = ToolStripStatusLabelBorderSides.Left;
            statusLabelDLCs.Margin = new Padding(8, 3, 0, 2);
            //
            // statusLabelOperation
            //
            statusLabelOperation.Name = "statusLabelOperation";
            statusLabelOperation.Size = new System.Drawing.Size(50, 17);
            statusLabelOperation.Text = "Ready";
            statusLabelOperation.BorderSides = ToolStripStatusLabelBorderSides.Left;
            statusLabelOperation.Margin = new Padding(8, 3, 0, 2);
            statusLabelOperation.Spring = true;
            statusLabelOperation.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // SelectForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(700, 500);
            Controls.Add(statusStrip);
            Controls.Add(topOptionsTable);
            Controls.Add(saveFlowPanel);
            Controls.Add(sortCheckBox);
            Controls.Add(progressLabelDLCs);
            Controls.Add(progressLabelGames);
            Controls.Add(uninstallButton);
            Controls.Add(scanButton);
            Controls.Add(programsGroupBox);
            Controls.Add(progressBar);
            Controls.Add(cancelButton);
            Controls.Add(installButton);
            Controls.Add(progressLabel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new System.Drawing.Size(650, 450);
            Name = "SelectForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SelectForm";
            Load += OnLoad;
            programsGroupBox.ResumeLayout(false);
            proxyFlowPanel.ResumeLayout(false);
            proxyFlowPanel.PerformLayout();
            blockedGamesFlowPanel.ResumeLayout(false);
            blockedGamesFlowPanel.PerformLayout();
            useSmokeAPILayoutPanel.ResumeLayout(false);
            useSmokeAPILayoutPanel.PerformLayout();
            darkModeFlowPanel.ResumeLayout(false);
            darkModeFlowPanel.PerformLayout();
            allCheckBoxLayoutPanel.ResumeLayout(false);
            allCheckBoxLayoutPanel.PerformLayout();
            saveFlowPanel.ResumeLayout(false);
            saveFlowPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button installButton;
        private Button cancelButton;
        private GroupBox programsGroupBox;
        private ProgressBar progressBar;
        private Label progressLabel;
        internal CheckBox allCheckBox;
        private Button scanButton;
        private Label noneFoundLabel;
        private CustomTreeView selectionTreeView;
        private CheckBox blockedGamesCheckBox;
        private Button blockProtectedHelpButton;
        private CheckBox useSmokeAPICheckBox;
        private Button useSmokeAPIHelpButton;
        private FlowLayoutPanel blockedGamesFlowPanel;
        private FlowLayoutPanel useSmokeAPILayoutPanel;
        private FlowLayoutPanel darkModeFlowPanel;
        private FlowLayoutPanel allCheckBoxLayoutPanel;
        private Button uninstallButton;
        private Label progressLabelGames;
        private Label progressLabelDLCs;
        private CheckBox sortCheckBox;
        private FlowLayoutPanel proxyFlowPanel;
        internal CheckBox proxyAllCheckBox;
        private Button saveButton;
        private Button loadButton;
        private Button resetButton;
        private FlowLayoutPanel saveFlowPanel;
        private TableLayoutPanel topOptionsTable;
        private CheckBox darkModeCheckBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabelGames;
        private ToolStripStatusLabel statusLabelSelected;
        private ToolStripStatusLabel statusLabelDLCs;
        private ToolStripStatusLabel statusLabelOperation;
    }
}

