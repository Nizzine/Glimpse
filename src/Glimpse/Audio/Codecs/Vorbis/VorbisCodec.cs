using Glimpse.API;
using Glimpse.API.Codecs;

namespace Glimpse.Audio.Codecs.Vorbis;

public class VorbisCodec : ICodec
{
    public bool FileIsSupported(string path, string extension)
    {
        return extension == ".ogg";
    }

    public TrackInfo GetTrackInfo(string path)
        => TrackInfo.FromFile(path);

    public ICodecStream CreateStream(string path)
    {
        return new VorbisStream(path);
    }
}