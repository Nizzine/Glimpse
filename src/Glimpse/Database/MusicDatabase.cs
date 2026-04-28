using Glimpse.API;
using Glimpse.API.Library;
using Newtonsoft.Json;

namespace Glimpse.Database;

public class MusicDatabase : IMusicLibrary
{
    public const string DatabaseName = "Library";
    public const uint DatabaseVersion = 1;

    private readonly Logger _logger;
    private readonly string _databasePath;

    private readonly Dictionary<string, Track> _tracks;
    
    //public Dictionary<string, Track> Tracks = [];
    public Dictionary<string, Album> Albums = [];
    public Dictionary<string, Artist> Artists = [];
    public Dictionary<string, Genre> Genres = [];
    
    public MusicDatabase(Logger logger)
    {
        _logger = logger;
        _databasePath = Path.Combine(IConfigManager.BaseDir, $"{DatabaseName}.json");

        _tracks = [];

        // Handle first-time usage. TODO This will also deal with the migration from Database.json -> Library.json
        if (!File.Exists(_databasePath))
        {
            SaveLibrary();
            return;
        }

        // TODO: Handle null
        Library library = JsonConvert.DeserializeObject<Library>(File.ReadAllText(_databasePath));
        foreach (Track track in library.Tracks)
            _tracks.Add(track.Path, track);
    }

    private void SaveLibrary()
    {
        Library library = new Library(DatabaseVersion, [], [], _tracks.Values);
        string json = JsonConvert.SerializeObject(library);
        File.WriteAllText(_databasePath, json);
    }

    /*public void AddIndexToDatabase(in IndexResult index)
    {
        _logger.Log($"Adding indexed directory {index.Directory} to dataabase.");

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

        foreach ((string name, Artist artist) in index.Artists)
            Artists[name] = artist;
        
        foreach ((string name, Genre genre) in index.Genres)
            Genres[name] = genre;
        
        Refresh();
    }

    public static IndexResult IndexDirectory(string directory, AudioPlayer player, Logger logger, ref string current)
    {
        logger.Log($"Indexing directory {directory}.");

        Dictionary<string, Track> tracks = [];
        Dictionary<string, Album> albums = [];
        Dictionary<string, Artist> artists = [];
        Dictionary<string, Genre> genres = [];

        foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles("*.*", SearchOption.AllDirectories).OrderBy(info => info.Name))
        {
            logger.Log($"Indexing {file}");
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
                logger.Log($"Exception occurred while getting track info: {e}");
                continue;
            }

            tracks.Add(file.FullName, new Track(info));

            //if (info.Album != null)
            string albumName = info.Album ?? string.Empty;
            if (!albums.TryGetValue(albumName, out Album album))
            {
                album = new Album(albumName);
                albums.Add(albumName, album);
            }
            
            album.Tracks.Add(file.FullName);

            if (info.Artist != null)
            {
                if (!artists.TryGetValue(info.Artist, out Artist artist))
                {
                    artist = new Artist(info.Artist);
                    artists.Add(info.Artist, artist);
                }
                
                artist.Tracks.Add(file.FullName);
            }

            if (info.Genre != null)
            {
                if (!genres.TryGetValue(info.Genre, out Genre genre))
                {
                    genre = new Genre(info.Genre);
                    genres.Add(info.Genre, genre);
                }
                
                genre.Tracks.Add(file.FullName);
            }
        }

        return new IndexResult(directory, tracks, albums, artists, genres);
    }*/
}