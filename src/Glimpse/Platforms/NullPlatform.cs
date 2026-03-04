using Glimpse.API;

namespace Glimpse.Platforms;

public class NullPlatform : Platform
{
    public override string FileManagerName { get; }
    
    public override void InitializeMainWindow(IntPtr hwnd) { }

    public override void OpenFileInExplorer(string path) { }

    public override void SetPlayState(TrackState state, TrackInfo? info, TimeSpan position) { }
    
    public override void Dispose() { }
}