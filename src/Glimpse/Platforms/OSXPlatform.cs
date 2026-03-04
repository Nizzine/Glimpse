using System.Diagnostics;
using Glimpse.API;

namespace Glimpse.Platforms;

public class OSXPlatform : Platform
{
    public override string FileManagerName => "Finder";
    
    public override void InitializeMainWindow(IntPtr hwnd) { }

    public override void OpenFileInExplorer(string path)
    {
        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo("open")
            {
                Arguments = $"--reveal {path}"
            }
        };

        process.Start();
        process.Dispose();
    }
    
    public override void SetPlayState(TrackState state, TrackInfo? info, TimeSpan position) { }
    
    public override void Dispose() { }
}