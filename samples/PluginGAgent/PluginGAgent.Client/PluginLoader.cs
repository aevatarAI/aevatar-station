public static class PluginLoader
{
    public static async Task<Dictionary<string, byte[]>> LoadPluginsAsync(string pluginsDirectory)
    {
        var pluginBytes = new Dictionary<string, byte[]>();

        if (Directory.Exists(pluginsDirectory))
        {
            var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                var bytes = await File.ReadAllBytesAsync(dllFile);
                pluginBytes[dllFile] = bytes;
            }
        }

        return pluginBytes;
    }
}