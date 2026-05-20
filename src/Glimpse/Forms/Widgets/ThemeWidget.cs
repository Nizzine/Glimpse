using System.Text.Json;
using Glimpse.Assets;
using Glimpse.Configs;
using Hexa.NET.ImGui;
using SDL3;

namespace Glimpse.Forms.Widgets;

public class ThemeWidget : IDisposable
{
    private readonly Popup _popup;
    private readonly Glimpse _glimpse;
    private bool _isSystemThemeLightMode;
    
    private Dictionary<string, Theme> _themes = [];

    public ThemeWidget(Popup popup)
    {
        _popup = popup;
        _glimpse = _popup.Glimpse;
        _isSystemThemeLightMode = SDL.GetSystemTheme() == SDL.SystemTheme.Light;
        
        _themes.Clear();
        foreach (string name in Asset.GetAllNamesInFolder("Themes"))
        {
            using Stream stream = Asset.GetAssetStream(name);
            Theme theme = JsonSerializer.Deserialize<Theme>(stream, ConfigManager.GetDefaultSerializerOptions());
            _themes.Add(name.Replace("Themes.", "").Replace(".json", ""), theme);
        }
    }
    
    public void Update(ref GlimpseConfig currentConfig)
    {
        Locale currentLocale = _glimpse.Locale;
        
        string syncToOS = currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme.SyncToOS");
        string dark = currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme.Dark");
        string light = currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme.Light");

        ref PreferredColorScheme scheme = ref currentConfig.Appearance.PreferredColorScheme;
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

        //_lightMode ??= Renderer.CreateImage("asset://Images.LightMode.png");
        //_darkMode ??= Renderer.CreateImage("asset://Images.DarkMode.png");

        ImGuiStylePtr currentStyle = ImGui.GetStyle();
        bool lightMode = shouldSyncToOS
            ? _isSystemThemeLightMode
            : scheme == PreferredColorScheme.Light;
        
        Theme currentTheme = _themes[currentConfig.Appearance.Theme];
        currentTheme.ApplyImGuiStyle(lightMode, currentStyle.Colors);

        if (ImGui.BeginListBox("##ThemesList"))
        {
            foreach ((string name, Theme theme) in _themes.OrderBy(pair => pair.Key))
            {
                if (ImGui.Selectable(theme.Name, currentConfig.Appearance.Theme == name))
                    currentConfig.Appearance.Theme = name;
                
                ImGui.SetItemTooltipUnformatted($"Version: {theme.Version}\nAuthor: {theme.Author}");
                if (ImGui.IsItemHovered())
                    theme.ApplyImGuiStyle(lightMode, currentStyle.Colors);
            }

            ImGui.EndListBox();
        }
    }
    
    public void Dispose()
    {
        
    }
}