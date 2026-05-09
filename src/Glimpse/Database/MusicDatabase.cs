using System.Collections.Concurrent;
using System.Text.Json;
using Glimpse.API;
using Glimpse.API.Library;
using Glimpse.Audio;
using Track = Glimpse.API.Library.Track;

namespace Glimpse.Database;

public class MusicDatabase : IMusicLibrary
{
    public const string DatabaseName = "Library";
    public const uint DatabaseVersion = 1;

    private readonly Logger _logger;
    private readonly AudioPlayer _player;
    private readonly string _databasePath;
    
    private Task? _indexTask;

    /// <summary>
    /// A list of directories that have been explicitly added to the library.
    /// </summary>
    private readonly HashSet<string> _libraryPaths;
    
    /// <summary>
    /// A list of directories, within the Library Paths, that have been removed from the library.
    /// This is done so that any new subdirectories within the Library Paths will automatically be added, without
    /// Glimpse (or the user) needing to do anything.
    /// </summary>
    private readonly HashSet<string> _excludedDirectories;

    private ConcurrentDictionary<string, Track> _tracks;
    private ConcurrentDictionary<string, Album> _albums;
    private ConcurrentDictionary<string, Artist> _artists;
    private ConcurrentDictionary<string, Genre> _genres;

    public bool IsIndexing => !_indexTask?.IsCompleted ?? false;

    public IReadOnlyCollection<string> LibraryPaths => _libraryPaths;

    public IReadOnlyCollection<string> ExcludedDirectories => _excludedDirectories;

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
        Library library = JsonSerializer.Deserialize<Library>(File.ReadAllText(_databasePath), ConfigManager.GetDefaultSerializerOptions());

        _libraryPaths = library.LibraryPaths.ToHashSet();
        _excludedDirectories = library.ExcludedDirectories.ToHashSet();
        
        foreach (Track track in library.Tracks)
            _tracks.TryAdd(track.Path, track);
        
        foreach (Album album in library.Albums)
            _albums.TryAdd(album.Name, album);
        
        foreach (Artist artist in library.Artists)
            _artists.TryAdd(artist.Name, artist);

        foreach (Genre genre in library.Genres)
            _genres.TryAdd(genre.Name, genre);
    }
    
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

    // TODO: Rename this method to EnumerateLibraryPaths() ?
    public IReadOnlyCollection<string> GetLibraryPaths()
    {
        List<string> paths = [];
        foreach (string libraryPath in _libraryPaths)
        {
            paths.Add(libraryPath);
            
            foreach (string directory in Directory.GetDirectories(libraryPath, "*", SearchOption.AllDirectories))
            {
                if (_excludedDirectories.Contains(directory))
                    continue;

                foreach (string excluded in _excludedDirectories)
                {
                    if (directory.StartsWith(excluded))
                        goto SKIP;
                }
                
                paths.Add(directory);
                
                SKIP: ;
            }
        }

        return paths;
    }
    
    public void AddLibraryPath(string path, bool includeSubdirectories = true)
    {
        if (!includeSubdirectories)
            throw new NotImplementedException("Cannot add library path and not include subdirectories yet!");

        if (!_excludedDirectories.Remove(path))
            _libraryPaths.Add(path);
        
        SaveLibrary();
    }
    
    public void RemoveLibaryPath(string path, bool includeSubdirectories = true)
    {
        if (!includeSubdirectories)
            throw new NotImplementedException("Cannot remove library path but keep subdirectories yet!");
        
        if (!_libraryPaths.Remove(path))
            _excludedDirectories.Add(path); 
        
        SaveLibrary();
    }

    public void RemoveAllLibraryPaths()
    {
        _libraryPaths.Clear();
        _excludedDirectories.Clear();
    }

    public void Index()
    {
        _indexTask = Task.Run(IndexLibrary);
        //IndexLibrary(); // TODO: Make this threaded again
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
    
    public bool TryGetTracksForAlbum(string albumName, out SizedCollection<Track> tracks)
    {
        if (!_albums.TryGetValue(albumName, out Album album))
        {
            tracks = [];
            return false;
        }

        tracks = GetTracksFromPathList(album.Tracks);
        return true;
    }

    public bool TryGetTracksForArtist(string artistName, out SizedCollection<Track> tracks)
    {
        if (!_artists.TryGetValue(artistName, out Artist artist))
        {
            tracks = [];
            return false;
        }

        tracks = GetTracksFromPathList(artist.Tracks);
        return true;
    }
    
    public bool TryGetTracksForGenre(string genreName, out SizedCollection<Track> tracks)
    {
        if (!_genres.TryGetValue(genreName, out Genre genre))
        {
            tracks = [];
            return false;
        }

        tracks = GetTracksFromPathList(genre.Tracks);
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

    public bool InsertOrUpdateTrack(Track track)
    {
        bool trackExists = _tracks.ContainsKey(track.Path);
        _tracks[track.Path] = track;
        return !trackExists;
    }
    
    public bool InsertOrUpdateAlbum(Album album)
    {
        bool albumExists = _albums.ContainsKey(album.Name);
        _albums[album.Name] = album;
        return !albumExists;
    }
    
    public bool InsertOrUpdateArtist(Artist artist)
    {
        bool artistExists = _artists.ContainsKey(artist.Name);
        _artists[artist.Name] = artist;
        return !artistExists;
    }
    
    public bool InsertOrUpdateGenre(Genre genre)
    {
        bool genreExists = _genres.ContainsKey(genre.Name);
        _genres[genre.Name] = genre;
        return !genreExists;
    }

    private SizedCollection<Track> GetTracksFromPathList(IReadOnlyCollection<string> paths)
    {
        List<Track> trackList = [];
        foreach (string path in paths)
        {
            if (!TryGetTrack(path, out Track? track))
                continue;
            
            trackList.Add(track);
        }

        IEnumerable<Track> trackEnumerable = trackList.OrderBy(track => track.TrackNumber).ThenBy(track => track.Title);
        return new SizedCollection<Track>(trackEnumerable, (uint) trackList.Count);
    }

    private void SaveLibrary()
    {
        _logger.Log($"Saving library to {_databasePath}.");
        Library library = new Library(DatabaseVersion, _libraryPaths, _excludedDirectories,
            (IReadOnlyCollection<Track>) _tracks.Values, (IReadOnlyCollection<Album>) _albums.Values,
            (IReadOnlyCollection<Artist>) _artists.Values, (IReadOnlyCollection<Genre>) _genres.Values);
        string json = JsonSerializer.Serialize(library, ConfigManager.GetDefaultSerializerOptions());
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
            foreach (string directory in GetLibraryPaths())
            {
                foreach (string path in Directory.GetFiles(directory))
                {
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
                    
                    // TODO: Test CPU and disk usage with and without this sleep. I have a strong suspicion this 1ms
                    //       sleep is MUCH gentler on the CPU and disk.
                    Thread.Sleep(1);
                }
            }
        }

        /*_tracks = tracks;
        _albums = albums;
        _artists = artists;
        _genres = genres;*/
        
        SaveLibrary();
        _logger.Log("Indexing complete!");
    }

    public delegate void OnUpdate();
}