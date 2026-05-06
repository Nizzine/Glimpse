using System.Numerics;
using Hexa.NET.ImGui;
using SDL3;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class ManageLibraryPopup : Popup
{
    private readonly SDL.DialogFileCallback _folderCallback;
    
    private Image _plus = null!;
    private Image _refresh = null!;

    private float _refreshFlipTimer;
    private bool _flipRefresh;

    private bool _needsRefresh;

    private IReadOnlyCollection<string> _libraryPaths = null!;

    private string? _selectedLibrary;

    public ManageLibraryPopup()
    {
        _folderCallback = FolderCallback;
    }

    public override void Open()
    {
        _plus = Renderer.CreateImage("Icons.Plus.png");
        _refresh = Renderer.CreateImage("Icons.Update.png");

        _needsRefresh = true;
    }

    public override void Update(float dt)
    {
        if (_needsRefresh)
            _libraryPaths = Glimpse.Database.GetLibraryPaths();
        
        string popupName = "Manage Library";
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);

        ImGui.SetNextWindowSize(new Vector2(600, 500));
        if (ImGui.BeginPopupModal(popupName))
        {
            ImGui.BeginDisabled(Glimpse.Database.IsIndexing);
            
            ImGui.BeginChild("PathsList", ScaleVec(400, 400), ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.HorizontalScrollbar);
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
                if (ImGui.ImageButton("AddNewFolders", _plus, ScaleVec(16)))
                {
                    string? defaultLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                    if (string.IsNullOrWhiteSpace(defaultLocation))
                        defaultLocation = null;

                    SDL.ShowOpenFolderDialog(_folderCallback, 0, Glimpse.MainWindow.Handle, defaultLocation, true);
                }
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

                if (ImGui.Button("Remove"))
                {
                    Glimpse.Database.RemoveLibaryPath(_selectedLibrary);
                    _needsRefresh = true;
                }
                
                ImGui.EndDisabled();
                
                ImGui.EndChild();
            }
            
            ImGui.EndDisabled();
            
            if (ImGui.Button("Close"))
                Close();

            ImGui.EndPopup();
        }
    }

    private unsafe void FolderCallback(IntPtr userdata, IntPtr filelist, int filter)
    {
        sbyte** fileList = (sbyte**) filelist;
        int index = 0;
        while (fileList[index] != null)
        {
            string directory = new string(fileList[index]);
            Glimpse.Database.AddLibraryPath(directory);
            _needsRefresh = true;
            index++;
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
        public Dictionary<string, LibraryDirectory> SubDirectories;

        public LibraryDirectory(string name)
        {
            DirectoryName = name;
            SubDirectories = [];
        }
    }
}