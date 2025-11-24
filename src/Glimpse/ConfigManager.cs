using Glimpse.API;
using Newtonsoft.Json;

namespace Glimpse;

public class ConfigManager : IConfigManager
{
    private readonly Logger _logger;
    
    public ConfigManager(Logger logger)
    {
        _logger = logger;
    }
    
    public string BaseDir
    {
        get
        {
#if DEBUG
            return "Config";
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Glimpse");
#endif
        }
    }

    public bool TryGetConfig<T>(string name, out T config) where T : IConfig
    {
        string fullPath = Path.Combine(BaseDir, $"{name}.json");
        _logger.Log($"Trying to load config {fullPath}.");

        if (!File.Exists(fullPath))
        {
            _logger.Log("    ... failed.");
            config = default;
            return false;
        }

        string json = File.ReadAllText(fullPath);

        config = JsonConvert.DeserializeObject<T>(json);
        
        _logger.Log("    ... loaded.");

        return config != null;
    }

    public void WriteConfig(string name, IConfig config)
    {
        string fullPath = Path.Combine(BaseDir, $"{name}.json");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        File.WriteAllText(fullPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}