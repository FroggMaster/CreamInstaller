using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class ThemeManager
{
    // -----------------------------------------------------------------
    // Color definitions (do not change values)
    // -----------------------------------------------------------------

    // ----------------------------
    // Dark mode colors
    // ----------------------------
    private static readonly Color DarkBack = ColorTranslator.FromHtml("#1E1E1E");
    private static readonly Color DarkBackAlt = ColorTranslator.FromHtml("#252525");
    private static readonly Color DarkBorder = ColorTranslator.FromHtml("#3F3F46");
    private static readonly Color DarkFore = ColorTranslator.FromHtml("#D4D4D4");
    private static readonly Color DarkForeDim = ColorTranslator.FromHtml("#9CA3AF");
    private static readonly Color Accent = ColorTranslator.FromHtml("#0E639C");
    private static readonly Color DarkLink = ColorTranslator.FromHtml("#64B5F6");

    // CustomTreeView dark-mode specific colors
    private static readonly Color DarkPlatform = ColorTranslator.FromHtml("#FFFF99");
    private static readonly Color DarkId = ColorTranslator.FromHtml("#99FFFF");
    private static readonly Color DarkProxy = ColorTranslator.FromHtml("#99FF99");
    private static readonly Color DarkSelectionBack = ColorTranslator.FromHtml("#2A2D2E");
    private static readonly Color DarkComboBack = DarkBackAlt; // #252525
    private static readonly Color DarkComboBorder = DarkBorder; // #3F3F46
    private static readonly Color DarkComboText = DarkFore; // #D4D4D4

    // Badge colors for unlockers
    private static readonly Color CreamAPIBadgeBack = ColorTranslator.FromHtml("#C8A078"); // Creamy latte
    private static readonly Color CreamAPIBadgeBackHighlight = ColorTranslator.FromHtml("#B48C64");
    private static readonly Color CreamAPIBadgeBorder = ColorTranslator.FromHtml("#DCB48C");
    private static readonly Color SmokeAPIBadgeBack = ColorTranslator.FromHtml("#69696E"); // Smoky grey
    private static readonly Color SmokeAPIBadgeBackHighlight = ColorTranslator.FromHtml("#5A5A5F");
    private static readonly Color SmokeAPIBadgeBorder = ColorTranslator.FromHtml("#8C8C91");
    private static readonly Color DefaultBadgeBack = ColorTranslator.FromHtml("#008C46"); // Default green
    private static readonly Color DefaultBadgeBackHighlight = ColorTranslator.FromHtml("#00783C");
    private static readonly Color DefaultBadgeBorder = ColorTranslator.FromHtml("#00B45A");

    // ScreamAPI badge: Epic Games black
    private static readonly Color ScreamAPIBadgeBack = ColorTranslator.FromHtml("#131313");
    private static readonly Color ScreamAPIBadgeBackHighlight = ColorTranslator.FromHtml("#0A0A0A");
    private static readonly Color ScreamAPIBadgeBorder = ColorTranslator.FromHtml("#2A2A2A");

    // Koaloader badge: koala gray
    private static readonly Color KoaloaderBadgeBack = ColorTranslator.FromHtml("#8C8C8C");
    private static readonly Color KoaloaderBadgeBackHighlight = ColorTranslator.FromHtml("#7A7A7A");
    private static readonly Color KoaloaderBadgeBorder = ColorTranslator.FromHtml("#A8A8A8");

    // Uplay badge: light blue
    private static readonly Color UplayBadgeBack = ColorTranslator.FromHtml("#64B5F6");
    private static readonly Color UplayBadgeBackHighlight = ColorTranslator.FromHtml("#42A5F5");
    private static readonly Color UplayBadgeBorder = ColorTranslator.FromHtml("#90CAF9");

    // ----------------------------
    // Light mode colors (system defaults)
    // ----------------------------
    private static readonly Color LightBack = SystemColors.Control;
    private static readonly Color LightBackAlt = SystemColors.ControlLightLight;
    private static readonly Color LightFore = SystemColors.ControlText;
    private static readonly Color LightBorder = SystemColors.ControlDark;

    // CustomTreeView light-mode specific colors
    private static readonly Color LightPlatform = ColorTranslator.FromHtml("#696900");
    private static readonly Color LightId = ColorTranslator.FromHtml("#006969");
    private static readonly Color LightProxy = ColorTranslator.FromHtml("#006900");
    private static readonly Color LightSelectionBack = ColorTranslator.FromHtml("#ADD6FF");
    private static readonly Color LightComboBack = SystemColors.Control;
    private static readonly Color LightComboBorder = SystemColors.ControlDark;
    private static readonly Color LightComboText = SystemColors.ControlText;

    // -----------------------------------------------------------------
    // Theme-aware properties used by other components (CustomTreeView etc.)
    // -----------------------------------------------------------------

    internal static bool IsDark => Program.DarkModeEnabled;

    internal static Color CustomTreeViewPlatformColor => IsDark ? DarkPlatform : LightPlatform;

    internal static Color CustomTreeViewIdColor => IsDark ? DarkId : LightId;

    internal static Color CustomTreeViewProxyColor => IsDark ? DarkProxy : LightProxy;

    internal static Color CustomTreeViewHighlightPlatformColor => DarkPlatform; // C1 (uses same color for highlight)
    internal static Color CustomTreeViewDisabledPlatformColor => ColorTranslator.FromHtml("#AAAA69"); // C3
    internal static Color CustomTreeViewHighlightIdColor => DarkId; // C4
    internal static Color CustomTreeViewDisabledIdColor => ColorTranslator.FromHtml("#69AAAA"); // C6
    internal static Color CustomTreeViewDisabledProxyColor => ColorTranslator.FromHtml("#69AA69"); // C8

    // Background color used when a tree node is selected.
    // Keeps light-mode behavior using the system highlight, but supplies a custom dark color for dark mode
    internal static Color CustomTreeViewSelectionBackColor => IsDark ? DarkSelectionBack : LightSelectionBack;

    internal static Color CustomTreeViewComboBackColor => IsDark ? DarkComboBack : LightComboBack;

    internal static Color CustomTreeViewComboBorderColor => IsDark ? DarkComboBorder : LightComboBorder;

    internal static Color CustomTreeViewComboTextColor => IsDark ? DarkComboText : LightComboText;

    // Badge colors for unlockers
    internal static Color CreamAPIBadgeBackgroundColor => CreamAPIBadgeBack;
    internal static Color CreamAPIBadgeBackgroundHighlightColor => CreamAPIBadgeBackHighlight;
    internal static Color CreamAPIBadgeBorderColor => CreamAPIBadgeBorder;
    internal static Color SmokeAPIBadgeBackgroundColor => SmokeAPIBadgeBack;
    internal static Color SmokeAPIBadgeBackgroundHighlightColor => SmokeAPIBadgeBackHighlight;
    internal static Color SmokeAPIBadgeBorderColor => SmokeAPIBadgeBorder;
    internal static Color DefaultBadgeBackgroundColor => DefaultBadgeBack;
    internal static Color DefaultBadgeBackgroundHighlightColor => DefaultBadgeBackHighlight;
    internal static Color DefaultBadgeBorderColor => DefaultBadgeBorder;

    // ScreamAPI badge colors
    internal static Color ScreamAPIBadgeBackgroundColor => ScreamAPIBadgeBack;
    internal static Color ScreamAPIBadgeBackgroundHighlightColor => ScreamAPIBadgeBackHighlight;
    internal static Color ScreamAPIBadgeBorderColor => ScreamAPIBadgeBorder;

    // Koaloader badge colors
    internal static Color KoaloaderBadgeBackgroundColor => KoaloaderBadgeBack;
    internal static Color KoaloaderBadgeBackgroundHighlightColor => KoaloaderBadgeBackHighlight;
    internal static Color KoaloaderBadgeBorderColor => KoaloaderBadgeBorder;

    // Uplay badge colors (shared by R1 and R2)
    internal static Color UplayBadgeBackgroundColor => UplayBadgeBack;
    internal static Color UplayBadgeBackgroundHighlightColor => UplayBadgeBackHighlight;
    internal static Color UplayBadgeBorderColor => UplayBadgeBorder;

    // -----------------------------------------------------------------
    // Public / Internal API
    // -----------------------------------------------------------------

    /// <summary>
    /// Toggle dark mode and re-apply theming to all open forms.
    /// </summary>
    internal static void ToggleDarkMode(Form anyForm)
    {
        Program.DarkModeEnabled = !Program.DarkModeEnabled;
        ApplyToAllOpenForms();
    }

    /// <summary>
    /// Apply current theme to a single form and its child controls.
    /// </summary>
    internal static void Apply(Form form)
    {
        if (form is null) return;
        if (!IsDark)
        {
            Reset(form);
            return;
        }

        form.SuspendLayout();
        form.BackColor = DarkBack;
        form.ForeColor = DarkFore;
        ApplyTitleBar(form);

        foreach (Control c in form.Controls)
            ApplyControlTheme(c, true);

        form.ResumeLayout(true);
    }

    /// <summary>
    /// Apply the theme to all currently open forms.
    /// </summary>
    internal static void ApplyToAllOpenForms()
    {
        foreach (Form openForm in Application.OpenForms.Cast<Form>())
            Apply(openForm);
    }

    // -----------------------------------------------------------------
    // Control theming helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Apply theming to a control tree. Entry point which recurses children
    /// then applies either the dark or light styling logic.
    /// </summary>
    private static void ApplyControlTheme(Control control, bool dark)
    {
        if (control is null) return;

        // Recurse first so parent layering still works correctly
        foreach (Control child in control.Controls)
            ApplyControlTheme(child, dark);

        if (dark)
            ApplyDarkControl(control);
        else
            ApplyLightControl(control);

        // Try to apply themed scrollbars where applicable
        TryApplyScrollbarTheme(control, dark);
    }

    // Separated dark/light cases to make the intent clearer and reduce duplication
    private static void ApplyDarkControl(Control control)
    {
        switch (control)
        {
            // Group box background/foreground
            case GroupBox gb:
                gb.ForeColor = DarkFore;
                gb.BackColor = DarkBackAlt;
                break;

            // Buttons: flat appearance, border and foreground
            case Button b:
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = DarkBorder;
                b.BackColor = DarkBackAlt;
                b.ForeColor = DarkFore;
                break;

            // Checkboxes: match form background and foreground
            case CheckBox cb:
                cb.BackColor = DarkBack;
                cb.ForeColor = DarkFore;
                break;

            // LinkLabel: color and active/visited styling
            case LinkLabel ll:
                ll.BackColor = DarkBack;
                ll.ForeColor = DarkFore;
                ll.LinkColor = DarkLink;
                ll.ActiveLinkColor = Color.White;
                ll.VisitedLinkColor = DarkLink;
                break;

            // Labels: transparent so they blend with whatever container they sit in
            case Label lbl:
                lbl.BackColor = Color.Transparent;
                lbl.ForeColor = DarkFore;
                break;

            // ProgressBar uses accent color for foreground
            case ProgressBar pb:
                pb.ForeColor = Accent;
                pb.BackColor = DarkBackAlt;
                break;

            // TreeView: darker alternate background, light text, darker lines
            case TreeView tv:
                tv.BackColor = DarkBackAlt;
                tv.ForeColor = DarkFore;
                tv.LineColor = DarkBorder;
                tv.Invalidate(); // Forces a redraw
                break;

            // RichTextBox follows alternate dark background
            case RichTextBox rtb:
                rtb.BackColor = DarkBackAlt;
                rtb.ForeColor = DarkFore;
                break;

            // ListBox follows alternate dark background
            case ListBox lb:
                lb.BackColor = DarkBackAlt;
                lb.ForeColor = DarkFore;
                break;

            // TextBox follows alternate dark background
            case TextBox tb:
                tb.BackColor = DarkBackAlt;
                tb.ForeColor = DarkFore;
                tb.BorderStyle = BorderStyle.FixedSingle;
                NativeMethods.RefreshCueBanner(tb);
                break;

            // Layout panels set a consistent background
            case TableLayoutPanel tlp:
                tlp.BackColor = DarkBack;
                break;
            case FlowLayoutPanel flp:
                flp.BackColor = DarkBack;
                break;
        }
    }

    private static void ApplyLightControl(Control control)
    {
        switch (control)
        {
            case GroupBox gb:
                gb.BackColor = LightBack;
                gb.ForeColor = LightFore;
                break;
            case Button b:
                b.FlatStyle = FlatStyle.Standard;
                b.BackColor = LightBack;
                b.ForeColor = LightFore;
                break;
            case CheckBox cb:
                cb.BackColor = LightBack;
                cb.ForeColor = LightFore;
                break;
            case LinkLabel ll:
                ll.BackColor = LightBack;
                ll.ForeColor = LightFore;
                ll.LinkColor = SystemColors.HotTrack;
                ll.ActiveLinkColor = SystemColors.Highlight;
                ll.VisitedLinkColor = SystemColors.HotTrack;
                break;
            case Label lbl:
                lbl.BackColor = Color.Transparent;
                lbl.ForeColor = LightFore;
                break;
            case ProgressBar pb:
                pb.BackColor = LightBack;
                pb.ForeColor = LightFore;
                break;
            case TreeView tv:
                tv.BackColor = LightBack;
                tv.ForeColor = LightFore;
                tv.LineColor = LightBorder;
                tv.Invalidate(); // Forces a redraw
                break;
            case RichTextBox rtb:
                rtb.BackColor = LightBack;
                rtb.ForeColor = LightFore;
                break;
            case ListBox lb:
                lb.BackColor = LightBackAlt;
                lb.ForeColor = LightFore;
                break;
            case TextBox tb:
                tb.BackColor = LightBackAlt;
                tb.ForeColor = LightFore;
                tb.BorderStyle = BorderStyle.Fixed3D;
                NativeMethods.RefreshCueBanner(tb);
                break;
            case TableLayoutPanel tlp:
                tlp.BackColor = LightBack;
                break;
            case FlowLayoutPanel flp:
                flp.BackColor = LightBack;
                break;
        }
    }

    private static void Reset(Form form)
    {
        form.SuspendLayout();
        form.BackColor = LightBack;
        form.ForeColor = LightFore;
        ApplyTitleBar(form);
        foreach (Control c in form.Controls)
            ApplyControlTheme(c, false);
        form.ResumeLayout(true);
    }

    // -----------------------------------------------------------------
    // Titlebar / platform-specific helpers
    // -----------------------------------------------------------------

    private static void ApplyTitleBar(Form form)
    {
        try
        {
            int useDark = IsDark ? 1 : 0;
            NativeMethods.EnableDarkTitleBar(form.Handle, useDark);
        }
        catch (Exception ex) { ProgramData.LogWarning($"[Theme] Title bar theming failed: {ex.Message}"); }
    }

    private static void TryApplyScrollbarTheme(Control control, bool dark)
    {
        try
        {
            string theme = dark ? "DarkMode_Explorer" : null;
            NativeImports.SetWindowTheme(control.Handle, theme, null);
        }
        catch (Exception ex) { ProgramData.LogWarning($"[Theme] Scrollbar theming failed: {ex.Message}"); }
    }

    // -----------------------------------------------------------------
    // Context menu / ToolStrip theming
    // -----------------------------------------------------------------

    /// <summary>
    /// Apply theme to a context menu (ContextMenuStrip).
    /// </summary>
    internal static void ApplyContextMenu(ContextMenuStrip contextMenu)
    {
        if (contextMenu is null) return;

        bool dark = IsDark;

        contextMenu.BackColor = dark ? DarkBackAlt : SystemColors.Menu;
        contextMenu.ForeColor = dark ? DarkFore : SystemColors.MenuText;
        contextMenu.Renderer = dark ? new DarkContextMenuRenderer() : new ToolStripProfessionalRenderer();

        foreach (ToolStripItem item in contextMenu.Items)
            ApplyContextMenuItem(item, dark);
    }

    private static void ApplyContextMenuItem(ToolStripItem item, bool dark)
    {
        if (item is null) return;

        item.BackColor = dark ? DarkBackAlt : SystemColors.Menu;
        item.ForeColor = dark ? DarkFore : SystemColors.MenuText;

        if (item is ToolStripMenuItem menuItem)
            foreach (ToolStripItem subItem in menuItem.DropDownItems)
                ApplyContextMenuItem(subItem, dark);
    }

    internal static void ApplyToolStripDropDown(ToolStripDropDown dropDown)
    {
        if (dropDown is null) return;

        bool dark = IsDark;

        dropDown.BackColor = dark ? DarkBackAlt : SystemColors.Menu;
        dropDown.ForeColor = dark ? DarkFore : SystemColors.MenuText;
        dropDown.Renderer = dark ? new DarkDropDownRenderer() : new ToolStripProfessionalRenderer();

        foreach (ToolStripItem item in dropDown.Items)
            ApplyToolStripItem(item, dark);
    }

    private static void ApplyToolStripItem(ToolStripItem item, bool dark)
    {
        if (item is null) return;

        item.BackColor = dark ? DarkBackAlt : SystemColors.Menu;
        item.ForeColor = dark ? DarkFore : SystemColors.MenuText;
    }

    // -----------------------------------------------------------------
    // Themed renderers for menus
    // -----------------------------------------------------------------

    private sealed class DarkContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkContextMenuRenderer() : base(new DarkMenuColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Selected)
                e.TextColor = DarkFore;
            base.OnRenderItemText(e);
        }
    }

    private sealed class DarkDropDownRenderer : ToolStripProfessionalRenderer
    {
        public DarkDropDownRenderer() : base(new DarkMenuColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Force text color to stay light even when selected
            e.TextColor = DarkFore;
            base.OnRenderItemText(e);
        }
    }

    private sealed class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => ColorTranslator.FromHtml("#2A2D2E");
        public override Color MenuItemSelectedGradientBegin => ColorTranslator.FromHtml("#2A2D2E");
        public override Color MenuItemSelectedGradientEnd => ColorTranslator.FromHtml("#2A2D2E");
        public override Color MenuItemBorder => ColorTranslator.FromHtml("#3F3F46");
        public override Color MenuBorder => ColorTranslator.FromHtml("#3F3F46");
        public override Color MenuItemPressedGradientBegin => ColorTranslator.FromHtml("#252525");
        public override Color MenuItemPressedGradientEnd => ColorTranslator.FromHtml("#252525");
        public override Color ImageMarginGradientBegin => ColorTranslator.FromHtml("#1E1E1E");
        public override Color ImageMarginGradientMiddle => ColorTranslator.FromHtml("#1E1E1E");
        public override Color ImageMarginGradientEnd => ColorTranslator.FromHtml("#1E1E1E");
        public override Color ToolStripDropDownBackground => ColorTranslator.FromHtml("#252525");
        public override Color SeparatorDark => ColorTranslator.FromHtml("#3F3F46");
        public override Color SeparatorLight => ColorTranslator.FromHtml("#3F3F46");
    }

    // -----------------------------------------------------------------
    // Theming helpers for CustomTreeView
    // All rendering logic for the CustomTreeView's proxy combo box and dropdown
    // button is centralized here so theming resides in ThemeManager.
    // -----------------------------------------------------------------

    // Dark checkbox colors � matched to how the system renders the "All" CheckBox control
    // in dark mode: dark fill, mid-gray border, light foreground tick.
    private static readonly Color DarkCbBorder = ColorTranslator.FromHtml("#6B6B6B");
    private static readonly Color DarkCbDisabledBorder = ColorTranslator.FromHtml("#454545");

    /// <summary>
    /// Draws a checkbox glyph in pure GDI that matches the appearance of a dark-themed
    /// WinForms CheckBox control (same background, border, tick colors, and rounded corners).
    /// Use this in owner-draw contexts where CheckBoxRenderer always paints a white background.
    /// </summary>
    internal static void DrawDarkCheckBox(Graphics g, Point point, Size glyphSize, bool isChecked, bool enabled = true)
    {
        if (g is null) return;
        int w = glyphSize.Width;
        int h = glyphSize.Height;
        Rectangle box = new(point.X, point.Y, w - 1, h - 1);
        int radius = Math.Max(2, w / 5);

        using System.Drawing.Drawing2D.GraphicsPath path = RoundedRect(box, radius);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (isChecked && enabled)
        {
            // Checked + enabled: accent fill, no border, white tick � matches Windows 11 dark CheckBox
            using SolidBrush fillBrush = new(Accent);
            g.FillPath(fillBrush, path);

            using Pen tickPen = new(Color.White, 1.7f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            };
            float scaleX = w / 13f;
            float scaleY = h / 13f;
            g.DrawLines(tickPen, new PointF[]
            {
                new(point.X + 2 * scaleX, point.Y + 6 * scaleY),
                new(point.X + 5 * scaleX, point.Y + 9 * scaleY),
                new(point.X + 10 * scaleX, point.Y + 3 * scaleY),
            });
        }
        else if (isChecked)
        {
            // Checked + disabled: dimmed accent fill, dimmed tick
            Color dimAccent = Color.FromArgb(120, Accent);
            using SolidBrush fillBrush = new(dimAccent);
            g.FillPath(fillBrush, path);

            using Pen tickPen = new(DarkForeDim, 1.7f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            };
            float scaleX = w / 13f;
            float scaleY = h / 13f;
            g.DrawLines(tickPen, new PointF[]
            {
                new(point.X + 2 * scaleX, point.Y + 6 * scaleY),
                new(point.X + 5 * scaleX, point.Y + 9 * scaleY),
                new(point.X + 10 * scaleX, point.Y + 3 * scaleY),
            });
        }
        else
        {
            // Unchecked: dark fill, gray border, no tick
            using SolidBrush fillBrush = new(DarkBackAlt);
            g.FillPath(fillBrush, path);

            using Pen borderPen = new(enabled ? DarkCbBorder : DarkCbDisabledBorder);
            g.DrawPath(borderPen, path);
        }

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        System.Drawing.Drawing2D.GraphicsPath path = new();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Draws the themed combobox area (background, border and text) used in CustomTreeView.
    /// This centralizes colors and rendering for light/dark modes.
    /// </summary>
    internal static void DrawCustomComboBox(Graphics graphics, Rectangle rect, Font font, string text)
    {
        if (graphics is null) return;
        using SolidBrush comboBrush = new(CustomTreeViewComboBackColor);
        using Pen borderPen = new(CustomTreeViewComboBorderColor);
        graphics.FillRectangle(comboBrush, rect);
        graphics.DrawRectangle(borderPen, rect);
        // Draw text inside the combobox
        Size textSize = TextRenderer.MeasureText(graphics, text, font);
        Point textPoint = new(rect.Left +3, rect.Top + rect.Height /2 - textSize.Height /2);
        TextRenderer.DrawText(graphics, text, font, textPoint, CustomTreeViewComboTextColor, TextFormatFlags.Default);
    }

    /// <summary>
    /// Draws the themed dropdown button (right-side arrow) used in CustomTreeView comboboxes.
    /// </summary>
    internal static void DrawCustomComboBoxButton(Graphics graphics, Rectangle rect)
    {
        if (graphics is null) return;
        using SolidBrush comboBrush = new(CustomTreeViewComboBackColor);
        using Pen borderPen = new(CustomTreeViewComboBorderColor);
        graphics.FillRectangle(comboBrush, rect);
        graphics.DrawRectangle(borderPen, rect);

        // Draw the arrow glyph centered in the rect
        int arrowSize =3;
        Point arrowTop = new(rect.X + rect.Width /2, rect.Y + rect.Height /2 -1);
        Point[] arrowPoints = new[]
        {
            arrowTop,
            new Point(arrowTop.X - arrowSize, arrowTop.Y - arrowSize),
            new Point(arrowTop.X + arrowSize, arrowTop.Y - arrowSize)
        };
        using SolidBrush arrowBrush = new(CustomTreeViewComboTextColor);
        graphics.FillPolygon(arrowBrush, arrowPoints);
    }
}

