using System.Drawing;
using System.Windows.Forms;

namespace CreamInstaller.Components;

/// <summary>
/// Button used for the primary action of a dialog. Uses a pastel lavender gradient so
/// it stands out from the regular rounded buttons.
/// </summary>
internal sealed class PrimaryButton : RoundedButton
{
    private static readonly Color NormalTop = ColorTranslator.FromHtml("#C8B3EE");
    private static readonly Color NormalBottom = ColorTranslator.FromHtml("#AF94E3");
    private static readonly Color NormalBorder = ColorTranslator.FromHtml("#9879CF");
    private static readonly Color NormalText = ColorTranslator.FromHtml("#291D40");

    private static readonly Color HoverTop = ColorTranslator.FromHtml("#D3C2F4");
    private static readonly Color HoverBottom = ColorTranslator.FromHtml("#BDA6E9");
    private static readonly Color HoverBorder = ColorTranslator.FromHtml("#A68BD8");

    private static readonly Color PressedTop = ColorTranslator.FromHtml("#BBA4E9");
    private static readonly Color PressedBottom = ColorTranslator.FromHtml("#A68ADF");
    private static readonly Color PressedBorder = ColorTranslator.FromHtml("#8B6EC4");

    internal PrimaryButton()
    {
        Font = new Font(Font, FontStyle.Bold);
    }

    protected override (Color top, Color bottom, Color border, Color text) GetPalette(VisualState state)
        => state switch
        {
            VisualState.Disabled => base.GetPalette(state),
            VisualState.Pressed => (PressedTop, PressedBottom, PressedBorder, NormalText),
            VisualState.Hover => (HoverTop, HoverBottom, HoverBorder, NormalText),
            _ => (NormalTop, NormalBottom, NormalBorder, NormalText)
        };
}
