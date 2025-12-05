namespace Glimpse.API;

public interface IGlimpse
{
    public SemVer Version { get; }
    
    public ILogger Logger { get; }
    
    public IConfigManager ConfigManager { get; }
    
    public IAudioPlayer Player { get; }
    
    public ILocale? Locale { get; }
}