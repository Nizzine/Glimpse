using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.Player.Codecs;
using Glimpse.Player.Codecs.Flac;
using Glimpse.Player.Codecs.Mp3;
using Glimpse.Player.Codecs.Vorbis;
using Glimpse.Player.Codecs.Wav;
using Glimpse.Player.Configs;
using Glimpse.Player.Plugins;
using MixrSharp;

namespace Glimpse.Player;

public class AudioPlayer : IDisposable
{
    public event OnTrackChanged TrackChanged = delegate { };

    public event OnStateChanged StateChanged = delegate { };
    
    private AssemblyLoadContext _pluginsContext;

    private readonly Context _context;
    private readonly AudioDevice _device;

    private readonly TrackInfo _defaultTrackInfo;
    
    private Track _activeTrack;

    private int _currentTrackIndex;
    private int _currentQueueIndex;

    public PlayerConfig Config;

    public readonly List<Codec> Codecs;

    public readonly Dictionary<string, Plugin> Plugins;

    public readonly List<string> QueuedTracks;

    public int ElapsedSeconds => _activeTrack?.ElapsedSeconds ?? 0;

    public int TrackLength => _activeTrack?.LengthInSeconds ?? 0;

    public TrackInfo TrackInfo => _activeTrack?.Info ?? _defaultTrackInfo;

    public TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public int CurrentTrackIndex => _currentTrackIndex;

    public string CurrentTrack => QueuedTracks.Count == 0 ? string.Empty : QueuedTracks[_currentTrackIndex];

    public AudioPlayer()
    {
        Logger.Log("Loading player configuration.");
        if (!IConfig.TryGetConfig(PlayerConfig.ConfigName, out Config))
        {
            Logger.Log("   ... Failed: Creating new config.");
            Config = new PlayerConfig();
            IConfig.WriteConfig(PlayerConfig.ConfigName, Config);
        }

        Logger.Log("Creating context.");
        _context = new Context(Config.SampleRate);
        _context.MasterVolume = Config.Volume;
        
        Logger.Log("Creating device.");
        _device = new AudioDevice(_context, Config.SampleRate);
        
        _defaultTrackInfo = new TrackInfo(null, "Unknown Title", "Unknown Artist", "Unknown Album", null);

        Logger.Log("Initializing codecs.");
        Codecs = [new Mp3Codec(), new FlacCodec(), new VorbisCodec(), new WavCodec()];

        QueuedTracks = new List<string>();

        Logger.Log("Searching for 'Plugins' directory.");
        if (Directory.Exists("Plugins"))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");

            Plugins = [];

            string pluginsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            
            Logger.Log($"Searching for plugins in {pluginsLocation}");
            foreach (string file in Directory.GetFiles(pluginsLocation, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Logger.Log($"Loading assembly from {file}");
                    _pluginsContext.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException e)
                {
                    Logger.Log($"Failed to load DLL: {e}");
                    // If this is thrown then it's likely a native DLL.
                }
            }

            AssemblyName currentName = Assembly.GetAssembly(typeof(AudioPlayer))?.GetName();
            
            foreach (Assembly assembly in _pluginsContext.Assemblies)
            {
                foreach (AssemblyName name in assembly.GetReferencedAssemblies())
                {
                    if (name.Name == currentName.Name)
                    {
                        if (name.Version != currentName.Version)
                            Console.WriteLine("WARNING: Plugin requires different version of Glimpse. It may cause errors.");
                        
                        goto ASSEMBLY_GOOD;
                    }
                }
                
                continue;
                
                ASSEMBLY_GOOD: ;
                
                Logger.Log($"Plugin {assembly} loaded.");
                
                foreach (Type type in assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(Plugin))))
                {
                    Logger.Log($"Initializing plugin {type}");
                    
                    Plugin plugin = (Plugin) Activator.CreateInstance(type);
                    if (plugin == null)
                        continue;

                    string assemblyName = assembly.GetName().Name;

                    if (Config.EnabledPlugins.Contains(assemblyName))
                    {
                        Logger.Log("    ... Initialize()");
                        plugin.Initialize(this);
                    }

                    Plugins.Add(assemblyName, plugin);
                }
            }
        }
    }

    /// <summary>
    /// Queue a track at the given slot.
    /// </summary>
    /// <param name="path">The path to the track file.</param>
    /// <param name="slot">The <see cref="QueueSlot"/> to insert the track at.</param>
    public void QueueTrack(string path, QueueSlot slot, bool autoPlay = true)
    {
        Logger.Log($"Queueing track {path}");

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
        Logger.Log($"Changing to track {queueIndex}.");
        
        if (queueIndex >= QueuedTracks.Count || queueIndex < 0)
            throw new Exception("Cannot queue track that is not in the queue.");
        
        Logger.Log("  Locking device.");
        _device.Lock();
        
        Logger.Log("  Disposing the active track.");
        _activeTrack?.Dispose();
        _currentTrackIndex = queueIndex;

        string path = QueuedTracks[queueIndex];
        
        Logger.Log($"  Creating codec stream from file {path}");
        CodecStream stream = CreateStreamFromFile(path);
        TrackInfo info = stream.TrackInfo;

        Logger.Log("  Creating track.");
        _activeTrack = new Track(_context, stream, info, Config, OnTrackFinish);

        TrackChanged(info, path);
        
        Logger.Log("  Unlocking device.");
        _device.Unlock();
        
        if (Config.AutoPlay)
            Play();
    }

    public void Play()
    {
        Logger.Log("Start playback.");
        _activeTrack.Play();
        StateChanged(TrackState.Playing);
        Logger.Log("  Playing device.");
        _device.Play();
    }
    
    public void Pause()
    {
        _activeTrack.Pause();
        StateChanged(TrackState.Paused);
    }

    public void Stop()
    {
        Logger.Log("Stopping.");
        
        Logger.Log("  Pausing device.");
        _device.Pause();
        Logger.Log("  Disposing active track.");
        _activeTrack?.Dispose();
        _activeTrack = null;
        
        Logger.Log("  Clearing queued tracks.");
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

    public bool FileIsSupported(string path, out Codec outCodec)
    {
        string extension = Path.GetExtension(path);
        foreach (Codec codec in Codecs)
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

    public TrackInfo GetTrackInfoForFile(string path)
    {
        Logger.Log("Checking for codec support.");
        if (!FileIsSupported(path, out Codec codec))
            throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
        
        Logger.Log("  Getting track info.");
        return codec.GetTrackInfo(path);
    }

    public CodecStream CreateStreamFromFile(string path)
    {
        Logger.Log("Checking for codec support.");
        if (FileIsSupported(path, out Codec codec))
        {
            Logger.Log("  Creating stream.");
            return codec.CreateStream(path);
        }

        throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
    }

    private void InsertTrackAtIndex(int index, string path)
    {
        Logger.Log($"Inserting track '{path}' at index {index}.");
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
        if (Plugins != null)
        {
            Logger.Log("Disposing all plugins.");
            foreach ((string name, Plugin plugin) in Plugins)
            {
                Logger.Log($"Disposing plugin {name}");
                plugin.Dispose();
            }
        }

        Logger.Log("Disposing track.");
        _activeTrack?.Dispose();
        Logger.Log("Disposing device.");
        _device.Dispose();
        Logger.Log("Disposing context.");
        _context.Dispose();
    }

    public delegate void OnTrackChanged(TrackInfo info, string path);

    public delegate void OnStateChanged(TrackState state);
}