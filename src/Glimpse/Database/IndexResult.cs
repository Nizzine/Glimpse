namespace Glimpse.Database;

public struct IndexResult
{
    public string Directory;
    
    public Dictionary<string, Track> Tracks;

    public Dictionary<string, Album> Albums;

    public Dictionary<string, Artist> Artists;

    public IndexResult(string directory, Dictionary<string, Track> tracks, Dictionary<string, Album> albums, Dictionary<string, Artist> artists)
    {
        Directory = directory;
        Tracks = tracks;
        Albums = albums;
        Artists = artists;
    }
}