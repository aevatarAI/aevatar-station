using System.Threading.Tasks;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Speech.GAgent;

public interface ISpeechGAgent : IStateGAgent<SpeechGAgentState>
{
    Task<string> SpeechToTextAsync(byte[] audioData);
    Task<byte[]> TextToSpeechAsync(string text);
}