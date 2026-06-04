using System.ComponentModel;
using System.Windows.Forms;

namespace CreamInstaller.Forms;

partial class DebugForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        debugTextBox = new RichTextBox();
        testGameButton = new Button();
        SuspendLayout();
        // 
        // testGameButton
        // 
        testGameButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        testGameButton.AutoSize = true;
        testGameButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        testGameButton.Location = new System.Drawing.Point(10, 10);
        testGameButton.Name = "testGameButton";
        testGameButton.Padding = new Padding(3, 0, 3, 0);
        testGameButton.Size = new System.Drawing.Size(540, 25);
        testGameButton.TabIndex = 1;
        testGameButton.Text = "Test Game";
        testGameButton.UseVisualStyleBackColor = true;
        testGameButton.Click += OnTestGame;
        // 
        // debugTextBox
        // 
        debugTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        debugTextBox.Location = new System.Drawing.Point(10, 41);
        debugTextBox.Name = "debugTextBox";
        debugTextBox.ReadOnly = true;
        debugTextBox.ScrollBars = RichTextBoxScrollBars.ForcedBoth;
        debugTextBox.Size = new System.Drawing.Size(540, 286);
        debugTextBox.TabIndex = 0;
        debugTextBox.TabStop = false;
        debugTextBox.Text = "";
        // 
        // DebugForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(560, 337);
        ControlBox = false;
        Controls.Add(testGameButton);
        Controls.Add(debugTextBox);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "DebugForm";
        Padding = new Padding(10);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Debug";
        ResumeLayout(false);
    }

    #endregion

    private RichTextBox debugTextBox;
    private Button testGameButton;
}