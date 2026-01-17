namespace Aevatar.Cli;

public class AevatarCliOptions
{
    public Dictionary<string, Type> Commands { get; }

    public List<string> DisabledModulesToAddToSolution { get; set; }

    /// <summary>
    /// Default value: true.
    /// </summary>
    public bool CacheTemplates { get; set; } = true;

    /// <summary>
    /// Default value: "CLI".
    /// </summary>
    public string ToolName { get; set; } = "CLI";
    
    public bool AlwaysHideExternalCommandOutput { get; set; }

    public AevatarCliOptions()
    {
        Commands = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        DisabledModulesToAddToSolution = [];
    }
}