namespace Glimpse.API.Codecs;

public interface ICodec
{
    public bool FileIsSupported(string path, string extension);

    public TrackInfo GetTrackInfo(string path);

    public ICodecStream CreateStream(string path);
}