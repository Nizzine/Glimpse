using Glimpse.API;
using Newtonsoft.Json;

namespace Glimpse.Locales;

public class Locale : ILocale
{
    public string ID { get; }
    
    public string DisplayName { get; }

    public readonly Dictionary<string, string> Strings;

    public Locale(string id, string displayName)
    {
        ID = id;
        DisplayName = displayName;
        Strings = [];
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

    public static Locale LoadLocale(string path)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, "Locales", path);
        string json = File.ReadAllText(fullPath);
        return JsonConvert.DeserializeObject<Locale>(json);
    }

    IReadOnlyDictionary<string, string> ILocale.Strings => Strings;
}