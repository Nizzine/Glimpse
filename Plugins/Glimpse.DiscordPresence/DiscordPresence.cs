using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordRPC;
using Glimpse.API;
using Hexa.NET.ImGui;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.CoverArt;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace Glimpse.DiscordPresence;

public partial class DiscordPresence : IPlugin
{
    private IGlimpse _glimpse;
    private bool _initialized;
    
    private string _currentUrl;

    public DiscordConfig Config;
    
    public DiscordRpcClient Client;

    public bool IsInitialized => _initialized;
    
    public string Name => "Discord RPC";

    public void DisplayGui()
    {
        if (ImGui.BeginTable("ArtTable", 2, ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Album", ImGuiTableColumnFlags.WidthStretch, 0.2f);
            ImGui.TableSetupColumn("Album Art", ImGuiTableColumnFlags.WidthStretch, 0.8f);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            foreach ((string album, string albumArt) in Config.AlbumArt)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(album.Replace("%", "%%"));
                ImGui.TableNextColumn();
                ImGui.Text(albumArt);
            }
            
            ImGui.EndTable();
        }
    }

    public void Initialize(IGlimpse glimpse)
    {
        _glimpse = glimpse;
        _currentUrl = "glimpse";
        
        Client = new DiscordRpcClient("1280266653950804111");
        
        if (!_glimpse.ConfigManager.TryGetConfig("Discord", out Config))
        {
            Config = new DiscordConfig();
            _glimpse.ConfigManager.WriteConfig("Discord", Config);
        }
        
        Client.Initialize();
        
        _glimpse.Player.StateChanged += PlayerOnStateChanged;

        _initialized = true;
    }

    void PlayerOnStateChanged(TrackState state)
    {
        IAudioPlayer player = _glimpse.Player;
        
        switch (state)
        {
            case TrackState.Playing:
                SetPresence(player.CurrentTrack, player.ElapsedSeconds, player.TrackLength);
                break;
            
            case TrackState.Paused:
            case TrackState.Stopped:
                Client.ClearPresence();
                break;
        }
    }

    private void SetPresence(TrackInfo info, int currentSecond, int totalSeconds)
    {
        _glimpse.Logger.Log($"Set discord presence to track: {info.Artist} - {info.Title}");
        _currentUrl = "glimpse";

        DateTime now = DateTime.UtcNow;
        TimeSpan current = TimeSpan.FromSeconds(currentSecond);
        TimeSpan total = TimeSpan.FromSeconds(totalSeconds);
        
        RichPresence presence = new RichPresence()
            .WithType(ActivityType.Listening)
            .WithStatusDisplay(StatusDisplayType.State)
            .WithDetails(info.Title)
            .WithState(info.Artist)
            .WithTimestamps(new Timestamps(now - current, now + (total - current)))
            .WithAssets(new Assets() { LargeImageText = info.Album, LargeImageKey = _currentUrl });
        
        Client.SetPresence(presence);

        // Only search for new album art if the album changes or the URL is null.
        // This saves queries to musicbrainz.
        if (info.Album is { } albumName)
        {
            _glimpse.Logger.Log($"AlbumName: {albumName}");
            albumName = RemoveDiscNumberRegex().Replace(albumName, "");
           _glimpse.Logger.Log($"Sanitized album name: {albumName}");

            if (Config.AlbumArt.TryGetValue(albumName, out _currentUrl))
            {
                Client.UpdateLargeAsset(_currentUrl);
                return;
            }
            
            Task.Run(() =>
            {
                const string app = "GlimpseAudioPlayer";
                const string contact = "https://github.com/aquagoose";
                string version = _glimpse.Version.ToString();

                using Query query = new Query(app, version, contact);
                var releases = query.FindReleases(albumName, 5);
                using CoverArt art = new CoverArt(app, version, contact);

                foreach (ISearchResult<MetaBrainz.MusicBrainz.Interfaces.Entities.IRelease> release in releases.Results)
                {
                    IImage image = null;

                    try
                    {
                        foreach (IImage img in art.FetchReleaseIfAvailable(release.Item.Id)?.Images)
                        {
                            if (img.Front)
                            {
                                image = img;
                                break;
                            }
                        }
                    }
                    catch (Exception) { }

                    if (image is not null)
                    {
                        _currentUrl = image.Location?.ToString();
                        Config.AlbumArt[albumName] = _currentUrl;
                        _glimpse.ConfigManager.WriteConfig("Discord", Config);
                        
                        Client.UpdateLargeAsset(_currentUrl);
                        break;
                    }
                }
            });
        }
    }

    public void Dispose()
    {
        if (!_initialized)
            return;
        _initialized = false;

        _glimpse.Player.StateChanged -= PlayerOnStateChanged;
        Client.Dispose();
    }

    [GeneratedRegex(@"\s*([\[(]*)\s*(disc|cd)(\s*)\d+\s*([)\]]*)",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant)]
    private static partial Regex RemoveDiscNumberRegex();
}