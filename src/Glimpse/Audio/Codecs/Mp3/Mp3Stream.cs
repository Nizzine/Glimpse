using Glimpse.API;
using Glimpse.API.Codecs;
using MixrSharp;
using AudioFormat = Glimpse.API.Codecs.AudioFormat;

namespace Glimpse.Audio.Codecs.Mp3;

public class Mp3Stream : ICodecStream
{
    private readonly MixrSharp.Stream.Mp3 _mp3;

    public TrackInfo TrackInfo { get; }
    
    public AudioFormat Format => _mp3.Format.ToGlimpse();

    public ulong LengthInSamples => _mp3.LengthInSamples;

    public Mp3Stream(string path)
    {
        _mp3 = new MixrSharp.Stream.Mp3(path);
        TrackInfo = CodecUtils.TrackInfoFromFile(path);
    }
    
    public ulong GetBuffer(Span<byte> buffer)
        => _mp3.GetBuffer(buffer);

    public void Seek(ulong sample)
    {
        _mp3.SeekToSample(sample);
    }

    public void Dispose()
    {
        _mp3.Dispose();
    }
}