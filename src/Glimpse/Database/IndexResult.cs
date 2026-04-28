using Glimpse.API.Library;

namespace Glimpse.Database;

public struct IndexResult(
    string directory,
    Dictionary<string, Track> tracks,
    Dictionary<string, Album> albums,
    Dictionary<string, Artist> artists,
    Dictionary<string, Genre> genres)
{
    public string Directory = directory;
    
    public Dictionary<string, Track> Tracks = tracks;

    public Dictionary<string, Album> Albums = albums;

    public Dictionary<string, Artist> Artists = artists;
    
    public Dictionary<string, Genre> Genres = genres;
}