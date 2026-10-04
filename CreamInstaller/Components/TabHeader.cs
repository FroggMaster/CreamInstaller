using System;
using System.Drawing;
using System.Windows.Forms;
using CreamInstaller.Utility;

namespace CreamInstaller.Components;

/// <summary>
/// A games tab-strip header. Painted entirely in managed code using the active theme colors so it blends with the
/// dark (or light) theme, unlike the native <see cref="TabControl"/> which always paints a light system body/frame.
/// </summary>
internal sealed class TabHeader : Control
{
    private bool hovered;
    private bool selected;

    internal TabHeader()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Height = 30;
        Width = 170;
        TabStop = false;
    }

    internal bool IsSelected
    {
        get => selected;
        set
        {
            if (selected == value)
                return;
            selected = value;
            Invalidate();
        }
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Color back = selected
            ? ThemeManager.TabSelectedBackgroundColor
            : hovered
                ? ThemeManager.TabHoverBackgroundColor
                : ThemeManager.TabBackgroundColor;
        Color fore = selected ? ThemeManager.TabSelectedTextColor : ThemeManager.TabTextColor;

        using (SolidBrush brush = new(back))
            e.Graphics.FillRectangle(brush, ClientRectangle);

        if (selected)
        {
            using Pen accent = new(ThemeManager.TabAccentColor, 2);
            e.Graphics.DrawLine(accent, 0, Height - 2, Width, Height - 2);
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
