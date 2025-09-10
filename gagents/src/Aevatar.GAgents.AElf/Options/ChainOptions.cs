using System.Collections.Generic;

namespace Aevatar.GAgents.AElf.Options;

public class ChainOptions
{
    public Dictionary<string, string> ChainNodeHosts { get; set; }
    
    // account name => PrivateKey
    public Dictionary<string, string> AccountDictionary { get; set; } = new();
}
