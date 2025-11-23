using Glimpse.Database;
using Glimpse.Player;
using Hexa.NET.ImGui;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public sealed class RemovePopup : Popup
{
    private readonly string _nameOrPath;
    private readonly bool _isAlbum;

    private bool _removeSongs;
    
    public RemovePopup(string nameOrPath, bool isAlbum)
    {
        _nameOrPath = nameOrPath;
        _isAlbum = isAlbum;
        _removeSongs = false;
    }
    
    public override void Update()
    {
        if (!ImGui.IsPopupOpen("Remove"))
            ImGui.OpenPopup("Remove");

        if (ImGui.BeginPopupModal("Remove", ImGuiWindowFlags.AlwaysAutoResize))
        {
            MusicDatabase db = Glimpse.Database;
            AudioPlayer player = Glimpse.Player;
            
            string name = _isAlbum ? _nameOrPath : (db.Tracks[_nameOrPath].Title ?? "Unknown Track");
            
            ImGui.Text($"Remove \"{name}\" from library?");

            if (_isAlbum)
                ImGui.Checkbox("Also remove songs from library?", ref _removeSongs);
            
            if (ImGui.Button("Yes"))
            {
                if (_isAlbum)
                {
                    if (db.Albums.Remove(_nameOrPath, out Album album) && _removeSongs)
                    {
                        foreach (string track in album.Tracks)
                        {
                            if (track == player.CurrentTrack)
                                player.Stop();
                            db.Tracks.Remove(track);
                        }
                    }
                }
                else
                {
                    if (_nameOrPath == player.CurrentTrack)
                        player.Stop();
                    
                    if (db.Tracks.Remove(_nameOrPath, out Track track) && track.Album != null)
                        db.Albums[track.Album].Tracks.Remove(_nameOrPath);
                }
                
                db.Refresh();
                Close();
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("No"))
                Close();
            
            ImGui.EndPopup();
        }
    }
}