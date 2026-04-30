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
    /// The master volume. A value of 1.0 is full volume. 0.0 is muted. -1.0 is just weird, don't try that (inverted output).
    /// </summary>
    public float Volume { get; set; }
    
    /// <summary>
    /// The speed at which audio will be played, where 1.0 is normal playback.
    /// </summary>
    public double Speed { get; set; }
    
    /// <summary>
    /// Get the list of currently registered codecs.
    /// </summary>
    public IReadOnlyList<ICodec> Codecs { get; }
    
    /// <summary>
    /// The amount of time that the current track has played for.
    /// </summary>
    public TimeSpan ElapsedTime { get; }
    
    /// <summary>
    /// The amount of time that has played since the track was selected. This does not include seeking, etc.
    /// </summary>
    public TimeSpan ConsumedTime { get; }
    
    /// <summary>
    /// The total length in seconds of the current track.
    /// </summary>
    public TimeSpan TrackLength { get; }
    
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
    /// Queue a track at the given slot.
    /// </summary>
    /// <param name="path">The path to the track file.</param>
    /// <param name="slot">The <see cref="QueueSlot"/> to insert the track at.</param>
    /// <param name="autoPlay">If true, the track will start playing immediately.</param>
    public void QueueTrack(string path, QueueSlot slot, bool autoPlay = true);

    /// <summary>
    /// Queue tracks at the given slot.
    /// </summary>
    /// <param name="paths">The paths to the track files.</param>
    /// <param name="slot">The <see cref="QueueSlot"/> to insert the tracks at.</param>
    public void QueueTracks(IEnumerable<string> paths, QueueSlot slot);

    /// <summary>
    /// Try and change to the given queue index.
    /// </summary>
    /// <param name="index">The index in the queue to change to.</param>
    /// <returns>True if the change was successful, false otherwise.</returns>
    public bool TryChangeTrack(int index);

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
    public void Seek(double second);

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