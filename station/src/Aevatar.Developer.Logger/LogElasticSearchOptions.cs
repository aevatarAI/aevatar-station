namespace Aevatar.Developer.Logger;

public class LogElasticSearchOptions
{
    public List<string> Uris { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public LogIndexILMPolicy ILMPolicy { get; set; } 
}

public class LogIndexILMPolicy
{
    public string HotMaxAge { get; set; } = "1d";
    public string HotMaxSize { get; set; } = "50G";
    public string ColdMinAge { get; set; } = "1d";
    public string DeleteMinAge { get; set; } = "7d";
}