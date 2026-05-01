using System.Collections;
using Glimpse.API;
using Glimpse.API.Codecs;
using Glimpse.Audio.Codecs.Flac;
using Glimpse.Audio.Codecs.Mp3;
using Glimpse.Audio.Codecs.Vorbis;
using Glimpse.Audio.Codecs.Wav;
using MixrSharp;

namespace Glimpse.Audio;

public class AudioPlayer : IAudioPlayer, IDisposable
{
    public event IAudioPlayer.OnTrackChanged TrackChanged = delegate { };

    public event IAudioPlayer.OnStateChanged StateChanged = delegate { };

    private readonly ILogger? _logger;
    private readonly Context _context;
    private readonly AudioDevice _device;
    
    private Track? _activeTrack;

    private int _currentTrackIndex;

    private PlayerSettings _settings;
    private readonly List<ICodec> _codecs;

    public readonly List<string> QueuedTracks;
    
    public IReadOnlyList<ICodec> Codecs => _codecs;

    public float Volume
    {
        get => _context.MasterVolume;
        set => _context.MasterVolume = value;
    }

    public double Speed
    {
        get => field;
        set
        {
            double speed = double.Clamp(value, 0, 32);
            field = speed;
            _activeTrack?.Speed = speed;
        }
    }

    public RepeatMode Repeat { get; set; }
    
    public ShuffleMode Shuffle { get; set; }

    public TimeSpan ElapsedTime => TimeSpan.FromSeconds(_activeTrack?.ElapsedSeconds ?? 0);

    public TimeSpan ConsumedTime => TimeSpan.FromSeconds(_activeTrack?.SecondsConsumed ?? 0);

    public TimeSpan TrackLength => TimeSpan.FromSeconds(_activeTrack?.LengthInSeconds ?? 0);

    public TrackInfo? CurrentTrack => _activeTrack?.Info;

    public TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public int CurrentTrackIndex => _currentTrackIndex;

    public string CurrentTrackPath => QueuedTracks.Count == 0 ? string.Empty : QueuedTracks[_currentTrackIndex];

    public AudioPlayer(ILogger? logger, PlayerSettings settings)
    {
        _logger = logger;
        _settings = settings;
        
        _logger?.Log("Creating context.");
        _context = new Context(_settings.SampleRate);
        Volume = _settings.Volume;
        Speed = settings.SpeedAdjust;
        
        _logger?.Log("Creating device.");
        _device = new AudioDevice(_context, _settings.SampleRate);

        _logger?.Log("Initializing codecs.");
        _codecs = [new Mp3Codec(), new FlacCodec(), new VorbisCodec(), new WavCodec()];

        QueuedTracks = new List<string>();
    }

    /// <summary>
    /// Queue a track at the given slot.
    /// </summary>
    /// <param name="path">The path to the track file.</param>
    /// <param name="slot">The <see cref="QueueSlot"/> to insert the track at.</param>
    public void QueueTrack(string path, QueueSlot slot, bool autoPlay = true)
    {
        _logger?.Log($"Queueing track {path}");

        bool isFirstQueue = autoPlay && QueuedTracks.Count == 0;

        switch (slot)
        {
            case QueueSlot.AtEnd:
                QueuedTracks.Add(path);
                break;
            case QueueSlot.NextTrack:
                InsertTrackAtIndex(_currentTrackIndex + 1, path);
                break;
            case QueueSlot.Clear:
                QueuedTracks.Clear();
                QueuedTracks.Add(path);
                isFirstQueue = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
        }
        
        if (isFirstQueue)
            TryChangeTrack(0);
    }

    public void QueueTracks(IEnumerable<string> paths, QueueSlot slot)
    {
        if (slot == QueueSlot.Clear)
        {
            QueuedTracks.Clear();
            slot = QueueSlot.AtEnd;
        }
        
        foreach (string path in paths)
            QueueTrack(path, slot, false);
    }

