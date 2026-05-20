using System.Diagnostics;
using System.Numerics;
using Glimpse.Forms.Widgets;
using Glimpse.Library;
using Hexa.NET.ImGui;
using SDL3;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class ManageLibraryPopup : Popup
{
    private ManageLibraryWidget _widget;
    private float _refreshProgressTimer;

    public override void Open()
    {
        _widget = new ManageLibraryWidget(this);
    }

    public override void Update(float dt)
    {
        if (ImGui.OpenPopupModal("Manage Library", ScaleVec(600, 500)))
        {
            _widget.Update();
            
            if (Glimpse.Library.IsIndexing)
            {
                _refreshProgressTimer += dt;
                if (_refreshProgressTimer >= 1)
                    _refreshProgressTimer -= 1;
            }
            else
                _refreshProgressTimer = 0;
            
            if (ImGui.Button("Update"))
                Glimpse.Library.Index();
            ImGui.SetItemTooltipUnformatted("Update the music library");
            
            ImGui.SameLine();
            if (ImGui.Button("Close"))
                Close();

            if (Glimpse.Library.CurrentlyIndexedFile != null)
            {
                ImGui.SameLine();
                ImGui.Text($"Indexing {Path.GetFileName(Glimpse.Library.CurrentlyIndexedFile)}");
                ImGui.SetItemTooltipUnformatted(Glimpse.Library.CurrentlyIndexedFile);
            }
            
            ImGui.ProgressBar(-_refreshProgressTimer, ScaleVec(580, 10), "");
            ImGui.EndPopup();
        }
    }

    public override void Dispose()
    {
        _widget.Dispose();
    }
}