using System.Runtime.CompilerServices;
using Glimpse.API;
using Glimpse.API.Codecs;
using OpenMPT.NET;

namespace Glimpse.OpenMPT;

public class MptStream : ICodecStream
{
    private readonly Module _module;

    private int _position;

    public TrackInfo TrackInfo { get; }

    public AudioFormat Format =>
        new AudioFormat(DataType.Float, (uint) _module.SampleRate, (byte) _module.Channels);

    public ulong LengthInSamples => (ulong) (_module.DurationInSeconds * _module.SampleRate);

    public MptStream(string path, MptConfig config)
    {
        _module = Module.FromMemory(File.ReadAllBytes(path), new ModuleOptions()
        {
            EmulateAmigaResampler = config.EmulateAmigaResampler,
            EndBehavior = config.FadeOutAtEnd ? EndBehavior.FadeOut : EndBehavior.Stop
        });
        
        _module.SetParameter(ModuleParameter.InterpolationFilterLength, config.ResamplerFilterMode);

        ModuleMetadata metadata = _module.Metadata;
        TrackInfo = new TrackInfo(null, metadata.Title ?? Path.GetFileNameWithoutExtension(path), metadata.Artist, null,
            TimeSpan.FromSeconds(_module.DurationInSeconds), null, null);
    }
    
    public unsafe ulong GetBuffer(Span<byte> buffer)
    {
        ulong totalBytes = 0;

        while (totalBytes < (ulong) buffer.Length)
        {
            uint samples = (uint) _module.AdvanceBuffer();

            if (samples == 0)
                break;

            uint copyAmount = (uint) (samples * _module.Channels * sizeof(float));
            if (totalBytes + copyAmount >= (ulong) buffer.Length)
                copyAmount = (uint) (buffer.Length - (int) totalBytes);
            
            fixed (byte* pBuffer = buffer)
            fixed (float* pModuleBuffer = _module.Buffer)
                Unsafe.CopyBlock(pBuffer + totalBytes, pModuleBuffer, copyAmount);

            totalBytes += copyAmount;
        }

        return totalBytes;
    }

    public void Seek(ulong sample)
    {
        _module.Seek(sample / (double) _module.SampleRate);
    }

    public void Dispose()
    {
        _module.Dispose();
    }
}