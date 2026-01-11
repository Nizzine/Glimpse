using System.Runtime.InteropServices;

namespace Glimpse.Platforms.Empress;

public static unsafe class Empress
{
    public const string DllName = "libempress.so";

    [DllImport(DllName, EntryPoint = "empCreate")]
    public static extern Result Create(ApplicationInfo* info, out Context* context);

    [DllImport(DllName, EntryPoint = "empDestroy")]
    public static extern void Destroy(Context* context);

    [DllImport(DllName, EntryPoint = "empSetFocusCallback")]
    public static extern void SetFocusCallback(Context* context, FocusCallback callback);
    
    [DllImport(DllName, EntryPoint = "empSetButtonPressedCallback")]
    public static extern void SetButtonPressedCallback(Context* context, ButtonPressedCallback callback);
    
    [DllImport(DllName, EntryPoint = "empSetSeekCallback")]
    public static extern void SetSeekCallback(Context* context, SeekCallback callback);

    [DllImport(DllName, EntryPoint = "empSetPositionCallback")]
    public static extern void SetPositionCallback(Context* context, PositionCallback callback);

    [DllImport(DllName, EntryPoint = "empSetPlayPosition")]
    public static extern void SetPlayPosition(Context* context, nuint position);
    
    [DllImport(DllName, EntryPoint = "empSetPlayState")]
    public static extern void SetPlayState(Context* context, PlayState state);

    [DllImport(DllName, EntryPoint = "empSetTrackMetadata")]
    public static extern void SetTrackMetadata(Context* context, TrackMetadata* metadata);

    [DllImport(DllName, EntryPoint = "empClearTrackMetadata")]
    public static extern void ClearTrackMetadata(Context* context);

    [DllImport(DllName, EntryPoint = "empSetCanPlay")]
    public static extern void SetCanPlay(Context* context, bool value);
    
    [DllImport(DllName, EntryPoint = "empSetCanPause")]
    public static extern void SetCanPause(Context* context, bool value);
    
    [DllImport(DllName, EntryPoint = "empSetCanSeek")]
    public static extern void SetCanSeek(Context* context, bool value);
    
    [DllImport(DllName, EntryPoint = "empSetCanGoNext")]
    public static extern void SetCanGoNext(Context* context, bool value);
    
    [DllImport(DllName, EntryPoint = "empSetCanGoPrevious")]
    public static extern void SetCanGoPrevious(Context* context, bool value);

    public delegate void FocusCallback(Context* context);
    public delegate void ButtonPressedCallback(Context* context, Button button);
    public delegate void SeekCallback(Context* context, nuint position, long seek);
    public delegate nuint PositionCallback(Context* context);
}