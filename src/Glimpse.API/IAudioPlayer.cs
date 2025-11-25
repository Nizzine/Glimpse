using Glimpse.API.Codecs;

namespace Glimpse.API;

public interface IAudioPlayer
{
    /// <summary>
    /// Called when the track is changed.
    /// </summary>
    public event OnTrackChanged TrackChanged;

    /// <summary>
    /// Called when the player state changes.
    /// </summary>
    public event OnStateChanged StateChanged;
    
    /// <summary>
    /// Get the list of currently registered codecs.
    /// </summary>
    public IReadOnlyList<ICodec> Codecs { get; }
    
    /// <summary>
    /// The number of elapsed seconds that the current track has played for.
    /// </summary>
    public int ElapsedSeconds { get; }
    
    /// <summary>
    /// The number of seconds that have played since the track was selected. This does not include seeking, etc.
    /// </summary>
    public int SecondsConsumed { get; }
    
    /// <summary>
    /// The total length in seconds of the current track.
    /// </summary>
    public int TrackLength { get; }
    
    /// <summary>
    /// Information about the currently playing track, if any.
    /// </summary>
    public TrackInfo? CurrentTrack { get; }
    
    /// <summary>
    /// The current track state.
    /// </summary>
    public TrackState TrackState { get; }
    
    /// <summary>
    /// The current index in the queue.
    /// </summary>
    public int CurrentTrackIndex { get; }
    
    /// <summary>
    /// The absolute path of the currently playing track.
    /// </summary>
    public string CurrentTrackPath { get; }

    /// <summary>
    /// Change to the given queue index. If auto play is enabled, the track will play immediately.
    /// </summary>
    /// <param name="index">The index in the queue to change to.</param>
    public void ChangeTrack(int index);

    /// <summary>
    /// Start playback of the currently playing track.
    /// </summary>
    public void Play();

    /// <summary>
    /// Pause playback of the currently playing track.
    /// </summary>
    public void Pause();

    /// <summary>
    /// Stop playback of the currently playing track.
    /// </summary>
    public void Stop();

    /// <summary>
    /// Skip to the next track in the queue. If there are no more tracks in the queue, playback stops.
    /// </summary>
    public void Next();

    /// <summary>
    /// Skip to the previous track in the queue. If the current track is the first track in the queue, playback will
    /// restart.
    /// </summary>
    public void Previous();
    
    /// <summary>
    /// Seek to the given second in the track.
    /// </summary>
    /// <param name="second">The second to skip to.</param>
    public void Seek(int second);

    /// <summary>
    /// Register a codec that can be used to play back certain audio files.
    /// </summary>
    /// <param name="codec">The <see cref="ICodec"/> to register.</param>
    public void RegisterCodec(ICodec codec);

    /// <summary>
    /// De-register a codec so it can no longer be used to play back certain audio files.
    /// </summary>
    /// <param name="codec">The <see cref="ICodec"/> to deregister.</param>
    public void DeregisterCodec(ICodec codec);

    /// <summary>
    /// Get <see cref="TrackInfo"/> for the given file path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The <see cref="TrackInfo"/> for the file.</returns>
    public TrackInfo GetTrackInfoForFile(string path);
    
    public delegate void OnTrackChanged(TrackInfo info, string path);
    public delegate void OnStateChanged(TrackState state);
}