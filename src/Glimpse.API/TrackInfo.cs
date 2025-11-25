namespace Glimpse.API;

public class TrackInfo
{
    public readonly uint? TrackNumber;
    
    public readonly string? Title;

    public readonly string? Artist;

    public readonly string? Album;

    public readonly TimeSpan? Length;

    public readonly string? Genre;

    public readonly Image? AlbumArt;

    public TrackInfo(uint? trackNumber, string? title, string? artist, string? album, TimeSpan? length, string? genre, Image? albumArt)
    {
        TrackNumber = trackNumber;
        Title = title;
        Artist = artist;
        Album = album;
        Length = length;
        Genre = genre;
        AlbumArt = albumArt;
    }

    public class Image
    {
        public byte[] Data;
        public string Location;

        public Image(byte[] data, string location)
        {
            Data = data;
            Location = location;
        }
    }
}