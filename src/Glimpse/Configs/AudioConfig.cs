using System.Text.Json.Serialization;

namespace Glimpse.Configs;

public struct AudioConfig() : IEquatable<AudioConfig>
{
    public uint SampleRate = 48000;

    public float Volume = 1.0f;
    
    public double SpeedAdjust = 1.0;

    public bool Equals(AudioConfig other)
    {
        return SampleRate == other.SampleRate && Volume.Equals(other.Volume) && SpeedAdjust.Equals(other.SpeedAdjust);
    }

    public override bool Equals(object? obj)
    {
        return obj is AudioConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SampleRate, Volume, SpeedAdjust);
    }

    public static bool operator ==(AudioConfig left, AudioConfig right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AudioConfig left, AudioConfig right)
    {
        return !left.Equals(right);
    }
}