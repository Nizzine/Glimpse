using System.Drawing;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.Audio;
using Glimpse.Configs;
using Glimpse.Database;
using Glimpse.Platforms;
using Glimpse.Plugins;
using Hexa.NET.ImGui;
using Silk.NET.SDL;
using Version = System.Version;

namespace Glimpse;

public static class Glimpse
{
    private static Sdl _sdl;
    private static List<Window> _windows;
    private static Dictionary<uint, Window> _windowIds;
    private static AssemblyLoadContext _pluginsContext;

    public static Version Version;

    public static PlayerConfig Config;

    public static Platform Platform;

    public static AudioPlayer Player;

    public static MusicDatabase Database;
    
    public static Dictionary<string, Plugin> Plugins;

    public static Window MainWindow => _windows[0];

    public static void AddWindow(Window window)
    {
        uint id = window.Create(_sdl, Platform);
        _windows.Add(window);
        _windowIds.Add(id, window);
    }

    public static unsafe void Run(Window window, string[] args)
    {
        Version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        Logger.Log($"Glimpse {Version}");
        
        Platform = Platform.AutoDetect();
        Logger.Log($"Detected platform {Platform.GetType()}");
        Platform.EnableDPIAwareness();
        
        Logger.Log("Loading player configuration.");
        if (!IConfig.TryGetConfig(PlayerConfig.ConfigName, out Config))
        {
            Logger.Log("   ... Failed: Creating new config.");
            Config = new PlayerConfig();
            IConfig.WriteConfig(PlayerConfig.ConfigName, Config);
        }
        
        _sdl = Sdl.GetApi();
        _sdl.SetHint(Sdl.HintMouseFocusClickthrough, "1");
        
        if (_sdl.Init(Sdl.InitVideo | Sdl.InitEvents) < 0)
            throw new Exception("Failed to initialize SDL.");

        _windows = new List<Window>();
        _windowIds = new Dictionary<uint, Window>();

        Player = new AudioPlayer(new PlayerSettings(Config.SampleRate, Config.Volume, Config.SpeedAdjust, Config.AutoPlay));

        if (!IConfig.TryGetConfig(MusicDatabase.DatabaseName, out Database))
        {
            Database = new MusicDatabase();
            IConfig.WriteConfig(MusicDatabase.DatabaseName, Database);
        }
        Database.Refresh();
        
        Logger.Log("Searching for 'Plugins' directory.");
        if (Directory.Exists("Plugins"))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");

            Plugins = [];

            string pluginsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            
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

            AssemblyName currentName = Assembly.GetAssembly(typeof(AudioPlayer))?.GetName();
            
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
                
                foreach (Type type in assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(Plugin))))
                {
                    Logger.Log($"Initializing plugin {type}");
                    
                    Plugin plugin = (Plugin) Activator.CreateInstance(type);
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
        
        if (args.Length > 0)
        {
            Player.QueueTrack(args[0], QueueSlot.AtEnd);
            Player.Play();
        }

        while (_windows.Count > 0)
        {
            Event winEvent;
            while (_sdl.PollEvent(&winEvent) != 0)
            {
                switch ((EventType) winEvent.Type)
                {
                    case EventType.Windowevent:
                    {
                        switch ((WindowEventID) winEvent.Window.Event)
                        {
                            case WindowEventID.Close:
                            {
                                Window wnd = _windowIds[winEvent.Window.WindowID];
                                wnd.Dispose();
                                _windowIds.Remove(winEvent.Window.WindowID);
                                _windows.Remove(wnd);
                                break;
                            }

                            case WindowEventID.Resized:
                            {
                                Window wnd = _windowIds[winEvent.Window.WindowID];
                                Size newSize = new Size(winEvent.Window.Data1, winEvent.Window.Data2);
                                wnd.SetActive();
                                wnd.Renderer.Resize(newSize);
                                
                                break;
                            }
                        }

                        break;
                    }

                    case EventType.Mousemotion:
                    {
                        Window wnd = _windowIds[winEvent.Motion.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMousePosEvent(winEvent.Motion.X, winEvent.Motion.Y);
                        break;
                    }

                    case EventType.Mousebuttondown:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseButtonEvent((int) SdlButtonToImGui(winEvent.Button.Button), true);
                        break;
                    }
                    
                    case EventType.Mousebuttonup:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseButtonEvent((int) SdlButtonToImGui(winEvent.Button.Button), false);
                        break;
                    }

                    case EventType.Mousewheel:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseWheelEvent(winEvent.Wheel.X, winEvent.Wheel.Y);
                        break;
                    }
                }
            }

            foreach (Window wnd in _windows)
            {
                wnd.SetActive();
                wnd.UpdateWindow();
                wnd.Present();
            }
        }
        
        Player.Dispose();
        IConfig.WriteConfig(MusicDatabase.DatabaseName, Database);
        
        _sdl.Quit();
        _sdl.Dispose();
    }

    private static ImGuiMouseButton SdlButtonToImGui(uint button)
    {
        return button switch
        {
            Sdl.ButtonLeft => ImGuiMouseButton.Left,
            Sdl.ButtonRight => ImGuiMouseButton.Right,
            Sdl.ButtonMiddle => ImGuiMouseButton.Middle,
            _ => ImGuiMouseButton.Count
        };
    }
}