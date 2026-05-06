using System.Text.Json.Serialization;
using Glimpse.API;

namespace Glimpse.Configs;

public struct OldConfig : IConfig
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
    public PreferredColorScheme Theme;

    [JsonConstructor]
    public OldConfig()
    {
        Language = null;
        SwapTransportControls = false;
        SampleRate = 48000;
        Volume = 1.0f;
        SpeedAdjust = 1.0;
        EnabledPlugins = ["Glimpse.OpenMPT"];
        EnableUpdateChecking = true;
        EnableFileDeletion = false;
        Theme = PreferredColorScheme.SyncToOS;
    }

    public void PopulateNewConfig(ref GlimpseConfig config)
    {
        config.General.Language = Language;
        config.General.EnableFileDeletion = EnableFileDeletion;
        config.General.EnableUpdateChecking = EnableUpdateChecking;
        config.Appearance.PreferredColorScheme = Theme;
        config.Appearance.SwapTransportControls = SwapTransportControls;
        config.Audio.SampleRate = SampleRate;
        config.Audio.Volume = Volume;
        config.Audio.SpeedAdjust = SpeedAdjust;
        config.Plugins.EnabledPlugins = EnabledPlugins;
    }
}