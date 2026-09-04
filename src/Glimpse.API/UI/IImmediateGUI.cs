using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Glimpse.API.UI;

public interface IImmediateGUI
{
    public void Separator();

    public void Separator(string heading);

    public void Text(string text);

    public void Text(string text, uint size);

    public bool Button(string text);

    public bool Button(string text, Size size);

    public bool Checkbox(string text, ref bool ticked);

    public bool Dropdown(string label, ref int value, params ReadOnlySpan<string> items);
}