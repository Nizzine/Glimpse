using Glimpse.Audio;
using Glimpse.Database;
using Hexa.NET.ImGui;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public sealed class RemovePopup : Popup
{
    private readonly string _nameOrPath;
    private readonly bool _isAlbum;
    private readonly bool _delete;

    private bool _removeSongs;
    
    public RemovePopup(string nameOrPath, bool isAlbum, bool delete)
    {
        _nameOrPath = nameOrPath;
        _isAlbum = isAlbum;
        _delete = delete;
        _removeSongs = isAlbum && delete;
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

            if (_delete)
            {
                ImGui.Text(
                    $"PERMANENTLY DELETE \"{name}\"{(_isAlbum ? " and ALL SONGS" : "")} from your computer?{(_isAlbum ? "\nNOTE: This will NOT delete the Album's folder(s) from your computer." : "")}");
            }
            else
                ImGui.Text($"Remove \"{name}\" from library?");

            if (_isAlbum && !_delete)
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
                            if (_delete)
                                File.Delete(track);
                        }
                    }
                }
                else
                {
                    if (_nameOrPath == player.CurrentTrack)
                        player.Stop();
                    
                    if (db.Tracks.Remove(_nameOrPath, out Track track) && track.Album != null)
                        db.Albums[track.Album].Tracks.Remove(_nameOrPath);
                    
                    if (_delete)
                        File.Delete(_nameOrPath);
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