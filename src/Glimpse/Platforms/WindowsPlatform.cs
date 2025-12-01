using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media;
using Windows.Storage.Streams;
using Glimpse.API;
using Glimpse.Audio;
using StbImageSharp;
using TerraFX.Interop.Windows;

namespace Glimpse.Platforms;

public unsafe class WindowsPlatform : Platform
{
    //private ISystemMediaTransportControls* _transportControls;
    private SystemMediaTransportControls _transportControls;

    private InMemoryRandomAccessStream? _albumArtStream;

    public WindowsPlatform()
    {
        TerraFX.Interop.Windows.Windows.CoInitialize(null);
    }

    public override void InitializeMainWindow(IntPtr hwnd)
    {
        _transportControls = SystemMediaTransportControlsInterop.GetForWindow(hwnd); 
        _transportControls.ButtonPressed += MediaButtonPressed;
        
        _transportControls.IsEnabled = true;
        _transportControls.IsPlayEnabled = true;
        _transportControls.IsPauseEnabled = true;
        _transportControls.IsNextEnabled = true;
        _transportControls.IsPreviousEnabled = true;
    }

    private void MediaButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        TransportButton button = args.Button switch
        {
            SystemMediaTransportControlsButton.Play => TransportButton.Play,
            SystemMediaTransportControlsButton.Pause => TransportButton.Pause,
            SystemMediaTransportControlsButton.Stop => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.Record => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.FastForward => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.Rewind => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.Next => TransportButton.Next,
            SystemMediaTransportControlsButton.Previous => TransportButton.Previous,
            SystemMediaTransportControlsButton.ChannelUp => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.ChannelDown => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException()
        };

        InvokeButtonPressed(button);
    }

    public override void EnableDPIAwareness()
    {
        TerraFX.Interop.Windows.Windows.SetProcessDPIAware();
    }

    public override void EnableDarkWindow(nint hwnd)
    {
        BOOL value = true;
        TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute((HWND) hwnd, 20, &value, (uint) sizeof(BOOL));
    }

    public override void OpenFileInExplorer(string path)
    {
        fixed (char* pPath = path)
        {
            ITEMIDLIST* list = TerraFX.Interop.Windows.Windows.ILCreateFromPathW(pPath);
            TerraFX.Interop.Windows.Windows.SHOpenFolderAndSelectItems(list, 0, null, 0);
            TerraFX.Interop.Windows.Windows.ILFree(list);
        }
    }

    public override void SetPlayState(TrackState state, TrackInfo? info)
    {
        switch (state)
        {
            case TrackState.Stopped:
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                break;
            case TrackState.Paused:
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                break;
            case TrackState.Playing:
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
        
        if (info == null)
            _transportControls.DisplayUpdater.ClearAll();
        else
        {
            _albumArtStream?.Dispose();
            _transportControls.DisplayUpdater.Thumbnail = null;
            
            if (info.AlbumArt != null)
            {
                _albumArtStream = new InMemoryRandomAccessStream();
                _albumArtStream.WriteAsync(info.AlbumArt.Data.AsBuffer()).GetAwaiter().GetResult();
                _transportControls.DisplayUpdater.Thumbnail =
                    RandomAccessStreamReference.CreateFromStream(_albumArtStream);
            }

            _transportControls.DisplayUpdater.AppMediaId = "Glimpse.Glimpse";
            _transportControls.DisplayUpdater.Type = MediaPlaybackType.Music;
            _transportControls.DisplayUpdater.MusicProperties.TrackNumber = info.TrackNumber ?? 0;
            _transportControls.DisplayUpdater.MusicProperties.Title = info.Title;
            _transportControls.DisplayUpdater.MusicProperties.Artist = info.Artist;
            _transportControls.DisplayUpdater.MusicProperties.AlbumTitle = info.Album;
        }
        
        _transportControls.DisplayUpdater.Update();
    }

    public override void Dispose() { }
}