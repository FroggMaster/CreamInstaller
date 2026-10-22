using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Components;

/// <summary>
/// Button with a rounded, theme-aware appearance. Painted manually so every button in
/// the application shares the same shape and hover/pressed behavior.
/// </summary>
internal class RoundedButton : Button
{
    protected enum VisualState
    {
        Normal,
        Hover,
        Pressed,
        Disabled
    }

    private static readonly Color DarkBack = Color.FromArgb(37, 37, 37);
    private static readonly Color DarkBackHover = Color.FromArgb(46, 46, 46);
    private static readonly Color DarkBackPressed = Color.FromArgb(31, 31, 31);
    private static readonly Color DarkBorder = Color.FromArgb(63, 63, 70);
    private static readonly Color DarkBorderHover = Color.FromArgb(74, 74, 82);
    private static readonly Color DarkFore = Color.FromArgb(212, 212, 212);
    private static readonly Color DarkForeHover = Color.FromArgb(230, 230, 230);
    private static readonly Color DarkForeDisabled = Color.FromArgb(110, 118, 129);

    private static readonly Color LightBackHover = Color.FromArgb(229, 241, 251);
    private static readonly Color LightBackPressed = Color.FromArgb(204, 228, 247);
    private static readonly Color LightBorderHover = Color.FromArgb(126, 180, 234);
    private static readonly Color LightBorderPressed = Color.FromArgb(86, 151, 206);

    private bool hovered;
    private bool pressed;

    internal RoundedButton()
    {
        SetStyle(ControlStyles.UserPaint
                 | ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 1;
    }

    protected VisualState State => !Enabled
        ? VisualState.Disabled
        : pressed
            ? VisualState.Pressed
            : hovered
                ? VisualState.Hover
                : VisualState.Normal;

    /// <summary>
    /// Colors for the button in the given state. Top and bottom are equal for a flat
    /// fill; <see cref="PrimaryButton"/> overrides this with an accent gradient.
    /// </summary>
    protected virtual (Color top, Color bottom, Color border, Color text) GetPalette(VisualState state)
    {
        if (!ThemeManager.IsDark)
            return state switch
            {
                VisualState.Disabled => (SystemColors.Control, SystemColors.Control,
                    SystemColors.ControlDark, SystemColors.GrayText),
                VisualState.Pressed => (LightBackPressed, LightBackPressed,
                    LightBorderPressed, SystemColors.ControlText),
                VisualState.Hover => (LightBackHover, LightBackHover,
                    LightBorderHover, SystemColors.ControlText),
                _ => (SystemColors.Control, SystemColors.Control,
                    SystemColors.ControlDark, SystemColors.ControlText)
            };

        return state switch
        {
            VisualState.Disabled => (DarkBack, DarkBack, DarkBorder, DarkForeDisabled),
            VisualState.Pressed => (DarkBackPressed, DarkBackPressed, DarkBorder, DarkFore),
            VisualState.Hover => (DarkBackHover, DarkBackHover, DarkBorderHover, DarkForeHover),
            _ => (DarkBack, DarkBack, DarkBorder, DarkFore)
        };
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // The entire surface, including the rounded corners, is painted in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using (SolidBrush background = new(Parent?.BackColor ?? BackColor))
            graphics.FillRectangle(background, ClientRectangle);

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        int radius = Math.Max(3, Height / 6);
        using GraphicsPath path = RoundedRectangle(bounds, radius);

        (Color top, Color bottom, Color border, Color text) = GetPalette(State);

        if (top == bottom)
        {
            using SolidBrush fill = new(top);
            graphics.FillPath(fill, path);
        }
        else
        {
            using LinearGradientBrush fill = new(bounds, top, bottom, LinearGradientMode.Vertical);
            graphics.FillPath(fill, path);
        }

        using (Pen pen = new(border))
            graphics.DrawPath(pen, path);

        TextRenderer.DrawText(graphics, Text, Font, bounds, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(bounds, -3, -3), text, top);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            pressed = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            pressed = true;
            Invalidate();
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            pressed = false;
            Invalidate();
        }
        base.OnKeyUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        hovered = false;
        pressed = false;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
