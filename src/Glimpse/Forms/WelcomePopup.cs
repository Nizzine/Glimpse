using System.Numerics;
using Glimpse.API;
using Glimpse.API.Library;
using Glimpse.Forms.Widgets;
using Glimpse.Library;
using Hexa.NET.ImGui;
using SDL3;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class WelcomePopup : Popup
{
    private ManageLibraryWidget _manageLibraryWidget;
    
    private bool _hasOldLibrary;
    
    private string? _disableNext;
    private readonly SDL.DialogFileCallback _folderDialog;
    
    private Image _glimpse;
    private uint _tabIndex;

    public override void Open()
    {
        _manageLibraryWidget = new ManageLibraryWidget(this);
        
        _hasOldLibrary = File.Exists(Path.Combine(IConfigManager.BaseDir, OldMusicDatabase.DatabaseName + ".json"));
        
        _glimpse = Renderer.CreateImage("asset://Icons.Glimpse.png");
        _tabIndex = 0;
    }

    public override unsafe void Update(float dt)
    {
        _disableNext = null;

        Vector2 windowSize = ImGui.GetIO().DisplaySize;
        Vector2 welcomeSize = ScaleVec(800, 500);
        Vector2 welcomePos = new Vector2(windowSize.X / 2 - welcomeSize.X / 2, windowSize.Y / 2 - welcomeSize.Y / 2);

        Vector4 windowBg = *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg);
        
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(windowSize);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, windowBg with { W = 0.5f });
        if (ImGui.Begin("Welcome", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove))
        {
            ImGui.SetNextWindowPos(welcomePos);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, windowBg);
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
                if (_tabIndex == 1 && !Glimpse.Library.IsIndexing) // Prevent indexing while indexing already.
                    Glimpse.Library.Index();
                _tabIndex++;
                
                if (_tabIndex >= 3)
                    Close();
            }

            if (disableNext)
                ImGui.SetItemTooltipUnformatted(_disableNext);
            ImGui.EndDisabled();
            
            ImGui.End();
        }
        ImGui.PopStyleColor();
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
                    Glimpse.Library.InsertOrUpdateTrack(track);

                    if (string.IsNullOrEmpty(basePath))
                        basePath = Path.GetDirectoryName(path);
                    else
                    {
                        while (!path.StartsWith(basePath))
                            basePath = Path.GetDirectoryName(basePath);
                    }
                }

                foreach ((_, Album album) in oldDb.Albums)
                    Glimpse.Library.InsertOrUpdateAlbum(album);

                foreach ((_, Artist artist) in oldDb.Artists)
                    Glimpse.Library.InsertOrUpdateArtist(artist);

                foreach ((_, Genre genre) in oldDb.Genres)
                    Glimpse.Library.InsertOrUpdateGenre(genre);
                
                //basePaths.Add(basePath);
                Glimpse.Library.AddLibraryPath(basePath);
                _manageLibraryWidget.Refresh();
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
        
        if (_manageLibraryWidget.LibraryPaths.Count == 0)
            _disableNext = "Add a folder to your library first!";
        
        ImGui.TextUnformatted("Let's start by importing your music.");
        _manageLibraryWidget.Update();
    }

    private void PreferencesTab()
    {
        
    }

    public override void Dispose()
    {
        _glimpse.Dispose();
        
        _manageLibraryWidget.Dispose();
    }
}