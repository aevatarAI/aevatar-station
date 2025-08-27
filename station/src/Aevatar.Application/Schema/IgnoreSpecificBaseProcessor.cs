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
        context.Schema.AllOf.Clear();
        
        foreach (var prop in context.Schema.Properties.ToList())
        {
            var propertyInfo = context.Type.GetProperty(prop.Key);
            if (propertyInfo?.DeclaringType != context.Type)
            {
                context.Schema.Properties.Remove(prop.Key);
                context.Schema.Definitions.Remove(prop.Key);
            }
        }

        context.Schema.Definitions.RemoveAll(f => !context.Schema.Properties.Keys.Contains(f.Key));
    }
}
