using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Glimpse.API;
using Glimpse.Assets;
using Glimpse.API.Library;
using Glimpse.Audio;
using Glimpse.Configs;
using Glimpse.Database;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using SDL3;
using Color = System.Drawing.Color;
using Image = Glimpse.Graphics.Image;
using Track = Glimpse.API.Library.Track;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private bool _init;
    private ImGuiStyle _defaultStyle;

    private Size _restoreSize;
    private bool _miniplayer;

    private SemVer _newVersion;
    private string? _newVersionURL;
    private float _newVersionBlinker;
    
    private string? _currentAlbum;
    private AlbumView _currentView;
    private SizedCollection<Track> _currentTracks;
    private SizedCollection<string> _albums;
    
    private bool _wasSeekClicked;
    private double? _seekPosition;
    private int _currentRowHover;
    private int _currentRatingHover;

    private Image _playButton;
    private Image _pauseButton;
    private Image _skipButton;
    private Image _stopButton;
    private Image _plusButton;
    private Image _star;
    private Image _starFilled;
    private Image _cogButton;
    private Image _bugButton;
    private Image _updateButton;
    private Image _shuffleButton;
    private Image _repeatButton;

    private Image _defaultAlbumArt;
    private Image? _albumArt;

    private byte[]? _newAlbumArt;
    private bool _shouldDeleteArt;

    private Timer _playCountTimer;
    private bool _hasIncrementedPlayCount;
    
    public GlimpsePlayer()
    {
#if DEBUG
        Title = "Glimpse DEBUG";
#else
        Title = "Glimpse";
#endif
        Size = new Size(1100, 650);
        //Size = new Size(620, 400);
    }

    protected override unsafe void Initialize()
    {
        _playButton = Renderer.CreateImage("Icons.PlayButton.png");
        _pauseButton = Renderer.CreateImage("Icons.PauseButton.png");
        _skipButton = Renderer.CreateImage("Icons.SkipButton.png");
        _stopButton = Renderer.CreateImage("Icons.StopButton.png");
        _plusButton = Renderer.CreateImage("Icons.Plus.png");
        _star = Renderer.CreateImage("Icons.Star.png");
        _starFilled = Renderer.CreateImage("Icons.Star-Filled.png");
        _cogButton = Renderer.CreateImage("Icons.Cog.png");
        _bugButton = Renderer.CreateImage("Icons.Bug.png");
        _updateButton = Renderer.CreateImage("Icons.Update.png");
        _shuffleButton = Renderer.CreateImage("Icons.Shuffle.png");
        _repeatButton = Renderer.CreateImage("Icons.Repeat.png");
        
        _defaultAlbumArt = Renderer.CreateImage("Icons.Glimpse.png");
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
        Glimpse.Player.StateChanged += PlayerOnStateChanged;
        Glimpse.Platform.ButtonPressed += PlatformOnButtonPressed;
        Glimpse.Platform.GetPosition += PlatformOnGetPosition;

        const uint fontSize = 18;
        Renderer.ImGui.AddFont("Fonts.Roboto-Regular.ttf", fontSize);
        Renderer.ImGui.AddFont("Fonts.NotoSansJP-Regular.ttf", fontSize);
        Renderer.ImGui.AddFont("Fonts.NotoSansSC-Regular.ttf", fontSize);
        Renderer.ImGui.AddFont("Fonts.NotoSansKR-Regular.ttf", fontSize);
        Renderer.ImGui.AddFont("Fonts.NotoEmoji-Regular.ttf", fontSize);
        Renderer.ImGui.AddFont("Fonts.MaterialSymbolsOutlined-Regular.ttf", fontSize);
        
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigInputTrickleEventQueue = false;

        ImGuiStylePtr style = ImGui.GetStyle();
        _defaultStyle = *style.Handle;
        
        RefreshLayout();
        ChangeView(AlbumView.Albums);
        ChangeAlbum(null); // Change to the default album view where all tracks are displayed.

        _playCountTimer = new Timer(CheckIncrementPlayCount, null, 0, 1000);
        
        if (_currentTracks.Count == 0)
            AddPopup(new WelcomePopup());

//#if !DEBUG
        // Only perform the update check if the user wants it!
        if (Glimpse.Config.General.EnableUpdateChecking)
            Task.Run(CheckForNewerVersion);
//#endif
    }

    private void CheckIncrementPlayCount(object? state)
    {
        AudioPlayer player = Glimpse.Player;

        if (player.TrackState != TrackState.Playing ||
            _hasIncrementedPlayCount ||
            player.ConsumedTime.TotalSeconds < double.Max(30, player.TrackLength.TotalSeconds * 0.6))
        {
            return;
        }

        _hasIncrementedPlayCount = true;
        // TODO: I don't like this.
        if (Glimpse.Database.TryGetTrack(player.CurrentTrackPath, out Track? track))
        {
            track.PlayCount++;
            track.LastPlayed = DateTime.Now;
            Glimpse.Database.UpdateTrack(track);
        }
    }

    protected override unsafe void Update(float dt)
    {
        //ImGui.ShowStyleEditor();
        
        Locale locale = Glimpse.Locale;
        
        if (_newAlbumArt != null)
        {
            _albumArt?.Dispose();
            try
            {
                _albumArt = Renderer.CreateImage(_newAlbumArt);
            }
            catch (Exception e)
            {
                _albumArt = null;
                Glimpse.Logger.Log($"Failed to load album art: {e}");
            }

            _newAlbumArt = null;
        }
        else if (_shouldDeleteArt)
        {
            _shouldDeleteArt = false;
            
            _albumArt?.Dispose();
            _albumArt = null;
        }
        
        AudioPlayer player = Glimpse.Player;
        
        // Perform seeking if necessary 
        if (_wasSeekClicked)
        {
            Debug.Assert(_seekPosition is not null);
            player.Seek(_seekPosition.Value);
            _seekPosition = null;
            _wasSeekClicked = false;
        }
        
        if (Glimpse.Database.IsIndexing)
        {
            ChangeAlbum(_currentAlbum);
            ChangeView(AlbumView.Albums);
        }

        Renderer.Clear(Color.Black);
        
/*#if DEBUG
        if (ImGui.BeginMainMenuBar())
        {
            ImGui.TextUnformatted("DEBUG Menu");

            ImGui.Spacing();
            
            if (ImGui.MenuItem("Style Editor"))
                AddPopup(new StyleEditorPopup());
            
            if (ImGui.MenuItem("Settings"))
                AddPopup(new SettingsPopup());
            
            ImGui.EndMainMenuBar();
        }
#endif*/

        const uint centralNode = 1 << 11;
        const uint noTabBar = 1 << 12;

        uint id = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(),
            ImGuiDockNodeFlags.PassthruCentralNode | (ImGuiDockNodeFlags) noTabBar);
        
        if (!_init)
        {
            _init = true;
            
            ImGuiP.DockBuilderRemoveNode(id);
            ImGuiP.DockBuilderAddNode(id, ImGuiDockNodeFlags.NoUndocking);
            uint transportId = id;

            if (!_miniplayer)
            {
                ImGuiDir dir = Glimpse.Config.Appearance.SwapTransportControls ? ImGuiDir.Up : ImGuiDir.Down;
                
                uint albumsSongsId;
                ImGuiP.DockBuilderSplitNode(id, dir, 0, &transportId, &albumsSongsId);

                ImGuiDockNodePtr transportNode = ImGuiP.DockBuilderGetNode(transportId);
                transportNode.LocalFlags = ImGuiDockNodeFlags.NoResize;
                transportNode.SizeRef = ScaleVec(1100, 122);
            
                uint albumsId;
                uint songsId;
                ImGuiP.DockBuilderSplitNode(albumsSongsId, ImGuiDir.Left, 0, &albumsId, &songsId);

                ImGuiDockNodePtr albumsNode = ImGuiP.DockBuilderGetNode(albumsId);
                albumsNode.SizeRef = ScaleVec(327, 650);
                //albumsNode.SizeRef = ScaleVec(200, 650);

                ImGuiDockNodePtr songsNode = ImGuiP.DockBuilderGetNode(songsId);
                songsNode.SizeRef = ScaleVec(772, 650);
                songsNode.LocalFlags = (ImGuiDockNodeFlags) centralNode;
                
                ImGuiP.DockBuilderDockWindow("Albums", albumsId);
                ImGuiP.DockBuilderDockWindow("Songs", songsId);
            }

            ImGuiP.DockBuilderDockWindow("Transport", transportId);
        
            ImGuiP.DockBuilderFinish(id);
        }

        Vector4 iconsColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Text];

        bool switchToTrackList = false;
        bool switchToQueueView = false;
        AlbumView? switchView = null;
        
        #region Transport Dock
        
        if (ImGui.Begin("Transport", ImGuiWindowFlags.NoResize))
        {
            Vector2 winSize = ImGui.GetContentRegionAvail();

            ImGui.BeginChild("AlbumArt", new Vector2(winSize.Y));
            {
                Image albumArt = _albumArt ?? _defaultAlbumArt;

                float aspectRatio = albumArt.Width / (float) albumArt.Height;
                float scale = winSize.Y / (aspectRatio > 1 ? albumArt.Width : albumArt.Height);
                
                Vector2 size = new Vector2(albumArt.Width, albumArt.Height) * scale;
                
                ImGui.SetCursorPosY(winSize.Y / 2 - size.Y / 2);
                ImGui.Image(albumArt, size);
                if (ImGui.IsItemClicked())
                {
                    Size windowSize = Size;

                    if (_restoreSize.IsEmpty)
                        _restoreSize = new Size(470, 122);
                    
                    _miniplayer = !_miniplayer;
                    Size = _restoreSize;
                    _restoreSize = windowSize;
                    RefreshLayout();
                }
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            ImGui.BeginChild("MainView");
            {
                ImGui.BeginChild("TrackInfo", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                {
                    if (player.TrackState == TrackState.Stopped)
                    {
                        ImGui.TextUnformatted(locale.GetString("Glimpse"));
                        ImGui.TextUnformatted("");
                        ImGui.TextUnformatted("");
                    }
                    else
                    {
                        // TODO: Scroll to the selected track & album/artist
                        if (ImGui.TextButton(player.CurrentTrack?.Title ?? locale.GetString("UnknownTrack")) &&
                            player.CurrentTrack?.Album != null)
                        {
                            switchToQueueView = true;
                        }

                        if (ImGui.TextButton(player.CurrentTrack?.Artist ?? locale.GetString("UnknownArtist")) &&
                            player.CurrentTrack?.Artist != null)
                        {
                            switchToTrackList = true;
                            switchView = AlbumView.Artists;
                            _currentAlbum = player.CurrentTrack.Artist;
                        }


                        if (ImGui.TextButton(player.CurrentTrack?.Album ?? locale.GetString("UnknownAlbum")) &&
                            player.CurrentTrack?.Album != null)
                        {
                            switchToTrackList = true;
                            switchView = AlbumView.Albums;
                            _currentAlbum = player.CurrentTrack.Album;
                        }
                    }

                    ImGui.EndChild();
                }

                ImGui.SameLine();

                float miniplayerScale = _miniplayer ? 0.75f : 1.0f;
                
                Vector2 iconSize = new Vector2(32) * Scale * miniplayerScale;
                // Even though there are 4 icons, 3 icons makes it *feel* more centered, even though it's shifted to the right.
                const int numIcons = 3;
                float spacing = ImGui.GetStyle().ItemSpacing.X * miniplayerScale;
                float padding = ImGui.GetStyle().FramePadding.X * miniplayerScale;
                float totalButtonsWidth = (iconSize.X + spacing + padding) * numIcons;

                Vector2 centerPos;
                if (_miniplayer)
                {
                    centerPos = new Vector2(winSize.X - totalButtonsWidth - 15, ImGui.GetCursorScreenPos().Y + (int) (40 * Scale));
                }
                else
                {
                    centerPos = new Vector2(winSize.X / 2 - totalButtonsWidth / 2, ImGui.GetCursorScreenPos().Y + (int) (10 * Scale));
                }

                ImGui.SetCursorScreenPos(centerPos);

                ImGui.BeginChild("TransportControls", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                {
                    //Vector2 centerPos = new Vector2(Size.Width / 2, ImGui.GetCursorScreenPos().Y);
                    //float padding = ImGui.GetStyle().WindowPadding.X + 10;

                    ImGui.BeginDisabled(player.TrackState == TrackState.Stopped);

                    Vector4 buttonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);

                    ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);

                    if (ImGui.ImageButton("BackwardButton", _skipButton, iconSize, new Vector2(1, 0),
                            new Vector2(0, 1), Vector4.Zero, iconsColor))
                    {
                        player.Previous();
                    }

                    ImGui.SameLine();

                    if (player.TrackState == TrackState.Playing)
                    {
                        if (ImGui.ImageButton("PauseButton", _pauseButton, iconSize, Vector4.Zero, iconsColor))
                            player.Pause();
                    }
                    else
                    {
                        if (ImGui.ImageButton("PlayButton", _playButton, iconSize, Vector4.Zero, iconsColor))
                            player.Play();
                    }

                    ImGui.SameLine();

                    if (ImGui.ImageButton("ForwardButton", _skipButton, iconSize, Vector4.Zero, iconsColor))
                    {
                        player.Next();
                    }

                    if (!_miniplayer)
                    {
                        ImGui.SameLine();
                        if (ImGui.ImageButton("StopButton", _stopButton, iconSize, Vector4.Zero, iconsColor))
                        {
                            player.Stop();
                        }
                    }

                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();

                    ImGui.EndDisabled();

                    ImGui.EndChild();
                }

                Vector2 cursorPos = ImGui.GetCursorPos();

                Vector2 contentRegion = ImGui.GetContentRegionAvail();
                ImGui.SetCursorPos(new Vector2(contentRegion.X - (_miniplayer ? 55 : 150) * Scale, _miniplayer ? 20 : 20));
                //if (!_miniplayer)
                {
                    ImGui.BeginChild("VolumeDock", ImGuiChildFlags.AutoResizeY);
                    {
                        bool shuffle = false;
                        bool repeat = player.Repeat != RepeatMode.Off;

                        Vector4 shuffleButtonTint = iconsColor;
                        if (!shuffle)
                            shuffleButtonTint.W = 0.5f;

                        Vector4 repeatButtonTint = iconsColor;
                        if (!repeat)
                            repeatButtonTint.W = 0.5f;
                        
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                        ImGui.ImageButton("ShuffleButton", _shuffleButton, ScaleVec(16) * miniplayerScale, Vector4.Zero, shuffleButtonTint);
                        ImGui.SameLine(0, 0);
                        if (ImGui.ImageButton("RepeatButton", _repeatButton, ScaleVec(16) * miniplayerScale, Vector4.Zero, repeatButtonTint))
                        {
                            player.Repeat = repeat ? RepeatMode.Off : RepeatMode.RepeatQueue;
                        }
                        ImGui.PopStyleColor();

                        //if (!_miniplayer)
                        {
                            int volume = (int) (Glimpse.Player.Volume * 100);

                            string format = "%d";
                            ImGuiSliderFlags sliderFlags = ImGuiSliderFlags.None;
                            if (_miniplayer)
                            {
                                ImGui.EndChild();
                                ImGui.SetCursorPos(new Vector2(contentRegion.X - 10, 0));
                                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4));
                                ImGui.PushFont(ImFontPtr.Null, 60);
                                sliderFlags |= (ImGuiSliderFlags) ImGuiSliderFlagsPrivate.Vertical;
                                ImGui.SetNextItemWidth(10);
                                format = "";
                            }
                            else
                            {
                                ImGui.SameLine(0, 2);
                                contentRegion = ImGui.GetContentRegionAvail();
                                ImGui.SetNextItemWidth(contentRegion.X);
                            }
                            
                            if (ImGui.SliderInt("##Volume", ref volume, 0, 100, format, sliderFlags))
                            {
                                float fVol = (float) volume / 100;
                                Glimpse.Player.Volume = fVol;
                                Glimpse.Config.Audio.Volume = fVol;
                            }

                            if (_miniplayer)
                            {
                                ImGui.PopFont();
                                ImGui.PopStyleVar();
                            }
                        }

                        if (!_miniplayer)
                            ImGui.EndChild();
                    }
                }

                // TODO: HACK
                ImGui.SetCursorPos(cursorPos);
                ImGui.BeginChild("SongPosition");
                {
                    float cursorPosY = ImGui.GetCursorPosY() + (int) (10 * Scale);
                    contentRegion = ImGui.GetContentRegionAvail();

                    float align = ImGui.GetStyle().FramePadding.Y;

                    double position = _seekPosition ?? player.ElapsedTime.TotalSeconds;
                    double length = player.TrackLength.TotalSeconds;
                    
                    string elapsedText = Utils.FormatTimespan(player.ElapsedTime);
                    string lengthText = Utils.FormatTimespan(player.TrackLength);

                    Vector2 elapsedTextSize = ImGui.CalcTextSize(elapsedText);
                    Vector2 lengthTextSize = ImGui.CalcTextSize(lengthText);

                    ImGui.SetCursorPosY(cursorPosY + align);
                    ImGui.TextUnformatted(elapsedText);
                    ImGui.SameLine();
                    
                    // TODO: Realllly need to work out a better way of working out positions rather than randomly
                    //   throwing numbers around and hoping it looks right. Fully expecting to run into a major MAJOR
                    //   headache some day.
                    ImGui.SetCursorPosY(cursorPosY + 7 * Scale);
                    Vector2 globalCursorPos = ImGui.GetCursorScreenPos();
                    float width = contentRegion.X - elapsedTextSize.X - lengthTextSize.X - (int) (20 * Scale);
                    ImGui.ProgressBar((float) (position / length), new Vector2(width, 10 * Scale), "");
                    
                    // ProgressBars in ImGui don't have any slider-like behaviours. Before we were using a slider and it
                    // worked well, but progress bars look so much better.
                    // We have to hack the slider-like behaviour in.
                    
                    // Start seeking when the progress bar is hovered OR if a seek has already been requested, so that
                    // if the user moves their mouse away from the bar, it will continue seeking as long as they are
                    // holding the mouse button
                    // TODO: The hitbox is too small. Increase the size of the hitbox
                    if ((ImGui.IsItemHovered() || _seekPosition != null) && player.TrackState != TrackState.Stopped)
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            // Calculate the mouse position relative to the bar then turn it into a 0-1 range.
                            float xFraction = (ImGui.GetMousePos().X - globalCursorPos.X) / width;
                            _seekPosition = length * xFraction;
                        }
                        else if (_seekPosition != null) // Only seek once the mouse button is let go.
                            _wasSeekClicked = true;
                    }
                    else
                    {
                        _seekPosition = null;
                        _wasSeekClicked = false;
                    }

                    ImGui.SameLine();
                    ImGui.SetCursorPosY(cursorPosY);
                    ImGui.TextUnformatted(lengthText);

                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }
        }
        ImGui.End();
        
        #endregion

        if (_miniplayer)
            return;
        
        #region Albums Dock
        
        if (ImGui.Begin("Albums", ImGuiWindowFlags.HorizontalScrollbar))
        {
            /*string newDirectory = null;

            if (ImGui.Selectable(".."))
                newDirectory = Path.GetDirectoryName(_currentDirectory);
            
            foreach (string directory in _directories)
            {
                if (ImGui.Selectable(Path.GetFileName(directory)))
                    newDirectory = directory;
            }
            
            if (newDirectory != null)
                ChangeDirectory(newDirectory);*/

            Vector2 contentRegion = ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemSpacing;
            const float split = 0.6f;
            
            ImGui.BeginDisabled();
            
            string str = "";
            ImGui.SetNextItemWidth(contentRegion.X * split);
            ImGui.InputTextWithHint("##SearchBox", locale.GetString("Player.SearchBar"), ref str, 1000);
            
            ImGui.EndDisabled();
            
            ImGui.SameLine();
            
            /*ImGui.SetNextItemWidth(contentRegion.X * (1.0f - split));
            string preview = _currentView switch
            {
                AlbumView.Albums => locale.GetString("Player.ViewSelect.Albums"),
                AlbumView.Artists => locale.GetString("Player.ViewSelect.Artists"),
                _ => throw new ArgumentOutOfRangeException()
            };
            if (ImGui.BeginCombo("##DisplaySelector", preview))
            {
                if (ImGui.Selectable(locale.GetString("Player.ViewSelect.Albums")))
                {
                    _currentView = AlbumView.Albums;
                    _currentAlbum = ShowAllString;
                }

                if (ImGui.Selectable(locale.GetString("Player.ViewSelect.Artists")))
                {
                    _currentView = AlbumView.Artists;
                    _currentAlbum = ShowAllString;
                }
                //ImGui.Selectable("Playlists");
                
                ImGui.EndCombo();
            }*/
            
            if (ImGui.BeginTabBar("AlbumTabs"))
            {
                //ImGui.PushFont(_iconsFont, 32);

                //if (switchView is AlbumView view)
                //    _currentView = view;

                if (ImGui.BeginTabItemTooltip("\ue019##Albums", locale.GetString("Player.ViewSelect.Albums"), switchView is AlbumView.Albums))
                {
                    if (_currentView != AlbumView.Albums)
                        ChangeView(AlbumView.Albums);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItemTooltip("\ue01a##Artists", locale.GetString("Player.ViewSelect.Artists"), switchView is AlbumView.Artists))
                {
                    if (_currentView != AlbumView.Artists)
                        ChangeView(AlbumView.Artists);
                    ImGui.EndTabItem();
                }
                
                if (ImGui.BeginTabItemTooltip("\ue521##Genres", locale.GetString("Player.ViewSelect.Genres"), switchView is AlbumView.Genres))
                {
                    if (_currentView != AlbumView.Genres)
                        ChangeView(AlbumView.Genres);
                    ImGui.EndTabItem();
                }

                ImGui.BeginDisabled();
                if (ImGui.BeginTabItemTooltip("\ue05f##Playlists", locale.GetString("Player.ViewSelect.Playlists")))
                {
                    ImGui.EndTabItem();
                }
                ImGui.EndDisabled();
                
                ImGui.BeginChild("AlbumList", ImGuiWindowFlags.HorizontalScrollbar);
                {
                    if (ImGui.Selectable(locale.GetString("Player.Albums.ShowAll"), _currentAlbum == null))
                    {
                        ChangeAlbum(null);
                        switchToTrackList = true;
                    }

                    ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                    clipper.Begin((int) _albums.Count);
                    
                    while (clipper.Step())
                    {
                        IEnumerable<string> albumsRange =
                            _albums.Collection.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                        
                        foreach (string name in albumsRange)
                        {
                            string albumName = name;
                            if (albumName == string.Empty)
                                albumName = locale.GetString("Player.Albums.NoAlbum");
                            
                            if (ImGui.Selectable(albumName, _currentAlbum == albumName))
                            {
                                ChangeAlbum(albumName);
                                switchToTrackList = true;
                            }

                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                {
                                    if (Glimpse.Database.TryGetAlbum(albumName, out Album album))
                                        player.QueueTracks(album.Tracks, QueueSlot.AtEnd);
                                }

                                ImGui.Separator();
                            
                                if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                    AddPopup(new RemovePopup(albumName, true, false));
                                if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteAlbum")))
                                    AddPopup(new RemovePopup(albumName, true, true));
                            
                                ImGui.EndPopup();
                            }
                        }
                    }
                    
                    ImGui.EndChild();
                }
                
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
        
        #endregion
        
        #region Songs Dock
        
        if (ImGui.Begin("Songs"))
        {
            /*foreach (string file in _files)
            {
                if (ImGui.Selectable(Path.GetFileName(file)))
                {
                    player.ChangeTrack(file);
                    player.Play();
                }
            }*/

            if (ImGui.BeginTabBar("SongsTabs"))
            {
                Vector2 currentCursorPos = ImGui.GetCursorPos();
                Vector2 contentRegion = ImGui.GetContentRegionAvail();

                bool updateAvailable = _newVersionURL != null;
                
                ImGui.SetCursorPos(new Vector2(contentRegion.X - (int) ((updateAvailable ? 114 : 82) * Scale), (int) (5 * Scale)));
                ImGui.BeginChild("SettingsButtons");
                {
                    if (updateAvailable)
                    {
                        Vector4 buttonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
                        Vector4 highlightColor = new Vector4(1, 0, 0, 1);
                        float amount = (float.Sin(_newVersionBlinker) + 1) / 2;
                        
                        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Lerp(buttonColor, highlightColor, amount));
                        if (ImGui.ImageButton("Update", _updateButton, ScaleVec(16), Vector4.Zero, iconsColor)) Utils.OpenLink(_newVersionURL);
                        
                        ImGui.SetItemTooltipUnformatted(locale.GetString("Player.UpdateAvailable", _newVersion));
                        
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                        
                        _newVersionBlinker += dt * 2;
                        if (_newVersionBlinker >= float.Pi * 2)
                            _newVersionBlinker -= float.Pi * 2;
                    }

                    if (ImGui.ImageButton("ReportBug", _bugButton, ScaleVec(16), Vector4.Zero, iconsColor)) Utils.OpenLink("https://github.com/aquagoose/Glimpse/issues/new?template=bug_report.md");

                    ImGui.SetItemTooltipUnformatted(locale.GetString("Player.ReportBug"));
                    
                    ImGui.SameLine();
                    
                    if (ImGui.ImageButton("Settings", _cogButton, ScaleVec(16), Vector4.Zero, iconsColor))
                        AddPopup(new SettingsPopup());
                    ImGui.SetItemTooltipUnformatted(locale.GetString("Player.Settings"));
            
                    ImGui.SameLine();
            
                    if (ImGui.ImageButton("AddDirs", _plusButton, ScaleVec(16), Vector4.Zero, iconsColor))
                        //AddPopup(new AddFolderPopup());
                        AddPopup(new ManageLibraryPopup());
                    ImGui.SetItemTooltipUnformatted(locale.GetString("Player.AddDirs"));
                    
                    ImGui.EndChild();
                }
                
                ImGui.SetCursorPos(currentCursorPos);
                
                ImGuiTabItemFlags trackFlags =
                    switchToTrackList ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Tracks"), trackFlags))
                {
                    if (ImGui.BeginTable("SongTable", 9, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.RowBg))
                    {
                        const int ratingColumn = 6;
                        ImGui.TableSetupColumn(locale.GetString("Track"), ImGuiTableColumnFlags.WidthFixed,  40.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Title"), ImGuiTableColumnFlags.WidthFixed, 265.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Artist"), ImGuiTableColumnFlags.WidthFixed, 160.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Album"), ImGuiTableColumnFlags.WidthFixed, 195.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Length"), ImGuiTableColumnFlags.WidthFixed, 48.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Plays"), ImGuiTableColumnFlags.WidthFixed, 40.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Rating"), ImGuiTableColumnFlags.WidthFixed, 85.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("LastPlayed"), ImGuiTableColumnFlags.WidthFixed, 160.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("FileName"), ImGuiTableColumnFlags.WidthFixed, 300.0f * Scale);
                        
                        ImGui.TableSetupScrollFreeze(0, 1);
                        
                        ImGui.TableHeadersRow();

                        string currentTrackPath = Glimpse.Player.CurrentTrackPath;
                        //int songEntryHeight = (int) (25 * Scale);

                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin((int) _currentTracks.Count/*, songEntryHeight*/);
                        while (clipper.Step())
                        {
                            int song = clipper.DisplayStart;
                            IEnumerable<Track> visibleTracks =
                                _currentTracks.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                            foreach (Track track in visibleTracks)
                            {
                                ImGui.TableNextRow(/*songEntryHeight*/);
                                int currentRow = ImGui.TableGetRowIndex();

                                //Console.WriteLine(song);

                                ImGui.TableNextColumn();
                                if (track.TrackNumber is uint trackNumber)
                                    ImGui.TextUnformatted(trackNumber.ToString());

                                ImGui.TableNextColumn();

                                string path = track.Path;
                                string title = track.Title ?? locale.GetString("UnknownTrack");
                                string artist = track.Artist ?? locale.GetString("UnknownArtist");
                                string album = track.Album ?? locale.GetString("UnknownAlbum");
                                string length = track.Length is TimeSpan trackLength
                                    ? Utils.FormatTimespan(trackLength)
                                    : "";
                                string playCount = track.PlayCount.ToString();
                                string lastPlayed = track.LastPlayed is DateTime last
                                    ? last.ToString(CultureInfo.CurrentUICulture)
                                    : "";

                                // In order to allow the rating buttons to be clicked, we tell the selectable to ignore
                                // the ratings column (otherwise the buttons won't click and instead the song will play)
                                // To do this we just disable the SpanAllColumns flag when the rating column is hovered.
                                bool isRatingHovered = ImGui.TableGetHoveredColumn() == ratingColumn;
                                if (ImGui.Selectable($"{title}##{path}", path == currentTrackPath, isRatingHovered ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    player.QueueTracks(_currentTracks.Select(trk => trk.Path), QueueSlot.Clear);
                                    if (!player.TryChangeTrack(song))
                                        AddPopup(new FileNotFoundPopup(title));
                                }
                                ImGui.SetColumnTooltip(title);

                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                        player.QueueTrack(path, QueueSlot.AtEnd);
                                    if (ImGui.Selectable(locale.GetString("Menu.PlayNext")))
                                        player.QueueTrack(path, QueueSlot.NextTrack);

                                    ImGui.Separator();

                                    if (ImGui.Selectable(locale.GetString("Menu.ShowInExplorer", Glimpse.Platform.FileManagerName)))
                                        Glimpse.Platform.OpenFileInExplorer(path);
                                    if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                        AddPopup(new RemovePopup(path, false, false));
                                    if (Glimpse.Config.General.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteFile")))
                                        AddPopup(new RemovePopup(path, false, true));

                                    ImGui.EndPopup();
                                }

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(artist);
                                ImGui.SetColumnTooltip(artist);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(album);
                                ImGuiE.SetColumnTooltip(album);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(length);
                                //ImGuiE.SetColumnTooltip(length);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(playCount);
                                //ImGuiE.SetColumnTooltip(playCount);
                                ImGui.TableNextColumn();
                                int rating = isRatingHovered && _currentRowHover == currentRow ? _currentRatingHover : track.Rating;
                                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0));
                                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
                                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
                                for (int i = 0; i < 5; i++)
                                {
                                    if (ImGui.ImageButton($"{path}rating{i}", i < rating ? _starFilled : _star, ScaleVec(16), Vector4.Zero, iconsColor))
                                    {
                                        track.Rating = (byte) (i + 1);
                                        Glimpse.Database.UpdateTrack(track);
                                    }

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        track.Rating = 0;
                                        Glimpse.Database.UpdateTrack(track);
                                    }

                                    if (ImGui.IsItemHovered())
                                    {
                                        _currentRowHover = currentRow;
                                        _currentRatingHover = i + 1;
                                    }

                                    ImGui.SameLine(0, 0);
                                }
                                ImGui.PopStyleColor(3);
                                ImGui.PopStyleVar();

                                ImGui.NewLine();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(lastPlayed);
                                ImGuiE.SetColumnTooltip(lastPlayed);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(path);
                                ImGuiE.SetColumnTooltip(path);

                                song++;
                            }
                        }

                        ImGui.EndTable();
                    }
                    
                    ImGui.EndTabItem();
                }

                ImGuiTabItemFlags queueFlags =
                    switchToQueueView ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Queue"), queueFlags))
                {
                    ImGui.BeginChild("QueuedTracks");
                    {
                        List<string> queuedTracks = player.QueuedTracks;
                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(queuedTracks.Count, (32 + 2) * Scale);

                        while (clipper.Step())
                        {
                            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        //for (int i = 0; i < queuedTracks.Count; i++)
                            {
                                string path = queuedTracks[i];

                                bool selected = i == player.CurrentTrackIndex;
                                bool dark = i < player.CurrentTrackIndex;

                                /*if (dark)
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

                                if (ImGui.Selectable($"{i + 1}. {Glimpse.Database.Tracks[path].Title}", selected))
                                    player.ChangeTrack(i);
                                if (dark)
                                    ImGui.PopStyleColor();*/

                                Vector2 cursorPos = ImGui.GetCursorPos();
                                int height = (int) ((32 + 6) * Scale);

                                // TODO: perform better checking here!
                                Glimpse.Database.TryGetTrack(path, out Track? track);
                                string title = track?.Title ?? locale.GetString("UnknownTrack");
                                string artist = track?.Artist ?? locale.GetString("UnknownArtist");
                                string album = track?.Album ?? locale.GetString("UnknownAlbum");
                                
                                if (ImGui.Selectable($"##Queue{i}", selected, ImGuiSelectableFlags.AllowOverlap, new Vector2(0, height)))
                                {
                                    if (!player.TryChangeTrack(i))
                                    {
                                        AddPopup(new FileNotFoundPopup(title));
                                    }
                                }
                                ImGui.SetCursorPos(cursorPos);
                                ImGui.SameLine();

                                ImGui.BeginChild($"QueueTrack{i}", new Vector2(0, height), ImGuiWindowFlags.NoInputs);
                                {
                                    float posY = ImGui.GetCursorPosY();
                                    ImGui.SetCursorPosY(posY + 1);
                                    ImGui.PushFont(ImFontPtr.Null, 32);
                                    ImGui.TextUnformatted($"{i + 1}");
                                    ImGui.PopFont();
                                    ImGui.SetCursorPosY(posY);
                                    ImGui.SameLine();
                                    ImGui.BeginChild($"QueueTrackInfo{i}", ImGuiWindowFlags.NoInputs);
                                    {
                                        ImGui.TextUnformatted(title);
                                        ImGui.PushFont(ImFontPtr.Null, 14);
                                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.75f, 0.75f, 0.75f, 1.0f));
                                        ImGui.TextUnformatted(
                                            $"{artist} • {album} • {track?.Length.GetValueOrDefault():mm\\:ss}");
                                        ImGui.PopStyleColor();
                                        ImGui.PopFont();

                                        ImGui.EndChild();
                                    }
                                    ImGui.EndChild();
                                }
                            }
                        }

                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
        
        #endregion
    }

    public void RefreshLayout()
    {
        _init = false;
        SetupStyle(ImGui.GetStyle());
    }
    
    private void PlayerOnTrackChanged(TrackInfo info, string path)
    {
        _hasIncrementedPlayCount = false;
        TrackInfo.Image? art = info.AlbumArt;

        if (art?.Data == null)
            _shouldDeleteArt = true;
        else
            _newAlbumArt = art.Data;
    }
    
    private void PlayerOnStateChanged(TrackState state)
    {
        Glimpse.Platform.SetPlayState(state, Glimpse.Player.CurrentTrack, Glimpse.Player.ElapsedTime);
        
        if (state != TrackState.Stopped)
            return;

        _shouldDeleteArt = true;
    }
    
    private void PlatformOnButtonPressed(TransportButton? button, int? position)
    {
        AudioPlayer player = Glimpse.Player;

        if (button is { } transportButton)
        {
            switch (transportButton)
            {
                case TransportButton.Play:
                    player.Play();
                    break;
                case TransportButton.Pause:
                    player.Pause();
                    break;
                case TransportButton.Next:
                    player.Next();
                    break;
                case TransportButton.Previous:
                    player.Previous();
                    break;
                case TransportButton.Stop:
                    player.Stop();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }
        
        if (position is { } pos)
            player.Seek(pos);
    }
    
    private TimeSpan PlatformOnGetPosition()
    {
        return Glimpse.Player.ElapsedTime;
    }

    private Vector2 ScaleVec(float x, float y)
    {
        float scale = Scale;
        return new Vector2((int) (x * scale), (int) (y * scale));
    }

    private Vector2 ScaleVec(float scalar)
        => ScaleVec(scalar, scalar);

    private async Task CheckForNewerVersion()
    {
        Logger logger = Glimpse.Logger;
        logger.Log("Checking for update...");
        
        try
        {
            using HttpClient client = new();
            client.BaseAddress = new Uri("https://glimpseaudio.co.uk");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "download/version.json");
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            JsonNode obj = JsonObject.Parse(json);

            SemVer thisVersion = Glimpse.Version;

            string? newVersionString = (string?) obj["version"];
            if (newVersionString == null)
                return;
            SemVer newVersion = new SemVer(newVersionString);

            if (newVersion <= thisVersion)
            {
                logger.Log("Glimpse is up to date.");
                return;
            }

            string? newVersionUrl = (string?) obj["url"];
            if (newVersionUrl == null)
                return;

            _newVersion = newVersion;
            _newVersionURL = newVersionUrl;
            logger.Log($"Version {_newVersion} is available!");
        }
        catch (Exception e)
        {
            logger.Log($"Error occurred while checking for update: {e}");
        }
    }

    public override void Dispose()
    {
        _playCountTimer.Dispose();
        
        _playButton.Dispose();
        _pauseButton.Dispose();
        _skipButton.Dispose();
        _stopButton.Dispose();
        _plusButton.Dispose();
        _star.Dispose();
        _starFilled.Dispose();
        _cogButton.Dispose();
        _bugButton.Dispose();
        _updateButton.Dispose();
        _shuffleButton.Dispose();
        _repeatButton.Dispose();
        
        base.Dispose();
    }

    protected override void OnScaleChanged()
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        SetupStyle(style);
        RefreshLayout();
    }

    /// <summary>
    /// Change the current album.
    /// </summary>
    /// <param name="albumName">The album name. Use <see langword="null"/> to view all.</param>
    private void ChangeAlbum(string? albumName)
    {
        _currentAlbum = albumName;
        if (albumName == null)
            _currentTracks = Glimpse.Database.GetTracks();
        else
        {
            switch (_currentView)
            {
                case AlbumView.Albums:
                {
                    if (!Glimpse.Database.TryGetTracksForAlbum(albumName, out _currentTracks))
                        goto default;
                    break;
                }

                case AlbumView.Artists:
                {
                    if (!Glimpse.Database.TryGetTracksForArtist(albumName, out _currentTracks))
                        goto default;
                    break;
                }

                case AlbumView.Genres:
                {
                    if (!Glimpse.Database.TryGetTracksForGenre(albumName, out _currentTracks))
                        goto default;
                    break;
                }
                
                default:
                    _currentTracks = Glimpse.Database.GetTracks();
                    break;
            }
            
        }
    }

    private void ChangeView(AlbumView view)
    {
        _currentView = view;
        switch (view)
        {
            case AlbumView.Albums:
            {
                SizedCollection<Album> albums = Glimpse.Database.GetAlbums();
                IEnumerable<string> albumNames = albums.Collection.Select(album => album.Name);
                _albums = new SizedCollection<string>(albumNames, albums.Count);
                break;
            }
            case AlbumView.Artists:
            {
                SizedCollection<Artist> artists = Glimpse.Database.GetArtists();
                IEnumerable<string> artistNames = artists.Collection.Select(artist => artist.Name);
                _albums = new SizedCollection<string>(artistNames, artists.Count);
                break;
            }
            case AlbumView.Genres:
            {
                SizedCollection<Genre> genres = Glimpse.Database.GetGenres();
                IEnumerable<string> genreNames = genres.Collection.Select(genre => genre.Name);
                _albums = new SizedCollection<string>(genreNames, genres.Count);
                break;
            }
        }
    }

    private unsafe void SetupStyle(ImGuiStylePtr style)
    {
        *style.Handle = _defaultStyle;
        
        const int rounding = 5;
        style.FrameRounding = rounding;
        style.GrabRounding = rounding;
        style.ChildRounding = rounding;
        style.PopupRounding = rounding;
        style.DockingSeparatorSize = (int) float.Ceiling(1 * Scale);
        style.ScaleAllSizes(Scale);
        style.FontScaleDpi = Scale;

        // TODO: This system is terrible!!!
        //   The theme should be stored in the config file as the theme's "Friendly Name", not as a "path" to the theme.
        string themeName = $"Themes.{Glimpse.Config.Appearance.Theme}.json";
        Stream stream;
        try
        {
            stream = Asset.GetAssetStream(themeName);
        }
        catch (Exception e)
        {
            Glimpse.Logger.Log("Couldn't load theme. Using default.");
            stream = Asset.GetAssetStream($"Themes.{Theme.DefaultTheme}.json");
        }

        bool useLightMode = Glimpse.Config.Appearance.PreferredColorScheme switch
        {
            PreferredColorScheme.SyncToOS => SDL.GetSystemTheme() == SDL.SystemTheme.Light,
            PreferredColorScheme.Dark => false,
            PreferredColorScheme.Light => true,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        Theme theme =
            JsonSerializer.Deserialize<Theme>(stream, ConfigManager.GetDefaultSerializerOptions());
        theme.ApplyImGuiStyle(useLightMode, ImGui.GetStyle().Colors);
        
        stream.Dispose();
    }

    private enum AlbumView
    {
        Albums,
        Artists,
        Genres
    }
}