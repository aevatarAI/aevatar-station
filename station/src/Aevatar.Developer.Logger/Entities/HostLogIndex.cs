using Aevetar.Developer.Logger.Entities;

namespace Aevatar.Developer.Logger.Entities;

public class HostLogIndex
{
    public DateTime Timestamp { get; set; }

    public AppLogInfo App_log { get; set; }
}