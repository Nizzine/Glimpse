using System.Text.Json;
using Glimpse.Assets;
using Glimpse.Configs;
using Hexa.NET.ImGui;
using piko.SDL3;

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

        //_lightMode ??= Renderer.CreateImage("asset://Images.LightMode.png");
        //_darkMode ??= Renderer.CreateImage("asset://Images.DarkMode.png");

        ImGuiStylePtr currentStyle = ImGui.GetStyle();
        
        Theme currentTheme = _themes[currentConfig.Appearance.Theme];
        currentTheme.ApplyImGuiStyle(currentStyle.Colors);

        if (ImGui.BeginListBox("##ThemesList"))
        {
            foreach ((string name, Theme theme) in _themes.OrderBy(pair => pair.Key))
            {
                if (ImGui.Selectable(theme.Name, currentConfig.Appearance.Theme == name))
                    currentConfig.Appearance.Theme = name;

                // TODO: Change this to include a description and other stuff.
                ImGui.SetItemTooltipUnformatted($"Version: {theme.Version}\nAuthor: {theme.Author}");
                if (ImGui.IsItemHovered())
                    theme.ApplyImGuiStyle(currentStyle.Colors);
            }

            ImGui.EndListBox();
        }
    }
    
    public void Dispose()
    {
        
    }
}