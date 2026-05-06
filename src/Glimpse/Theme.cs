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
        
        colors[(int) ImGuiCol.Text] = UintToVector4(scheme.Text);
        colors[(int) ImGuiCol.WindowBg] = UintToVector4(scheme.MainBackground);
        colors[(int) ImGuiCol.PopupBg] = UintToVector4(scheme.PopupBackground);
        colors[(int) ImGuiCol.FrameBg] = UintToVector4(scheme.Container);
        colors[(int) ImGuiCol.FrameBgHovered] = UintToVector4(scheme.ContainerHovered);
        colors[(int) ImGuiCol.FrameBgActive] = UintToVector4(scheme.ContainerClicked);
        colors[(int) ImGuiCol.TitleBgActive] = UintToVector4(scheme.PopupTitle);
        colors[(int) ImGuiCol.ScrollbarBg] = UintToVector4(scheme.ScrollbarBackground);
        colors[(int) ImGuiCol.ScrollbarGrab] = UintToVector4(scheme.Scrollbar);
        colors[(int) ImGuiCol.ScrollbarGrabHovered] = UintToVector4(scheme.ScrollbarHovered);
        colors[(int) ImGuiCol.ScrollbarGrabActive] = UintToVector4(scheme.ScrollbarClicked);
        colors[(int) ImGuiCol.CheckMark] = UintToVector4(scheme.Checkmark);
        colors[(int) ImGuiCol.SliderGrab] = UintToVector4(scheme.SliderGrip);
        colors[(int) ImGuiCol.SliderGrabActive] = UintToVector4(scheme.SliderGripClicked);
        colors[(int) ImGuiCol.Button] = UintToVector4(scheme.Button);
        colors[(int) ImGuiCol.ButtonHovered] = UintToVector4(scheme.ButtonHovered);
        colors[(int) ImGuiCol.ButtonActive] = UintToVector4(scheme.ButtonClicked);
        colors[(int) ImGuiCol.Header] = UintToVector4(scheme.ListEntrySelected);
        colors[(int) ImGuiCol.HeaderHovered] = UintToVector4(scheme.ListEntryHovered);
        colors[(int) ImGuiCol.HeaderActive] = UintToVector4(scheme.ListEntryClicked);
        colors[(int) ImGuiCol.Separator] = UintToVector4(scheme.Separator);
        colors[(int) ImGuiCol.SeparatorHovered] = UintToVector4(scheme.SeparatorHovered);
        colors[(int) ImGuiCol.SeparatorActive] = UintToVector4(scheme.SeparatorClicked);
        colors[(int) ImGuiCol.TabHovered] = UintToVector4(scheme.TabHovered);
        colors[(int) ImGuiCol.Tab] = UintToVector4(scheme.Tab);
        colors[(int) ImGuiCol.TabSelected] = UintToVector4(scheme.TabActive);
        colors[(int) ImGuiCol.PlotHistogram] = UintToVector4(scheme.SeekBar);
        colors[(int) ImGuiCol.TableHeaderBg] = UintToVector4(scheme.TableHeader);
        colors[(int) ImGuiCol.TextLink] = UintToVector4(scheme.Link);
        colors[(int) ImGuiCol.ModalWindowDimBg] = UintToVector4(scheme.PopupDimBackground);
    }

    public static Theme FromImGuiStyle(string name, bool isLightTheme, Span<Vector4> colors)
    {
        ColorScheme scheme = new ColorScheme();
        scheme.Text = Vector4ToUint(colors[(int) ImGuiCol.Text]);
        scheme.MainBackground = Vector4ToUint(colors[(int) ImGuiCol.WindowBg]);
        scheme.PopupBackground = Vector4ToUint(colors[(int) ImGuiCol.PopupBg]);
        scheme.Container = Vector4ToUint(colors[(int) ImGuiCol.FrameBg]);
        scheme.ContainerHovered = Vector4ToUint(colors[(int) ImGuiCol.FrameBgHovered]);
        scheme.ContainerClicked = Vector4ToUint(colors[(int) ImGuiCol.FrameBgActive]);
        scheme.PopupTitle = Vector4ToUint(colors[(int) ImGuiCol.TitleBgActive]);
        scheme.ScrollbarBackground = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarBg]);
        scheme.Scrollbar = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarGrab]);
        scheme.ScrollbarHovered = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarGrabHovered]);
        scheme.ScrollbarClicked = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarGrabActive]);
        scheme.Checkmark = Vector4ToUint(colors[(int) ImGuiCol.CheckMark]);
        scheme.SliderGrip = Vector4ToUint(colors[(int) ImGuiCol.SliderGrab]);
        scheme.SliderGripClicked = Vector4ToUint(colors[(int) ImGuiCol.SliderGrabActive]);
        scheme.Button = Vector4ToUint(colors[(int) ImGuiCol.Button]);
        scheme.ButtonHovered = Vector4ToUint(colors[(int) ImGuiCol.ButtonHovered]);
        scheme.ButtonClicked = Vector4ToUint(colors[(int) ImGuiCol.ButtonActive]);
        scheme.ListEntrySelected = Vector4ToUint(colors[(int) ImGuiCol.Header]);
        scheme.ListEntryHovered = Vector4ToUint(colors[(int) ImGuiCol.HeaderHovered]);
        scheme.ListEntryClicked = Vector4ToUint(colors[(int) ImGuiCol.HeaderActive]);
        scheme.Separator = Vector4ToUint(colors[(int) ImGuiCol.Separator]);
        scheme.SeparatorHovered = Vector4ToUint(colors[(int) ImGuiCol.SeparatorHovered]);
        scheme.SeparatorClicked = Vector4ToUint(colors[(int) ImGuiCol.SeparatorActive]);
        scheme.TabHovered = Vector4ToUint(colors[(int) ImGuiCol.TabHovered]);
        scheme.Tab = Vector4ToUint(colors[(int) ImGuiCol.Tab]);
        scheme.TabActive = Vector4ToUint(colors[(int) ImGuiCol.TabSelected]);
        scheme.SeekBar = Vector4ToUint(colors[(int) ImGuiCol.PlotHistogram]);
        scheme.TableHeader = Vector4ToUint(colors[(int) ImGuiCol.TableHeaderBg]);
        scheme.Link = Vector4ToUint(colors[(int) ImGuiCol.TextLink]);
        scheme.PopupDimBackground = Vector4ToUint(colors[(int) ImGuiCol.ModalWindowDimBg]);
        
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

    public static Vector4 UintToVector4(uint value)
    {
        byte r = (byte) (value >> 24);
        byte g = (byte) ((value >> 16) & 0xFF);
        byte b = (byte) ((value >> 8) & 0xFF);
        byte a = (byte) (value & 0xFF);

        return new Vector4(r / (float) byte.MaxValue, g / (float) byte.MaxValue, b / (float) byte.MaxValue,
            a / (float) byte.MaxValue);
    }

    public static uint Vector4ToUint(Vector4 value)
    {
        byte r = (byte) (value.X * byte.MaxValue);
        byte g = (byte) (value.Y * byte.MaxValue);
        byte b = (byte) (value.Z * byte.MaxValue);
        byte a = (byte) (value.W * byte.MaxValue);

        return (uint) ((r << 24) | (g << 16) | (b << 8) | a);
    }
    
    public record struct ColorScheme
    {
        /// <summary>
        /// Text color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Text;
        
        /// <summary>
        /// The main Glimpse background color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint MainBackground;
        
        /// <summary>
        /// The background color of popups.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint PopupBackground;
        
        /// <summary>
        /// Container backgrounds, such as text boxes, checkboxes, and sliders.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Container;
        
        /// <summary>
        /// Container backgrounds, such as text boxes, checkboxes, and sliders, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ContainerHovered;
        
        /// <summary>
        /// Container backgrounds, such as text boxes, checkboxes, and sliders, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ContainerClicked;
        
        /// <summary>
        /// Popup title background color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint PopupTitle;
        
        /// <summary>
        /// The background that a scrollbar is contained in.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ScrollbarBackground;
        
        /// <summary>
        /// The scrollbar color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Scrollbar;
        
        /// <summary>
        /// The scrollbar color when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ScrollbarHovered;
        
        /// <summary>
        /// The scrollbar color when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ScrollbarClicked;
        
        /// <summary>
        /// The color of the checkmark in a checkbox.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Checkmark;
        
        /// <summary>
        /// The grip color of a slider.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint SliderGrip;
        
        /// <summary>
        /// The grip color of a slider, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint SliderGripClicked;
        
        /// <summary>
        /// The button color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Button;
        
        /// <summary>
        /// The button color, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ButtonHovered;
        
        /// <summary>
        /// The button color, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ButtonClicked;
        
        /// <summary>
        /// The color of a list &amp;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ListEntrySelected;
        
        /// <summary>
        /// The color of a list &amp;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ListEntryHovered;
        
        /// <summary>
        /// The color of a list &amp;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint ListEntryClicked;
        
        /// <summary>
        /// The table separator color;
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Separator;
        
        /// <summary>
        /// Table separator color, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint SeparatorHovered;
        
        /// <summary>
        /// Table separator color, when clicked by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint SeparatorClicked;
        
        /// <summary>
        /// The tab color, when hovered by the mouse.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint TabHovered;
        
        /// <summary>
        /// The tab color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Tab;
        
        /// <summary>
        /// The tab color, when this current tab is active.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint TabActive;
        
        /// <summary>
        /// The color of the seek bar.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint SeekBar;
        
        /// <summary>
        /// The table header color.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint TableHeader;
        
        /// <summary>
        /// Text links.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint Link;
        
        /// <summary>
        /// The color that the background will be dimmed by when a popup is shown.
        /// </summary>
        [JsonConverter(typeof(HexColorConverter))]
        public uint PopupDimBackground;
    }
}

public sealed class HexColorConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? str = reader.GetString();
        if (str == null || !str.StartsWith('#'))
            throw new JsonException("Expected hex code beginning with '#'");
        return Convert.ToUInt32(str.Trim()[1..], 16);
    }
    
    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"#{value:X8}");
    }
}