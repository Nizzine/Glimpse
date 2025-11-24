using Glimpse.API;

namespace Glimpse.OpenMPT;

public class MptCodec : Codec
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
        throw new NotImplementedException();
    }

    public CodecStream CreateStream(string path)
    {
        return new MptStream(path, Config);
    }
}