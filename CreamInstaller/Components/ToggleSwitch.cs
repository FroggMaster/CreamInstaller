using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CreamInstaller.Components;

internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private const int TrackPadding = 1;
    private const int ThumbDiameter = 18;
    private static readonly Color DarkOffTrack = ColorTranslator.FromHtml("#3A3A3D");
    private static readonly Color DarkOnTrack = ColorTranslator.FromHtml("#0E639C");
    private static readonly Color LightOffTrack = ColorTranslator.FromHtml("#CCCCCC");
    private static readonly Color LightOnTrack = SystemColors.Highlight;
    private static readonly Color ThumbColor = Color.White;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(44, 22);
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler CheckedChanged;

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        bool isDarkMode = Program.DarkModeEnabled;

        int trackHeight = Height - (TrackPadding * 2);
        int trackWidth = Width - (TrackPadding * 2);

        // Draw track (rounded)
        Color trackColor = _checked
            ? (isDarkMode ? DarkOnTrack : LightOnTrack)
            : (isDarkMode ? DarkOffTrack : LightOffTrack);

        int cornerRadius = trackHeight / 2;
        Rectangle trackRect = new(TrackPadding, TrackPadding, trackWidth, trackHeight);
        using GraphicsPath trackPath = CreateRoundedRect(trackRect, cornerRadius);
        using SolidBrush trackBrush = new(trackColor);
        g.FillPath(trackBrush, trackPath);

        // Draw track border
        using Pen trackPen = new(isDarkMode ? ColorTranslator.FromHtml("#555555") : ColorTranslator.FromHtml("#A0A0A0"), 1);
        g.DrawPath(trackPen, trackPath);

        // Draw thumb
        int thumbX = _checked
            ? Width - TrackPadding - ThumbDiameter
            : TrackPadding;
        int thumbY = (Height - ThumbDiameter) / 2;

        using SolidBrush thumbBrush = new(ThumbColor);
        g.FillEllipse(thumbBrush, thumbX, thumbY, ThumbDiameter, ThumbDiameter);

        // Draw thumb shadow
        using Pen thumbShadowPen = new(Color.FromArgb(60, 0, 0, 0), 1);
        g.DrawEllipse(thumbShadowPen, thumbX, thumbY, ThumbDiameter, ThumbDiameter);
    }

    private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new();
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
