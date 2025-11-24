using Glimpse.API;
using Glimpse.API.Codecs;

namespace Glimpse.Audio.Codecs.Flac;

public class FlacCodec : ICodec
{
    public bool FileIsSupported(string path, string extension)
    {
        return extension == ".flac";
    }

    public TrackInfo GetTrackInfo(string path)
        => TrackInfo.FromFile(path);

    public ICodecStream CreateStream(string path)
    {
        return new FlacStream(path);
    }
}