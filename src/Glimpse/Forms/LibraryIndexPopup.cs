using System.Numerics;
using Glimpse.Graphics;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class LibraryIndexPopup : Popup
{
    private Image _plus;
    private Image _refresh;

    private float _refreshFlipTimer;
    private bool _flipRefresh;

    private IReadOnlyCollection<string> _libraryPaths;

    private string? _selectedLibrary;

    public override void Open()
    {
        _plus = Renderer.CreateImage("Assets/Icons/Plus.png");
        _refresh = Renderer.CreateImage("Assets/Icons/Update.png");

        _libraryPaths = Glimpse.Database.GetLibraryPaths();
    }

    public override void Update(float dt)
    {
        string popupName = "Library";
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);

        ImGui.SetNextWindowSize(new Vector2(600, 500));
        if (ImGui.BeginPopupModal(popupName))
        {
            ImGui.BeginDisabled(Glimpse.Database.IsIndexing);
            
            ImGui.BeginChild("PathsList", ScaleVec(400, 400));
            {
                foreach (string path in _libraryPaths)
                {
                    if (ImGui.Selectable(path, path == _selectedLibrary))
                        _selectedLibrary = path;
                }
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            ImGui.BeginChild("Settings", ImGuiChildFlags.AutoResizeY);
            {
                ImGui.ImageButton("AddNewFolders", _plus, ScaleVec(16));
                ImGui.SetItemTooltipUnformatted("Add Folders to Library");
                ImGui.SameLine();

                Vector2 uv0 = new Vector2(0, 0);
                Vector2 uv1 = new Vector2(1, 1);

                if (Glimpse.Database.IsIndexing)
                {
                    const float flipTime = 0.35f;
                    _refreshFlipTimer += dt;
                    if (_refreshFlipTimer >= flipTime)
                    {
                        _refreshFlipTimer -= flipTime;
                        _flipRefresh = !_flipRefresh;
                    }
                    
                    if (_flipRefresh)
                    {
                        uv0.Y = 1;
                        uv1.Y = 0;
                    }
                }

                if (ImGui.ImageButton("RefreshLibrary", _refresh, ScaleVec(16), uv0, uv1))
                    Glimpse.Database.Index();
                ImGui.SetItemTooltipUnformatted("Refresh Library");

                ImGui.Separator();
                
                ImGui.BeginDisabled(_selectedLibrary == null);
                
                ImGui.Button("Remove");
                
                ImGui.EndDisabled();
                
                ImGui.EndChild();
            }
            
            ImGui.EndDisabled();
            
            if (ImGui.Button("Close"))
                Close();

            ImGui.EndPopup();
        }
    }

    public override void Dispose()
    {
        _refresh.Dispose();
        _plus.Dispose();
    }

    private struct LibraryDirectory
    {
        public string DirectoryName;

        public List<LibraryDirectory> SubDirectories;
    }
}