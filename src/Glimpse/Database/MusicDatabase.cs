using Glimpse.API;
using Glimpse.API.Library;
using Glimpse.Audio;
using Newtonsoft.Json;
using Track = Glimpse.API.Library.Track;

namespace Glimpse.Database;

public class MusicDatabase : IMusicLibrary
{
    public event OnUpdate Update = delegate { };
    
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

    public SizedCollection<Track> GetTracks()
    {
        var tracks = _tracks.Values;
        IEnumerable<Track> trackEnumerable = tracks.OrderBy(track => track.Album).ThenBy(track => track.TrackNumber);

        return new SizedCollection<Track>(trackEnumerable, (uint) tracks.Count);
    }
    
    public SizedCollection<Album> GetAlbums()
    {
        var albums = _albums.Values;
        IEnumerable<Album> albumEnumerable = albums.OrderBy(album => album.Name);

        return new SizedCollection<Album>(albumEnumerable, (uint) albums.Count);
    }
    
    public SizedCollection<Artist> GetArtists()
    {
        var artists = _artists.Values;
        IEnumerable<Artist> artistEnumerable = artists.OrderBy(artist => artist.Name);

        return new SizedCollection<Artist>(artistEnumerable, (uint) artists.Count);
    }
    
    public SizedCollection<Genre> GetGenres()
    {
        var genres = _genres.Values;
        IEnumerable<Genre> genreEnumerable = genres.OrderBy(genre => genre.Name);

        return new SizedCollection<Genre>(genreEnumerable, (uint) genres.Count);
    }

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
        Task.Run(IndexLibrary);
        //IndexLibrary(); // TODO: Make this threaded again
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
    
    public bool TryGetTracksFromAlbum(string albumName, out SizedCollection<Track> tracks)
    {
        if (!_albums.TryGetValue(albumName, out Album album))
        {
            tracks = [];
            return false;
        }

        List<Track> trackList = [];
        foreach (string path in album.Tracks)
        {
            if (!TryGetTrack(path, out Track? track))
                continue;
            
            trackList.Add(track);
        }

        IEnumerable<Track> trackEnumerable = trackList.OrderBy(track => track.TrackNumber).ThenBy(track => track.Title);
        tracks = new SizedCollection<Track>(trackEnumerable, (uint) trackList.Count);
        return true;
    }
    
    public bool UpdateTrack(Track track)
    {
        _tracks[track.Path] = track;
        SaveLibrary();

        // TODO: Check if track exists in the library.
        return true;
    }

    public bool TryGetAlbum(string albumName, out Album album)
    {
        return _albums.TryGetValue(albumName, out album);
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
        /*Dictionary<string, Track> tracks = [];
        Dictionary<string, Album> albums = [];
        Dictionary<string, Artist> artists = [];
        Dictionary<string, Genre> genres = [];*/
        
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
                _tracks[path] = track;

                if (track.Album != null)
                {
                    if (!_albums.TryGetValue(track.Album, out Album album))
                    {
                        album = new Album(track.Album, []);
                        _albums[album.Name] = album;
                    }

                    album.Tracks.Add(track.Path);
                }

                if (track.Artist != null)
                {
                    if (!_artists.TryGetValue(track.Artist, out Artist artist))
                    {
                        artist = new Artist(track.Artist, []);
                        _artists[artist.Name] = artist;
                    }

                    artist.Tracks.Add(track.Path);
                }

                if (track.Genre != null)
                {
                    if (!_genres.TryGetValue(track.Genre, out Genre genre))
                    {
                        genre = new Genre(track.Genre, []);
                        _genres[genre.Name] = genre;
                    }

                    genre.Tracks.Add(track.Path);
                }
                
                // TODO: Not having this here seems to cause major issues with the UI.
                //       obviously this is not a solution! Fix this!
                Thread.Sleep(10);

                Update();
            }
        }

        /*_tracks = tracks;
        _albums = albums;
        _artists = artists;
        _genres = genres;*/
        
        SaveLibrary();
        _logger.Log("Indexing complete!");
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

    public delegate void OnUpdate();
}