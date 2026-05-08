using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hexa.NET.ImGui;

namespace Glimpse;

public record struct Theme
{
    public const string DefaultTheme = "Glimpse";
    
    public string Name;

    public ColorScheme? DarkColors;

    public ColorScheme? LightColors;

    public void ApplyImGuiStyle(bool useLightThemeIfPossible, Span<Vector4> colors)
    {
        if (DarkColors == null && LightColors == null)
            throw new InvalidOperationException("The theme does not have any valid colors!");

        ColorScheme scheme;
        
        // Apply the light theme if it is selected and available.
        // We also apply the light theme if there is no dark color scheme available.
        // Otherwise, since both cannot be null, apply the dark scheme.
        if (useLightThemeIfPossible && LightColors is ColorScheme lightColorScheme)
            scheme = lightColorScheme;
        else if (DarkColors == null)
            scheme = LightColors!.Value;
        else
            scheme = DarkColors.Value;
        
        colors[(int) ImGuiCol.Text] = scheme.Text;
        colors[(int) ImGuiCol.WindowBg] = scheme.MainBackground;
        colors[(int) ImGuiCol.PopupBg] = scheme.PopupBackground;
        colors[(int) ImGuiCol.FrameBg] = scheme.Container;
        colors[(int) ImGuiCol.FrameBgHovered] = scheme.ContainerHovered;
        colors[(int) ImGuiCol.FrameBgActive] = scheme.ContainerClicked;
        colors[(int) ImGuiCol.TitleBgActive] = scheme.PopupTitle;
        colors[(int) ImGuiCol.ScrollbarBg] = scheme.ScrollbarBackground;
        colors[(int) ImGuiCol.ScrollbarGrab] = scheme.Scrollbar;
        colors[(int) ImGuiCol.ScrollbarGrabHovered] = scheme.ScrollbarHovered;
        colors[(int) ImGuiCol.ScrollbarGrabActive] = scheme.ScrollbarClicked;
        colors[(int) ImGuiCol.CheckMark] = scheme.Checkmark;
        colors[(int) ImGuiCol.SliderGrab] = scheme.SliderGrip;
        colors[(int) ImGuiCol.SliderGrabActive] = scheme.SliderGripClicked;
        colors[(int) ImGuiCol.Button] = scheme.Button;
        colors[(int) ImGuiCol.ButtonHovered] = scheme.ButtonHovered;
        colors[(int) ImGuiCol.ButtonActive] = scheme.ButtonClicked;
        colors[(int) ImGuiCol.Header] = scheme.ListEntrySelected;
        colors[(int) ImGuiCol.HeaderHovered] = scheme.ListEntryHovered;
        colors[(int) ImGuiCol.HeaderActive] = scheme.ListEntryClicked;
        colors[(int) ImGuiCol.Separator] = scheme.Separator;
        colors[(int) ImGuiCol.SeparatorHovered] = scheme.SeparatorHovered;
        colors[(int) ImGuiCol.SeparatorActive] = scheme.SeparatorClicked;
        colors[(int) ImGuiCol.TabHovered] = scheme.TabHovered;
        colors[(int) ImGuiCol.Tab] = scheme.Tab;
        colors[(int) ImGuiCol.TabSelected] = scheme.TabActive;
        colors[(int) ImGuiCol.PlotHistogram] = scheme.SeekBar;
        colors[(int) ImGuiCol.TableHeaderBg] = scheme.TableHeader;
        colors[(int) ImGuiCol.TextLink] = scheme.Link;
        colors[(int) ImGuiCol.ModalWindowDimBg] = scheme.PopupDimBackground;
    }

