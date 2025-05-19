namespace Aevatar.Plugins;

/// <summary>
/// Used to load plugins from a directory.
/// Keep this helper for testing.
/// </summary>
public static class PluginLoader
{
    public static List<byte[]> LoadPlugins(string pluginsDirectory)
    {
        var pluginCodeList = new List<byte[]>();

        if (Directory.Exists(pluginsDirectory))
        {
            var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                var bytes = File.ReadAllBytes(dllFile);
                pluginCodeList.Add(bytes);
            }
        }

        return pluginCodeList;
    }
}