using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;
using Aevatar.Core.Interception.Context;

namespace Aevatar.Core.Interception.Filters;

/// <summary>
/// Outgoing grain call filter that propagates trace context from the current 
/// AsyncLocal context to Orleans RequestContext for the target grain.
/// </summary>
public class TraceOutgoingGrainCallFilter : IOutgoingGrainCallFilter
{
    /// <summary>
    /// Invokes the filter, propagating trace context to Orleans RequestContext.
    /// </summary>
    /// <param name="context">The outgoing grain call context.</param>
    /// <returns>A task representing the filter work.</returns>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        // Propagate current thread context to Orleans RequestContext
        TraceContext.PropagateToOrleansContext();
        
        // Orleans handles returning the result to the caller automatically
        // The filter just needs to call context.Invoke() and let it pass through
        await context.Invoke();
    }
}
