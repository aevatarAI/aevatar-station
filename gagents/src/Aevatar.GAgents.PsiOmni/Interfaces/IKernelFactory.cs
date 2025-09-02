using Microsoft.SemanticKernel;
using Aevatar.GAgents.PsiOmni.Models;

namespace Aevatar.GAgents.PsiOmni.Interfaces;

public interface IKernelFactory
{
    Kernel CreateKernel(AgentConfiguration configuration, IEnumerable<string>? toolNames = null);
    IKernelFunctionRegistry? FunctionRegistry { get; }
}