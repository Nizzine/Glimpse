using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class NewPlaylistPopup : Popup
{
    private readonly Action<string> _onCreate;
    private string _text = "";

    public NewPlaylistPopup(Action<string> onCreate)
    {
        _onCreate = onCreate;
    }

    protected override void Update(float dt)
    {
        Locale locale = Glimpse.Locale;

        if (ImGui.OpenPopupModal(locale.GetString("Popup.NewPlaylist.Name")))
        {
            ImGui.InputTextWithHint("##PlaylistName", locale.GetString("Popup.NewPlaylist.TextBoxHint"), ref _text, 1000);

            if (ImGui.Button(locale.GetString("Button.Create")))
            {
                _onCreate(_text);
                Close();
            }

            ImGui.SameLine();

            if (ImGui.Button(locale.GetString("Button.Cancel")))
                Close();

            ImGui.EndPopup();
        }
    }
}