using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;

namespace Aevatar.Application.Grains;

public class GodGPTSerializationBinder : DefaultSerializationBinder
{
    private static readonly HashSet<string> TypesToReplace = new HashSet<string>
    {
        "Aevatar.Application.Grains.Agents.ChatManager.ChatManagerGAgentState",
        "Aevatar.Application.Grains.Agents.ChatManager.SessionInfo",
        "Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent.ConfigurationState",
        "Aevatar.Application.Grains.Agents.ChatManager.Chat.GodChatState",
        "Aevatar.Application.Grains.Agents.ChatManager.ProxyAgent.AIAgentStatusProxyState"
    };
    
    private const string OldAssemblyName = "Aevatar.Application.Grains";
    private const string NewAssemblyName = "GodGPT.GAgents";

    public override Type BindToType(string assemblyName, string typeName)
    {
        if (assemblyName == OldAssemblyName && TypesToReplace.Contains(typeName))
        {
            assemblyName = NewAssemblyName;
        }
        else
        {
            if (TypesToReplace.Any(typeToReplace => typeName.Contains(typeToReplace)))
            {
                typeName = ReplaceAssemblyName(typeName, OldAssemblyName, NewAssemblyName);
            }
        }

        return base.BindToType(assemblyName, typeName);
    }

    public static string ReplaceAssemblyName(string input, string oldAssemblyName, string newAssemblyName)
    {
        var pattern = $@",\s*{Regex.Escape(oldAssemblyName)}";
        return Regex.Replace(input, pattern, $", {newAssemblyName}");
    }
}