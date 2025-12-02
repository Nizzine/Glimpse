namespace Glimpse.API;

public interface ILocale
{
    public string ID { get; }
    
    public string DisplayName { get; }
    
    public IReadOnlyDictionary<string, string> Strings { get; }

    public string GetString(string str);
    
    public string GetString(string str, params object[] format);
}