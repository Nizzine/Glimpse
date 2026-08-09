using System.Text.Json.Serialization;

namespace Glimpse.Configs;

public struct AppearanceConfig() : IEquatable<AppearanceConfig>
{
    public string Theme = global::Glimpse.Theme.DefaultThemeLight;
    
    public bool SwapTransportControls = false;

    public bool ConfineAlbumArtToSquare = true;

    public bool Equals(AppearanceConfig other)
    {
        return Theme == other.Theme && SwapTransportControls == other.SwapTransportControls &&
               ConfineAlbumArtToSquare == other.ConfineAlbumArtToSquare;
    }

    public override bool Equals(object? obj)
    {
        return obj is AppearanceConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Theme, SwapTransportControls, ConfineAlbumArtToSquare);
    }

    public static bool operator ==(AppearanceConfig left, AppearanceConfig right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AppearanceConfig left, AppearanceConfig right)
    {
        return !left.Equals(right);
    }
}