    public bool TryChangeTrack(int queueIndex)
    {
        _logger?.Log($"Changing to track {queueIndex}.");

        if (queueIndex >= QueuedTracks.Count || queueIndex < 0)
        {
            Stop();
            return false;
        }

        string path = QueuedTracks[queueIndex];

        if (!File.Exists(path))
            return false;
        
        //_logger?.Log("  Locking device.");
        //_device.Lock();
        Track oldTrack = _activeTrack;

        _currentTrackIndex = queueIndex;
        
        _logger?.Log($"  Creating codec stream from file {path}");
        ICodecStream stream = CreateStreamFromFile(path);
        TrackInfo info = stream.TrackInfo;

        _logger?.Log("  Creating track.");
        _activeTrack = new Track(_context, stream, info, OnTrackFinish, _logger);
        _activeTrack.Speed = Speed;
        //_activeTrack.UpdateBuffers += () => Stop();

        TrackChanged(info, path);
        Play();
        
        _logger?.Log("  Disposing the old track.");
        oldTrack?.Dispose();
        
        //_logger?.Log("  Unlocking device.");
        //_device.Unlock();

        return true;
    }

    public void Play()
    {
        if (_activeTrack == null)
            return;
        
        _logger?.Log("Start playback.");
        _activeTrack.Play();
        StateChanged(TrackState.Playing);
        _logger?.Log("  Playing device.");
        _device.Play();
    }
    
    public void Pause()
    {
        if (_activeTrack == null)
            return;
        
        _activeTrack.Pause();
        StateChanged(TrackState.Paused);
    }

    public void Stop()
    {
        if (_activeTrack == null)
            return;
        
        _logger?.Log("Stopping.");
        
        _logger?.Log("  Pausing device.");
        _device.Pause();
        _logger?.Log("  Disposing active track.");
        _activeTrack.Dispose();
        _activeTrack = null;
        
        _logger?.Log("  Clearing queued tracks.");
        QueuedTracks.Clear();
        _currentTrackIndex = 0;
        StateChanged(TrackState.Stopped);
    }

    public void Next()
    {
        do
        {
            switch (Repeat)
            {
                case RepeatMode.Off:
                    _currentTrackIndex++;
                    break;
                case RepeatMode.RepeatQueue:
                    _currentTrackIndex++;
                    if (_currentTrackIndex >= QueuedTracks.Count)
                        _currentTrackIndex = 0;
                    break;
                case RepeatMode.RepeatOne:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_currentTrackIndex >= QueuedTracks.Count)
            {
                Stop();
                return;
            }
        } while (!TryChangeTrack(_currentTrackIndex));
    }

    public void Previous()
    {
        do
        {
            _currentTrackIndex--;

            if (_currentTrackIndex < 0)
                _currentTrackIndex = 0;
        } while(!TryChangeTrack(_currentTrackIndex));
    }

    public void Seek(double second)
    {
        _activeTrack.Seek(second);
        StateChanged(TrackState);
    }

    public void RegisterCodec(ICodec codec)
    {
        _codecs.Add(codec);
    }
    
    public void DeregisterCodec(ICodec codec)
    {
        _codecs.Remove(codec);
    }

    public TrackInfo GetTrackInfoForFile(string path)
    {
        _logger?.Log("Checking for codec support.");
        if (!FileIsSupported(path, out ICodec codec))
            throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
        
        _logger?.Log("  Getting track info.");
        return codec.GetTrackInfo(path);
    }
    
    private bool FileIsSupported(string path, out ICodec outCodec)
    {
        string extension = Path.GetExtension(path).ToLower();
        foreach (ICodec codec in Codecs)
        {
            if (codec.FileIsSupported(path, extension))
            {
                outCodec = codec;
                return true;
            }
        }

        outCodec = null;
        return false;
    }

    private ICodecStream CreateStreamFromFile(string path)
    {
        _logger?.Log("Checking for codec support.");
        if (FileIsSupported(path, out ICodec codec))
        {
            _logger?.Log("  Creating stream.");
            return codec.CreateStream(path);
        }

        throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
    }

    private void InsertTrackAtIndex(int index, string path)
    {
        _logger?.Log($"Inserting track '{path}' at index {index}.");
        if (index >= QueuedTracks.Count)
            QueuedTracks.Add(path);
        else
            QueuedTracks.Insert(index, path);
    }

    private void OnTrackFinish()
    {
        Next();
    }

    public void Dispose()
    {
        _logger?.Log("Disposing track.");
        _activeTrack?.Dispose();
        _logger?.Log("Disposing device.");
        _device.Dispose();
        _logger?.Log("Disposing context.");
        _context.Dispose();
    }
}