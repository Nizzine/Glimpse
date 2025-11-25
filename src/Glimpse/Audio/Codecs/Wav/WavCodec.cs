using Glimpse.API;
using Glimpse.API.Codecs;

namespace Glimpse.Audio.Codecs.Wav;

public class WavCodec : ICodec
{
    public bool FileIsSupported(string path, string extension)
    {
        return extension == ".wav";
    }

    public TrackInfo GetTrackInfo(string path)
        => CodecUtils.TrackInfoFromFile(path);
    
    public ICodecStream CreateStream(string path)
    {
        return new WavStream(path);
    }
}