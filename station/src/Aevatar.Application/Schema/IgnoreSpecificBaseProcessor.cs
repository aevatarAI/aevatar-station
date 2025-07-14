using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

public class IgnoreSpecificBaseProcessor : ISchemaProcessor
{
    [Obsolete("Obsolete")]
    public void Process(SchemaProcessorContext context)
    {
    }
}
