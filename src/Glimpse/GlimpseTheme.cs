using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hexa.NET.ImGui;

namespace Glimpse;

public struct GlimpseTheme
{
    public string Name;

    public ThemeColors Colors;
    
    public struct ThemeColors
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

    public void ApplyImGuiStyle(Span<Vector4> colors)
    {
        colors[(int) ImGuiCol.Text] = UintToVector4(Colors.Text);
        colors[(int) ImGuiCol.WindowBg] = UintToVector4(Colors.MainBackground);
        colors[(int) ImGuiCol.PopupBg] = UintToVector4(Colors.PopupBackground);
        colors[(int) ImGuiCol.FrameBg] = UintToVector4(Colors.Container);
        colors[(int) ImGuiCol.FrameBgHovered] = UintToVector4(Colors.ContainerHovered);
        colors[(int) ImGuiCol.FrameBgActive] = UintToVector4(Colors.ContainerClicked);
        colors[(int) ImGuiCol.TitleBgActive] = UintToVector4(Colors.PopupTitle);
        colors[(int) ImGuiCol.ScrollbarBg] = UintToVector4(Colors.ScrollbarBackground);
        colors[(int) ImGuiCol.ScrollbarGrab] = UintToVector4(Colors.Scrollbar);
        colors[(int) ImGuiCol.ScrollbarGrabHovered] = UintToVector4(Colors.ScrollbarHovered);
        colors[(int) ImGuiCol.ScrollbarGrabActive] = UintToVector4(Colors.ScrollbarClicked);
        colors[(int) ImGuiCol.CheckMark] = UintToVector4(Colors.Checkmark);
        colors[(int) ImGuiCol.SliderGrab] = UintToVector4(Colors.SliderGrip);
        colors[(int) ImGuiCol.SliderGrabActive] = UintToVector4(Colors.SliderGripClicked);
        colors[(int) ImGuiCol.Button] = UintToVector4(Colors.Button);
        colors[(int) ImGuiCol.ButtonHovered] = UintToVector4(Colors.ButtonHovered);
        colors[(int) ImGuiCol.ButtonActive] = UintToVector4(Colors.ButtonClicked);
        colors[(int) ImGuiCol.Header] = UintToVector4(Colors.ListEntrySelected);
        colors[(int) ImGuiCol.HeaderHovered] = UintToVector4(Colors.ListEntryHovered);
        colors[(int) ImGuiCol.HeaderActive] = UintToVector4(Colors.ListEntryClicked);
        colors[(int) ImGuiCol.Separator] = UintToVector4(Colors.Separator);
        colors[(int) ImGuiCol.SeparatorHovered] = UintToVector4(Colors.SeparatorHovered);
        colors[(int) ImGuiCol.SeparatorActive] = UintToVector4(Colors.SeparatorClicked);
        colors[(int) ImGuiCol.TabHovered] = UintToVector4(Colors.TabHovered);
        colors[(int) ImGuiCol.Tab] = UintToVector4(Colors.Tab);
        colors[(int) ImGuiCol.TabSelected] = UintToVector4(Colors.TabActive);
        colors[(int) ImGuiCol.PlotHistogram] = UintToVector4(Colors.SeekBar);
        colors[(int) ImGuiCol.TableHeaderBg] = UintToVector4(Colors.TableHeader);
        colors[(int) ImGuiCol.TextLink] = UintToVector4(Colors.Link);
        colors[(int) ImGuiCol.ModalWindowDimBg] = UintToVector4(Colors.PopupDimBackground);
    }

