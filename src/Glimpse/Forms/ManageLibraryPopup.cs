using System.Diagnostics;
using System.Numerics;
using Glimpse.Library;
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

    private float _refreshProgressTimer;

    private bool _needsRefresh;

    private List<LibraryDirectory> _libraryPaths = null!;

    private string? _selectedLibrary;

    public ManageLibraryPopup()
    {
        _folderCallback = FolderCallback;
    }

    public override void Open()
    {
        _plus = Renderer.CreateImage("asset://Icons.Plus.png");
        _minus = Renderer.CreateImage("asset://Icons.Minus.png");
        _refresh = Renderer.CreateImage("asset://Icons.Update.png");

        _libraryPaths = [];
        _needsRefresh = true;
    }

    public override void Update(float dt)
    {
        if (_needsRefresh)
            RefreshLibrary();
        
        if (ImGui.OpenPopupModal("Manage Library", ScaleVec(600, 500)))
        {
            ImGui.BeginDisabled(Glimpse.Library.IsIndexing);

            if (ImGui.BeginListBox("##Libraries", ScaleVec(400, 415)))
            {
                foreach (LibraryDirectory dir in _libraryPaths)
                    dir.Update(Glimpse.Library, ref _selectedLibrary);
                
                ImGui.EndListBox();
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
                    Glimpse.Library.RemoveLibaryPath(_selectedLibrary);
                    _selectedLibrary = null;
                    _needsRefresh = true;
                }
                ImGui.EndDisabled();
                
                ImGui.SetItemTooltipUnformatted("Remove selected folder from library");
                ImGui.SameLine();

                if (ImGui.ImageButton("Refresh", _refresh, ScaleVec(16)))
                    _needsRefresh = true;
                ImGui.SetItemTooltipUnformatted("Refresh");

                ImGui.Separator();
                
                if (ImGui.Button("Remove All"))
                {
                    Glimpse.Library.RemoveAllLibraryPaths();
                }

                bool bFalse = false;
                
                /*ImGui.Checkbox("Refresh on launch", ref bFalse);
                ImGui.SetItemTooltipUnformatted("Refresh the music library when Glimpse is launched.");
                ImGui.Checkbox("Auto remove deleted files", ref bFalse);
                ImGui.SetItemTooltipUnformatted("Automatically remove tracks that no longer exist on the filesystem.");*/
                
                ImGui.EndChild();
            }

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
            
            ImGui.EndDisabled();
            
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

    private unsafe void FolderCallback(IntPtr userdata, IntPtr filelist, int filter)
    {
        sbyte** fileList = (sbyte**) filelist;
        int index = 0;
        while (fileList[index] != null)
        {
            string directory = new string(fileList[index]);
            Glimpse.Library.AddLibraryPath(directory);
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

        foreach (string path in Glimpse.Library.LibraryPaths)
            _libraryPaths.Add(new LibraryDirectory(path, true, true));
    }

    private class LibraryDirectory
    {
        private bool _exists;
        
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
            _exists = true;
        }

        public unsafe void Update(MusicLibrary library, ref string selectedDirectory)
        {
            if (SubDirectories == null && _exists)
            {
                if (Directory.Exists(Path))
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
                        if (library.ExcludedDirectories.Contains(directory))
                            enabled = false;

                        SubDirectories.Add(directory, new LibraryDirectory(directory, false, enabled));
                    }
                }
                else
                    _exists = false;
            }
            
            string pathName = IsBaseDir ? Path : System.IO.Path.GetFileName(Path);

            if (!_exists)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                ImGui.TreeNodeEx(pathName,
                    ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen |
                    (Path == selectedDirectory ? ImGuiTreeNodeFlags.Selected : 0));
                if (ImGui.IsItemClicked())
                    selectedDirectory = Path;

                ImGui.PopStyleColor();
                ImGui.SetItemTooltipUnformatted("Path could not be found. Was it moved/deleted?");
                return;
            }

            if (!Enabled)
                ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));
            
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.DrawLinesFull;
            if (Path == selectedDirectory)
                flags |= ImGuiTreeNodeFlags.Selected;
            
            bool treeNodeOpened = ImGui.TreeNodeEx(pathName, flags);
            if (ImGui.IsItemClicked())
                selectedDirectory = Path;
            if (treeNodeOpened)
            {
                foreach ((_, LibraryDirectory directory) in SubDirectories)
                    directory.Update(library, ref selectedDirectory);
                
                ImGui.TreePop();
            }

            if (!Enabled)
                ImGui.PopStyleColor();
        }
    }
}