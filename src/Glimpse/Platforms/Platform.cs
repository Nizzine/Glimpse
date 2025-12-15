using Glimpse.API;
using Glimpse.Audio;

namespace Glimpse.Platforms;

public abstract class Platform : IDisposable
{
    public event OnButtonPressed ButtonPressed = delegate { };

    public abstract void InitializeMainWindow(nint hwnd);

    public abstract void OpenFileInExplorer(string path);

    public abstract void SetPlayState(TrackState state, TrackInfo? info, int position);

    public abstract void Dispose();

    protected void InvokeButtonPressed(TransportButton? button, int? position)
    {
        ButtonPressed(button, position);
    }

    public static Platform AutoDetect()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPlatform();
        
        if (OperatingSystem.IsLinux())
            return new LinuxPlatform();

        return new NullPlatform();
    }

    public delegate void OnButtonPressed(TransportButton? button, int? position);
}