using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CreamInstaller.Utility;

internal static class ThemeManager
{
    // VS-like dark colors
    private static readonly Color DarkBack = ColorTranslator.FromHtml("#1E1E1E");
    private static readonly Color DarkBackAlt = ColorTranslator.FromHtml("#252525");
    private static readonly Color DarkBorder = ColorTranslator.FromHtml("#3F3F46");
    private static readonly Color DarkFore = ColorTranslator.FromHtml("#D4D4D4");
    private static readonly Color DarkForeDim = ColorTranslator.FromHtml("#9CA3AF");
    private static readonly Color Accent = ColorTranslator.FromHtml("#0E639C");
    private static readonly Color DarkLink = ColorTranslator.FromHtml("#64B5F6");
    private static readonly Color LightBack = SystemColors.Control;
    private static readonly Color LightBackAlt = SystemColors.ControlLightLight;
    private static readonly Color LightFore = SystemColors.ControlText;
    private static readonly Color LightBorder = SystemColors.ControlDark;

    internal static void ToggleDarkMode(Form anyForm)
    {
        Program.DarkModeEnabled = !Program.DarkModeEnabled;
        ApplyToAllOpenForms();
    }

    internal static void Apply(Form form)
    {
        if (form is null) return;
        if (!Program.DarkModeEnabled)
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

    private static void ApplyControlTheme(Control control, bool dark)
    {
        if (control is null) return;
        foreach (Control child in control.Controls)
            ApplyControlTheme(child, dark);
        if (dark)
        {
            switch (control)
            {
                case GroupBox gb:
                    gb.ForeColor = DarkFore;
                    gb.BackColor = DarkBackAlt;
                    break;
                case Button b:
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = DarkBorder;
                    b.BackColor = DarkBackAlt;
                    b.ForeColor = DarkFore;
                    break;
                case CheckBox cb:
                    cb.BackColor = DarkBack;
                    cb.ForeColor = DarkFore;
                    break;
                case LinkLabel ll:
                    ll.BackColor = DarkBack;
                    ll.ForeColor = DarkFore;
                    ll.LinkColor = DarkLink;
                    ll.ActiveLinkColor = Color.White;
                    ll.VisitedLinkColor = DarkLink;
                    break;
                case Label lbl:
                    lbl.BackColor = DarkBack;
                    lbl.ForeColor = DarkFore;
                    break;
                case ProgressBar pb:
                    pb.ForeColor = Accent;
                    pb.BackColor = DarkBackAlt;
                    break;
                case TreeView tv:
                    tv.BackColor = DarkBackAlt;
                    tv.ForeColor = DarkFore;
                    tv.LineColor = DarkBorder;
                    break;
                case RichTextBox rtb:
                    rtb.BackColor = DarkBackAlt;
                    rtb.ForeColor = DarkFore;
                    break;
                case TableLayoutPanel tlp:
                    tlp.BackColor = DarkBack;
                    break;
                case FlowLayoutPanel flp:
                    flp.BackColor = DarkBack;
                    break;
            }
            TryApplyScrollbarTheme(control, true);
        }
        else
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
                    lbl.BackColor = LightBack;
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
                    break;
                case RichTextBox rtb:
                    rtb.BackColor = LightBack;
                    rtb.ForeColor = LightFore;
                    break;
                case TableLayoutPanel tlp:
                    tlp.BackColor = LightBack;
                    break;
                case FlowLayoutPanel flp:
                    flp.BackColor = LightBack;
                    break;
            }
            TryApplyScrollbarTheme(control, false);
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

    internal static void ApplyToAllOpenForms()
    {
        foreach (Form openForm in Application.OpenForms.Cast<Form>())
            Apply(openForm);
    }

    private static void ApplyTitleBar(Form form)
    {
        try
        {
            int useDark = Program.DarkModeEnabled ? 1 : 0;
            NativeMethods.EnableDarkTitleBar(form.Handle, useDark);
        }
        catch { }
    }

    private static void TryApplyScrollbarTheme(Control control, bool dark)
    {
        try
        {
            string theme = dark ? "DarkMode_Explorer" : null;
            NativeImports.SetWindowTheme(control.Handle, theme, null);
        }
        catch { }
    }

    // Right Click context menu styling
    internal static void ApplyContextMenu(ContextMenuStrip contextMenu)
    {
        if (contextMenu is null) return;

        bool dark = Program.DarkModeEnabled;

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

    private class DarkContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkContextMenuRenderer() : base(new DarkMenuColorTable()) { }
    }

    private class DarkMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => ColorTranslator.FromHtml("#3F3F46");
        public override Color MenuItemSelectedGradientBegin => ColorTranslator.FromHtml("#3F3F46");
        public override Color MenuItemSelectedGradientEnd => ColorTranslator.FromHtml("#3F3F46");
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
}

internal static class NativeMethods
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    internal static void EnableDarkTitleBar(System.IntPtr handle, int useDark)
    {
        _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
    }
}
