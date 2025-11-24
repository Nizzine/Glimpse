using Glimpse.API.Codecs;

namespace Glimpse.Audio.Codecs;

public static class CodecUtils
{
    public static MixrSharp.AudioFormat ToMixr(this AudioFormat format)
    {
        MixrSharp.DataType dataType = format.Type switch
        {
            DataType.Byte => MixrSharp.DataType.U8,
            DataType.SByte => MixrSharp.DataType.I8,
            DataType.Short => MixrSharp.DataType.I16,
            DataType.Int => MixrSharp.DataType.I32,
            DataType.Float => MixrSharp.DataType.F32,
            _ => throw new ArgumentOutOfRangeException()
        };

        return new MixrSharp.AudioFormat(dataType, format.SampleRate, format.Channels);
    }

    public static AudioFormat ToGlimpse(this MixrSharp.AudioFormat format)
    {
        DataType dataType = format.DataType switch
        {
            MixrSharp.DataType.I8 => DataType.SByte,
            MixrSharp.DataType.U8 => DataType.Byte,
            MixrSharp.DataType.I16 => DataType.Short,
            MixrSharp.DataType.I32 => DataType.Int,
            MixrSharp.DataType.F32 => DataType.Float,
            _ => throw new ArgumentOutOfRangeException()
        };

        return new AudioFormat(dataType, format.SampleRate, format.Channels);
    }
}