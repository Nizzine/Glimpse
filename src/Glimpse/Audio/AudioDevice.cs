using MixrSharp;
using SDL3;

namespace Glimpse.Audio;

public sealed unsafe class AudioDevice : IDisposable
{
    private readonly Context _context;
    private readonly IntPtr _device;

    public AudioDevice(Context context, uint sampleRate)
    {
        _context = context;
        
        if (!SDL.Init(SDL.InitFlags.Audio))
            throw new Exception($"Failed to initialize SDL: {SDL.GetError()}");

        SDL.AudioSpec spec = new()
        {
            Freq = (int) sampleRate,
            Format = SDL.AudioFormat.AudioF32LE,
            Channels = 2
        };

        _device = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, in spec, AudioCallback, 0);
        if (_device == 0)
            throw new Exception($"Failed to open audio device: {SDL.GetError()}");
    }

    public void Play()
    {
        SDL.ResumeAudioStreamDevice(_device);
    }

    public void Pause()
    {
        SDL.PauseAudioStreamDevice(_device);
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
        SDL.DestroyAudioStream(_device);
    }
    
    private void AudioCallback(IntPtr userData, IntPtr stream, int additionalAmount, int totalAmount)
    {
        Span<float> buffer = new Span<float>((void*) stream, additionalAmount / 4);
        _context.MixToStereoF32Buffer(buffer);
    }
}