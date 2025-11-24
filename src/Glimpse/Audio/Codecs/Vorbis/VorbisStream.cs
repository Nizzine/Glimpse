using Glimpse.API;
using Glimpse.API.Codecs;
using MixrSharp;
using AudioFormat = Glimpse.API.Codecs.AudioFormat;

namespace Glimpse.Audio.Codecs.Vorbis;

public class VorbisStream : ICodecStream
{
    private readonly MixrSharp.Stream.Vorbis _vorbis;

    public TrackInfo TrackInfo { get; }
    
    public AudioFormat Format => _vorbis.Format.ToGlimpse();

    public ulong LengthInSamples => _vorbis.LengthInSamples;

    public VorbisStream(string path)
    {
        _vorbis = new MixrSharp.Stream.Vorbis(path);
        TrackInfo = TrackInfo.FromFile(path);
    }
    
    public ulong GetBuffer(Span<byte> buffer)
        => _vorbis.GetBuffer(buffer);

    public void Seek(ulong sample)
    {
        _vorbis.SeekToSample(sample);
    }

    public void Dispose()
    {
        _vorbis.Dispose();
    }
}