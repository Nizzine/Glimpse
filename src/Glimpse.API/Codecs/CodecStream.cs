namespace Glimpse.API.Codecs;

public interface ICodecStream : IDisposable
{
    public TrackInfo TrackInfo { get; }
    
    public AudioFormat Format { get; }
    
    public ulong LengthInSamples { get; }

    public ulong GetBuffer(Span<byte> buffer);

    // TODO: Return ulong of the current seek position.
    public void Seek(ulong sample);
    
    public void Dispose();
}