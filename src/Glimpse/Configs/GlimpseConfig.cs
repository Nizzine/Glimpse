using System.Text.Json.Serialization;
using Glimpse.API;
using Glimpse.Forms;

namespace Glimpse.Configs;

public struct GlimpseConfig : IConfig, IEquatable<GlimpseConfig>
{
    public const string ConfigName = "Player";

    public string? Language;

    public bool SwapTransportControls;
    
    public uint SampleRate;

    public float Volume;
    
    public double SpeedAdjust;

    public HashSet<string> EnabledPlugins;

    public bool EnableFileDeletion;

    public bool EnableUpdateChecking;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Theme Theme;

    [JsonConstructor]
    public GlimpseConfig()
    {
        Language = null;
        SwapTransportControls = false;
        SampleRate = 48000;
        Volume = 1.0f;
        SpeedAdjust = 1.0;
        EnabledPlugins = ["Glimpse.OpenMPT"];
        EnableUpdateChecking = true;
        EnableFileDeletion = false;
        Theme = Theme.SyncToOS;
    }

    public bool Equals(GlimpseConfig other)
    {
        return Language == other.Language && SwapTransportControls == other.SwapTransportControls &&
               SampleRate == other.SampleRate && Volume.Equals(other.Volume) && SpeedAdjust.Equals(other.SpeedAdjust)
               && EnabledPlugins.SetEquals(other.EnabledPlugins) && EnableFileDeletion == other.EnableFileDeletion &&
               EnableUpdateChecking == other.EnableUpdateChecking && Theme == other.Theme;
    }

    public override bool Equals(object obj)
    {
        return obj is GlimpseConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
        hashCode.Add(Language);
        hashCode.Add(SwapTransportControls);
        hashCode.Add(SampleRate);
        hashCode.Add(Volume);
        hashCode.Add(SpeedAdjust);
        hashCode.Add(EnabledPlugins);
        hashCode.Add(EnableFileDeletion);
        hashCode.Add(EnableUpdateChecking);
        hashCode.Add(Theme);

        return hashCode.ToHashCode();
    }

    public static bool operator ==(GlimpseConfig left, GlimpseConfig right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GlimpseConfig left, GlimpseConfig right)
    {
        return !left.Equals(right);
    }
}