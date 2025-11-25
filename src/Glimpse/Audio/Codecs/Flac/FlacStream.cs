using Glimpse.API;
using Glimpse.API.Codecs;
using MixrSharp;
using AudioFormat = Glimpse.API.Codecs.AudioFormat;

namespace Glimpse.Audio.Codecs.Flac;

public class FlacStream : ICodecStream
{
    private readonly MixrSharp.Stream.Flac _flac;

    public TrackInfo TrackInfo { get; }
    
    public AudioFormat Format => _flac.Format.ToGlimpse();

    public ulong LengthInSamples => _flac.LengthInSamples;

    public FlacStream(string path)
    {
        _flac = new MixrSharp.Stream.Flac(path);
        TrackInfo = CodecUtils.TrackInfoFromFile(path);
    }

    public ulong GetBuffer(Span<byte> buffer)
        => _flac.GetBuffer(buffer);

    public void Seek(ulong sample)
    {
        _flac.SeekToSample(sample);
    }

    public void Dispose()
    {
        _flac.Dispose();
    }
}