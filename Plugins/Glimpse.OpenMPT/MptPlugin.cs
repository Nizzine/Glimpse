using Glimpse.Player;
using Glimpse.Player.Configs;
using Glimpse.Player.Plugins;

namespace Glimpse.OpenMPT;

public class MptPlugin : Plugin
{
    private AudioPlayer _player;
    private bool _initialized;

    private MptCodec _codec;
    
    public MptConfig Config;

    public override bool IsInitialized => _initialized;
    
    public override string Name => "OpenMPT Integration";

    public override void Initialize(AudioPlayer player)
    {
        _player = player;
        
        if (!IConfig.TryGetConfig("MPT", out MptConfig Config))
        {
            Config = new MptConfig();
            IConfig.WriteConfig("MPT", Config);
        }

        _codec = new MptCodec(Config);
        _player.Codecs.Add(_codec);

        _initialized = true;
    }

    public override void Dispose()
    {
        if (!_initialized)
            return;
        _initialized = false;
        
        _player.Codecs.Remove(_codec);
    }
}