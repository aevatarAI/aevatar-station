using System.Threading.Tasks;
using Aevatar.Core.Interception.Context;
using Orleans.Runtime;
using Orleans;

namespace Aevatar.Core.Interception.Filters;

/// <summary>or
/// Incoming grain call filter that reads trace context from Orleans RequestContext
/// and sets it in the local TraceContext for method interception.
/// </summary>
public class TraceIncomingGrainCallFilter : IIncomingGrainCallFilter
{

    /// <summary>
    /// Intercepts incoming grain calls to set trace context from RequestContext
    /// </summary>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        // Read context from Orleans RequestContext and set local AsyncLocal
        TraceContext.ReadFromOrleansContext();
        
        await context.Invoke();
        
    }
}
