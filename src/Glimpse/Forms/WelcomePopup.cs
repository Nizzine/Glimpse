using System.Numerics;
using Glimpse.API;
using Glimpse.API.Library;
using Glimpse.Database;
using Hexa.NET.ImGui;
using SDL3;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class WelcomePopup : Popup
{
    private bool _hasOldLibrary;
    
    private string? _disableNext;
    private readonly SDL.DialogFileCallback _folderDialog;
    
    private Image _glimpse;
    private uint _tabIndex;

    private bool _needsRefresh;
    private IReadOnlyCollection<string> _libraryPaths;

    public WelcomePopup()
    {
        _folderDialog = FolderDialog;
    }

    public override void Open()
    {
        _hasOldLibrary = File.Exists(Path.Combine(IConfigManager.BaseDir, OldMusicDatabase.DatabaseName + ".json"));
        
        _glimpse = Renderer.CreateImage("Icons.Glimpse.png");
        _tabIndex = 0;

        _needsRefresh = true;
    }

    public override void Update(float dt)
    {
        _disableNext = null;
        if (_needsRefresh)
        {
            _needsRefresh = false;
            _libraryPaths = Glimpse.Database.GetLibraryPaths();
        }

        Vector2 windowSize = ImGui.GetIO().DisplaySize;
        Vector2 welcomeSize = ScaleVec(800, 500);
        Vector2 welcomePos = new Vector2(windowSize.X / 2 - welcomeSize.X / 2, windowSize.Y / 2 - welcomeSize.Y / 2);
        
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(windowSize);
        if (ImGui.Begin("Welcome", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove))
        {
            // TODO: Make this work with themes instead of being hardcoded.
            // TODO: Because night theme has no transparency, the background is entirely opaque. It should perhaps have some transparency or something.
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(new Vector3(0.12f), 1.0f));
            ImGui.SetNextWindowPos(welcomePos);
            ImGui.BeginChild("TabChild", welcomeSize, ImGuiChildFlags.Borders);
            {
                if (ImGui.BeginTabBar("Tabs"))
                {
                    Tab("Welcome", 0, WelcomeTab);
                    Tab("Import", 1, ImportTab);
                    Tab("Preferences", 2, PreferencesTab);
                    
                    ImGui.EndTabBar();
                }

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            Vector2 buttonSize = ScaleVec(100, 50);
            
            float right = welcomePos.X + welcomeSize.X;
            float padding = ImGui.GetStyle().ItemSpacing.X;
            float totalButtonsWidth = buttonSize.X * 2 + padding;
            ImGui.SetCursorPosX(right - totalButtonsWidth);
            
            ImGui.BeginDisabled(_tabIndex == 0);
            if (ImGui.Button("Previous", buttonSize))
                _tabIndex--;
            ImGui.EndDisabled();
            
            ImGui.SameLine();

            bool disableNext = _disableNext != null;
            ImGui.BeginDisabled(disableNext);
            if (ImGui.Button("Next", buttonSize))
            {
                // TODO: This is very manual. Perhaps add a "next button" action or something?
                if (_tabIndex == 1)
                    Glimpse.Database.Index();
                _tabIndex++;
                
                if (_tabIndex >= 3)
                    Close();
            }

            if (disableNext)
                ImGui.SetItemTooltipUnformatted(_disableNext);
            ImGui.EndDisabled();
            
            ImGui.End();
        }
    }

    private void Tab(string name, int index, Action tabFunc)
    {
        ImGui.BeginDisabled(_tabIndex != index);

        if (ImGui.BeginTabItem(name, _tabIndex == index ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
        {
            tabFunc();
            ImGui.EndTabItem();
        }
        
        ImGui.EndDisabled();
    }

    private void WelcomeTab()
    {
        ImGui.BeginChild("GlimpseLogo", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
        {
            ImGui.Image(_glimpse, ScaleVec(256));
            ImGui.EndChild();
        }

        ImGui.SameLine();

        ImGui.BeginChild("WelcomeText", ImGuiChildFlags.AutoResizeY);
        {
            ImGui.PushFont(ImFontPtr.Null, 48);
            ImGui.TextUnformatted("Welcome to Glimpse");
            ImGui.PopFont();

            ImGui.TextUnformatted("The free, fast, and extensible music player.");
            ImGui.TextUnformatted("Let's get you started.");

            ImGui.EndChild();
        }
    }

    private void ImportTab()
    {
        string importOldLibraryName = "Migration Assistant";
        
        if (_hasOldLibrary && !ImGui.IsPopupOpen(importOldLibraryName))
            ImGui.OpenPopup(importOldLibraryName);

        if (ImGui.BeginPopupModal(importOldLibraryName, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("An alpha (< 0.1.0) music library was found.\nWould you like to import it?\nThe original library will NOT be deleted!");

            if (ImGui.Button("Yes"))
            {
                Glimpse.ConfigManager.TryGetConfig(OldMusicDatabase.DatabaseName, out OldMusicDatabase oldDb);

                string basePath = string.Empty;
                List<string> basePaths = [];
                foreach ((string path, Track track) in oldDb.Tracks)
                {
                    track.Path = path;
                    Glimpse.Database.InsertOrUpdateTrack(track);

                    if (string.IsNullOrEmpty(basePath))
                        basePath = Path.GetDirectoryName(path);
                    else
                    {
                        while (!path.StartsWith(basePath))
                            basePath = Path.GetDirectoryName(basePath);
                    }
                }

                foreach ((_, Album album) in oldDb.Albums)
                    Glimpse.Database.InsertOrUpdateAlbum(album);

                foreach ((_, Artist artist) in oldDb.Artists)
                    Glimpse.Database.InsertOrUpdateArtist(artist);

                foreach ((_, Genre genre) in oldDb.Genres)
                    Glimpse.Database.InsertOrUpdateGenre(genre);
                
                //basePaths.Add(basePath);
                Glimpse.Database.AddLibraryPath(basePath);
                _needsRefresh = true;
                _hasOldLibrary = false;
                ImGui.CloseCurrentPopup();
            }
            
            ImGui.SameLine();

            if (ImGui.Button("No"))
            {
                _hasOldLibrary = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        
        if (_libraryPaths.Count == 0)
            _disableNext = "Add a folder to your library first!";
        
        ImGui.TextUnformatted("Let's start by importing your music.");

        ImGui.BeginChild("ItemsDisplay", ScaleVec(400, 430), ImGuiChildFlags.Borders, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.HorizontalScrollbar);
        {
            if (_libraryPaths.Count == 0)
                ImGui.Text("No folders added.");
            
            foreach (string path in _libraryPaths)
            {
                ImGui.Selectable(path);
            }
            
            ImGui.EndChild();
        }

        ImGui.SameLine();
        
        ImGui.BeginChild("Settings");
        {
            if (ImGui.Button("Add Folder"))
            {
                string? defaultLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                if (string.IsNullOrWhiteSpace(defaultLocation))
                    defaultLocation = null;

                SDL.ShowOpenFolderDialog(_folderDialog, 0, Glimpse.MainWindow.Handle, defaultLocation, true);
            }
            
            ImGui.SetCursorPosY(ImGui.GetContentRegionAvail().Y);
            ImGui.TextUnformatted("Click Next to start importing your music!");
            
            ImGui.EndChild();
        }
    }

    private unsafe void FolderDialog(IntPtr userdata, IntPtr filelist, int filter)
    {
        // TODO: This is copied straight from ManageLibraryPopup. All this stuff should be moved into as many reusable
        //       widget functions as possible.
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

    private void PreferencesTab()
    {
        
    }
}