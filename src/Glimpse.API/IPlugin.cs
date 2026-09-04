using Glimpse.API.UI;

namespace Glimpse.API;

public interface IPlugin : IDisposable
{
    public bool IsInitialized { get; }
    
    public string Name { get; }

    public bool HasConfigChanged => false;
    
    public void Initialize(IGlimpse glimpse);

    public void DisplayGui(IImmediateGUI ui) { }

    public void SaveConfig() { }

    public void Dispose();
}