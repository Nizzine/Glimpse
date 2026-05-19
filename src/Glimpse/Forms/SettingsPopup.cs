using System.Numerics;
using System.Text.Json;
using Glimpse.API;
using Glimpse.Assets;
using Glimpse.Configs;
using Glimpse.Graphics;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class SettingsPopup : Popup
{
    private GlimpseConfig _currentConfig;
    
    private Image? _glimpseLogo;
    private string? _currentPlugin;

    private List<(string name, Theme theme)> _themes = [];

    private Image? _transportDown;
    private Image? _transportUp;

    public override void Open()
    {
        _currentConfig = Glimpse.Config;
        _currentConfig.Plugins.EnabledPlugins = new HashSet<string>(Glimpse.Config.Plugins.EnabledPlugins);

        _themes.Clear();
        foreach (string name in Asset.GetAllNamesInFolder("Themes"))
        {
            using Stream stream = Asset.GetAssetStream(name);
            Theme theme = JsonSerializer.Deserialize<Theme>(stream, ConfigManager.GetDefaultSerializerOptions());
            _themes.Add((name.Replace("Themes.", "").Replace(".json", ""), theme));
        }
        
        _themes.Sort((theme, theme1) => theme.theme.Name.CompareTo(theme1.theme.Name, StringComparison.InvariantCulture));
    }

    public override void Update(float dt)
    {
        Locale currentLocale = Glimpse.Locale;

        ImGuiWindowFlags flags = ImGuiWindowFlags.None;
        if (Glimpse.Config != _currentConfig)
            flags |= ImGuiWindowFlags.UnsavedDocument;
        
        if (ImGui.OpenPopupModal(currentLocale.GetString("Popup.Settings.Name"), ScaleVec(620, 570), flags))
        {
            ImGui.BeginChild("SettingsItems", ScaleVec(600, 500));
            {
                if (ImGui.BeginTabBar("SettingsTab"))
                {
                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.General")))
                    {
                        if (ImGui.BeginCombo(currentLocale.GetString("Popup.Settings.Tab.General.Language"),
                                currentLocale.DisplayName))
                        {
                            foreach ((string id, Locale.AvailableLocale locale) in Locale.AvailableLocales.Locales)
                            {
                                if (ImGui.Selectable(locale.DisplayName, currentLocale.ID == id ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None))
                                {
                                    _currentConfig.General.Language = id;
                                    Glimpse.Locale = Locale.LoadLocale(id);
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Checkbox(currentLocale.GetString("Popup.Settings.Tab.General.EnableDeleteFile"),
                            ref _currentConfig.General.EnableFileDeletion);
                        ImGui.SetItemTooltipUnformatted(
                            currentLocale.GetString("Popup.Settings.Tab.General.EnableDeleteFile.Tooltip"));

                        ImGui.Checkbox(currentLocale.GetString("Popup.Settings.Tab.General.CheckForUpdates"),
                            ref _currentConfig.General.EnableUpdateChecking);
                        ImGui.SetItemTooltipUnformatted(currentLocale.GetString("Popup.Settings.Tab.General.CheckForUpdates.Tooltip"));

                        ImGui.EndTabItem();
                    }
                    
                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.Appearance")))
                    {
                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme"));

                        string syncToOS = currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme.SyncToOS");
                        string dark = currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme.Dark");
                        string light = currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme.Light");

                        ref PreferredColorScheme scheme = ref _currentConfig.Appearance.PreferredColorScheme;
                        bool shouldSyncToOS = scheme == PreferredColorScheme.SyncToOS;
                        if (ImGui.Checkbox(syncToOS, ref shouldSyncToOS))
                            scheme = shouldSyncToOS ? PreferredColorScheme.SyncToOS : PreferredColorScheme.Dark;
                        
                        ImGui.BeginDisabled(shouldSyncToOS);

                        if (ImGui.BeginCombo("Colour Scheme", scheme == PreferredColorScheme.Light ? light : dark))
                        {
                            if (ImGui.Selectable(dark, scheme == PreferredColorScheme.Dark))
                                scheme = PreferredColorScheme.Dark;
                            if (ImGui.Selectable(light, scheme == PreferredColorScheme.Light))
                                scheme = PreferredColorScheme.Light;
                            
                            ImGui.EndCombo();
                        }
                        
                        ImGui.EndDisabled();

                        //_lightMode ??= Renderer.CreateImage("Images.LightMode.png");
                        //_darkMode ??= Renderer.CreateImage("Images.DarkMode.png");

                        if (ImGui.BeginListBox("##ThemesList"))
                        {
                            foreach ((string name, Theme theme) in _themes)
                            {
                                if (ImGui.Selectable(theme.Name, _currentConfig.Appearance.Theme == name))
                                    _currentConfig.Appearance.Theme = name;
                            }

                            ImGui.EndListBox();
                        }

                        if (ImGui.Button("Open Theme Editor"))
                        {
                            Close();
                            Glimpse.MainWindow.AddPopup(new ThemeEditor());
                        }

                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.Appearance.TransportLocation"));

                        _transportDown ??= Renderer.CreateImage("Images.TransportDown.png");
                        _transportUp ??= Renderer.CreateImage("Images.TransportUp.png");
                        
                        string up = currentLocale.GetString("Popup.Settings.Tab.Appearance.TransportLocation.Up");
                        string down = currentLocale.GetString("Popup.Settings.Tab.Appearance.TransportLocation.Down");
                        
                        if (ImGui.SelectButton("TransportDown", _transportDown,
                            ScaleVec(_transportDown.Width * 0.25f, _transportDown.Height * 0.25f),
                            !_currentConfig.Appearance.SwapTransportControls))
                        {
                            _currentConfig.Appearance.SwapTransportControls = false;
                        }
                        ImGui.SetItemTooltipUnformatted(down);

                        ImGui.SameLine();
                        
                        if (ImGui.SelectButton("TransportUp", _transportUp,
                                ScaleVec(_transportUp.Width * 0.25f, _transportUp.Height * 0.25f),
                                _currentConfig.Appearance.SwapTransportControls))
                        {
                            _currentConfig.Appearance.SwapTransportControls = true;
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
#if !PUBLISH_AOT
                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.Plugins")))
                    {
                        if (Glimpse.Plugins == null || Glimpse.Plugins.Count == 0)
                        {
                            ImGui.TextUnformatted(currentLocale.GetString("Popup.Settings.Tab.Plugins.NoneAvailable"));
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
                                        bool enabled = _currentConfig.Plugins.EnabledPlugins.Contains(_currentPlugin);
                                        if (ImGui.Checkbox(currentLocale.GetString("Checkbox.Enabled"), ref enabled))
                                        {
                                            if (enabled)
                                                _currentConfig.Plugins.EnabledPlugins.Add(_currentPlugin);
                                            else
                                                _currentConfig.Plugins.EnabledPlugins.Remove(_currentPlugin);
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
#endif

                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.About")))
                    {
                        _glimpseLogo ??= Renderer.CreateImage("Icons.Glimpse.png");

                        ImGui.BeginChild("GlimpseLogo", ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                        {
                            ImGui.Image(_glimpseLogo, ScaleVec(128, 128));
                            ImGui.EndChild();
                        }

                        ImGui.SameLine();

                        ImGui.BeginChild("GlimpseText", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                        {
                            ImGui.PushFont(null, 32);
                            ImGui.TextUnformatted(currentLocale.GetString("Popup.Settings.Tab.About.AppName", Glimpse.Version));
                            ImGui.PopFont();
                            ImGui.TextUnformatted("2026 aquagoose");

                            ImGui.Spacing();
                            ImGui.TextUnformatted(currentLocale.GetString("Popup.Settings.Tab.About.Credits"));
                            
                            ImGui.Spacing();
                            if (ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Website")))
                                Utils.OpenLink("https://glimpseaudio.co.uk");
                            
                            ImGui.SameLine();
                            
                            if (ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Donate")))
                                Utils.OpenLink("https://glimpseaudio.co.uk/donate");
                            
                            ImGui.SameLine();
                            
                            if (ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Repository")))
                                Utils.OpenLink("https://glimpseaudio.co.uk/repo");
                            
                            ImGui.SameLine();
                            
                            if (ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Discord")))
                                Utils.OpenLink("https://glimpseaudio.co.uk/discord");
                            
                            ImGui.EndChild();
                        }

                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.About.OpenSourceLibraries"));
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

                                ImGui.EndChild();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    ImGui.EndChild();
                }

                if (ImGui.Button(currentLocale.GetString("Button.Save")))
                {
                    Apply();
                    Close();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button(currentLocale.GetString("Button.Cancel")))
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
        
        if (_currentConfig.Appearance.SwapTransportControls != oldConfig.Appearance.SwapTransportControls || _currentConfig.Appearance.Theme != oldConfig.Appearance.Theme || _currentConfig.Appearance.PreferredColorScheme != oldConfig.Appearance.PreferredColorScheme)
            ((GlimpsePlayer) Glimpse.MainWindow).RefreshLayout();

        if (Glimpse.Plugins == null)
            return;
        
        foreach ((string name, IPlugin plugin) in Glimpse.Plugins)
        {
            // Plugin has been disabled
            if (oldConfig.Plugins.EnabledPlugins.Contains(name) && !_currentConfig.Plugins.EnabledPlugins.Contains(name))
            {
                logger.Log($"Disabling plugin {name}");
                plugin.Dispose();
            }
            // Plugin has been enabled
            else if (_currentConfig.Plugins.EnabledPlugins.Contains(name) && !oldConfig.Plugins.EnabledPlugins.Contains(name))
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