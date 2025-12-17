using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text.Json.Nodes;
using Glimpse.API;
using Glimpse.Audio;
using Glimpse.Database;
using Glimpse.Locales;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using Newtonsoft.Json.Linq;
using SDL3;
using Color = System.Drawing.Color;
using Image = Glimpse.Graphics.Image;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private const string ShowAllString = "*";
    
    private bool _init;
    private ImGuiStyle _defaultStyle;

    private SemVer _newVersion;
    private string? _newVersionURL;
    private float _newVersionBlinker;
    
    private string _currentAlbum;
    private int _seekPosition;
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

    private Image _defaultAlbumArt;
    private Image? _albumArt;

    private byte[]? _newAlbumArt;
    private bool _shouldDeleteArt;

    private bool _hasIncrementedPlayCount;
    
    public GlimpsePlayer()
    {
#if DEBUG
        Title = "Glimpse DEBUG";
#else
        Title = "Glimpse";
#endif
        Size = new Size(1100, 650);
    }

    protected override unsafe void Initialize()
    {
        _playButton = Renderer.CreateImage("Assets/Icons/PlayButton.png");
        _pauseButton = Renderer.CreateImage("Assets/Icons/PauseButton.png");
        _skipButton = Renderer.CreateImage("Assets/Icons/SkipButton.png");
        _stopButton = Renderer.CreateImage("Assets/Icons/StopButton.png");
        _plusButton = Renderer.CreateImage("Assets/Icons/Plus.png");
        _star = Renderer.CreateImage("Assets/Icons/Star.png");
        _starFilled = Renderer.CreateImage("Assets/Icons/Star-Filled.png");
        _cogButton = Renderer.CreateImage("Assets/Icons/Cog.png");
        _bugButton = Renderer.CreateImage("Assets/Icons/Bug.png");
        _updateButton = Renderer.CreateImage("Assets/Icons/Update.png");
        
        _defaultAlbumArt = Renderer.CreateImage("Assets/Icons/Glimpse.png");
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
        Glimpse.Player.StateChanged += PlayerOnStateChanged;
        Glimpse.Platform.ButtonPressed += PlatformOnButtonPressed;

        const uint fontSize = 18;
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/Roboto-Regular.ttf"), fontSize, "Roboto");
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/NotoSansJP-Regular.ttf"), fontSize, "NotoJP");
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/NotoSansSC-Regular.ttf"), fontSize, "NotoSC");
        
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGuiStylePtr style = ImGui.GetStyle();
        _defaultStyle = *style.Handle;
        SetupStyle(style);

        _currentAlbum = ShowAllString;
        
        if (Glimpse.Database.Tracks.Count == 0)
            AddPopup(new AddFolderPopup());

#if !DEBUG
        Task.Run(CheckForNewerVersion);
#endif
    }

    protected override unsafe void Update()
    {
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
        
        Renderer.Clear(Color.Black);
        
/*#if DEBUG
        if (ImGui.BeginMainMenuBar())
        {
            ImGui.Text("DEBUG Menu");

            ImGui.Spacing();
            
            if (ImGui.MenuItem("Style Editor"))
                AddPopup(new StyleEditorPopup());
            
            if (ImGui.MenuItem("Settings"))
                AddPopup(new SettingsPopup());
            
            ImGui.EndMainMenuBar();
        }
#endif*/
        
        //ImGui.ShowStyleEditor();

        const uint centralNode = 1 << 11;
        const uint noTabBar = 1 << 12;

        uint id = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(),
            ImGuiDockNodeFlags.PassthruCentralNode | (ImGuiDockNodeFlags) noTabBar);
        
        if (!_init)
        {
            _init = true;
            
            ImGuiP.DockBuilderRemoveNode(id);
            ImGuiP.DockBuilderAddNode(id, ImGuiDockNodeFlags.NoUndocking);

            ImGuiDir dir = Glimpse.Config.SwapTransportControls ? ImGuiDir.Up : ImGuiDir.Down;
            
            uint transportId;
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

            ImGuiDockNodePtr songsNode = ImGuiP.DockBuilderGetNode(songsId);
            songsNode.SizeRef = ScaleVec(772, 650);
            songsNode.LocalFlags = (ImGuiDockNodeFlags) centralNode;
            
            Console.WriteLine(albumsId.ToString("x8"));
            Console.WriteLine(albumsId.ToString("x8"));
            Console.WriteLine(transportId.ToString("x8"));
            
            ImGuiP.DockBuilderDockWindow("Transport", transportId);
            ImGuiP.DockBuilderDockWindow("Albums", albumsId);
            ImGuiP.DockBuilderDockWindow("Songs", songsId);
        
            ImGuiP.DockBuilderFinish(id);
        }

        if (ImGui.Begin("Transport", ImGuiWindowFlags.NoResize))
        {
            Vector2 winSize = ImGui.GetContentRegionAvail();

            ImGui.BeginChild("AlbumArt", new Vector2(winSize.Y));
            {
                ImGui.Image(_albumArt ?? _defaultAlbumArt, new Vector2(winSize.Y));
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            ImGui.BeginChild("MainThing");

            ImGui.BeginChild("TrackInfo", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
            {
                if (player.TrackState == TrackState.Stopped)
                {
                    ImGui.Text(locale.GetString("Glimpse"));
                    ImGui.Text("");
                    ImGui.Text("");
                }
                else
                {
                    ImGui.Text(EscapeString(player.CurrentTrack?.Title) ?? locale.GetString("UnknownTrack"));
                    ImGui.Text(EscapeString(player.CurrentTrack?.Artist) ?? locale.GetString("UnknownArtist"));
                    ImGui.Text(EscapeString(player.CurrentTrack?.Album) ?? locale.GetString("UnknownAlbum"));
                }

                ImGui.EndChild();
            }
            
            ImGui.SameLine();
            
            Vector2 centerPos = new Vector2(Size.Width / 2 - 40, ImGui.GetCursorScreenPos().Y + (int) (10 * Scale));
            ImGui.SetNextWindowPos(centerPos);

            ImGui.BeginChild("TransportControls", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
            {
                //Vector2 centerPos = new Vector2(Size.Width / 2, ImGui.GetCursorScreenPos().Y);
                //float padding = ImGui.GetStyle().WindowPadding.X + 10;
                
                ImGui.BeginDisabled(player.TrackState == TrackState.Stopped);
                
                Vector4 buttonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
                
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);
                
                if (ImGui.ImageButton("BackwardButton", _skipButton, ScaleVec(32), new Vector2(1, 0), new Vector2(0, 1)))
                {
                    player.Previous();
                }
                
                ImGui.SameLine();
                
                if (player.TrackState == TrackState.Playing)
                {
                    if (ImGui.ImageButton("PauseButton", _pauseButton, ScaleVec(32)))
                        player.Pause();
                }
                else
                {
                    if (ImGui.ImageButton("PlayButton", _playButton, ScaleVec(32)))
                        player.Play();
                }
                
                ImGui.SameLine();

                if (ImGui.ImageButton("ForwardButton", _skipButton, ScaleVec(32)))
                {
                    player.Next();
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("StopButton", _stopButton, ScaleVec(32)))
                {
                    player.Stop();
                }
                
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                
                ImGui.EndDisabled();
                
                ImGui.EndChild();
            }

            //ImGui.BeginChild("SongPosition", ImGuiChildFlags.AutoResizeX)
            {
                float cursorPos = ImGui.GetCursorPosY() + (int) (10 * Scale);
                Vector2 contentRegion = ImGui.GetContentRegionAvail();

                float align = ImGui.GetStyle().FramePadding.Y;

                int position = player.ElapsedSeconds;
                int length = player.TrackLength;
                
                string elapsedText = $"{position / 60:0}:{position % 60:00}";
                string lengthText = $"{length / 60:0}:{length % 60:00}";

                Vector2 elapsedTextSize = ImGui.CalcTextSize(elapsedText);
                Vector2 lengthTextSize = ImGui.CalcTextSize(lengthText);
                
                ImGui.SetCursorPosY(cursorPos + align);
                ImGui.Text(elapsedText);
                ImGui.SameLine();
                ImGui.SetCursorPosY(cursorPos);
                ImGui.SetNextItemWidth(contentRegion.X - elapsedTextSize.X - lengthTextSize.X - (int) (20 * Scale));
                if (ImGui.SliderInt("##transport", ref position, 0, length, ""))
                    _seekPosition = position;

                if (ImGui.IsItemDeactivatedAfterEdit())
                    player.Seek(_seekPosition);

                ImGui.SameLine();
                ImGui.SetCursorPosY(cursorPos);
                ImGui.Text(lengthText);
                
                //ImGui.EndChild();
            }
            
            ImGui.EndChild();
        }
        ImGui.End();
        
        bool switchToTrackList = false;
        
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

            ImGui.BeginChild("AlbumList", ImGuiWindowFlags.HorizontalScrollbar);
            {
                if (ImGui.Selectable(locale.GetString("Player.Albums.ShowAll"), _currentAlbum == ShowAllString))
                {
                    _currentAlbum = ShowAllString;
                    switchToTrackList = true;
                }

                Dictionary<string, Album> albums = Glimpse.Database.Albums;
                ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                clipper.Begin(albums.Count);

                while (clipper.Step())
                {
                    IEnumerable<KeyValuePair<string, Album>> albumsRange =
                        albums.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                    
                    foreach ((string name, Album album) in albumsRange)
                    {
                        if (ImGui.Selectable(name, _currentAlbum == name))
                        {
                            _currentAlbum = name;
                            switchToTrackList = true;
                        }

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                player.QueueTracks(album.Tracks, QueueSlot.AtEnd);
                        
                            ImGui.Separator();
                        
                            if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                AddPopup(new RemovePopup(name, true, false));
                            if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteAlbum")))
                                AddPopup(new RemovePopup(name, true, true));
                        
                            ImGui.EndPopup();
                        }
                    }
                }
                
                ImGui.EndChild();
            }
        }
        ImGui.End();
        
        if (_currentAlbum != null && ImGui.Begin("Songs"))
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
                        if (ImGui.ImageButton("Update", _updateButton, ScaleVec(16)))
                            OpenLink(_newVersionURL);
                        
                        ImGui.SetItemTooltip(locale.GetString("Player.UpdateAvailable", _newVersion));
                        
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        // TODO: DeltaTime
                        const float dt = 1 / 60.0f;
                        _newVersionBlinker += dt * 2;
                        if (_newVersionBlinker >= float.Pi * 2)
                            _newVersionBlinker -= float.Pi * 2;
                    }

                    if (ImGui.ImageButton("ReportBug", _bugButton, ScaleVec(16)))
                        OpenLink("https://github.com/aquagoose/Glimpse/issues/new?template=bug_report.md");

                    ImGui.SetItemTooltip(locale.GetString("Player.ReportBug"));
                    
                    ImGui.SameLine();
                    
                    if (ImGui.ImageButton("Settings", _cogButton, ScaleVec(16)))
                        AddPopup(new SettingsPopup());
                    ImGui.SetItemTooltip(locale.GetString("Player.Settings"));
            
                    ImGui.SameLine();
            
                    if (ImGui.ImageButton("AddDirs", _plusButton, ScaleVec(16)))
                        AddPopup(new AddFolderPopup());
                    ImGui.SetItemTooltip(locale.GetString("Player.AddDirs"));
                    
                    ImGui.EndChild();
                }
                
                ImGui.SetCursorPos(currentCursorPos);
                
                ImGuiTabItemFlags trackFlags =
                    switchToTrackList ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Tracks"), trackFlags))
                {
                    ICollection<string> trackList;

                    if (_currentAlbum == ShowAllString || !Glimpse.Database.Albums.TryGetValue(_currentAlbum, out Album currentAlbum))
                    {
                        trackList = Glimpse.Database.Tracks.Keys;
                        _currentAlbum = ShowAllString;
                    }
                    else
                        trackList = currentAlbum.Tracks;

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
                        int songEntryHeight = (int) (25 * Scale);

                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(trackList.Count, songEntryHeight);
                        while (clipper.Step())
                        {
                            int song = clipper.DisplayStart;
                            IEnumerable<string> visibleTracks =
                                trackList.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                            foreach (string path in visibleTracks)
                            {
                                Track track = Glimpse.Database.Tracks[path];

                                ImGui.TableNextRow(songEntryHeight);
                                int currentRow = ImGui.TableGetRowIndex();

                                //Console.WriteLine(song);

                                ImGui.TableNextColumn();
                                if (track.TrackNumber is uint trackNumber)
                                    ImGui.Text(trackNumber.ToString());

                                ImGui.TableNextColumn();

                                string title = EscapeString(track.Title) ?? locale.GetString("UnknownTrack");
                                string artist = EscapeString(track.Artist) ?? locale.GetString("UnknownArtist");
                                string album = EscapeString(track.Album) ?? locale.GetString("UnknownAlbum");
                                string escapedPath = EscapeString(path);

                                // In order to allow the rating buttons to be clicked, we tell the selectable to ignore
                                // the ratings column (otherwise the buttons won't click and instead the song will play)
                                // To do this we just disable the SpanAllColumns flag when the rating column is hovered.
                                bool isRatingHovered = ImGui.TableGetHoveredColumn() == ratingColumn;
                                if (ImGui.Selectable($"{title}##{path}", path == currentTrackPath, isRatingHovered ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    player.QueueTracks(trackList, QueueSlot.Clear);
                                    player.ChangeTrack(song);
                                }

                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                        player.QueueTrack(path, QueueSlot.Queue);
                                    if (ImGui.Selectable(locale.GetString("Menu.PlayNext")))
                                        player.QueueTrack(path, QueueSlot.NextTrack);
                                    if (ImGui.Selectable(locale.GetString("Menu.AddToEnd")))
                                        player.QueueTrack(path, QueueSlot.AtEnd);

                                    ImGui.Separator();

                                    if (ImGui.Selectable(locale.GetString("Menu.ShowInExplorer")))
                                        Glimpse.Platform.OpenFileInExplorer(path);
                                    if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                        AddPopup(new RemovePopup(path, false, false));
                                    if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteFile")))
                                        AddPopup(new RemovePopup(path, false, true));

                                    ImGui.EndPopup();
                                }

                                ImGui.TableNextColumn();
                                ImGui.Text(artist);
                                ImGui.TableNextColumn();
                                ImGui.Text(album);
                                ImGui.TableNextColumn();
                                ImGui.Text(track.Length?.ToString(@"mm\:ss") ?? "");
                                ImGui.TableNextColumn();
                                ImGui.Text(track.PlayCount.ToString());
                                ImGui.TableNextColumn();
                                int rating = isRatingHovered && _currentRowHover == currentRow ? _currentRatingHover : track.Rating;
                                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0));
                                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
                                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
                                for (int i = 0; i < 5; i++)
                                {
                                    if (ImGui.ImageButton($"{path}rating{i}", i < rating ? _starFilled : _star, ScaleVec(16)))
                                    {
                                        track.Rating = (byte) (i + 1);
                                        Glimpse.Database.Tracks[path] = track;
                                    }

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        track.Rating = 0;
                                        Glimpse.Database.Tracks[path] = track;
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
                                ImGui.Text(track.LastPlayed is { } lastPlayed ? lastPlayed.ToString("yyyy-MM-dd HH:mm:ss") : "");
                                ImGui.TableNextColumn();
                                ImGui.Text(EscapeString(escapedPath));
                                ImGui.SetItemTooltip(escapedPath);

                                song++;
                            }
                        }

                        ImGui.EndTable();
                    }
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Queue")))
                {
                    ImGui.BeginChild("QueuedTracks");
                    {
                        List<string> queuedTracks = player.QueuedTracks;
                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(queuedTracks.Count);

                        while (clipper.Step())
                        {
                            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                            {
                                string path = queuedTracks[i];

                                bool selected = i == player.CurrentTrackIndex;
                                bool dark = i < player.CurrentTrackIndex;

                                if (dark)
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

                                if (ImGui.Selectable($"{i + 1}. {Glimpse.Database.Tracks[path].Title}", selected))
                                    player.ChangeTrack(i);
                                if (dark)
                                    ImGui.PopStyleColor();
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

        if (player.TrackState == TrackState.Playing && player.SecondsConsumed >= double.Max(30, player.TrackLength * 0.6) &&
            !_hasIncrementedPlayCount)
        {
            _hasIncrementedPlayCount = true;
            if (Glimpse.Database.Tracks.TryGetValue(player.CurrentTrackPath, out Track track))
            {
                track.PlayCount++;
                track.LastPlayed = DateTime.Now;
                Glimpse.Database.Tracks[player.CurrentTrackPath] = track;
            }
        }
    }

    public void RefreshLayout()
    {
        _init = false;
    }
    
    private void PlayerOnTrackChanged(TrackInfo info, string path)
    {
        _hasIncrementedPlayCount = false;
        TrackInfo.Image art = info.AlbumArt;

        if (art?.Data == null)
            _shouldDeleteArt = true;
        else
            _newAlbumArt = art.Data;
    }
    
    private void PlayerOnStateChanged(TrackState state)
    {
        Glimpse.Platform.SetPlayState(state, Glimpse.Player.CurrentTrack, Glimpse.Player.ElapsedSeconds);
        
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }
        
        if (position is { } pos)
            player.Seek(pos);
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
            JObject obj = JObject.Parse(json);

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

    public static void OpenLink(string link)
    {
        SDL.OpenURL(link);
    }

    protected override void OnScaleChanged()
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        SetupStyle(style);
        RefreshLayout();
    }

    private static string? EscapeString(string? @string)
    {
        return @string?.Replace("%", "%%");
    }

    private unsafe void SetupStyle(ImGuiStylePtr style)
    {
        *style.Handle = _defaultStyle;
        
        int rounding = (int) (5 * Scale);
        style.FrameRounding = rounding;
        style.GrabRounding = rounding;
        style.ChildRounding = rounding;
        style.PopupRounding = rounding;
        style.DockingSeparatorSize = (int) float.Ceiling(1 * Scale);
        style.ScaleAllSizes(Scale);
        style.FontScaleDpi = Scale;
        
        Span<Vector4> colors = style.Colors;
        colors[(int) ImGuiCol.Text]                   = new Vector4(0.93f, 0.93f, 0.93f, 1.00f);
        colors[(int) ImGuiCol.TextDisabled]           = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int) ImGuiCol.WindowBg]               = new Vector4(0.12f, 0.12f, 0.14f, 0.94f);
        colors[(int) ImGuiCol.ChildBg]                = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.PopupBg]                = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
        colors[(int) ImGuiCol.Border]                 = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
        colors[(int) ImGuiCol.BorderShadow]           = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.FrameBg]                = new Vector4(0.16f, 0.29f, 0.48f, 0.54f);
        colors[(int) ImGuiCol.FrameBgHovered]         = new Vector4(0.26f, 0.59f, 0.98f, 0.40f);
        colors[(int) ImGuiCol.FrameBgActive]          = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        colors[(int) ImGuiCol.TitleBg]                = new Vector4(0.04f, 0.04f, 0.04f, 1.00f);
        colors[(int) ImGuiCol.TitleBgActive]          = new Vector4(0.16f, 0.29f, 0.48f, 1.00f);
        colors[(int) ImGuiCol.TitleBgCollapsed]       = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);
        colors[(int) ImGuiCol.MenuBarBg]              = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int) ImGuiCol.ScrollbarBg]            = new Vector4(0.02f, 0.02f, 0.02f, 0.20f);
        colors[(int) ImGuiCol.ScrollbarGrab]          = new Vector4(0.44f, 0.53f, 0.64f, 0.71f);
        colors[(int) ImGuiCol.ScrollbarGrabHovered]   = new Vector4(0.44f, 0.53f, 0.64f, 1.00f);
        colors[(int) ImGuiCol.ScrollbarGrabActive]    = new Vector4(0.26f, 0.93f, 0.59f, 1.00f);
        colors[(int) ImGuiCol.CheckMark]              = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.SliderGrab]             = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.SliderGrabActive]       = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.Button]                 = new Vector4(1.00f, 0.69f, 0.22f, 0.78f);
        colors[(int) ImGuiCol.ButtonHovered]          = new Vector4(0.62f, 0.93f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.ButtonActive]           = new Vector4(0.06f, 0.53f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.Header]                 = new Vector4(0.23f, 0.66f, 0.87f, 0.16f);
        colors[(int) ImGuiCol.HeaderHovered]          = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.HeaderActive]           = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        //colors[(int) ImGuiCol.Separator]              = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
        colors[(int) ImGuiCol.SeparatorHovered]       = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
        colors[(int) ImGuiCol.SeparatorActive]        = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        colors[(int) ImGuiCol.ResizeGrip]             = new Vector4(0.26f, 0.59f, 0.98f, 0.20f);
        colors[(int) ImGuiCol.ResizeGripHovered]      = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        colors[(int) ImGuiCol.ResizeGripActive]       = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        colors[(int) ImGuiCol.TabHovered]             = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        colors[(int) ImGuiCol.Tab]                    = new Vector4(0.27f, 0.27f, 0.27f, 0.78f);
        colors[(int) ImGuiCol.TabSelected]            = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.TabSelectedOverline]    = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TabDimmed]              = new Vector4(0.07f, 0.10f, 0.15f, 0.97f);
        colors[(int) ImGuiCol.TabDimmedSelected]      = new Vector4(0.14f, 0.26f, 0.42f, 1.00f);
        colors[(int) ImGuiCol.TabDimmedSelectedOverline]  = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int) ImGuiCol.DockingPreview]         = new Vector4(0.26f, 0.59f, 0.98f, 0.70f);
        colors[(int) ImGuiCol.DockingEmptyBg]         = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        colors[(int) ImGuiCol.PlotLines]              = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        colors[(int) ImGuiCol.PlotLinesHovered]       = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        colors[(int) ImGuiCol.PlotHistogram]          = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.PlotHistogramHovered]   = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.TableHeaderBg]          = new Vector4(0.19f, 0.19f, 0.26f, 1.00f);
        colors[(int) ImGuiCol.TableBorderStrong]      = new Vector4(0.31f, 0.31f, 0.35f, 1.00f);
        colors[(int) ImGuiCol.TableBorderLight]       = new Vector4(0.23f, 0.23f, 0.25f, 1.00f);
        colors[(int) ImGuiCol.TableRowBg]             = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.TableRowBgAlt]          = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        colors[(int) ImGuiCol.TextLink]               = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TextSelectedBg]         = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        colors[(int) ImGuiCol.DragDropTarget]         = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
        colors[(int) ImGuiCol.NavWindowingHighlight]  = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int) ImGuiCol.NavWindowingDimBg]      = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        colors[(int) ImGuiCol.ModalWindowDimBg]       = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
    }
}