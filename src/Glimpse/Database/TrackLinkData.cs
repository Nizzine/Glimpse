namespace Glimpse.Database;

/// <summary>
/// Represents named data that links to a track.
/// </summary>
public abstract class TrackLinkData
{
    public string Name;

    public List<string> Tracks;

    protected TrackLinkData(string name)
    {
        Name = name;
        Tracks = [];
    }
}