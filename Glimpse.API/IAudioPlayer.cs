namespace Glimpse.API;

public interface IAudioPlayer
{
    public int ElapsedSeconds { get; }
    
    public int SecondsConsumed { get; }
}