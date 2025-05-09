namespace Aevatar.Plugins;

public enum LoadStatus
{
    Unload = -1,
    Success = 0,
    GAgentDuplicated,
    AlreadyLoaded,
    Error,
}