using System.Text.Json.Serialization;

namespace Glimpse.Configs;

public struct PluginsConfig() : IEquatable<PluginsConfig>
{
    public HashSet<string> EnabledPlugins =
    [
        "Glimpse.OpenMPT"
    ];

    public bool Equals(PluginsConfig other)
    {
        return EnabledPlugins.SetEquals(other.EnabledPlugins);
    }

    public override bool Equals(object? obj)
    {
        return obj is PluginsConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return EnabledPlugins.GetHashCode();
    }

    public static bool operator ==(PluginsConfig left, PluginsConfig right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PluginsConfig left, PluginsConfig right)
    {
        return !left.Equals(right);
    }
}