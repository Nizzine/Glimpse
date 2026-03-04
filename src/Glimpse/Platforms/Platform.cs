using Glimpse.API;

namespace Glimpse.Platforms;

public abstract class Platform : IDisposable
{
    public event OnButtonPressed ButtonPressed = delegate { };

    public event OnGetPosition? GetPosition;
    
    public abstract string FileManagerName { get; }

    public abstract void InitializeMainWindow(nint hwnd);

    public abstract void OpenFileInExplorer(string path);

    public abstract void SetPlayState(TrackState state, TrackInfo? info, TimeSpan position);

    public abstract void Dispose();

    protected void InvokeButtonPressed(TransportButton? button, int? position)
    {
        ButtonPressed(button, position);
    }

    protected TimeSpan InvokeGetPosition()
    {
        return GetPosition?.Invoke() ?? TimeSpan.Zero;
    }

    public static Platform AutoDetect()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPlatform();
        
        if (OperatingSystem.IsLinux())
            return new LinuxPlatform();

        if (OperatingSystem.IsMacOS())
            return new OSXPlatform();

        return new NullPlatform();
    }

    public delegate void OnButtonPressed(TransportButton? button, int? position);

    public delegate TimeSpan OnGetPosition();
}