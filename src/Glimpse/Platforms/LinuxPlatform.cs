using System.Diagnostics;
using System.Text;
using Glimpse.API;
using Glimpse.Platforms.Empress;
using Silk.NET.Core.Native;

namespace Glimpse.Platforms;

public unsafe class LinuxPlatform : Platform
{
    private readonly Context* _context;
    private readonly Empress.Empress.ButtonPressedCallback _callback;
    
    public readonly string DefaultFileManager;
    
    public LinuxPlatform()
    {
        using Process process = new Process()
        {
            StartInfo = new ProcessStartInfo("xdg-mime")
            {
                Arguments = "query default inode/directory",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        string? fileManager = process.StandardOutput.ReadLine();

        if (fileManager == null)
            return;

        fileManager = fileManager.ToLower();

        if (fileManager.Contains("nautilus"))
            DefaultFileManager = "nautilus";
        else if (fileManager.Contains("dolphin"))
            DefaultFileManager = "dolphin";


#if DEBUG
        fixed (byte* pAppUniqueName = "glimpse.dbg"u8)
#else
        fixed (byte* pAppUniqueName = "glimpse"u8)
#endif
        fixed (byte* pAppFriendlyName = "Glimpse"u8)
        {
            ApplicationInfo appInfo = new()
            {
                AppUniqueName = (sbyte*) pAppUniqueName,
                AppFriendlyName = (sbyte*) pAppFriendlyName
            };
            Result result = Empress.Empress.Create(&appInfo, out _context);
            if (result != Result.Ok)
                throw new Exception($"{result}");
        }

        _callback = ButtonPressedCallback;
        Empress.Empress.SetCanPlay(_context, true);
        Empress.Empress.SetCanPause(_context, true);
        Empress.Empress.SetCanSeek(_context, false);
        Empress.Empress.SetCanGoNext(_context, true);
        Empress.Empress.SetCanGoPrevious(_context, true);
        Empress.Empress.SetButtonPressedCallback(_context, _callback);
    }
    
    // This shouldn't be necessary on Linux platforms.
    public override void InitializeMainWindow(IntPtr hwnd) { }

    public override void EnableDPIAwareness() { }
    
    public override void EnableDarkWindow(nint hwnd) { }

    public override void OpenFileInExplorer(string path)
    {
        Process process;
        
        if (DefaultFileManager == null)
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo("xdg-open")
                {
                    Arguments = $"\"{Path.GetDirectoryName(path)}\""
                }
            };
        }
        else
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo(DefaultFileManager)
                {
                    Arguments = $"--select \"{path}\""
                }
            };
        }

        process.Start();
        process.Dispose();
    }

    public override void SetPlayState(TrackState state, TrackInfo? info)
    {
        if (info == null)
            Empress.Empress.ClearTrackMetadata(_context);
        else
        {
            nint pTitle = SilkMarshal.StringToPtr(info.Title);
            nint pArtist = info.Artist == null ? 0 : SilkMarshal.StringArrayToPtr([info.Artist]);
            nint pAlbum = SilkMarshal.StringToPtr(info.Album);
            nuint length = (nuint?) info.Length?.TotalSeconds ?? 0;
            nint pGenre = info.Genre == null ? 0 : SilkMarshal.StringArrayToPtr([info.Genre]);

            TrackMetadata metadata = new()
            {
                Title = (sbyte*) pTitle,
                NumArtists = info.Artist == null ? 0 : 1,
                Artists = (sbyte**) pArtist,
                Album = (sbyte*) pAlbum,
                Length = length,
                NumGenres = info.Genre == null ? 0 : 1,
                Genres = (sbyte**) pGenre
            };
            
            Empress.Empress.SetTrackMetadata(_context, &metadata);

            SilkMarshal.Free(pGenre);
            SilkMarshal.FreeString(pAlbum);
            SilkMarshal.Free(pArtist);
            SilkMarshal.FreeString(pTitle);
        }
        
        PlayState playState = state switch
        {
            TrackState.Stopped => PlayState.Stopped,
            TrackState.Paused => PlayState.Paused,
            TrackState.Playing => PlayState.Playing,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
        
        Empress.Empress.SetPlayState(_context, playState);
    }

    private void ButtonPressedCallback(Context* context, Button button)
    {
        TransportButton transportButton = button switch
        {
            Button.Play => TransportButton.Play,
            Button.Pause => TransportButton.Pause,
            Button.Stop => throw new NotImplementedException(),
            Button.Next => TransportButton.Next,
            Button.Previous => TransportButton.Previous,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
        
        InvokeButtonPressed(transportButton);
    }
    
    public override void Dispose()
    {
        Empress.Empress.Destroy(_context);
    }
}