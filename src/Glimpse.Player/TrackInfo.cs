using System;
using TagLib;

namespace Glimpse.Player;

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

    public static TrackInfo Null => new TrackInfo(null, null, null, null, null, null, null);

    public static TrackInfo FromFile(string path)
    {
        using File file = File.Create(path);

        uint trackNumber = file.Tag.Track;
        string title = file.Tag.Title;
        string artist = file.Tag.FirstPerformer;
        string album = file.Tag.Album;
        TimeSpan length = file.Properties.Duration;
        string genre = file.Tag.FirstGenre;
        
        Image albumArt = null;
        if (file.Tag.Pictures is { Length: > 0 })
        {
            IPicture picture = file.Tag.Pictures[0];
            albumArt = new Image(picture.Data?.Data, picture.Filename);
        }

        return new TrackInfo(trackNumber, title, artist, album, length, genre, albumArt);
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