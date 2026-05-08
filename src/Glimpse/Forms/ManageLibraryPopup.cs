using System.Numerics;
using Glimpse.Database;
using Hexa.NET.ImGui;
using SDL3;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class ManageLibraryPopup : Popup
{
    private readonly SDL.DialogFileCallback _folderCallback;
    
    private Image _plus = null!;
    private Image _minus = null!;
    private Image _refresh = null!;

    private float _refreshFlipTimer;
    private bool _flipRefresh;

    private bool _needsRefresh;

    private List<LibraryDirectory> _libraryPaths = null!;

    private string? _selectedLibrary;

    public ManageLibraryPopup()
    {
        _folderCallback = FolderCallback;
    }

    public override void Open()
    {
        _plus = Renderer.CreateImage("Icons.Plus.png");
        _minus = Renderer.CreateImage("Icons.Minus.png");
        _refresh = Renderer.CreateImage("Icons.Update.png");

        _libraryPaths = [];
        _needsRefresh = true;
    }

    public override void Update(float dt)
    {
        if (_needsRefresh)
            RefreshLibrary();
        
        string popupName = "Manage Library";
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);

        ImGui.SetNextWindowSize(ScaleVec(600, 500));
        if (ImGui.BeginPopupModal(popupName))
        {
            ImGui.BeginDisabled(Glimpse.Database.IsIndexing);
            
            ImGui.BeginChild("PathsList", ScaleVec(400, 400), ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.HorizontalScrollbar);
            {
                foreach (LibraryDirectory dir in _libraryPaths)
                    dir.Update(Glimpse.Database, ref _selectedLibrary);
                
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
                
                ImGui.BeginDisabled(_selectedLibrary == null);
                if (ImGui.ImageButton("Remove", _minus, ScaleVec(16)))
                {
                    Glimpse.Database.RemoveLibaryPath(_selectedLibrary);
                    _selectedLibrary = null;
                    _needsRefresh = true;
                }
                ImGui.EndDisabled();
                
                ImGui.SetItemTooltipUnformatted("Remove selected folder from library");
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
                
                if (ImGui.Button("Remove All"))
                {
                    Glimpse.Database.RemoveAllLibraryPaths();
                }

                bool bFalse = false;
                
                /*ImGui.Checkbox("Refresh on launch", ref bFalse);
                ImGui.SetItemTooltipUnformatted("Refresh the music library when Glimpse is launched.");
                ImGui.Checkbox("Auto remove deleted files", ref bFalse);
                ImGui.SetItemTooltipUnformatted("Automatically remove tracks that no longer exist on the filesystem.");*/
                
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

    private void RefreshLibrary()
    {
        _libraryPaths = [];

        foreach (string path in Glimpse.Database.LibraryPaths)
            _libraryPaths.Add(new LibraryDirectory(path, true, true));
    }

    private class LibraryDirectory
    {
        /// <summary>
        /// The path of the directory.
        /// </summary>
        public string Path;

        /// <summary>
        /// If the directory is a base library path. The full directory path will only be displayed if true.
        /// </summary>
        public bool IsBaseDir;
        
        /// <summary>
        /// Whether the directory is enabled. If disabled, the directory will not be indexed.
        /// </summary>
        public bool Enabled;
        
        /// <summary>
        /// Subdirectories contained in this directory.
        /// </summary>
        public Dictionary<string, LibraryDirectory>? SubDirectories;

        public LibraryDirectory(string path, bool isBaseDir, bool enabled)
        {
            Path = path;
            IsBaseDir = isBaseDir;
            Enabled = enabled;
        }

        public unsafe void Update(MusicDatabase database, ref string selectedDirectory)
        {
            if (SubDirectories == null)
            {
                SubDirectories = [];
                EnumerationOptions options = new()
                {
                    IgnoreInaccessible = true
                };
                foreach (string directory in Directory.GetDirectories(Path, "*", options))
                {
                    bool enabled = true;
                    // TODO: Expose the hash set.
                    if (database.ExcludedDirectories.Contains(directory))
                        enabled = false;
                    
                    SubDirectories.Add(directory, new LibraryDirectory(directory, false, enabled));
                }
            }
            
            string pathName = IsBaseDir ? Path : System.IO.Path.GetFileName(Path);
            
            if (!Enabled)
                ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));
            
            bool treeNodeOpened = ImGui.TreeNodeEx(pathName, ImGuiTreeNodeFlags.OpenOnArrow | (Path == selectedDirectory ? ImGuiTreeNodeFlags.Selected : 0));
            if (ImGui.IsItemClicked())
                selectedDirectory = Path;
            if (treeNodeOpened)
            {
                foreach ((_, LibraryDirectory directory) in SubDirectories)
                    directory.Update(database, ref selectedDirectory);
                
                ImGui.TreePop();
            }
            
            if (!Enabled)
                ImGui.PopStyleColor();
        }
    }
}