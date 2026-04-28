namespace Glimpse.API.Library;

public interface IMusicLibrary
{
    public IReadOnlyCollection<Track> Tracks { get; }
}