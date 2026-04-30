using System.Reflection;

namespace Glimpse.Assets;

public static class Asset
{
    public static Stream GetAssetStream(string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream? stream = assembly.GetManifestResourceStream($"Glimpse.Assets.{name}");

        if (stream == null)
            throw new Exception("Stream was null.");

        return stream;
    }
}