using Glimpse.API;
using Glimpse.API.Library;
using Glimpse.Audio;
using Newtonsoft.Json;
using Track = Glimpse.API.Library.Track;

namespace Glimpse.Database;

public class MusicDatabase : IMusicLibrary
{
    public const string DatabaseName = "Library";
    public const uint DatabaseVersion = 1;

    private readonly Logger _logger;
    private readonly AudioPlayer _player;
    private readonly string _databasePath;

    /// <summary>
    /// A list of directories that have been explicitly added to the library.
    /// </summary>
    private readonly List<string> _libraryPaths;
    
    /// <summary>
    /// A list of directories, within the Library Paths, that have been removed from the library.
    /// This is done so that any new subdirectories within the Library Paths will automatically be added, without
    /// Glimpse (or the user) needing to do anything.
    /// </summary>
    private readonly List<string> _excludedDirectories;

    private Dictionary<string, Track> _tracks;
    private Dictionary<string, Album> _albums;
    private Dictionary<string, Artist> _artists;
    private Dictionary<string, Genre> _genres;

    public IReadOnlyCollection<Track> Tracks => _tracks.Values;

    public IReadOnlyCollection<Album> Albums => _albums.Values;

    public IReadOnlyCollection<Artist> Artists => _artists.Values;

    public IReadOnlyCollection<Genre> Genres => _genres.Values;
    
    public IReadOnlyCollection<string> GetLibraryPaths()
    {
        throw new NotImplementedException();
    }
    
    public void AddLibraryPath(string path, bool includeSubdirectories = true)
    {
        throw new NotImplementedException();
    }
    
    public void RemoveLibaryPath(string path, bool includeSubdirectories = true)
    {
        throw new NotImplementedException();
    }
    
    public void Index()
    {
        //Task.Run(IndexLibrary);
        IndexLibrary(); // TODO: Make this threaded again
    }

    public MusicDatabase(Logger logger, AudioPlayer player)
    {
        _logger = logger;
        _player = player;
        _databasePath = Path.Combine(IConfigManager.BaseDir, $"{DatabaseName}.json");

        _libraryPaths = [];
        _excludedDirectories = [];
        
        _tracks = [];
        _albums = [];
        _artists = [];
        _genres = [];

        // Handle first-time usage. TODO This will also deal with the migration from Database.json -> Library.json
        if (!File.Exists(_databasePath))
        {
            SaveLibrary();
            return;
        }

        // TODO: Handle null
        Library library = JsonConvert.DeserializeObject<Library>(File.ReadAllText(_databasePath));

        _libraryPaths = library.LibraryPaths.ToList();
        _excludedDirectories = library.ExcludedDirectories.ToList();
        
        foreach (Track track in library.Tracks)
            _tracks.Add(track.Path, track);
        
        foreach (Album album in library.Albums)
            _albums.Add(album.Name, album);
        
        foreach (Artist artist in library.Artists)
            _artists.Add(artist.Name, artist);

        foreach (Genre genre in library.Genres)
            _genres.Add(genre.Name, genre);
    }
    
    public bool TryGetTrack(string path, out Track? track)
    {
        if (!_tracks.TryGetValue(path, out Track trk))
        {
            track = null;
            return false;
        }

        track = trk;
        return true;
    }
    
    public bool UpdateTrack(Track track)
    {
        _tracks[track.Path] = track;
        SaveLibrary();

        // TODO: Check if track exists in the library.
        return true;
    }

    private void SaveLibrary()
    {
        _logger.Log($"Saving library to {_databasePath}.");
        Library library = new Library(DatabaseVersion, _libraryPaths, _excludedDirectories, _tracks.Values, _albums.Values, _artists.Values,
            _genres.Values);
        string json = JsonConvert.SerializeObject(library, Formatting.Indented);
        File.WriteAllText(_databasePath, json);
    }

    private void IndexLibrary()
    {
        // TODO: Make this thread safe.
        Dictionary<string, Track> tracks = [];
        Dictionary<string, Album> albums = [];
        
        foreach (string libraryPath in _libraryPaths)
        {
            _logger.Log($"Indexing library path: {libraryPath}");

            // TODO: Could probably be made more efficient using recursive-esque pattern?
            string[] paths = Directory.GetFiles(libraryPath, "*", SearchOption.AllDirectories);
            foreach (string path in paths)
            {
                // TODO: Excluded Paths
                _logger.Log($"  Indexing {path}");
                if (!_player.TryGetTrackInfoForFile(path, out TrackInfo info))
                {
                    _logger.Log("    ... failed.");
                    continue;
                }

                TryGetTrack(path, out Track? oldTrack);
                Track track = new Track(path, info, oldTrack);
                
                tracks.Add(path, track);
            }
        }

        _tracks = tracks;
        _albums = albums;
        
        SaveLibrary();
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