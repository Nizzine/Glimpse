using System.Text.Json.Serialization;

namespace Glimpse.Configs;

public struct GeneralConfig() : IEquatable<GeneralConfig>
{
    public string? Language = null;
    
    public bool EnableFileDeletion = false;

    public bool EnableUpdateChecking = true;

    public bool Equals(GeneralConfig other)
    {
        return Language == other.Language && EnableFileDeletion == other.EnableFileDeletion &&
               EnableUpdateChecking == other.EnableUpdateChecking;
    }

    public override bool Equals(object? obj)
    {
        return obj is GeneralConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Language, EnableFileDeletion, EnableUpdateChecking);
    }

    public static bool operator ==(GeneralConfig left, GeneralConfig right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GeneralConfig left, GeneralConfig right)
    {
        return !left.Equals(right);
    }
}