using System;
using System.Threading.Tasks;
using Glimpse.Player.Codecs;
using Glimpse.Player.Configs;
using MixrSharp;

namespace Glimpse.Player;

public class Track : IDisposable
{
    private CodecStream _stream;
    private AudioFormat _format;
    private AudioSource _source;

    private byte[] _audioBuffer;
    private AudioBuffer[] _buffers;
    private int _currentBuffer;

    private ulong _elapsedBytes;
    private ulong _bytesConsumed;

    private Action _onFinish;

    public readonly TrackInfo Info;
    
    public readonly int LengthInSeconds;

    public int ElapsedSeconds
    {
        get
        {
            ulong elapsedSamples = _elapsedBytes / _format.BytesPerSample / _format.Channels;
            elapsedSamples += _source.Position;

            // TODO: Make this better.
            return (int) (elapsedSamples / _format.SampleRate);
        }
    }

    public int SecondsConsumed
    {
        get
        {
            ulong samplesConsumed = _bytesConsumed / _format.BytesPerSample / _format.Channels;
            samplesConsumed += _source.Position;
            return (int) (samplesConsumed / _format.SampleRate);
        }
    }
    
    public TrackState State
    {
        get
        {
            return _source.State switch
            {
                SourceState.Stopped => TrackState.Stopped,
                SourceState.Paused => TrackState.Paused,
                SourceState.Playing => TrackState.Playing,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    internal Track(Context context, CodecStream stream, TrackInfo info, PlayerConfig config, Action onFinish)
    {
        _stream = stream;
        _onFinish = onFinish;
        Info = info;

        _format = stream.Format;
        Logger.Log($"DataType: {_format.DataType}");
        Logger.Log($"SampleRate: {_format.SampleRate}");
        Logger.Log($"Channels: {_format.Channels}");

        LengthInSeconds = (int) (_stream.LengthInSamples / _format.SampleRate);
        Logger.Log($"LengthInSeconds: {LengthInSeconds}");

        Logger.Log("Creating source.");
        _source = context.CreateSource(new SourceDescription(SourceType.Pcm, _format));
        
        _audioBuffer = new byte[_format.SampleRate * _format.Channels * _format.BytesPerSample];

        Logger.Log("Creating audio buffers.");
        _buffers = new AudioBuffer[2];
        for (int i = 0; i < _buffers.Length; i++)
        {
            _stream.GetBuffer(_audioBuffer);
            _buffers[i] = context.CreateBuffer(_audioBuffer);
            _source.SubmitBuffer(_buffers[i]);
        }

        // The source will loop the last buffer if it runs out of buffers. It won't sound nice but at least it will
        // continue to play.
        _source.Looping = true;

        _source.Volume = config.Volume;
        _source.Speed = config.SpeedAdjust;
        
        _source.BufferFinished += BufferFinished;
    }

    public void Play()
    {
        Logger.Log("Playing.");
        _source.Play();
    }

    public void Pause()
    {
        Logger.Log("Pausing.");
        _source.Pause();
    }

    public void Seek(int second)
    {
        Logger.Log($"Seeking to {second}s.");
        
        SourceState state = _source.State;
        Logger.Log("  Pausing source.");
        _source.Pause();
        Logger.Log("  Seeking stream.");
        _stream.Seek((ulong) (second * _format.SampleRate));
        Logger.Log("  Clearing buffers.");
        _source.ClearBuffers();
        _currentBuffer = 0;
        Logger.Log("  Updating buffers.");
        for (int i = 0; i < _buffers.Length; i++)
        {
            _stream.GetBuffer(_audioBuffer);
            _buffers[i].Update(_audioBuffer);
            _source.SubmitBuffer(_buffers[i]);
        }

        if (state == SourceState.Playing)
        {
            Logger.Log("  Playing.");
            _source.Play();
        }

        _elapsedBytes = (ulong) (second * _format.SampleRate * _format.Channels * _format.BytesPerSample);
    }
    
    private void BufferFinished()
    {
        _elapsedBytes += (ulong) _audioBuffer.Length;
        _bytesConsumed += (ulong) _audioBuffer.Length;
        
        Task.Run(() =>
        {
            ulong bytesProcessed = _stream.GetBuffer(_audioBuffer);

            if (bytesProcessed == 0)
            {
                // Disable looping so the source can successfully stop.
                _source.Looping = false;
                _onFinish();
                return;
            }

            if ((int) bytesProcessed < _audioBuffer.Length)
                _buffers[_currentBuffer].Update(_audioBuffer[..(int) bytesProcessed]);
            else
                _buffers[_currentBuffer].Update(_audioBuffer);
            _source.SubmitBuffer(_buffers[_currentBuffer]);

            _currentBuffer++;
            if (_currentBuffer >= _buffers.Length)
                _currentBuffer = 0;
        });
    }

    public void Dispose()
    {
        Logger.Log("Disposing source.");
        _source.Dispose();
        Logger.Log("Disposing buffers.");
        foreach (AudioBuffer buffer in _buffers)
            buffer.Dispose();
        Logger.Log("Disposing stream.");
        _stream.Dispose();
    }
}