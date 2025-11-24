namespace Glimpse.API.Codecs;

public struct AudioFormat
{
    public DataType Type;

    public uint SampleRate;

    public byte Channels;

    public AudioFormat(DataType type, uint sampleRate, byte channels)
    {
        Type = type;
        SampleRate = sampleRate;
        Channels = channels;
    }
}