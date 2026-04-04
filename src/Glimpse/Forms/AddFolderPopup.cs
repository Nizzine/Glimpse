using Glimpse.Configs;
using Glimpse.Database;
using Glimpse.Locales;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class AddFolderPopup : Popup
{
    private DirectorySource _baseDirectory;
    private Task _currentTask;
    private string _currentFile;
    private object _lockObj;

    private IndexResult _result;

    public string Selected;
    
    public override void Update()
    {
        Locale locale = Glimpse.Locale;
        string popupName = locale.GetString("Popup.AddDirs.Name");
        
        if (!ImGui.IsPopupOpen(popupName))
        {
            _baseDirectory = new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            {
                SubDirectories = 
                [
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
                ]
            };

            // Deduplicate the drives. For some reason Linux systems report multiple drives in the same location.
            HashSet<string> drives = [];
            foreach (DriveInfo info in DriveInfo.GetDrives())
            {
                if (!drives.Add(info.Name))
                    continue;
                
                _baseDirectory.SubDirectories.Add(new DirectorySource(info.Name));
            }

            Selected = "";
            _lockObj = new object();
            
            ImGui.OpenPopup(popupName);
        }
        
        if (ImGui.BeginPopupModal(popupName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            if (ImGui.BeginChild("FoldersList", ScaleVec(600, 500), ImGuiWindowFlags.HorizontalScrollbar))
            {
                _baseDirectory.Update(ref Selected);
                ImGui.EndChild();
            }

            ImGui.SetNextItemWidth(500);
            ImGui.InputTextWithHint("##FolderPath", "Path", ref Selected, 5000);

            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(Selected) || _currentTask != null);
            
            if (ImGui.Button(locale.GetString("Button.Add")))
            {
                Glimpse.Player.Stop();

                _currentTask = Task.Run(() =>
                {
                    _result = MusicDatabase.IndexDirectory(Selected, Glimpse.Player, Glimpse.Logger, ref _currentFile);
                });
            }
            
            ImGui.EndDisabled();
            
            ImGui.SameLine();
            
            ImGui.BeginDisabled(_currentTask != null);
            
            if (ImGui.Button(locale.GetString("Button.Cancel")))
                Close();
            
            ImGui.EndDisabled();

            if (_currentTask is Task task)
            {
                lock (_lockObj)
                {
                    if (_currentFile != null)
                        ImGui.TextUnformatted(Path.GetFileName(_currentFile));
                }

                if (task.IsCompleted)
                {
                    Glimpse.Database.AddIndexToDatabase(_result);
                    Glimpse.ConfigManager.WriteConfig(MusicDatabase.DatabaseName, Glimpse.Database);
                    _result = default;
                    Close();
                }
            }
            
            ImGui.EndPopup();
        }
    }

    private class DirectorySource
    {
        public string Path;
        
        public List<DirectorySource>? SubDirectories;

        public DirectorySource(string path)
        {
            Path = path;
        }

        public void Update(ref string selected)
        {
            if (SubDirectories == null)
            {
                SubDirectories = new List<DirectorySource>();
                DirectoryInfo info = new DirectoryInfo(Path);
                foreach (DirectoryInfo dir in info.EnumerateDirectories().OrderBy(info => info.Name))
                {
                    if ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;
                    
                    SubDirectories.Add(new DirectorySource(dir.FullName));
                }
            }
            
            foreach (DirectorySource directory in SubDirectories)
            {
                if (string.IsNullOrWhiteSpace(directory.Path))
                    continue;
                
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
                if (directory.Path == selected)
                    flags |= ImGuiTreeNodeFlags.Selected;
                if (directory.SubDirectories != null && directory.SubDirectories.Count == 0)
                    flags |= ImGuiTreeNodeFlags.Leaf;

                string dirName = System.IO.Path.GetFileName(directory.Path);
                // GetFileName can return empty strings if the path ends with a directory separator.
                // If that happens, just print the path of the directory.
                if (string.IsNullOrWhiteSpace(dirName))
                    dirName = directory.Path;
                
                // We don't usually need to provide directory.Path as an ID, however, when calling DriveiInfo.GetDrives()
                // Linux systems may report directories with the same name but with different paths, causing ImGui to
                // throw a warning about duplicated names. This shoudl fix that.
                bool node = ImGui.TreeNodeEx($"{dirName}##{directory.Path}", flags);
                if (ImGui.IsItemClicked())
                    selected = directory.Path;
                    
                if (node) 
                {
                    directory.Update(ref selected);
                    ImGui.TreePop();
                }
            }
        }
    }
}