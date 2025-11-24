using Glimpse.Audio;

namespace Glimpse.Plugins;

public abstract class Plugin : IDisposable
{
    public abstract bool IsInitialized { get; }
    
    public abstract string Name { get; }
    
    public abstract void Initialize(AudioPlayer player);

    public virtual void DisplayGui() { }

    public abstract void Dispose();
}