    public static GlimpseTheme FromImGuiStyle(string name, Span<Vector4> colors)
    {
        GlimpseTheme theme = new()
        {
            Name = name
        };
        
        theme.Colors.Text = Vector4ToUint(colors[(int) ImGuiCol.Text]);
        theme.Colors.MainBackground = Vector4ToUint(colors[(int) ImGuiCol.WindowBg]);
        theme.Colors.PopupBackground = Vector4ToUint(colors[(int) ImGuiCol.PopupBg]);
        theme.Colors.Container = Vector4ToUint(colors[(int) ImGuiCol.FrameBg]);
        theme.Colors.ContainerHovered = Vector4ToUint(colors[(int) ImGuiCol.FrameBgHovered]);
        theme.Colors.ContainerClicked = Vector4ToUint(colors[(int) ImGuiCol.FrameBgActive]);
        theme.Colors.PopupTitle = Vector4ToUint(colors[(int) ImGuiCol.TitleBgActive]);
        theme.Colors.ScrollbarBackground = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarBg]);
        theme.Colors.Scrollbar = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarGrab]);
        theme.Colors.ScrollbarHovered = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarGrabHovered]);
        theme.Colors.ScrollbarClicked = Vector4ToUint(colors[(int) ImGuiCol.ScrollbarGrabActive]);
        theme.Colors.Checkmark = Vector4ToUint(colors[(int) ImGuiCol.CheckMark]);
        theme.Colors.SliderGrip = Vector4ToUint(colors[(int) ImGuiCol.SliderGrab]);
        theme.Colors.SliderGripClicked = Vector4ToUint(colors[(int) ImGuiCol.SliderGrabActive]);
        theme.Colors.Button = Vector4ToUint(colors[(int) ImGuiCol.Button]);
        theme.Colors.ButtonHovered = Vector4ToUint(colors[(int) ImGuiCol.ButtonHovered]);
        theme.Colors.ButtonClicked = Vector4ToUint(colors[(int) ImGuiCol.ButtonActive]);
        theme.Colors.ListEntrySelected = Vector4ToUint(colors[(int) ImGuiCol.Header]);
        theme.Colors.ListEntryHovered = Vector4ToUint(colors[(int) ImGuiCol.HeaderHovered]);
        theme.Colors.ListEntryClicked = Vector4ToUint(colors[(int) ImGuiCol.HeaderActive]);
        theme.Colors.Separator = Vector4ToUint(colors[(int) ImGuiCol.Separator]);
        theme.Colors.SeparatorHovered = Vector4ToUint(colors[(int) ImGuiCol.SeparatorHovered]);
        theme.Colors.SeparatorClicked = Vector4ToUint(colors[(int) ImGuiCol.SeparatorActive]);
        theme.Colors.TabHovered = Vector4ToUint(colors[(int) ImGuiCol.TabHovered]);
        theme.Colors.Tab = Vector4ToUint(colors[(int) ImGuiCol.Tab]);
        theme.Colors.TabActive = Vector4ToUint(colors[(int) ImGuiCol.TabSelected]);
        theme.Colors.SeekBar = Vector4ToUint(colors[(int) ImGuiCol.PlotHistogram]);
        theme.Colors.TableHeader = Vector4ToUint(colors[(int) ImGuiCol.TableHeaderBg]);
        theme.Colors.Link = Vector4ToUint(colors[(int) ImGuiCol.TextLink]);
        theme.Colors.PopupDimBackground = Vector4ToUint(colors[(int) ImGuiCol.ModalWindowDimBg]);

        return theme;
    }

    private static Vector4 UintToVector4(uint value)
    {
        byte r = (byte) (value >> 24);
        byte g = (byte) ((value >> 16) & 0xFF);
        byte b = (byte) ((value >> 8) & 0xFF);
        byte a = (byte) (value & 0xFF);

        return new Vector4(r / (float) byte.MaxValue, g / (float) byte.MaxValue, b / (float) byte.MaxValue,
            a / (float) byte.MaxValue);
    }

    private static uint Vector4ToUint(Vector4 value)
    {
        byte r = (byte) (value.X * byte.MaxValue);
        byte g = (byte) (value.Y * byte.MaxValue);
        byte b = (byte) (value.Z * byte.MaxValue);
        byte a = (byte) (value.W * byte.MaxValue);

        return (uint) ((r << 24) | (g << 16) | (b << 8) | a);
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