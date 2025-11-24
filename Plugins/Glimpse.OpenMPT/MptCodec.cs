using Glimpse.API;
using Glimpse.API.Codecs;

namespace Glimpse.OpenMPT;

public class MptCodec : ICodec
{
    public MptConfig Config;

    public MptCodec(MptConfig config)
    {
        Config = config;
    }
    
    public bool FileIsSupported(string path, string extension)
    {
        return extension is ".it" or ".xm" or ".mod" or ".s3m" or ".mptm";
    }

    public TrackInfo GetTrackInfo(string path)
    {
        using MptStream stream = new MptStream(path, Config);
        return stream.TrackInfo;
    }

    public ICodecStream CreateStream(string path)
    {
        return new MptStream(path, Config);
    }
}