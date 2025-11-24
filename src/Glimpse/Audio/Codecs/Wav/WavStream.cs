using Glimpse.API;
using Glimpse.API.Codecs;
using AudioFormat = Glimpse.API.Codecs.AudioFormat;

namespace Glimpse.Audio.Codecs.Wav;

public class WavStream : ICodecStream
{
    private readonly MixrSharp.Stream.Wav _wav;

    public TrackInfo TrackInfo { get; }
    
    public AudioFormat Format => _wav.Format.ToGlimpse();

    public ulong LengthInSamples => _wav.LengthInSamples;

    public WavStream(string path)
    {
        _wav = new MixrSharp.Stream.Wav(path);
        TrackInfo = TrackInfo.FromFile(path);
    }

    public ulong GetBuffer(Span<byte> buffer)
        => _wav.GetBuffer(buffer);

    public void Seek(ulong sample)
    {
        _wav.SeekToSample(sample);
    }

    public void Dispose()
    {
        _wav.Dispose();
    }
}