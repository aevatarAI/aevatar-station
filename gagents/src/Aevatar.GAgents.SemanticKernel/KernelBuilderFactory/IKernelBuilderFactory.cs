using System;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;

public interface IKernelBuilderFactory
{
    IKernelBuilder GetKernelBuilder(string id);
}