/// <summary>
/// Wraps Win32 API calls that have no managed equivalent in WinForms.
/// These P/Invoke declarations are required because .NET does not expose
/// the underlying Windows messages or DWM attributes through its own APIs.
/// </summary>
internal static class NativeMethods
{
    // DWM attribute index for enabling/disabling the immersive dark title bar.
    // Documented in dwmapi.h; value 20 corresponds to DWMWA_USE_IMMERSIVE_DARK_MODE
    // (Windows 10 build 19041+ / Windows 11).
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    // DwmSetWindowAttribute allows setting per-window Desktop Window Manager attributes.
    // We use it here to flip the title bar to dark or light depending on the active theme,
    // since WinForms has no built-in API to control title bar coloring.
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>
    /// Toggles the dark/light title bar chrome for the given window handle.
    /// Pass <c>1</c> for dark mode, <c>0</c> for light mode.
    /// </summary>
    internal static void EnableDarkTitleBar(System.IntPtr handle, int useDark)
    {
        _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
    }

    // Win32 Edit control message that sets or updates the cue (placeholder) banner text.
    // WinForms sets PlaceholderText once at creation time via this same message internally,
    // but does not re-send it when the control's colors change.  When we restyle a TextBox
    // for dark/light mode the cue banner can disappear, so we must re-send the message
    // manually to make the placeholder visible again.
    private const int EM_SETCUEBANNER = 0x1501;

    // SendMessage is the standard Win32 mechanism for posting messages directly to a
    // window/control handle.  We use the Unicode variant so the placeholder string is
    // transmitted without any ANSI conversion.
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, string lParam);

    /// <summary>
    /// Re-sends <c>EM_SETCUEBANNER</c> to the given TextBox so its placeholder text
    /// is redrawn after a theme change has altered the control's background or foreground colors.
    /// Does nothing if the control handle has not yet been created or the placeholder is empty.
    /// </summary>
    internal static void RefreshCueBanner(System.Windows.Forms.TextBox textBox)
    {
        if (textBox?.IsHandleCreated == true && textBox.PlaceholderText is { Length: > 0 })
            SendMessage(textBox.Handle, EM_SETCUEBANNER, (System.IntPtr)1, textBox.PlaceholderText);
    }
}