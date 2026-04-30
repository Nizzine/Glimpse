using System.Text.Json;
using System.Text.Json.Serialization;
using Glimpse.API;

namespace Glimpse.Locales;

[JsonSerializable(typeof(Locale))]
public class Locale : ILocale
{
    public string ID { get; }
    
    public string DisplayName { get; }

    public readonly Dictionary<string, string> Strings;
    
    public Locale(string id, string displayName, Dictionary<string, string> strings)
    {
        ID = id;
        DisplayName = displayName;
        Strings = strings;
    }

    public string GetString(string str)
    {
        if (!Strings.TryGetValue(str, out string localeString))
            return str;

        return localeString;
    }

    public string GetString(string str, params object[] format)
    {
        if (!Strings.TryGetValue(str, out string localeString))
            return str;
            
        return string.Format(localeString, format);
    }

    public static readonly Dictionary<string, (string path, string name)> AvailableLocales;

    static Locale()
    {
        AvailableLocales = [];
    }

    public static Locale LoadLocale(string id)
    {
        string json = File.ReadAllText(AvailableLocales[id].path);
        return JsonSerializer.Deserialize<Locale>(json, ConfigManager.GetDefaultSerializerOptions());
    }

    public static void LoadAvailableLocales()
    {
        // TODO: There must be a better way to do this. This feels very inefficient.
        //       I'd normally just load the entire locale in at once, but that feels even more inefficient...
        foreach (string path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Locales"), "*.json"))
        {
            Locale locale = JsonSerializer.Deserialize<Locale>(File.ReadAllText(path), ConfigManager.GetDefaultSerializerOptions());
            AvailableLocales.Add(locale.ID, (path, locale.DisplayName));
        }
    }

    IReadOnlyDictionary<string, string> ILocale.Strings => Strings;
}