using System.Numerics;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

/// <summary>
/// ImGui Extensions
/// </summary>
public static class ImGuiE
{
    public static void SetItemTooltipUnformatted(string text)
    {
        if (ImGui.BeginItemTooltip())
        {
            ImGui.TextUnformatted(text);
            ImGui.End();
        }
    }

    public static bool BeginTabItemTooltip(string name, string tooltip)
    {
        bool open = ImGui.BeginTabItem(name);
        SetItemTooltipUnformatted(tooltip);
        return open;
    }

    public static void SetColumnTooltip(string text)
    {
        Vector2 textSize = ImGui.CalcTextSize(text);
        float width = ImGui.GetColumnWidth(-1);
        if (textSize.X > width)
            SetItemTooltipUnformatted(text);
    }
}