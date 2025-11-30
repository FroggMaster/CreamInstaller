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
 private static readonly Color AccentHover = ColorTranslator.FromHtml("#1177BB");
 private static readonly Color DarkLink = ColorTranslator.FromHtml("#64B5F6");
 private static readonly Color LightBack = SystemColors.Control;
 private static readonly Color LightBackAlt = SystemColors.ControlLightLight;
 private static readonly Color LightFore = SystemColors.ControlText;
 private static readonly Color LightBorder = SystemColors.ControlDark;
 private static readonly Color LightAccent = ColorTranslator.FromHtml("#0078D4");

 /// <summary>
 /// Apply accent styling to a primary action button.
 /// </summary>
 internal static void ApplyAccentButton(Button button)
 {
     if (button is null) return;
     button.FlatStyle = FlatStyle.Flat;
     button.FlatAppearance.BorderSize = 0;
     if (Program.DarkModeEnabled)
     {
         button.BackColor = Accent;
         button.ForeColor = Color.White;
         button.FlatAppearance.MouseOverBackColor = AccentHover;
         button.FlatAppearance.MouseDownBackColor = AccentHover;
     }
     else
     {
         button.BackColor = LightAccent;
         button.ForeColor = Color.White;
         button.FlatAppearance.MouseOverBackColor = Accent;
         button.FlatAppearance.MouseDownBackColor = Accent;
     }
 }

 /// <summary>
 /// Apply secondary/danger styling to a button.
 /// </summary>
 internal static void ApplySecondaryButton(Button button)
 {
     if (button is null) return;
     button.FlatStyle = FlatStyle.Flat;
     if (Program.DarkModeEnabled)
     {
         button.BackColor = DarkBackAlt;
         button.ForeColor = DarkFore;
         button.FlatAppearance.BorderColor = DarkBorder;
     }
     else
     {
         button.FlatStyle = FlatStyle.Standard;
         button.BackColor = LightBack;
         button.ForeColor = LightFore;
     }
 }

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
 ll.ForeColor = DarkFore; // normal text
 ll.LinkColor = DarkLink;
 ll.ActiveLinkColor = Color.White; // high contrast when pressed
 ll.VisitedLinkColor = DarkLink; // keep consistent
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
 case StatusStrip ss:
 ss.BackColor = DarkBackAlt;
 ss.ForeColor = DarkFore;
 foreach (ToolStripItem item in ss.Items)
 {
     item.BackColor = DarkBackAlt;
     item.ForeColor = DarkFore;
 }
 break;
 }
 TryApplyScrollbarTheme(control, true);
 }
 else
 {
 // Light reset per control type
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
 // allow system defaults for link colors
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
 case StatusStrip ss:
 ss.BackColor = LightBack;
 ss.ForeColor = LightFore;
 foreach (ToolStripItem item in ss.Items)
 {
     item.BackColor = LightBack;
     item.ForeColor = LightFore;
 }
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

 private static void ApplyToAllOpenForms()
 {
 foreach (Form openForm in Application.OpenForms.Cast<Form>())
 Apply(openForm);
 }

 private static void ApplyTitleBar(Form form)
 {
 try
 {
 int useDark = Program.DarkModeEnabled ?1 :0;
 NativeMethods.EnableDarkTitleBar(form.Handle, useDark);
 }
 catch { }
 }

 private static void TryApplyScrollbarTheme(Control control, bool dark)
 {
 // RichTextBox & TreeView host scrollbars internally; use window theme API
 try
 {
 string theme = dark ? "DarkMode_Explorer" : null; // reset with null
 NativeImports.SetWindowTheme(control.Handle, theme, null);
 }
 catch { }
 }

 /// <summary>
 /// Apply theme to a context menu strip.
 /// </summary>
 internal static void ApplyContextMenu(ContextMenuStrip menu)
 {
     if (menu is null) return;

     if (Program.DarkModeEnabled)
     {
         menu.BackColor = DarkBackAlt;
         menu.ForeColor = DarkFore;
         menu.Renderer = new DarkMenuRenderer();
         foreach (ToolStripItem item in menu.Items)
             ApplyToolStripItem(item, true);
     }
     else
     {
         menu.BackColor = LightBack;
         menu.ForeColor = LightFore;
         menu.Renderer = new ToolStripProfessionalRenderer();
         foreach (ToolStripItem item in menu.Items)
             ApplyToolStripItem(item, false);
     }
 }

 private static void ApplyToolStripItem(ToolStripItem item, bool dark)
 {
     if (dark)
     {
         item.BackColor = DarkBackAlt;
         item.ForeColor = DarkFore;
     }
     else
     {
         item.BackColor = LightBack;
         item.ForeColor = LightFore;
     }

     if (item is ToolStripMenuItem menuItem)
         foreach (ToolStripItem subItem in menuItem.DropDownItems)
             ApplyToolStripItem(subItem, dark);
 }

 /// <summary>
 /// Custom renderer for dark mode context menus.
 /// </summary>
 private class DarkMenuRenderer : ToolStripProfessionalRenderer
 {
     public DarkMenuRenderer() : base(new DarkMenuColors()) { }
 }

 private class DarkMenuColors : ProfessionalColorTable
 {
     private static readonly Color DarkMenuBack = ColorTranslator.FromHtml("#252525");
     private static readonly Color DarkMenuBorder = ColorTranslator.FromHtml("#3F3F46");
     private static readonly Color DarkMenuHighlight = ColorTranslator.FromHtml("#3F3F46");
     private static readonly Color DarkMenuHighlightBorder = ColorTranslator.FromHtml("#0E639C");

     public override Color MenuItemSelected => DarkMenuHighlight;
     public override Color MenuItemSelectedGradientBegin => DarkMenuHighlight;
     public override Color MenuItemSelectedGradientEnd => DarkMenuHighlight;
     public override Color MenuItemBorder => DarkMenuHighlightBorder;
     public override Color MenuBorder => DarkMenuBorder;
     public override Color ToolStripDropDownBackground => DarkMenuBack;
     public override Color ImageMarginGradientBegin => DarkMenuBack;
     public override Color ImageMarginGradientMiddle => DarkMenuBack;
     public override Color ImageMarginGradientEnd => DarkMenuBack;
     public override Color SeparatorDark => DarkMenuBorder;
     public override Color SeparatorLight => DarkMenuBorder;
 }
}

internal static class NativeMethods
{
 private const int DWMWA_USE_IMMERSIVE_DARK_MODE =20;

 [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
 private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

 internal static void EnableDarkTitleBar(System.IntPtr handle, int useDark)
 {
 _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
 }
}
