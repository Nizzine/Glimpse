namespace Glimpse.Platforms.Empress;

public unsafe struct TrackMetadata
{
    public int TrackNumber;
    public sbyte* Title;
    public int NumArtists;
    public sbyte** Artists;
    public sbyte* Album;
    public nuint Length;
    public int NumGenres;
    public sbyte** Genres;
    public sbyte* ImageUri;
}