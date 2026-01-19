using System.Drawing;
using System.Runtime.InteropServices;
using Glimpse.Forms;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using SDL3;
using Silk.NET.OpenGL;
using StbImageSharp;
using Renderer = Glimpse.Graphics.Renderer;

namespace Glimpse;

public abstract unsafe class Window : IDisposable
{
    private bool _isCreated;
    private string _title;
    private Size _size;
    private float _scale;
    private float _pixelDensity;
    
    private IntPtr _window;
    private IntPtr _glContext;

    private List<Popup> _popups;

    public Glimpse Glimpse;
    
    public Renderer Renderer;

    public nint Handle => _window;

    public string Title
    {
        get
        {
            if (!_isCreated)
                return _title;

            return SDL.GetWindowTitle(_window);
        }
        set
        {
            if (!_isCreated)
                _title = value;
            else
                SDL.SetWindowTitle(_window, value);
        }
    }

    public Size Size
    {
        get
        {
            if (!_isCreated)
                return _size;
            
            SDL.GetWindowSizeInPixels(_window, out int w, out int h);

            return new Size(w, h);
        }
        set
        {
            if (!_isCreated)
                _size = value;
            else
                SDL.SetWindowSize(_window, value.Width, value.Height);
        }
    }

    public float Scale => _scale;

    public float PixelDensity => _pixelDensity;

    protected Window()
    {
        Title = "Window";
        Size = new Size(800, 450);

        _popups = new List<Popup>();
    }

    protected virtual void Initialize() { }

    protected virtual void Update() { }

    public void AddPopup(Popup popup)
    {
        popup.Glimpse = Glimpse;
        popup.Renderer = Renderer;
        popup.Scale = _scale;
        popup.Open();
        _popups.Add(popup);
    }

    protected virtual void OnScaleChanged() { }

    public void NotifyScaleChanged()
    {
        _scale = SDL.GetWindowDisplayScale(_window);
        _pixelDensity = SDL.GetWindowPixelDensity(_window);
        OnScaleChanged();
    }

    internal uint Create(Platform platform)
    {
        SDL.GLSetAttribute(SDL.GLAttr.ContextMajorVersion, 3);
        SDL.GLSetAttribute(SDL.GLAttr.ContextMinorVersion, 3);
        SDL.GLSetAttribute(SDL.GLAttr.ContextProfileMask, (int) SDL.GLProfile.Core);
        SDL.GLSetAttribute(SDL.GLAttr.ContextFlags, (int) SDL.GLContextFlag.ForwardCompatible);
        SDL.GLSetAttribute(SDL.GLAttr.DepthSize, 0);
        SDL.GLSetAttribute(SDL.GLAttr.AlphaSize, 0);

        uint windowProps = SDL.CreateProperties();
        SDL.SetStringProperty(windowProps, SDL.Props.WindowCreateTitleString, _title);
        SDL.SetBooleanProperty(windowProps, SDL.Props.WindowCreateOpenGLBoolean, true);
        SDL.SetBooleanProperty(windowProps, SDL.Props.WindowCreateResizableBoolean, true);
        SDL.SetBooleanProperty(windowProps, SDL.Props.WindowCreateHighPixelDensityBoolean, true);
        SDL.SetBooleanProperty(windowProps, SDL.Props.WindowCreateHiddenBoolean, true);

        // Attempt to set the window centered on the display the mouse cursor is on.
        // If that fails, just make the window centered. On platforms such as Wayland, this will do nothing.
        SDL.GetGlobalMouseState(out float mouseX, out float mouseY);
        SDL.Point mousePoint = new() { X = (int) mouseX, Y = (int) mouseY };
        uint display = SDL_GetDisplayForPoint(in mousePoint);
        uint displayPos = display == 0 ? SDL.WindowPosCentered() : SDL.WindowPosCenteredDisplay((int) display);
        float displayScale = SDL.GetDisplayContentScale(display);
        
        SDL.SetNumberProperty(windowProps, SDL.Props.WindowCreateWidthNumber, (int) (_size.Width * displayScale));
        SDL.SetNumberProperty(windowProps, SDL.Props.WindowCreateHeightNumber, (int) (_size.Height * displayScale));
        
        SDL.SetNumberProperty(windowProps, SDL.Props.WindowCreateXNumber, displayPos);
        SDL.SetNumberProperty(windowProps, SDL.Props.WindowCreateYNumber, displayPos);
        
        _window = SDL.CreateWindowWithProperties(windowProps);

        if (_window == IntPtr.Zero)
            throw new Exception($"Failed to open window: {SDL.GetError()}");
        
        _scale = SDL.GetWindowDisplayScale(_window);
        _pixelDensity = SDL.GetWindowPixelDensity(_window);
        
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(Glimpse.GetPath("Assets/Icons/Glimpse.png")));
        IntPtr surface;
        fixed (byte* pData = result.Data)
        {
            surface = SDL.CreateSurfaceFrom(result.Width, result.Height, SDL.PixelFormat.ABGR8888, (IntPtr) pData,
                result.Width * 4);
        }

        SDL.SetWindowIcon(_window, surface);

        _glContext = SDL.GLCreateContext(_window);
        
        _isCreated = true;

        SDL.GLMakeCurrent(_window, _glContext);
        Renderer = new Renderer(GL.GetApi(s => Marshal.GetFunctionPointerForDelegate(SDL.GLGetProcAddress(s))), Size, _scale);
        
        Initialize();
        
        if (OperatingSystem.IsWindows())
        {
            uint props = SDL.GetWindowProperties(_window);
            nint hwnd = SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, 0);
            platform.InitializeMainWindow(hwnd);
        }
        
        SDL.ShowWindow(_window);

        return SDL.GetWindowID(_window);
    }

    internal void SetActive()
    {
        SDL.GLMakeCurrent(_window, _glContext);
    }

    internal void UpdateWindow()
    {
        ImGui.SetCurrentContext(Renderer.ImGui.ImGuiContext);
        ImGui.GetIO().DeltaTime = 1 / 60.0f;
        ImGui.NewFrame();
        
        Renderer.GL.Disable(EnableCap.ScissorTest);
        Update();

        for (int i = 0; i < _popups.Count; i++)
        {
            Popup popup = _popups[i];
            popup.Update();

            if (popup.IsRemoved)
            {
                popup.Dispose();
                _popups.RemoveAt(i);
                i--;
            }
        }
    }

    internal void Present()
    {
        Renderer.ImGui.Render();
        SDL.GLSetSwapInterval(1);
        SDL.GLSwapWindow(_window);
    }

    public virtual void Dispose()
    {
        SDL.DestroyWindow(_window);
    }
    
    // Workaround because SDL3-CS doesn't use a pointer for this method when the ABI expects one, meaning it fails.
    [DllImport("SDL3")]
    private static extern uint SDL_GetDisplayForPoint(in SDL.Point point);
}