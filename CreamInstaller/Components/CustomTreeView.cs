using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using CreamInstaller.Forms;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Components;

internal sealed class CustomTreeView : TreeView
{
    private const string ProxyToggleString = "Proxy";
    private const string ExtraProtectionToggleString = "Extra Protection";

    private readonly Dictionary<Selection, Rectangle> checkBoxBounds = [];
    private readonly Dictionary<Selection, Rectangle> extraProtectionCheckBoxBounds = [];
    private readonly Dictionary<Selection, Rectangle> comboBoxBounds = [];

    private readonly Dictionary<TreeNode, Rectangle> selectionBounds = [];
    private SolidBrush backBrush;
    private Color lastBackColor; // Tracks the last background color

    // Selection background brush (used instead of SystemBrushes.Highlight to support dark mode)
    private SolidBrush selectionBrush;
    private Color lastSelectionBackColor;

    private ToolStripDropDown comboBoxDropDown;
    private Font comboBoxFont;
    private Form form;

    internal CustomTreeView()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        Invalidated += OnInvalidated;
        DrawNode += DrawTreeNode;
        Disposed += OnDisposed;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x203)
            m.Result = nint.Zero;
        else
            base.WndProc(ref m);
        form = FindForm();
    }

    private void OnDisposed(object sender, EventArgs e)
    {
        backBrush?.Dispose();
        backBrush = null;
        selectionBrush?.Dispose();
        selectionBrush = null;
        comboBoxFont?.Dispose();
        comboBoxFont = null;
        comboBoxDropDown?.Dispose();
        comboBoxDropDown = null;
    }

    private void OnInvalidated(object sender, EventArgs e)
    {
        checkBoxBounds.Clear();
        extraProtectionCheckBoxBounds.Clear();
        comboBoxBounds.Clear();
        selectionBounds.Clear();
        backBrush?.Dispose();
        backBrush = null;
        lastBackColor = Color.Empty;

        selectionBrush?.Dispose();
        selectionBrush = null;
        lastSelectionBackColor = Color.Empty;
    }

    private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
    {
        TreeNode node = e.Node;
        if (node is not { IsVisible: true })
        {
            e.DrawDefault = true;
            return;
        }

        bool dark = Program.DarkModeEnabled;
        bool highlighted = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected && Focused;
        Graphics graphics = e.Graphics;

        // Recreate back brush if background color changed
        if (backBrush == null || lastBackColor != BackColor)
        {
            backBrush?.Dispose();
            backBrush = new(BackColor);
            lastBackColor = BackColor;
        }

        // If highlighted, prepare a selection brush that respects the theme
        if (highlighted)
        {
            Color selColor = ThemeManager.CustomTreeViewSelectionBackColor;
            if (selectionBrush == null || lastSelectionBackColor != selColor)
            {
                selectionBrush?.Dispose();
                selectionBrush = new(selColor);
                lastSelectionBackColor = selColor;
            }
        }

        Form form = FindForm();

        if (dark && CheckBoxes)
        {
            // In dark mode we take full ownership of the row so the system never
            // gets a chance to paint a light-background checkbox.
            e.DrawDefault = false;

            // Row background
            Rectangle rowRect = new(0, node.Bounds.Top, ClientSize.Width, node.Bounds.Height);
            graphics.FillRectangle(highlighted ? selectionBrush : backBrush, rowRect);

            // Node text
            Font nodeFont = node.NodeFont ?? Font;
            Color textColor = Enabled ? ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(graphics, node.Text, nodeFont,
                new Point(node.Bounds.Left, node.Bounds.Top + 1), textColor, TextFormatFlags.Default);

            // Checkbox glyph – pure GDI so it matches the dark-themed CheckBox controls
            CheckBoxState cbState = node.Checked
                ? (Enabled ? CheckBoxState.CheckedNormal : CheckBoxState.CheckedDisabled)
                : (Enabled ? CheckBoxState.UncheckedNormal : CheckBoxState.UncheckedDisabled);
            Size cbSize = CheckBoxRenderer.GetGlyphSize(graphics, cbState);
            int cbX = node.Bounds.Left - cbSize.Width - 2;
            int cbY = node.Bounds.Top + node.Bounds.Height / 2 - cbSize.Height / 2;
            ThemeManager.DrawDarkCheckBox(graphics, new Point(cbX, cbY), cbSize, node.Checked, Enabled);

            // Expander glyph (expand/collapse) – the system skips this when DrawDefault=false
            if (node.Nodes.Count > 0)
            {
                int indent = Indent;
                int level = node.Level;
                int glyphSize = 13;
                int glyphX = level * indent + (indent - glyphSize) / 2 + (ShowRootLines ? 0 : -indent);
                int glyphY = node.Bounds.Top + node.Bounds.Height / 2 - glyphSize / 2;
                Rectangle glyphRect = new(glyphX, glyphY, glyphSize, glyphSize);
                Color glyphBorder = Color.FromArgb(0x6B, 0x6B, 0x6B);
                Color glyphBack = Color.FromArgb(0x2D, 0x2D, 0x2D);
                Color glyphFore = Color.FromArgb(0xD4, 0xD4, 0xD4);
                using (SolidBrush backFill = new(glyphBack))
                    graphics.FillRectangle(backFill, glyphRect);
                using (Pen borderPen = new(glyphBorder))
                    graphics.DrawRectangle(borderPen, glyphRect);
                int mid = glyphY + glyphSize / 2;
                int left = glyphX + 3;
                int right = glyphX + glyphSize - 3;
                using (Pen linePen = new(glyphFore))
                {
                    graphics.DrawLine(linePen, left, mid, right, mid); // horizontal minus
                    if (!node.IsExpanded)
                        graphics.DrawLine(linePen, glyphX + glyphSize / 2, glyphY + 3, glyphX + glyphSize / 2, glyphY + glyphSize - 3); // vertical plus
                }
            }
        }
        else
        {
            if (highlighted && CheckBoxes)
            {
                // In light mode, take ownership of the row when selected so the
                // highlight fills the full width (same approach as dark mode).
                e.DrawDefault = false;

                Rectangle rowRect = new(0, node.Bounds.Top, ClientSize.Width, node.Bounds.Height);
                graphics.FillRectangle(selectionBrush, rowRect);

                Font nodeFont = node.NodeFont ?? Font;
                Color textColor = Enabled ? ForeColor : SystemColors.GrayText;
                TextRenderer.DrawText(graphics, node.Text, nodeFont,
                    new Point(node.Bounds.Left, node.Bounds.Top + 1), textColor, TextFormatFlags.Default);

                CheckBoxState cbState = node.Checked
                    ? (Enabled ? CheckBoxState.CheckedNormal : CheckBoxState.CheckedDisabled)
                    : (Enabled ? CheckBoxState.UncheckedNormal : CheckBoxState.UncheckedDisabled);
                Size cbSize = CheckBoxRenderer.GetGlyphSize(graphics, cbState);
                Point cbPoint = new(node.Bounds.Left - cbSize.Width - 2,
                    node.Bounds.Top + node.Bounds.Height / 2 - cbSize.Height / 2);
                CheckBoxRenderer.DrawCheckBox(graphics, cbPoint, cbState);
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        Font font = node.NodeFont ?? Font;
        Brush brush = highlighted ? (Brush)selectionBrush : backBrush;
        Rectangle bounds = node.Bounds;
        Rectangle selectionBounds = bounds;

        if (form is not SelectForm and not SelectDialogForm)
            return;

        string id = node.Name;
        Platform platform = (node.Tag as Platform?).GetValueOrDefault(Platform.None);
        DLCType dlcType = (node.Tag as DLCType?).GetValueOrDefault(DLCType.None);
        if (string.IsNullOrWhiteSpace(id) || platform is Platform.None && dlcType is DLCType.None)
            return;

        Color color = highlighted
            ? ThemeManager.CustomTreeViewHighlightPlatformColor
            : Enabled
                ? ThemeManager.CustomTreeViewPlatformColor
                : ThemeManager.CustomTreeViewDisabledPlatformColor;
        string text;
        if (dlcType is not DLCType.None)
        {
            SelectionDLC dlc = SelectionDLC.FromId(dlcType, node.Parent?.Name, id);
            text = dlc?.Selection != null ? dlc.Selection.Platform.ToString() : dlcType.ToString();
        }
        else text = platform.ToString();

        Size size = TextRenderer.MeasureText(graphics, text, font);
        bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
        selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
        graphics.FillRectangle(brush, bounds);
        Point point = new(bounds.Location.X - 1, bounds.Location.Y + 1);
        TextRenderer.DrawText(graphics, text, font, point, color, TextFormatFlags.Default);

        if (platform is not Platform.Paradox)
        {
            color = highlighted
                ? ThemeManager.CustomTreeViewHighlightIdColor
                : Enabled
                    ? ThemeManager.CustomTreeViewIdColor
                    : ThemeManager.CustomTreeViewDisabledIdColor;
            text = id;
            size = TextRenderer.MeasureText(graphics, text, font);
            const int left = -4;
            bounds = bounds with { X = bounds.X + bounds.Width + left, Width = size.Width };
            selectionBounds = new(selectionBounds.Location,
                selectionBounds.Size + new Size(bounds.Size.Width + left, 0));
            graphics.FillRectangle(brush, bounds);
            point = new(bounds.Location.X - 1, bounds.Location.Y + 1);
            TextRenderer.DrawText(graphics, text, font, point, color, TextFormatFlags.Default);
        }

        if (form is SelectForm)
        {
            Selection selection = Selection.FromId(platform, id);
            if (selection is not null)
            {
                if (bounds == node.Bounds)
                {
                    size = new(4, 0);
                    bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
                    graphics.FillRectangle(brush, bounds);
                }

                if (!Program.UseSmokeAPI)
                {
                    CheckBoxState extraProtState = selection.UseExtraProtection
                        ? (Enabled ? CheckBoxState.CheckedNormal : CheckBoxState.CheckedDisabled)
                        : (Enabled ? CheckBoxState.UncheckedNormal : CheckBoxState.UncheckedDisabled);
                    size = CheckBoxRenderer.GetGlyphSize(graphics, extraProtState);
                    bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
                    selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
                    Rectangle extraProtCheckBoxBounds = bounds;
                    graphics.FillRectangle(brush, bounds);
                    point = new(bounds.Left, bounds.Top + bounds.Height / 2 - size.Height / 2 - 1);
                    if (dark)
                        ThemeManager.DrawDarkCheckBox(graphics, point, size, selection.UseExtraProtection, Enabled);
                    else
                        CheckBoxRenderer.DrawCheckBox(graphics, point, extraProtState);

                    text = ExtraProtectionToggleString;
                    size = TextRenderer.MeasureText(graphics, text, font);
                    int leftEP = 1;
                    bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width + leftEP };
                    selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
                    extraProtCheckBoxBounds = new(extraProtCheckBoxBounds.Location, extraProtCheckBoxBounds.Size + bounds.Size with { Height = 0 });
                    graphics.FillRectangle(brush, bounds);
                    point = new(bounds.Location.X - 1 + leftEP, bounds.Location.Y + 1);
                    TextRenderer.DrawText(graphics, text, font, point,
                        Enabled ? ThemeManager.CustomTreeViewProxyColor : ThemeManager.CustomTreeViewDisabledProxyColor,
                        TextFormatFlags.Default);

                    extraProtectionCheckBoxBounds[selection] = RectangleToClient(extraProtCheckBoxBounds);

                    // Add spacing before proxy checkbox
                    size = new(4, 0);
                    bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
                    graphics.FillRectangle(brush, bounds);
                }

                CheckBoxState proxyState = selection.UseProxy
                    ? (Enabled ? CheckBoxState.CheckedNormal : CheckBoxState.CheckedDisabled)
                    : (Enabled ? CheckBoxState.UncheckedNormal : CheckBoxState.UncheckedDisabled);
                size = CheckBoxRenderer.GetGlyphSize(graphics, proxyState);
                bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width };
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
                Rectangle checkBoxBounds = bounds;
                graphics.FillRectangle(brush, bounds);
                point = new(bounds.Left, bounds.Top + bounds.Height / 2 - size.Height / 2 - 1);
                if (dark)
                    ThemeManager.DrawDarkCheckBox(graphics, point, size, selection.UseProxy, Enabled);
                else
                    CheckBoxRenderer.DrawCheckBox(graphics, point, proxyState);

                text = ProxyToggleString;
                size = TextRenderer.MeasureText(graphics, text, font);
                int left = 1;
                bounds = bounds with { X = bounds.X + bounds.Width, Width = size.Width + left };
                selectionBounds = new(selectionBounds.Location, selectionBounds.Size + bounds.Size with { Height = 0 });
                checkBoxBounds = new(checkBoxBounds.Location, checkBoxBounds.Size + bounds.Size with { Height = 0 });
                graphics.FillRectangle(brush, bounds);
                point = new(bounds.Location.X - 1 + left, bounds.Location.Y + 1);
                TextRenderer.DrawText(graphics, text, font, point,
                    Enabled ? ThemeManager.CustomTreeViewProxyColor : ThemeManager.CustomTreeViewDisabledProxyColor,
                    TextFormatFlags.Default);

                this.checkBoxBounds[selection] = RectangleToClient(checkBoxBounds);

                if (selection.UseProxy)
                {
                    comboBoxFont ??= new(font.FontFamily, 6, font.Style, font.Unit, font.GdiCharSet,
                        font.GdiVerticalFont);

                    bool darkMode = Program.DarkModeEnabled;
                    Color comboBackColor = ThemeManager.CustomTreeViewComboBackColor;
                    Color comboBorderColor = ThemeManager.CustomTreeViewComboBorderColor;
                    Color comboTextColor = ThemeManager.CustomTreeViewComboTextColor;

                    text = (selection.Proxy ?? Selection.DefaultProxy) + ".dll";
                    size = TextRenderer.MeasureText(graphics, text, comboBoxFont) + new Size(6, 0);
                    const int padding = 2;
                    bounds = new(bounds.X + bounds.Width, bounds.Y + padding / 2, size.Width, bounds.Height - padding);
                    selectionBounds = new(selectionBounds.Location,
                        selectionBounds.Size + bounds.Size with { Height = 0 });
                    Rectangle comboBoxBounds = bounds;

                    // Themed combobox background + text (centralized in ThemeManager)
                    ThemeManager.DrawCustomComboBox(graphics, bounds, comboBoxFont, text);

                    size = new(14, 0);
                    left = -1;
                    bounds = bounds with { X = bounds.X + bounds.Width + left, Width = size.Width };
                    selectionBounds = new(selectionBounds.Location,
                        selectionBounds.Size + new Size(bounds.Size.Width + left, 0));
                    comboBoxBounds = new(comboBoxBounds.Location,
                        comboBoxBounds.Size + new Size(bounds.Size.Width + left, 0));

                    // Themed combobox dropdown button (centralized in ThemeManager)
                    ThemeManager.DrawCustomComboBoxButton(graphics, bounds);

                    this.comboBoxBounds[selection] = RectangleToClient(comboBoxBounds);
                }
                else
                    _ = comboBoxBounds.Remove(selection);
            }
        }

        this.selectionBounds[node] = RectangleToClient(selectionBounds);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Refresh();
        Point clickPoint = PointToClient(e.Location);
        SelectForm selectForm = (form ??= FindForm()) as SelectForm;
        foreach (KeyValuePair<TreeNode, Rectangle> pair in selectionBounds)
            if (pair.Key.TreeView is null)
                _ = selectionBounds.Remove(pair.Key);
            else if (pair.Key.IsVisible && pair.Value.Contains(clickPoint))
            {
                SelectedNode = pair.Key;
                if (e.Button is MouseButtons.Right && selectForm is not null)
                    selectForm.OnNodeRightClick(pair.Key, e.Location);
                break;
            }

        if (e.Button is not MouseButtons.Left || !ComboBoxRenderer.IsSupported)
            return;

        if (comboBoxBounds.Count > 0 && selectForm is not null)
            foreach (KeyValuePair<Selection, Rectangle> pair in comboBoxBounds)
                if (!Selection.All.ContainsKey(pair.Key))
                    _ = comboBoxBounds.Remove(pair.Key);
                else if (pair.Value.Contains(clickPoint))
                {
                    IEnumerable<string> proxies = pair.Key.GetAvailableProxies();
                    comboBoxDropDown ??= new();
                    comboBoxDropDown.ShowItemToolTips = false;
                    comboBoxDropDown.Items.Clear();

                    foreach (string proxy in proxies)
                    {
                        bool canUse = true;
                        foreach ((string directory, BinaryType _) in pair.Key.ExecutableDirectories)
                        {
                            string path = directory + @"\" + proxy + ".dll";
                            if (!path.FileExists() || path.IsResourceFile(ResourceIdentifier.Koaloader) ||
                                path.IsResourceFile(ResourceIdentifier.Steamworks32) ||
                                path.IsResourceFile(ResourceIdentifier.Steamworks64))
                                continue;
                            canUse = false;
                            break;
                        }

                        if (canUse)
                        {
                            ToolStripMenuItem menuItem = new(proxy + ".dll", null, (_, _) =>
                            {
                                pair.Key.Proxy = proxy == Selection.DefaultProxy ? null : proxy;
                                selectForm.OnProxyChanged();
                            })
                            {
                                Font = comboBoxFont
                            };
                            _ = comboBoxDropDown.Items.Add(menuItem);
                        }
                    }

                    // Apply theme using ThemeManager
                    ThemeManager.ApplyToolStripDropDown(comboBoxDropDown);

                    comboBoxDropDown.Show(this, PointToScreen(new(pair.Value.Left, pair.Value.Bottom - 1)));
                    break;
                }

        foreach (KeyValuePair<Selection, Rectangle> pair in checkBoxBounds)
            if (!Selection.All.ContainsKey(pair.Key))
                _ = checkBoxBounds.Remove(pair.Key);
            else if (pair.Value.Contains(clickPoint))
            {
                pair.Key.UseProxy = !pair.Key.UseProxy;
                selectForm?.OnProxyChanged();
                break;
            }

        foreach (KeyValuePair<Selection, Rectangle> pair in extraProtectionCheckBoxBounds)
            if (!Selection.All.ContainsKey(pair.Key))
                _ = extraProtectionCheckBoxBounds.Remove(pair.Key);
            else if (pair.Value.Contains(clickPoint))
            {
                pair.Key.UseExtraProtection = !pair.Key.UseExtraProtection;
                selectForm?.OnExtraProtectionChanged();
                break;
            }
    }
}