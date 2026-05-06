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

    /// <summary>
    /// Gets all asset names in a certain folder.
    /// </summary>
    /// <param name="folder">The folder to get the names from.</param>
    /// <returns>A list of asset names.</returns>
    /// <remarks>This is horribly inefficient. Call this sparingly.</remarks>
    public static IEnumerable<string> GetAllNamesInFolder(string folder)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames().Where(s => s.StartsWith(folder));
    }
}