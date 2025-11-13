using System;
using System.Collections.Generic;

namespace Glimpse.Player.Configs;

public struct PlayerConfig : IConfig, IEquatable<PlayerConfig>
{
    public const string ConfigName = "Player";

    public bool SwapTransportControls;
    
    public uint SampleRate;

    public float Volume;
    
    public double SpeedAdjust;

    public bool AutoPlay;

    public HashSet<string> EnabledPlugins;

    public PlayerConfig()
    {
        SwapTransportControls = false;
        SampleRate = 48000;
        Volume = 1.0f;
        SpeedAdjust = 1.0;
        AutoPlay = true;
        EnabledPlugins = ["Glimpse.OpenMPT"];
    }

    public bool Equals(PlayerConfig other)
    {
        return SwapTransportControls == other.SwapTransportControls && SampleRate == other.SampleRate &&
               Volume.Equals(other.Volume) && SpeedAdjust.Equals(other.SpeedAdjust) && AutoPlay == other.AutoPlay &&
               EnabledPlugins.SetEquals(other.EnabledPlugins);
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerConfig other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SampleRate, Volume, SpeedAdjust, AutoPlay, EnabledPlugins);
    }

    public static bool operator ==(PlayerConfig left, PlayerConfig right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlayerConfig left, PlayerConfig right)
    {
        return !left.Equals(right);
    }
}