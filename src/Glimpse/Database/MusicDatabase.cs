using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Glimpse.Player;
using Glimpse.Player.Codecs;
using Glimpse.Player.Configs;

namespace Glimpse.Database;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public class MusicDatabase : IConfig
{
    public Dictionary<string, Track> Tracks;
    
    public Dictionary<string, Album> Albums;
    
    public MusicDatabase()
    {
        Tracks = new Dictionary<string, Track>();
        Albums = new Dictionary<string, Album>();
    }

    public void Refresh()
    {
        Tracks = Tracks.OrderBy(pair => pair.Value.TrackNumber).ToDictionary();
        Albums = Albums.OrderBy(pair => pair.Key).ToDictionary();
    }

    public void AddIndexToDatabase(in IndexResult index)
    {
        Logger.Log($"Adding indexed directory {index.Directory} to dataabase.");
        
        foreach ((string path, Track track) in index.Tracks)
            Tracks[path] = track;
        
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

            // TODO: This is a bit crude. Improve this.
            try
            {
                CodecStream stream = player.CreateStreamFromFile(file.FullName);
                info = stream.TrackInfo;
                stream.Dispose();
            }
            catch (Exception)
            {
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