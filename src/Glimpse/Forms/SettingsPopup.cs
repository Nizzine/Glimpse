using System.Numerics;
using Glimpse.Audio;
using Glimpse.Configs;
using Glimpse.Graphics;
using Glimpse.Plugins;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class SettingsPopup : Popup
{
    private PlayerConfig _currentConfig;
    
    private Image _glimpseLogo;
    private string _currentPlugin;

    public override void Open()
    {
        _currentConfig = Glimpse.Player.Config;
        _currentConfig.EnabledPlugins = new HashSet<string>(Glimpse.Player.Config.EnabledPlugins);
    }

    public override void Update()
    {
        if (!ImGui.IsPopupOpen("Settings"))
            ImGui.OpenPopup("Settings");

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;
        if (Glimpse.Player.Config != _currentConfig)
            flags |= ImGuiWindowFlags.UnsavedDocument;
        
        if (ImGui.BeginPopupModal("Settings", flags))
        {
            if (ImGui.BeginChild("SettingsItems", ScaleVec(500, 350)))
            {
                if (ImGui.BeginTabBar("SettingsTab"))
                {
                    if (ImGui.BeginTabItem("Theme"))
                    {
                        if (ImGui.BeginCombo("Transport Location", _currentConfig.SwapTransportControls ? "Up" : "Down"))
                        {
                            if (ImGui.Selectable("Up", _currentConfig.SwapTransportControls ? ImGuiSelectableFlags.Highlight : 0))
                                _currentConfig.SwapTransportControls = true;
                            if (ImGui.Selectable("Down", !_currentConfig.SwapTransportControls ? ImGuiSelectableFlags.Highlight : 0))
                                _currentConfig.SwapTransportControls = false;
                            
                            ImGui.EndCombo();
                        }
                        ImGui.SetItemTooltip("Set the location of the transport bar. PLEASE RESTART after saving!");
                        
                        ImGui.Separator();
                        
                        ImGui.Checkbox("Enable \"delete file\" context menu item", ref _currentConfig.EnableFileDeletion);
                        ImGui.SetItemTooltip("Enables the \"delete file\" context menu item, allowing files to be permanently deleted from your computer.");
                        
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Player"))
                    {
                        ref float volume = ref _currentConfig.Volume;
                        ref bool autoPlay = ref _currentConfig.AutoPlay;
                        ref uint sampleRate = ref _currentConfig.SampleRate;
                        float speed = (float) _currentConfig.SpeedAdjust;

                        ImGui.SeparatorText("Playback");
                        
                        ImGui.SliderFloat("Volume", ref volume, 0, 1, "%.3f", ImGuiSliderFlags.Logarithmic);
                        
                        ImGui.Checkbox("Auto Play", ref autoPlay);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Start playing when a track is selected or added to queue.");
                        
                        if (ImGui.DragFloat("Speed Adjustment", ref speed, 0.01f, 0.01f, 10))
                            _currentConfig.SpeedAdjust = speed;
                        
                        ImGui.SeparatorText("Device");
                        ImGui.BeginDisabled();
                        if (ImGui.BeginCombo("Sample Rate", sampleRate.ToString()))
                        {
                            ImGui.EndCombo();
                        }
                        ImGui.EndDisabled();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Plugins"))
                    {
                        foreach ((string name, Plugin plugin) in Glimpse.Player.Plugins)
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
                                    if (ImGui.Checkbox("Enabled", ref enabled))
                                    {
                                        if (enabled)
                                            _currentConfig.EnabledPlugins.Add(_currentPlugin);
                                        else
                                            _currentConfig.EnabledPlugins.Remove(_currentPlugin);
                                    }

                                    ImGui.Separator();
                                    Glimpse.Player.Plugins[_currentPlugin].DisplayGui();
                                }

                                ImGui.EndChild();
                            }
                        }
                        
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("About"))
                    {
                        _glimpseLogo ??= Renderer.CreateImage("Assets/Icons/Glimpse.png");

                        if (ImGui.BeginChild("GlimpseLogo", ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                        {
                            ImGui.Image(_glimpseLogo.ID, ScaleVec(128, 128));
                            ImGui.EndChild();
                        }

                        ImGui.SameLine();
                        
                        if (ImGui.BeginChild("GlimpseText"))
                        {
                            ImGui.Text($"Glimpse {Glimpse.Version}");
                            ImGui.Text("2025 aquagoose");

                            ImGui.Spacing();
                            ImGui.Text("Made by aquagoose");
                            ImGui.Text("Themed by Nizzine");
                            
                            ImGui.EndChild();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    ImGui.EndChild();
                }

                if (ImGui.Button("Save"))
                {
                    Apply();
                    Close();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel"))
                    Close();
            }
            
            ImGui.EndPopup();
        }
    }

    private void Apply()
    {
        PlayerConfig oldConfig = Glimpse.Player.Config;
        if (oldConfig == _currentConfig)
            return;
        
        Logger.Log("Saving and applying config changes.");
        Glimpse.Player.Stop();
        
        Glimpse.Player.Config = _currentConfig;
        IConfig.WriteConfig(PlayerConfig.ConfigName, Glimpse.Player.Config);
        
        //((GlimpsePlayer) Glimpse.MainWindow).RefreshLayout();

        if (Glimpse.Player.Plugins == null)
            return;
        
        foreach ((string name, Plugin plugin) in Glimpse.Player.Plugins)
        {
            // Plugin has been disabled
            if (oldConfig.EnabledPlugins.Contains(name) && !_currentConfig.EnabledPlugins.Contains(name))
            {
                Logger.Log($"Disabling plugin {name}");
                plugin.Dispose();
            }
            // Plugin has been enabled
            else if (_currentConfig.EnabledPlugins.Contains(name) && !oldConfig.EnabledPlugins.Contains(name))
            {
                Logger.Log($"Enabling plugin {name}");
                plugin.Initialize(Glimpse.Player);
            }
        }
    }

    public override void Dispose()
    {
        _glimpseLogo?.Dispose();
    }
}