using System;
using Microsoft.SemanticKernel;

namespace Aevatar.AI.KernelBuilderFactory;

public interface IKernelBuilderFactory
{
    IKernelBuilder GetKernelBuilder(string id);
}