using System.Text.Json.Serialization;

namespace Glimpse.Configs;

public struct AppearanceConfig() : IEquatable<AppearanceConfig>
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PreferredColorScheme PreferredColorScheme = PreferredColorScheme.SyncToOS;
    
    public string Theme = "Glimpse";
    
    public bool SwapTransportControls = false;

    public bool Equals(AppearanceConfig other)
    {
        return PreferredColorScheme == other.PreferredColorScheme && Theme == other.Theme &&
               SwapTransportControls == other.SwapTransportControls;
    }

    public override bool Equals(object? obj)
    {
        return obj is AppearanceConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) PreferredColorScheme, Theme, SwapTransportControls);
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