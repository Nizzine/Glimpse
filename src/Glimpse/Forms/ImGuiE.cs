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
}