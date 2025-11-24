using Glimpse.API;

namespace Glimpse.Audio.Codecs;

public abstract class Codec
{
    public abstract bool FileIsSupported(string path, string extension);

    public abstract TrackInfo GetTrackInfo(string path);

    public abstract CodecStream CreateStream(string path);
}