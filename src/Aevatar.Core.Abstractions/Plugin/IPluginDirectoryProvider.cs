namespace Aevatar.Core.Abstractions.Plugin;

public interface IPluginDirectoryProvider
{
    string GetDirectory();
}

public class DefaultPluginDirectoryProvider : IPluginDirectoryProvider
{
    public string GetDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    }
}