    public static Theme FromImGuiStyle(string name, bool isLightTheme, Span<Vector4> colors)
    {
        ColorScheme scheme = new ColorScheme();
        scheme.Text = colors[(int) ImGuiCol.Text];
        scheme.MainBackground = colors[(int) ImGuiCol.WindowBg];
        scheme.PopupBackground = colors[(int) ImGuiCol.PopupBg];
        scheme.Container = colors[(int) ImGuiCol.FrameBg];
        scheme.ContainerHovered = colors[(int) ImGuiCol.FrameBgHovered];
        scheme.ContainerClicked = colors[(int) ImGuiCol.FrameBgActive];
        scheme.PopupTitle = colors[(int) ImGuiCol.TitleBgActive];
        scheme.ScrollbarBackground = colors[(int) ImGuiCol.ScrollbarBg];
        scheme.Scrollbar = colors[(int) ImGuiCol.ScrollbarGrab];
        scheme.ScrollbarHovered = colors[(int) ImGuiCol.ScrollbarGrabHovered];
        scheme.ScrollbarClicked = colors[(int) ImGuiCol.ScrollbarGrabActive];
        scheme.Checkmark = colors[(int) ImGuiCol.CheckMark];
        scheme.SliderGrip = colors[(int) ImGuiCol.SliderGrab];
        scheme.SliderGripClicked = colors[(int) ImGuiCol.SliderGrabActive];
        scheme.Button = colors[(int) ImGuiCol.Button];
        scheme.ButtonHovered = colors[(int) ImGuiCol.ButtonHovered];
        scheme.ButtonClicked = colors[(int) ImGuiCol.ButtonActive];
        scheme.ListEntrySelected = colors[(int) ImGuiCol.Header];
        scheme.ListEntryHovered = colors[(int) ImGuiCol.HeaderHovered];
        scheme.ListEntryClicked = colors[(int) ImGuiCol.HeaderActive];
        scheme.Separator = colors[(int) ImGuiCol.Separator];
        scheme.SeparatorHovered = colors[(int) ImGuiCol.SeparatorHovered];
        scheme.SeparatorClicked = colors[(int) ImGuiCol.SeparatorActive];
        scheme.TabHovered = colors[(int) ImGuiCol.TabHovered];
        scheme.Tab = colors[(int) ImGuiCol.Tab];
        scheme.TabActive = colors[(int) ImGuiCol.TabSelected];
        scheme.SeekBar = colors[(int) ImGuiCol.PlotHistogram];
        scheme.TableHeader = colors[(int) ImGuiCol.TableHeaderBg];
        scheme.Link = colors[(int) ImGuiCol.TextLink];
        scheme.PopupDimBackground = colors[(int) ImGuiCol.ModalWindowDimBg];
        
        Theme theme = new()
        {
            Name = name
        };

        if (isLightTheme)
            theme.LightColors = scheme;
        else
            theme.DarkColors = scheme;

        return theme;
    }
    
    public record struct ColorScheme
    {
        /// <summary>
        /// Text color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Text;
        
        /// <summary>
        /// The main Glimpse background color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 MainBackground;
        
        /// <summary>
        /// The background color of popups.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 PopupBackground;
        
        /// <summary>
        /// Container backgrounds, such as text boxes, checkboxes, and sliders.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Container;
        
        /// <summary>
        /// Container backgrounds, such as text boxes, checkboxes, and sliders, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ContainerHovered;
        
        /// <summary>
        /// Container backgrounds, such as text boxes, checkboxes, and sliders, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ContainerClicked;
        
        /// <summary>
        /// Popup title background color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 PopupTitle;
        
        /// <summary>
        /// The background that a scrollbar is contained in.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ScrollbarBackground;
        
        /// <summary>
        /// The scrollbar color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Scrollbar;
        
        /// <summary>
        /// The scrollbar color when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ScrollbarHovered;
        
        /// <summary>
        /// The scrollbar color when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ScrollbarClicked;
        
        /// <summary>
        /// The color of the checkmark in a checkbox.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Checkmark;
        
        /// <summary>
        /// The grip color of a slider.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 SliderGrip;
        
        /// <summary>
        /// The grip color of a slider, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 SliderGripClicked;
        
        /// <summary>
        /// The button color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Button;
        
        /// <summary>
        /// The button color, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ButtonHovered;
        
        /// <summary>
        /// The button color, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ButtonClicked;
        
        /// <summary>
        /// The color of a list &amp;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ListEntrySelected;
        
        /// <summary>
        /// The color of a list &amp;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ListEntryHovered;
        
        /// <summary>
        /// The color of a list &amp;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 ListEntryClicked;
        
        /// <summary>
        /// The table separator color;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Separator;
        
        /// <summary>
        /// Table separator color, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 SeparatorHovered;
        
        /// <summary>
        /// Table separator color, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 SeparatorClicked;
        
        /// <summary>
        /// The tab color, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 TabHovered;
        
        /// <summary>
        /// The tab color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Tab;
        
        /// <summary>
        /// The tab color, when this current tab is active.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 TabActive;
        
        /// <summary>
        /// The color of the seek bar.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 SeekBar;
        
        /// <summary>
        /// The table header color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 TableHeader;
        
        /// <summary>
        /// Text links.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 Link;
        
        /// <summary>
        /// The color that the background will be dimmed by when a popup is shown.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public Vector4 PopupDimBackground;
    }
}

public sealed class HexColorConverter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? str = reader.GetString();
        if (str == null || !str.StartsWith('#'))
            throw new JsonException("Expected hex code beginning with '#'");
        // ImGUI expects the color code to be AGBR format, however the color code returned here is RGBA.
        uint colorCode = SwapEndianness(Convert.ToUInt32(str.Trim()[1..], 16));
        return ImGui.ColorConvertU32ToFloat4(colorCode);
    }
    
    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        // As above, the color code in the json file is RGBA, however ImGUI returns the color code in ABGR format.
        uint colorCode = SwapEndianness(ImGui.ColorConvertFloat4ToU32(value));
        writer.WriteStringValue($"#{colorCode:X8}");
    }

    private static uint SwapEndianness(uint value)
    {
        return ((value & 0xFF) << 24) | (((value >> 8) & 0xFF) << 16) | (((value >> 16) & 0xFF) << 8) | (value >> 24);
    }
}