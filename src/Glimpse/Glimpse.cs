using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.API;
using Glimpse.Audio;
using Glimpse.Configs;
using Glimpse.Database;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using SDL3;
using Locale = Glimpse.Locales.Locale;
using Version = System.Version;

namespace Glimpse;

public class Glimpse : IGlimpse, IDisposable
{
    private List<Window> _windows;
    private Dictionary<uint, Window> _windowIds;
    private AssemblyLoadContext _pluginsContext;

    public Logger Logger;

    public SemVer Version;
    
    public ConfigManager ConfigManager;

    public GlimpseConfig Config;

    public Platform Platform;

    public AudioPlayer Player;

    public Locale Locale;

    public MusicDatabase? Database;
    
    public Dictionary<string, IPlugin>? Plugins;

    public Window MainWindow => _windows[0];

    public void AddWindow(Window window)
    {
        window.Glimpse = this;
        uint id = window.Create(Platform);
        _windows.Add(window);
        _windowIds.Add(id, window);
    }

    public void Run(Window window, string[] args)
    {
        Logger = new Logger();

        // get the ass
        Assembly? ass = Assembly.GetEntryAssembly();
        // no ass
        if (ass == null)
            Version = new SemVer();
        else
        {
            // i love as- no nevermind
            AssemblyInformationalVersionAttribute? infoVersion =
                ass.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

#if !DEBUG
            try
            {
#endif
                Version = new SemVer(infoVersion!.InformationalVersion);
#if !DEBUG
            }
            catch (Exception e)
            {
                Logger.Log($"Could not parse version number! Using fallback. {e}.");

                Version? assVersion = ass.GetName().Version;
                // ass does not have a version
                if (assVersion == null)
                    Version = new SemVer();
                else
                    Version = new SemVer(assVersion.Major, assVersion.Minor, assVersion.Build, assVersion.Revision);
            }
#endif
        }
            
        Logger.Log($"Glimpse {Version}");
        
        Logger.Log("Creating config manager.");
        ConfigManager = new ConfigManager(Logger);
        
        Platform = Platform.AutoDetect();
        Logger.Log($"Detected platform {Platform.GetType()}");
        
        Logger.Log("Loading player configuration.");
        if (!ConfigManager.TryGetConfig(GlimpseConfig.ConfigName, out Config))
        {
            Logger.Log("   ... Failed: Creating new config.");
            Config = new GlimpseConfig();
            ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Config);
        }
        
