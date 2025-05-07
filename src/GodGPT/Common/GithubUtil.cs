namespace Aevatar.Application.Grains.Agents.ChatManager.Common;

public static class GithubUtil
{
    
    private const string FormalTheoryCoreUrl = "https://raw.githubusercontent.com/loning/universe/trae/formal_theory_core.md";
    
    private const string CoreUrl = "https://raw.githubusercontent.com/loning/universe/trae/core.md";
    
    private const string FormalTheoryCosmicOntologyUrl = "https://raw.githubusercontent.com/loning/universe/refs/heads/cosmos/formal_theory/formal_theory_cosmic_ontology.md";
    
    private const string FormalTheoryTranscendentalHyperintelligenceUrl = "https://raw.githubusercontent.com/loning/universe/refs/heads/cosmos/formal_theory/formal_theory_transcendental_hyperintelligence.md";

    
    /// <summary>
    /// Asynchronously fetches the content of a file from the specified URL.
    /// </summary>
    /// <param name="url">The URL of the target file</param>
    /// <returns>The content of the file as a string</returns>
    private static async Task<string> FetchContentAsync(string url)
    {
        using HttpClient client = new HttpClient();
        // Add a User-Agent header to prevent GitHub from rejecting the request
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        // Make a request to fetch the file content
        string content = await client.GetStringAsync(url);
        return content;
    }

    public static async Task<string> FetchFormalTheoryCoreContentAsync()
    {
        return await FetchContentAsync(FormalTheoryCoreUrl);
    }
    
    public static async Task<string> FetchCoreContentAsync()
    {
        return await FetchContentAsync(CoreUrl);
    }
    
    public static async Task<string> FetchFormalTheoryCosmicOntologyContentAsync()
    {
        return await FetchContentAsync(FormalTheoryCosmicOntologyUrl);
    }
    
    public static async Task<string> FetcHyperintelligenceContentAsync()
    {
        return await FetchContentAsync(FormalTheoryTranscendentalHyperintelligenceUrl);
    }

}