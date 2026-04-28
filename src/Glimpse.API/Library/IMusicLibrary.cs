namespace Glimpse.API.Library;

public interface IMusicLibrary
{
    /// <summary>
    /// Gets the <see cref="Track"/>s in the library.
    /// </summary>
    public IReadOnlyCollection<Track> Tracks { get; }
    
    /// <summary>
    /// Gets the <see cref="Album"/>s in the library.
    /// </summary>
    public IReadOnlyCollection<Album> Albums { get; }
    
    /// <summary>
    /// Gets the <see cref="Artist"/>s in the library.
    /// </summary>
    public IReadOnlyCollection<Artist> Artists { get; }
    
    /// <summary>
    /// Gets the <see cref="Genre"/>s in the library.
    /// </summary>
    public IReadOnlyCollection<Genre> Genres { get; }

    /// <summary>
    /// Gets a full list of directories that will be indexed by the music library.
    /// </summary>
    /// <returns>A list of directories added to the music library.</returns>
    /// <remarks>Contrary to the data provided, internally, Glimpse only keeps track of directories that are
    /// <b>excluded</b> from the library. Therefore, Glimpse must calculate this every time the function is called.
    /// As such this can be a rather slow function to call, so only call it once, then cache the result.</remarks>
    public IReadOnlyCollection<string> GetLibraryPaths();

    /// <summary>
    /// Add a new directory to the library, that will be indexed.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="includeSubdirectories">If <see langword="true"/>, then all subdirectories will be added. Otherwise,
    /// only the given directory will be added.</param>
    /// <remarks>This does not need to be manually called whenever a new subdirectory is added
    /// (if <paramref name="includeSubdirectories"/> is <see langword="true"/>), as Glimpse will automatically add any
    /// new subdirectories to the library.</remarks>
    public void AddLibraryPath(string path, bool includeSubdirectories = true);

    /// <summary>
    /// Remove a directory in the library from indexing.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="includeSubdirectories">If <see langword="true"/>, all subdirectories will also be removed.</param>
    public void RemoveLibaryPath(string path, bool includeSubdirectories = true);

    /// <summary>
    /// Re-index the music library.
    /// </summary>
    /// <remarks>This function runs asynchronously.</remarks>
    public void Index();

    /// <summary>
    /// Get a <see cref="Track"/> from the library.
    /// </summary>
    /// <param name="path">The path to the track.</param>
    /// <returns>The <see cref="Track"/>.</returns>
    public bool TryGetTrack(string path, out Track? track);

    /// <summary>
    /// Update a <see cref="Track"/> in the library.
    /// </summary>
    /// <param name="track">The <see cref="Track"/> to update.</param>
    public bool UpdateTrack(Track track);

    public bool TryGetTracksFromAlbum(string albumName, out IReadOnlyCollection<Track> tracks);
}