        SDL.SetHint(SDL.Hints.MouseFocusClickthrough, "1");
        SDL.SetHint(SDL.Hints.VideoAllowScreensaver, "1");
        
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events))
            throw new Exception("Failed to initialize SDL.");

        _windows = new List<Window>();
        _windowIds = new Dictionary<uint, Window>();

        Player = new AudioPlayer(Logger, new PlayerSettings(Config.SampleRate, Config.Volume, Config.SpeedAdjust));

        Logger.Log("Loading locales.");
        Locale.LoadAvailableLocales();
        
        const string defaultLocale = "en-gb";
        string requestedLocale = Config.Language ?? CultureInfo.CurrentUICulture.Name.ToLower();
        Logger.Log($"Requesting locale '{requestedLocale}'.");
        if (Locale.AvailableLocales.ContainsKey(requestedLocale))
        {
            Locale = Locale.LoadLocale(requestedLocale);
            Config.Language = requestedLocale;
        }
        else
        {
            Logger.Log($"Requested locale '{requestedLocale}' is not available. Loading default locale '{defaultLocale}'.");
            Locale = Locale.LoadLocale(defaultLocale);
            Config.Language = defaultLocale;
        }

        if (!ConfigManager.TryGetConfig(MusicDatabase.DatabaseName, out Database))
        {
            Database = new MusicDatabase();
            ConfigManager.WriteConfig(MusicDatabase.DatabaseName, Database);
        }

        Database!.Logger = Logger;
        Database!.Refresh();
        
        Logger.Log("Searching for 'Plugins' directory.");
        string pluginsLocation = GetPath("Plugins");
        if (Directory.Exists(pluginsLocation))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");

            Plugins = [];
            
            Logger.Log($"Searching for plugins in {pluginsLocation}");
            foreach (string file in Directory.GetFiles(pluginsLocation, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Logger.Log($"Loading assembly from {file}");
                    _pluginsContext.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException e)
                {
                    Logger.Log($"Failed to load DLL: {e}");
                    // If this is thrown then it's likely a native DLL.
                }
            }

            AssemblyName currentName = Assembly.GetAssembly(typeof(IPlugin))?.GetName();
            
            foreach (Assembly assembly in _pluginsContext.Assemblies)
            {
                foreach (AssemblyName name in assembly.GetReferencedAssemblies())
                {
                    if (name.Name == currentName.Name)
                    {
                        if (name.Version != currentName.Version)
                            Console.WriteLine("WARNING: Plugin requires different version of Glimpse. It may cause errors.");
                        
                        goto ASSEMBLY_GOOD;
                    }
                }
                
                continue;
                
                ASSEMBLY_GOOD: ;
                
                Logger.Log($"Plugin {assembly} loaded.");
                
                foreach (Type type in assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(IPlugin))))
                {
                    Logger.Log($"Initializing plugin {type}");
                    
                    IPlugin? plugin = (IPlugin?) Activator.CreateInstance(type);
                    if (plugin == null)
                        continue;

                    string assemblyName = assembly.GetName().Name;

                    if (Config.EnabledPlugins.Contains(assemblyName))
                    {
                        Logger.Log("    ... Initialize()");
                        plugin.Initialize(this);
                    }

                    Plugins.Add(assemblyName, plugin);
                }
            }
        }
        
        AddWindow(window);
        SDL.AddEventWatch(WindowExposedEventWatch, 0);
        
        if (args.Length > 0)
        {
            Player.QueueTrack(args[0], QueueSlot.AtEnd);
            Player.Play();
        }

        while (_windows.Count > 0)
        {
            SDL.Event winEvent;

            SDL.WindowFlags flags = SDL.GetWindowFlags(SDL.GetWindowFromID(_windowIds.First().Key));
            if ((flags & SDL.WindowFlags.Minimized) != 0 && SDL.WaitEvent(out winEvent) ||
                (flags & SDL.WindowFlags.InputFocus) == 0 && SDL.WaitEventTimeout(out winEvent, 250))
            {
                ProcessEvent(winEvent);
            }

            while (SDL.PollEvent(out winEvent))
                ProcessEvent(winEvent);

            foreach (Window wnd in _windows)
            {
                wnd.SetActive();
                wnd.UpdateWindow();
                wnd.Present();
            }
        }
        
        SDL.RemoveEventWatch(WindowExposedEventWatch, 0);
    }

    private bool WindowExposedEventWatch(IntPtr userdata, ref SDL.Event @event)
    {
        switch ((SDL.EventType) @event.Type)
        {
            case SDL.EventType.WindowResized:
            {
                Window wnd = _windowIds[@event.Window.WindowID];
                wnd.SetActive();
                wnd.Renderer.Resize(wnd.Size);
                break;
            }
            
            case SDL.EventType.WindowDisplayScaleChanged:
            {
                Window wnd = _windowIds[@event.Window.WindowID];
                wnd.SetActive();
                //Size winSize = wnd.Size;
                //float scale = SDL.GetWindowDisplayScale(SDL.GetWindowFromID(@event.Window.WindowID));
                //wnd.Size = new Size((int) (winSize.Width * scale), (int) (winSize.Height * scale));
                wnd.Renderer.Resize(wnd.Size);
                wnd.NotifyScaleChanged();
                break;
            }
            
            case SDL.EventType.WindowExposed:
            {
                foreach (Window wnd in _windows)
                {
                    wnd.SetActive();
                    wnd.UpdateWindow();
                    wnd.Present();
                }

                break;
            }
            
            default:
                return true;
        }

        return false;
    }

    private ImGuiMouseButton? SdlButtonToImGui(uint button)
    {
        return button switch
        {
            SDL.ButtonLeft => ImGuiMouseButton.Left,
            SDL.ButtonRight => ImGuiMouseButton.Right,
            SDL.ButtonMiddle => ImGuiMouseButton.Middle,
            _ => null
        };
    }
    
    public void Dispose()
    {
        ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Config);
        
        if (Plugins != null)
        {
            Logger.Log("Disposing all plugins.");
            foreach ((string name, IPlugin plugin) in Plugins)
            {
                Logger.Log($"Disposing plugin {name}");
                plugin.Dispose();
            }
        }
        
        Player.Dispose();
        ConfigManager.WriteConfig(MusicDatabase.DatabaseName, Database);
        Platform.Dispose();
        
        SDL.Quit();
    }

    private void ProcessEvent(SDL.Event winEvent)
    {
        switch ((SDL.EventType) winEvent.Type)
        {
            case SDL.EventType.WindowCloseRequested:
            {
                Window wnd = _windowIds[winEvent.Window.WindowID];
                wnd.Dispose();
                _windowIds.Remove(winEvent.Window.WindowID);
                _windows.Remove(wnd);
                break;
            }

            case SDL.EventType.MouseMotion:
            {
                Window wnd = _windowIds[winEvent.Motion.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);

                ImGui.GetIO().AddMousePosEvent(winEvent.Motion.X * MainWindow.PixelDensity,
                    winEvent.Motion.Y * MainWindow.PixelDensity);
                break;
            }

            case SDL.EventType.MouseButtonDown:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                if (SdlButtonToImGui(winEvent.Button.Button) is { } button)
                    ImGui.GetIO().AddMouseButtonEvent((int) button, true);
                break;
            }
            
            case SDL.EventType.MouseButtonUp:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                if (SdlButtonToImGui(winEvent.Button.Button) is { } button)
                    ImGui.GetIO().AddMouseButtonEvent((int) button, false);
                break;
            }

            case SDL.EventType.MouseWheel:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                ImGui.GetIO().AddMouseWheelEvent(winEvent.Wheel.X, winEvent.Wheel.Y);
                break;
            }
        }
    }

    public static string GetPath(string path)
    {
        return Path.Combine(AppContext.BaseDirectory, path);
    }

    SemVer IGlimpse.Version => Version;
    ILogger IGlimpse.Logger => Logger;
    IConfigManager IGlimpse.ConfigManager => ConfigManager;
    IAudioPlayer IGlimpse.Player => Player;
    ILocale? IGlimpse.Locale => Locale;
}