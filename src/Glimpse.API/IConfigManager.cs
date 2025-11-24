namespace Glimpse.API;

public interface IConfigManager
{
    public string BaseDir { get; }

    public bool TryGetConfig<T>(string name, out T config) where T : IConfig;
    
    public void WriteConfig(string name, IConfig config);
}