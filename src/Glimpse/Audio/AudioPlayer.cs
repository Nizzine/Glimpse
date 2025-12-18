using Glimpse.API;
using Glimpse.API.Codecs;
using Glimpse.Audio.Codecs;
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

    private readonly Logger _logger;
    private readonly Context _context;
    private readonly AudioDevice _device;
    
    private Track? _activeTrack;

    private int _currentTrackIndex;
    private int _currentQueueIndex;

    private PlayerSettings _settings;
    private readonly List<ICodec> _codecs;

    public readonly List<string> QueuedTracks;

    public IReadOnlyList<ICodec> Codecs => _codecs;

    public float Volume
    {
        get => _context.MasterVolume;
        set => _context.MasterVolume = value;
    }

    public TimeSpan ElapsedTime => TimeSpan.FromSeconds(_activeTrack?.ElapsedSeconds ?? 0);

    public TimeSpan ConsumedTime => TimeSpan.FromSeconds(_activeTrack?.SecondsConsumed ?? 0);

    public TimeSpan TrackLength => TimeSpan.FromSeconds(_activeTrack?.LengthInSeconds ?? 0);

    public TrackInfo? CurrentTrack => _activeTrack?.Info;

    public TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public int CurrentTrackIndex => _currentTrackIndex;

    public string CurrentTrackPath => QueuedTracks.Count == 0 ? string.Empty : QueuedTracks[_currentTrackIndex];

    public AudioPlayer(Logger logger, PlayerSettings settings)
    {
        _logger = logger;
        _settings = settings;
        
        _logger.Log("Creating context.");
        _context = new Context(_settings.SampleRate);
        Volume = _settings.Volume;
        
        _logger.Log("Creating device.");
        _device = new AudioDevice(_context, _settings.SampleRate);

        _logger.Log("Initializing codecs.");
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
        _logger.Log($"Queueing track {path}");

        bool isFirstQueue = autoPlay && QueuedTracks.Count == 0;

        switch (slot)
        {
            case QueueSlot.AtEnd:
                QueuedTracks.Add(path);
                break;
            case QueueSlot.Queue:
                InsertTrackAtIndex(_currentTrackIndex + ++_currentQueueIndex, path);
                break;
            case QueueSlot.NextTrack:
                InsertTrackAtIndex(_currentTrackIndex + 1, path);
                _currentQueueIndex++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
        }
        
        if (isFirstQueue)
            ChangeTrack(0);
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

    public void ChangeTrack(int queueIndex)
    {
        _logger.Log($"Changing to track {queueIndex}.");
        
        if (queueIndex >= QueuedTracks.Count || queueIndex < 0)
            throw new Exception("Cannot queue track that is not in the queue.");
        
        //_logger.Log("  Locking device.");
        //_device.Lock();
        Track oldTrack = _activeTrack;

        string path = QueuedTracks[queueIndex];
        _currentTrackIndex = queueIndex;
        
        _logger.Log($"  Creating codec stream from file {path}");
        ICodecStream stream = CreateStreamFromFile(path);
        TrackInfo info = stream.TrackInfo;

        _logger.Log("  Creating track.");
        _activeTrack = new Track(_context, stream, info, OnTrackFinish, _logger);

        TrackChanged(info, path);
        
        if (_settings.AutoPlay)
            Play();
        
        _logger.Log("  Disposing the old track.");
        oldTrack?.Dispose();
        
        //_logger.Log("  Unlocking device.");
        //_device.Unlock();
    }

    public void Play()
    {
        if (_activeTrack == null)
            return;
        
        _logger.Log("Start playback.");
        _activeTrack.Play();
        StateChanged(TrackState.Playing);
        _logger.Log("  Playing device.");
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
        
        _logger.Log("Stopping.");
        
        _logger.Log("  Pausing device.");
        _device.Pause();
        _logger.Log("  Disposing active track.");
        _activeTrack.Dispose();
        _activeTrack = null;
        
        _logger.Log("  Clearing queued tracks.");
        QueuedTracks.Clear();
        _currentTrackIndex = 0;
        StateChanged(TrackState.Stopped);
    }

    public void Next()
    {
        _currentTrackIndex++;

        if (_currentTrackIndex >= QueuedTracks.Count)
        {
            Stop();
            return;
        }

        _currentQueueIndex--;
        if (_currentQueueIndex < 0)
            _currentQueueIndex = 0;
        
        ChangeTrack(_currentTrackIndex);
    }

    public void Previous()
    {
        _currentTrackIndex--;
        
        if (_currentTrackIndex < 0)
            _currentTrackIndex = 0;

        if (_currentQueueIndex != 0)
            _currentQueueIndex++;
        
        ChangeTrack(_currentTrackIndex);
    }

    public void Seek(int second)
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
        _logger.Log("Checking for codec support.");
        if (!FileIsSupported(path, out ICodec codec))
            throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
        
        _logger.Log("  Getting track info.");
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
        _logger.Log("Checking for codec support.");
        if (FileIsSupported(path, out ICodec codec))
        {
            _logger.Log("  Creating stream.");
            return codec.CreateStream(path);
        }

        throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
    }

    private void InsertTrackAtIndex(int index, string path)
    {
        _logger.Log($"Inserting track '{path}' at index {index}.");
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
        _logger.Log("Disposing track.");
        _activeTrack?.Dispose();
        _logger.Log("Disposing device.");
        _device.Dispose();
        _logger.Log("Disposing context.");
        _context.Dispose();
    }
}