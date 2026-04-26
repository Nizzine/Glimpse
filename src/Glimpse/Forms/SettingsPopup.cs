using System.Numerics;
using Glimpse.API;
using Glimpse.Configs;
using Glimpse.Graphics;
using Glimpse.Locales;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class SettingsPopup : Popup
{
    private GlimpseConfig _currentConfig;
    
    private Image? _glimpseLogo;
    private string? _currentPlugin;

    private Image? _darkMode;
    private Image? _lightMode;

    private Image? _transportDown;
    private Image? _transportUp;

    public override void Open()
    {
        _currentConfig = Glimpse.Config;
        _currentConfig.EnabledPlugins = new HashSet<string>(Glimpse.Config.EnabledPlugins);
    }

    public override void Update()
    {
        Locale locale = Glimpse.Locale;
        string popupName = locale.GetString("Popup.Settings.Name");
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;
        if (Glimpse.Config != _currentConfig)
            flags |= ImGuiWindowFlags.UnsavedDocument;
        
        if (ImGui.BeginPopupModal(popupName, flags))
        {
            if (ImGui.BeginChild("SettingsItems", ScaleVec(600, 500)))
            {
                if (ImGui.BeginTabBar("SettingsTab"))
                {
                    if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.General")))
                    {
                        if (ImGui.BeginCombo(locale.GetString("Popup.Settings.Tab.General.Language"),
                                locale.DisplayName))
                        {
                            foreach ((string id, (_, string name)) in Locale.AvailableLocales)
                            {
                                if (ImGui.Selectable(name,
                                        locale.ID == id ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None))
                                {
                                    _currentConfig.Language = id;
                                    Glimpse.Locale = Locale.LoadLocale(id);
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Checkbox(locale.GetString("Popup.Settings.Tab.General.EnableDeleteFile"),
                            ref _currentConfig.EnableFileDeletion);
                        ImGui.SetItemTooltipUnformatted(
                            locale.GetString("Popup.Settings.Tab.General.EnableDeleteFile.Tooltip"));

                        ImGui.Checkbox(locale.GetString("Popup.Settings.Tab.General.CheckForUpdates"),
                            ref _currentConfig.EnableUpdateChecking);
                        ImGui.SetItemTooltipUnformatted(locale.GetString("Popup.Settings.Tab.General.CheckForUpdates.Tooltip"));

                        ImGui.EndTabItem();
                    }
                    
                    if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.Appearance")))
                    {
                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Appearance.Theme"));

                        string syncToOS = locale.GetString("Popup.Settings.Tab.Appearance.Theme.SyncToOS");
                        string dark = locale.GetString("Popup.Settings.Tab.Appearance.Theme.Dark");
                        string light = locale.GetString("Popup.Settings.Tab.Appearance.Theme.Light");

                        bool shouldSyncToOS = _currentConfig.Theme == Theme.SyncToOS;
                        if (ImGui.Checkbox(syncToOS, ref shouldSyncToOS))
                            _currentConfig.Theme = shouldSyncToOS ? Theme.SyncToOS : Theme.Dark;

                        _lightMode ??= Renderer.CreateImage("Assets/Images/LightMode.png");
                        _darkMode ??= Renderer.CreateImage("Assets/Images/DarkMode.png");

                        ImGui.BeginDisabled(shouldSyncToOS);
                        
                        if (ImGui.SelectButton("LightMode", _lightMode,
                                ScaleVec(_darkMode.Width * 0.25f, _darkMode.Height * 0.25f),
                                _currentConfig.Theme == Theme.Light))
                        {
                            _currentConfig.Theme = Theme.Light;
                        }
                        ImGui.SetItemTooltipUnformatted(light);
                        
                        ImGui.SameLine();
                        
                        if (ImGui.SelectButton("DarkMode", _darkMode,
                                ScaleVec(_darkMode.Width * 0.25f, _darkMode.Height * 0.25f),
                                _currentConfig.Theme == Theme.Dark))
                        {
                            _currentConfig.Theme = Theme.Dark;
                        }
                        ImGui.SetItemTooltipUnformatted(dark);
                        
                        ImGui.EndDisabled();
                        
                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Appearance.TransportLocation"));

                        _transportDown ??= Renderer.CreateImage("Assets/Images/TransportDown.png");
                        _transportUp ??= Renderer.CreateImage("Assets/Images/TransportUp.png");
                        
                        string up = locale.GetString("Popup.Settings.Tab.Appearance.TransportLocation.Up");
                        string down = locale.GetString("Popup.Settings.Tab.Appearance.TransportLocation.Down");
                        
                        if (ImGui.SelectButton("TransportDown", _transportDown,
                            ScaleVec(_transportDown.Width * 0.25f, _transportDown.Height * 0.25f),
                            !_currentConfig.SwapTransportControls))
                        {
                            _currentConfig.SwapTransportControls = false;
                        }
                        ImGui.SetItemTooltipUnformatted(down);

                        ImGui.SameLine();
                        
                        if (ImGui.SelectButton("TransportUp", _transportUp,
                                ScaleVec(_transportUp.Width * 0.25f, _transportUp.Height * 0.25f),
                                _currentConfig.SwapTransportControls))
                        {
                            _currentConfig.SwapTransportControls = true;
                        }
                        ImGui.SetItemTooltipUnformatted(up);
                        
                        ImGui.EndTabItem();
                    }

                    /*if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.Player")))
                    {
                        ref float volume = ref _currentConfig.Volume;
                        ref uint sampleRate = ref _currentConfig.SampleRate;
                        //float speed = (float) _currentConfig.SpeedAdjust;

                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Player.PlaybackHeading"));

                        if (ImGui.SliderFloat(locale.GetString("Popup.Settings.Tab.Player.Volume"), ref volume, 0, 1, "%.3f"))
                            Glimpse.Player.Volume = volume;
                        
                        /*ImGui.Checkbox("Auto Play", ref autoPlay);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Start playing when a track is selected or added to queue.");
                        
                        //if (ImGui.DragFloat("Speed Adjustment", ref speed, 0.01f, 0.01f, 10))
                        //    _currentConfig.SpeedAdjust = speed;
                        
                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Player.DeviceHeading"));
                        ImGui.BeginDisabled();
                        if (ImGui.BeginCombo(locale.GetString("SampleRate"), sampleRate.ToString()))
                        {
                            ImGui.EndCombo();
                        }
                        ImGui.EndDisabled();

                        ImGui.EndTabItem();
                    }*/

                    if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.Plugins")))
                    {
                        if (Glimpse.Plugins == null || Glimpse.Plugins.Count == 0)
                        {
                            ImGui.TextUnformatted(locale.GetString("Popup.Settings.Tab.Plugins.NoneAvailable"));
                        }
                        else
                        {
                            foreach ((string name, IPlugin plugin) in Glimpse.Plugins)
                            {
                                ImGui.BeginChild("PluginsList", new Vector2(150, 0));
                                {
                                    if (ImGui.Selectable(plugin.Name, name == _currentPlugin))
                                        _currentPlugin = name;

                                    ImGui.EndChild();
                                }

                                ImGui.SameLine();

                                ImGui.BeginChild("PluginSettings");
                                {
                                    if (name == _currentPlugin)
                                    {
                                        bool enabled = _currentConfig.EnabledPlugins.Contains(_currentPlugin);
                                        if (ImGui.Checkbox(locale.GetString("Checkbox.Enabled"), ref enabled))
                                        {
                                            if (enabled)
                                                _currentConfig.EnabledPlugins.Add(_currentPlugin);
                                            else
                                                _currentConfig.EnabledPlugins.Remove(_currentPlugin);
                                        }
                                        
                                        // TODO: Hack - ideally would display the GUI for all plugins even if disabled
                                        //       Need some sort of API to ensure the plugins always know the config is
                                        //       valid before displaying the GUI?
                                        if (enabled)
                                        {
                                            ImGui.Separator();
                                            if (Glimpse.Plugins[_currentPlugin].IsInitialized)
                                                Glimpse.Plugins[_currentPlugin].DisplayGui();
                                        }
                                    }

                                    ImGui.EndChild();
                                }
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.About")))
                    {
                        _glimpseLogo ??= Renderer.CreateImage("Assets/Icons/Glimpse.png");

                        if (ImGui.BeginChild("GlimpseLogo", ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                        {
                            ImGui.Image(_glimpseLogo, ScaleVec(128, 128));
                            ImGui.EndChild();
                        }

                        ImGui.SameLine();
                        
                        if (ImGui.BeginChild("GlimpseText", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                        {
                            ImGui.PushFont(null, 32);
                            ImGui.TextUnformatted(locale.GetString("Popup.Settings.Tab.About.AppName", Glimpse.Version));
                            ImGui.PopFont();
                            ImGui.TextUnformatted("2026 aquagoose");

                            ImGui.Spacing();
                            ImGui.TextUnformatted(locale.GetString("Popup.Settings.Tab.About.Credits"));
                            
                            ImGui.Spacing();
                            if (ImGui.TextLink(locale.GetString("Popup.Settings.Tab.About.Website")))
                                Utils.OpenLink("https://glimpseaudio.co.uk");
                            
                            ImGui.SameLine();
                            
                            if (ImGui.TextLink(locale.GetString("Popup.Settings.Tab.About.Donate")))
                                Utils.OpenLink("https://glimpseaudio.co.uk/donate");
                            
                            ImGui.SameLine();
                            
                            if (ImGui.TextLink(locale.GetString("Popup.Settings.Tab.About.Repository")))
                                Utils.OpenLink("https://glimpseaudio.co.uk/repo");
                            
                            ImGui.SameLine();
                            
                            if (ImGui.TextLink(locale.GetString("Popup.Settings.Tab.About.Discord")))
                                Utils.OpenLink("https://glimpseaudio.co.uk/discord");
                            
                            ImGui.EndChild();
                        }

                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.About.OpenSourceLibraries"));
                        {
                            ImGui.BeginChild("OSLibraries");
                            {
                                if (ImGui.TextLink("mixr"))
                                    Utils.OpenLink("https://github.com/Aquatic-Games/mixr");
                                if (ImGui.TextLink("Hexa.NET.ImGui"))
                                    Utils.OpenLink("https://github.com/HexaEngine/Hexa.NET.ImGui");
                                if (ImGui.TextLink("Silk.NET"))
                                    Utils.OpenLink("https://dotnet.github.io/Silk.NET/");
                                if (ImGui.TextLink("SDL3-CS"))
                                    Utils.OpenLink("github.com/edwardgushchin/SDL3-CS");
                                if (ImGui.TextLink("TagLibSharp"))
                                    Utils.OpenLink("https://github.com/mono/taglib-sharp");
                                if (ImGui.TextLink("ImageSharp"))
                                    Utils.OpenLink("https://sixlabors.com/products/imagesharp/");
                                if (ImGui.TextLink("empress"))
                                    Utils.OpenLink("https://github.com/aquagoose/empress");
                                if (ImGui.TextLink("DiscordRichPresence"))
                                    Utils.OpenLink("https://github.com/Lachee/discord-rpc-csharp");
                                if (ImGui.TextLink("MetaBrainz.MusicBrainz"))
                                    Utils.OpenLink("https://github.com/Zastai/MetaBrainz.MusicBrainz");
                                if (ImGui.TextLink("MetaBrainz.MusicBrainz.CoverArt"))
                                    Utils.OpenLink("https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt");
                                if (ImGui.TextLink("TerraFX.Interop.Windows"))
                                    Utils.OpenLink("https://github.com/terrafx/terrafx.interop.windows");
                                if (ImGui.TextLink("Newtonsoft.Json"))
                                    Utils.OpenLink("https://www.newtonsoft.com/json");

                                ImGui.EndChild();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    ImGui.EndChild();
                }

                if (ImGui.Button(locale.GetString("Button.Save")))
                {
                    Apply();
                    Close();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button(locale.GetString("Button.Cancel")))
                    Close();
            }
            
            ImGui.EndPopup();
        }
    }

    private void Apply()
    {
        GlimpseConfig oldConfig = Glimpse.Config;
        if (oldConfig == _currentConfig)
            return;

        Logger logger = Glimpse.Logger;
        
        logger.Log("Saving and applying config changes.");
        //Glimpse.Player.Stop();
        
        Glimpse.Config = _currentConfig;
        Glimpse.ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Glimpse.Config);
        
        if (_currentConfig.SwapTransportControls != oldConfig.SwapTransportControls || _currentConfig.Theme != oldConfig.Theme)
            ((GlimpsePlayer) Glimpse.MainWindow).RefreshLayout();

        if (Glimpse.Plugins == null)
            return;
        
        foreach ((string name, IPlugin plugin) in Glimpse.Plugins)
        {
            // Plugin has been disabled
            if (oldConfig.EnabledPlugins.Contains(name) && !_currentConfig.EnabledPlugins.Contains(name))
            {
                logger.Log($"Disabling plugin {name}");
                plugin.Dispose();
            }
            // Plugin has been enabled
            else if (_currentConfig.EnabledPlugins.Contains(name) && !oldConfig.EnabledPlugins.Contains(name))
            {
                logger.Log($"Enabling plugin {name}");
                plugin.Initialize(Glimpse);
            }
        }
    }

    public override void Dispose()
    {
        _glimpseLogo?.Dispose();
    }
}