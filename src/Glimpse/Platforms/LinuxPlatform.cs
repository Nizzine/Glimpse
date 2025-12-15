using System.Diagnostics;
using System.Text;
using Glimpse.API;
using Glimpse.Platforms.Empress;
using Silk.NET.Core.Native;

namespace Glimpse.Platforms;

public unsafe class LinuxPlatform : Platform
{
    private readonly Context* _context;
    private readonly Empress.Empress.ButtonPressedCallback _buttonCallback;
    private readonly Empress.Empress.SeekCallback _seekCallback;
    
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


        string desktopFile = Path.Combine(AppContext.BaseDirectory, "Glimpse.desktop");
        Console.WriteLine(desktopFile);
        byte[] bDesktopFile = Encoding.UTF8.GetBytes(desktopFile);
        
#if DEBUG
        fixed (byte* pAppUniqueName = "glimpse.dbg"u8)
#else
        fixed (byte* pAppUniqueName = "glimpse"u8)
#endif
        fixed (byte* pAppFriendlyName = "Glimpse"u8)
        fixed (byte* pDesktopFile = bDesktopFile)
        {
            ApplicationInfo appInfo = new()
            {
                AppUniqueName = (sbyte*) pAppUniqueName,
                AppFriendlyName = (sbyte*) pAppFriendlyName,
                DesktopEntry = (sbyte*) pDesktopFile
            };
            Result result = Empress.Empress.Create(&appInfo, out _context);
            if (result != Result.Ok)
                throw new Exception($"{result}");
        }

        _buttonCallback = ButtonPressedCallback;
        _seekCallback = SeekCallback;
        Empress.Empress.SetCanPlay(_context, false);
        Empress.Empress.SetCanPause(_context, false);
        Empress.Empress.SetCanSeek(_context, false);
        Empress.Empress.SetCanGoNext(_context, false);
        Empress.Empress.SetCanGoPrevious(_context, false);
        Empress.Empress.SetButtonPressedCallback(_context, _buttonCallback);
        Empress.Empress.SetSeekCallback(_context, _seekCallback);
    }

    // This shouldn't be necessary on Linux platforms.
    public override void InitializeMainWindow(IntPtr hwnd) { }

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

    public override void SetPlayState(TrackState state, TrackInfo? info, int position)
    {
        if (info == null)
            Empress.Empress.ClearTrackMetadata(_context);
        else
        {
            string? art = null;
            if (info.AlbumArt is { } albumArt)
            {
                if (albumArt.Location is { } location)
                    art = location;
                else if (albumArt is { Data: { } data, MimeType: { } mimeType })
                {
                    string base64Data = Convert.ToBase64String(data);
                    art = $"data:{mimeType};base64,{base64Data}";
                }
            }
            
            nint pTitle = SilkMarshal.StringToPtr(info.Title);
            nint pArtist = info.Artist == null ? 0 : SilkMarshal.StringArrayToPtr([info.Artist]);
            nint pAlbum = SilkMarshal.StringToPtr(info.Album);
            nuint length = (nuint?) info.Length?.TotalSeconds ?? 0;
            nint pGenre = info.Genre == null ? 0 : SilkMarshal.StringArrayToPtr([info.Genre]);
            nint pArt = SilkMarshal.StringToPtr(art);

            TrackMetadata metadata = new()
            {
                Title = (sbyte*) pTitle,
                NumArtists = info.Artist == null ? 0 : 1,
                Artists = (sbyte**) pArtist,
                Album = (sbyte*) pAlbum,
                Length = length * 1000 * 1000, // MPRIS wants the length in microseconds.
                NumGenres = info.Genre == null ? 0 : 1,
                Genres = (sbyte**) pGenre,
                ImageUri = (sbyte*) pArt
            };
            
            Empress.Empress.SetTrackMetadata(_context, &metadata);

            SilkMarshal.Free(pArt);
            SilkMarshal.Free(pGenre);
            SilkMarshal.FreeString(pAlbum);
            SilkMarshal.Free(pArtist);
            SilkMarshal.FreeString(pTitle);
        }
        
        Empress.Empress.SetPlayPosition(_context, (nuint) position * 1000 * 1000);
        bool canControl = state != TrackState.Stopped;
        
        Empress.Empress.SetCanPlay(_context, canControl);
        Empress.Empress.SetCanPause(_context, canControl);
        Empress.Empress.SetCanGoNext(_context, canControl);
        Empress.Empress.SetCanGoPrevious(_context, canControl);
        Empress.Empress.SetCanSeek(_context, canControl);
        
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
        
        InvokeButtonPressed(transportButton, null);
    }
    
    private void SeekCallback(Context* context, UIntPtr position, long seek)
    {
        InvokeButtonPressed(null, (int) (position / 1000 / 1000));
    }
    
    public override void Dispose()
    {
        Empress.Empress.Destroy(_context);
    }
}