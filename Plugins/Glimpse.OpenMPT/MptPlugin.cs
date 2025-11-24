using Glimpse.API;

namespace Glimpse.OpenMPT;

public class MptPlugin : IPlugin
{
    private IAudioPlayer _player;
    private bool _initialized;

    private MptCodec _codec;
    
    public MptConfig Config;

    public bool IsInitialized => _initialized;
    
    public string Name => "OpenMPT Integration";

    public void Initialize(IGlimpse glimpse)
    {
        _player = glimpse.Player;
        
        if (!glimpse.ConfigManager.TryGetConfig("MPT", out MptConfig Config))
        {
            Config = new MptConfig();
            glimpse.ConfigManager.WriteConfig("MPT", Config);
        }

        _codec = new MptCodec(Config);
        _player.RegisterCodec(_codec);

        _initialized = true;
    }

    public void Dispose()
    {
        if (!_initialized)
            return;
        _initialized = false;
        
        _player.DeregisterCodec(_codec);
    }
}