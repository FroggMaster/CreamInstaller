using System;
using System.Drawing;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;

namespace CreamInstaller.Forms;

internal sealed partial class DebugForm : CustomForm
{
    private static DebugForm current;
    private static readonly object currentLock = new();

    internal static bool IsOpen { get; private set; }

    private Form attachedForm;

    private DebugForm()
    {
        InitializeComponent();
        debugTextBox.BackColor = LogTextBox.Background;
    }

    internal static DebugForm Current
    {
        get
        {
            lock (currentLock)
            {
                if (current is null || current.Disposing || current.IsDisposed)
                {
                    current = new DebugForm();
                }
                return current;
            }
        }
    }

    protected override void WndProc(ref Message message) // make form immovable by user
    {
        if (message.Msg == 0x0112) // WM_SYSCOMMAND
        {
            int command = message.WParam.ToInt32() & 0xFFF0;
            if (command == 0xF010) // SC_MOVE
                return;
        }

        base.WndProc(ref message);
    }

    internal void Attach(Form form)
    {
        if (attachedForm is not null)
        {
            attachedForm.Activated -= OnChange;
            attachedForm.LocationChanged -= OnChange;
            attachedForm.SizeChanged -= OnChange;
            attachedForm.VisibleChanged -= OnChange;
        }

        attachedForm = form;
        attachedForm.Activated += OnChange;
        attachedForm.LocationChanged += OnChange;
        attachedForm.SizeChanged += OnChange;
        attachedForm.VisibleChanged += OnChange;
        UpdateAttachment();

        if (!IsOpen)
        {
            IsOpen = true;
            ProgramData.OnLog += args =>
            {
                Color color = args.Level switch
                {
                    LogLevel.Warning => LogTextBox.Warning,
                    LogLevel.Error => LogTextBox.Error,
                    _ => args.Message switch
                    {
                        string m when m.Contains("not found", StringComparison.OrdinalIgnoreCase) => LogTextBox.Failure,
                        string m when m.Contains("Skipping", StringComparison.Ordinal) || m.Contains("skipped", StringComparison.Ordinal) || m.Contains("not accessible", StringComparison.Ordinal) => LogTextBox.Warning,
                        string m when m.Contains("failed", StringComparison.OrdinalIgnoreCase) || m.Contains("timed out", StringComparison.OrdinalIgnoreCase) || m.Contains("cancelled", StringComparison.OrdinalIgnoreCase) || m.Contains("rate limited", StringComparison.OrdinalIgnoreCase) || m.Contains("unsuccessful", StringComparison.OrdinalIgnoreCase) || m.Contains("exceeded", StringComparison.OrdinalIgnoreCase) => LogTextBox.Failure,
                        _ => LogTextBox.Action
                    }
                };
                Log(args.Message, color);
            };
        }
    }

    private void OnChange(object sender, EventArgs args) => UpdateAttachment();

    private void UpdateAttachment()
    {
        if (attachedForm is null || !attachedForm.Visible)
            return;
        //Size = new(Size.Width, attachedForm.Size.Height);
        Location = new(attachedForm.Right, attachedForm.Top);
        BringToFrontWithoutActivation();
    }

    internal void Log(string text) => Log(text, LogTextBox.Error);

    internal void Log(string text, Color color)
    {
        if (!debugTextBox.Disposing && !debugTextBox.IsDisposed)
            Invoke(() =>
            {
                if (debugTextBox.Text.Length > 0)
                    debugTextBox.AppendText(Environment.NewLine, color, true);
                debugTextBox.AppendText(text, color, true);
            });
    }

    private void OnTestGame(object sender, EventArgs e)
    {
        using TestGameForm form = new(this);
        _ = form.ShowDialog(this);
    }
}