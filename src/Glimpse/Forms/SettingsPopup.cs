using System.Numerics;
using Glimpse.API;
using Glimpse.Audio;
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
                        if (ImGui.BeginCombo(locale.GetString("Popup.Settings.Tab.General.Language"), locale.DisplayName))
                        {
                            foreach ((string id, (_, string name)) in Locale.AvailableLocales)
                            {
                                if (ImGui.Selectable(name, locale.ID == id ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None))
                                {
                                    _currentConfig.Language = id;
                                    Glimpse.Locale = Locale.LoadLocale(id);
                                }
                            }
                            
                            ImGui.EndCombo();
                        }

                        string up = locale.GetString("Popup.Settings.Tab.General.TransportLocation.Up");
                        string down = locale.GetString("Popup.Settings.Tab.General.TransportLocation.Down");
                        if (ImGui.BeginCombo(locale.GetString("Popup.Settings.Tab.General.TransportLocation"), _currentConfig.SwapTransportControls ? up : down))
                        {
                            if (ImGui.Selectable(up, _currentConfig.SwapTransportControls ? ImGuiSelectableFlags.Highlight : 0))
                                _currentConfig.SwapTransportControls = true;
                            if (ImGui.Selectable(down, !_currentConfig.SwapTransportControls ? ImGuiSelectableFlags.Highlight : 0))
                                _currentConfig.SwapTransportControls = false;
                            
                            ImGui.EndCombo();
                        }
                        ImGui.SetItemTooltip(locale.GetString("Popup.Settings.Tab.General.TransportLocation.Tooltip"));
                        
                        ImGui.Separator();
                        
                        ImGui.Checkbox(locale.GetString("Popup.Settings.Tab.General.EnableDeleteFile"), ref _currentConfig.EnableFileDeletion);
                        ImGui.SetItemTooltip(locale.GetString("Popup.Settings.Tab.General.EnableDeleteFile.Tooltip"));
                        
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.Player")))
                    {
                        ref float volume = ref _currentConfig.Volume;
                        ref bool autoPlay = ref _currentConfig.AutoPlay;
                        ref uint sampleRate = ref _currentConfig.SampleRate;
                        //float speed = (float) _currentConfig.SpeedAdjust;

                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Player.PlaybackHeading"));

                        if (ImGui.SliderFloat(locale.GetString("Popup.Settings.Tab.Player.Volume"), ref volume, 0, 1, "%.3f"))
                            Glimpse.Player.Volume = volume;
                        
                        /*ImGui.Checkbox("Auto Play", ref autoPlay);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Start playing when a track is selected or added to queue.");*/
                        
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
                    }

                    if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.Plugins")))
                    {
                        if (Glimpse.Plugins == null || Glimpse.Plugins.Count == 0)
                        {
                            ImGui.Text(locale.GetString("Popup.Settings.Tab.Plugins.NoneAvailable"));
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
                            ImGui.PushFont(null, 34 * Scale);
                            ImGui.Text(locale.GetString("Popup.Settings.Tab.About.AppName", Glimpse.Version));
                            ImGui.PopFont();
                            ImGui.Text(locale.GetString("Popup.Settings.Tab.About.Copyright"));

                            ImGui.Spacing();
                            ImGui.Text(locale.GetString("Popup.Settings.Tab.About.Credits"));
                            
                            ImGui.EndChild();
                        }

                        if (ImGui.CollapsingHeader(locale.GetString("Popup.Settings.Tab.About.OpenSourceLibraries")))
                        {
                            ImGui.BeginChild("OSLibraries");
                            {
                                if (ImGui.TextLink("mixr"))
                                    GlimpsePlayer.OpenLink("https://github.com/Aquatic-Games/mixr");
                                if (ImGui.TextLink("Hexa.NET.ImGui"))
                                    GlimpsePlayer.OpenLink("https://github.com/HexaEngine/Hexa.NET.ImGui");
                                if (ImGui.TextLink("Silk.NET"))
                                    GlimpsePlayer.OpenLink("https://github.com/dotnet/Silk.NET");
                                if (ImGui.TextLink("SDL3-CS"))
                                    GlimpsePlayer.OpenLink("github.com/edwardgushchin/SDL3-CS");
                                if (ImGui.TextLink("TagLibSharp"))
                                    GlimpsePlayer.OpenLink("https://github.com/mono/taglib-sharp");
                                if (ImGui.TextLink("StbImageSharp"))
                                    GlimpsePlayer.OpenLink("https://github.com/StbSharp/StbImageSharp");
                                if (ImGui.TextLink("empress"))
                                    GlimpsePlayer.OpenLink("https://github.com/aquagoose/empress");
                                if (ImGui.TextLink("DiscordRichPresence"))
                                    GlimpsePlayer.OpenLink("https://github.com/Lachee/discord-rpc-csharp");
                                if (ImGui.TextLink("MetaBrainz.MusicBrainz"))
                                    GlimpsePlayer.OpenLink("https://github.com/Zastai/MetaBrainz.MusicBrainz");
                                if (ImGui.TextLink("MetaBrainz.MusicBrainz.CoverArt"))
                                    GlimpsePlayer.OpenLink("https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt");
                                if (ImGui.TextLink("TerraFX.Interop.Windows"))
                                    GlimpsePlayer.OpenLink("https://github.com/terrafx/terrafx.interop.windows");
                                if (ImGui.TextLink("Newtonsoft.Json"))
                                    GlimpsePlayer.OpenLink("https://github.com/JamesNK/Newtonsoft.Json");

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
        
        //((GlimpsePlayer) Glimpse.MainWindow).RefreshLayout();

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