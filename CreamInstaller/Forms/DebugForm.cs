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

    internal void Open(Form owner = null)
    {
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
        if (owner is not null)
        {
            Owner = owner;
            StartPosition = FormStartPosition.Manual;
            if (owner.Visible)
            {
                Location = new(owner.Right, owner.Top);
                Show();
                Activate();
            }
            else
            {
                EventHandler onShown = null;
                onShown = (_, _) =>
                {
                    Location = new(owner.Right, owner.Top);
                    owner.Shown -= onShown;
                    Show();
                    Activate();
                };
                owner.Shown += onShown;
            }
        }
        else
        {
            Show();
            Activate();
        }
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
