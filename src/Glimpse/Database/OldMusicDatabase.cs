using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Glimpse.API;
using Glimpse.API.Library;

namespace Glimpse.Database;

public class OldMusicDatabase : IConfig
{
    public const string DatabaseName = "Database";
    public const uint DatabaseVersion = 1;

    public uint Version;
    
    public Dictionary<string, Track> Tracks;
    public Dictionary<string, Album> Albums;
    public Dictionary<string, Artist> Artists;
    public Dictionary<string, Genre> Genres;
    
    [JsonConstructor]
    public OldMusicDatabase()
    {
        Version = DatabaseVersion;
        Tracks = [];
        Albums = [];
        Artists = [];
        Genres = [];
    }
}