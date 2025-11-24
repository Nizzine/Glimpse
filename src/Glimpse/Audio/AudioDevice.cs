using MixrSharp;
using Silk.NET.SDL;

namespace Glimpse.Audio;

public sealed unsafe class AudioDevice : IDisposable
{
    private readonly Context _context;
    private readonly Sdl _sdl;
    private readonly uint _device;

    public AudioDevice(Context context, uint sampleRate)
    {
        _context = context;

        _sdl = Sdl.GetApi();
        if (_sdl.Init(Sdl.InitAudio) < 0)
            throw new Exception($"Failed to initialize SDL: {_sdl.GetErrorS()}");

        AudioSpec spec = new AudioSpec
        {
            Freq = (int) sampleRate,
            Format = Sdl.AudioF32,
            Channels = 2,
            Samples = 512,
            Callback = new PfnAudioCallback(AudioCallback)
        };

        _device = _sdl.OpenAudioDevice((byte*) null, 0, &spec, null, 0);
        if (_device == 0)
            throw new Exception($"Failed to open audio device: {_sdl.GetErrorS()}");
    }

    public void Play()
    {
        _sdl.PauseAudioDevice(_device, 0);
    }

    public void Pause()
    {
        _sdl.PauseAudioDevice(_device, 1);
    }

    /*public void Lock()
    {
        _sdl.LockAudioDevice(_device);
    }

    public void Unlock()
    {
        _sdl.UnlockAudioDevice(_device);
    }*/
    
    public void Dispose()
    {
        _sdl.CloseAudioDevice(_device);
    }
    
    private void AudioCallback(void* arg0, byte* arg1, int arg2)
    {
        Span<float> buffer = new Span<float>(arg1, arg2 / 4);
        _context.MixToStereoF32Buffer(buffer);
    }
}