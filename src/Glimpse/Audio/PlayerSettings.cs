namespace Glimpse.Audio;

public struct PlayerSettings
{
    public uint SampleRate;

    public float Volume;
    
    public double SpeedAdjust;

    public bool AutoPlay;

    public PlayerSettings(uint sampleRate = 48000, float volume = 1.0f, double speedAdjust = 1.0, bool autoPlay = true)
    {
        SampleRate = sampleRate;
        Volume = volume;
        SpeedAdjust = speedAdjust;
        AutoPlay = autoPlay;
    }
}