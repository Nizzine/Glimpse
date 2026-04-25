using System.Drawing;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class FileNotFoundPopup(string songName) : Popup
{
    public override void Update()
    {
        string popupName = Glimpse.Locale.GetString("Popup.FileNotFound.Name");
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);
        
        if (ImGui.BeginPopupModal(popupName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text(Glimpse.Locale.GetString("Popup.FileNotFound.Text", songName));
            
            if (ImGui.Button(Glimpse.Locale.GetString("Button.Ok")))
                Close();
            
            ImGui.EndPopup();
        }
    }
}