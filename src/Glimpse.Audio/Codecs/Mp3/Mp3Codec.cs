using Glimpse.API;
using Glimpse.API.Codecs;

namespace Glimpse.Audio.Codecs.Mp3;

public class Mp3Codec : ICodec
{
    public bool FileIsSupported(string path, string extension)
    {
        return extension == ".mp3";
    }

    public TrackInfo GetTrackInfo(string path)
        => CodecUtils.TrackInfoFromFile(path);

    public ICodecStream CreateStream(string path)
    {
        return new Mp3Stream(path);
    }
}