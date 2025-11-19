using System.Diagnostics.CodeAnalysis;
using Glimpse.Player;
using Glimpse.Player.Configs;

namespace Glimpse.Database;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public class MusicDatabase : IConfig
{
    public const string DatabaseName = "Database/MusicDatabase";
    
    public Dictionary<string, Track> Tracks;
    public Dictionary<string, Album> Albums;
    
    public MusicDatabase()
    {
        Tracks = new Dictionary<string, Track>();
        Albums = new Dictionary<string, Album>();
    }

    public void Refresh()
    {
        Tracks = Tracks.OrderBy(pair => pair.Value.Album).ThenBy(pair => pair.Value.TrackNumber).ToDictionary();
        Albums = Albums.OrderBy(pair => pair.Key).ToDictionary();
    }

    public void AddIndexToDatabase(in IndexResult index)
    {
        Logger.Log($"Adding indexed directory {index.Directory} to dataabase.");

        foreach ((string path, Track track) in index.Tracks)
        {
            Track trk = track;
            
            if (Tracks.TryGetValue(path, out Track oldTrack))
            {
                // Copy over playback metadata to the new track.
                trk.Rating = oldTrack.Rating;
                trk.PlayCount = oldTrack.PlayCount;
                trk.LastPlayed = oldTrack.LastPlayed;
            }
            
            Tracks[path] = trk;
        }

        foreach ((string name, Album album) in index.Albums)
            Albums[name] = album;
        
        Refresh();
    }

    public static IndexResult IndexDirectory(string directory, AudioPlayer player, ref string current)
    {
        Logger.Log($"Indexing directory {directory}.");

        Dictionary<string, Track> tracks = new Dictionary<string, Track>();
        Dictionary<string, Album> albums = new Dictionary<string, Album>();

        foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles("*.*", SearchOption.AllDirectories).OrderBy(info => info.Name))
        {
            Logger.Log($"Indexing {file}");
            current = file.FullName;
            
            TrackInfo info;

            // As GetTrackInfoForFile throws an exception if the track is supported, simply catch all errors, log them,
            // then carry on.
            try
            {
                info = player.GetTrackInfoForFile(file.FullName);
            }
            catch (Exception e)
            {
                Logger.Log($"Exception occurred while getting track info: {e}");
                continue;
            }

            tracks.Add(file.FullName, new Track(info));

            if (info.Album != null)
            {
                if (!albums.TryGetValue(info.Album, out Album album))
                {
                    album = new Album(info.Album);
                    albums.Add(info.Album, album);
                }
                
                album.Tracks.Add(file.FullName);
            }
        }

        return new IndexResult(directory, tracks, albums);
    }
}