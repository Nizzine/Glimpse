namespace Glimpse.Audio;

public struct PlayerSettings
{
    public uint SampleRate;

    public float Volume;
    
    public double SpeedAdjust;
    
    public PlayerSettings(uint sampleRate = 48000, float volume = 1.0f, double speedAdjust = 1.0)
    {
        SampleRate = sampleRate;
        Volume = volume;
        SpeedAdjust = speedAdjust;
    }
}