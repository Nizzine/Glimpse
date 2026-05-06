using System.Numerics;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

/// <summary>
/// ImGui Extensions
/// </summary>
public static class ImGuiE
{
    extension(ImGui)
    {
        public static void SetItemTooltipUnformatted(string text)
        {
            if (ImGui.BeginItemTooltip())
            {
                ImGui.TextUnformatted(text);
                ImGui.End();
            }
        }

        public static bool BeginTabItemTooltip(string name, string tooltip, bool select = false)
        {
            bool open = ImGui.BeginTabItem(name, select ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None);
            ImGui.SetItemTooltipUnformatted(tooltip);
            return open;
        }

        public static void SetColumnTooltip(string text)
        {
            Vector2 textSize = ImGui.CalcTextSize(text);
            float width = ImGui.GetColumnWidth(-1);
            if (textSize.X > width)
                SetItemTooltipUnformatted(text);
        }

        public static bool SelectButton(string name, ImTextureRef image, Vector2 size, bool selected)
        {
            if (!selected)
                ImGui.PushStyleColor(ImGuiCol.Button, 0);

            bool pressed = ImGui.ImageButton(name, image, size);

            if (!selected)
                ImGui.PopStyleColor();

            return pressed;
        }

        public static bool TextButton(string text)
        {
            ImGui.TextUnformatted(text);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    return true;
            }

            return false;
        }

        public static bool ColorEdit4(string label, ref uint color, string tooltip)
        {
            Vector4 vecColor = Theme.UintToVector4(color);
            bool isEdited = ImGui.ColorEdit4(label, ref vecColor, ImGuiColorEditFlags.AlphaBar);
            ImGui.SetItemTooltipUnformatted(tooltip);
            if (isEdited)
                color = Theme.Vector4ToUint(vecColor);

            return isEdited;
        }
    }
}