using System.Text.Json.Serialization;

namespace Aevetar.Developer.Logger.Entities;

public class AppLogInfo
{
    public string Message { get; set; }

    public string LogId { get; set; }
    
    public DateTime Time { get; set; }
    
    public string Level { get; set; }
    public string Exception { get; set; }

    public string SourceContext { get; set; }

    public string HostId { get; set; }

    public string Version { get; set; }

    public string Application { get; set; }

    public string Environment { get; set; }
}