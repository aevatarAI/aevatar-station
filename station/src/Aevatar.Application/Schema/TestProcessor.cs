using System.Collections.Generic;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// Test processor to verify ISchemaProcessor mechanism works
/// Adds x-test-processor field to every schema
/// </summary>
public class TestProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        // 强制输出 - 确保能看到
        System.Console.WriteLine("🚀🚀🚀 [TestProcessor] CALLED! 🚀🚀🚀");
        System.Console.Error.WriteLine("🚀🚀🚀 [TestProcessor] CALLED! 🚀🚀🚀");
        
        // Skip null schemas
        if (context.Schema == null)
        {
            System.Console.WriteLine("[TestProcessor] Skipped: Schema is null");
            System.Console.Error.WriteLine("[TestProcessor] Skipped: Schema is null");
            return;
        }

        var schemaName = context.Schema.Title ?? context.ContextualType?.Type?.Name ?? "Unknown";
        System.Console.WriteLine($"🔧 [TestProcessor] Processing schema: {schemaName}");
        System.Console.Error.WriteLine($"🔧 [TestProcessor] Processing schema: {schemaName}");
        
        // Initialize ExtensionData if needed
        if (context.Schema.ExtensionData == null)
        {
            context.Schema.ExtensionData = new Dictionary<string, object>();
            System.Console.WriteLine($"[TestProcessor] Created new ExtensionData for {schemaName}");
        }

        // Add test field to every schema
        context.Schema.ExtensionData["x-test-processor"] = "TestProcessor is working!";
        System.Console.WriteLine($"✅ [TestProcessor] Added x-test-processor to {schemaName}");
        System.Console.Error.WriteLine($"✅ [TestProcessor] Added x-test-processor to {schemaName}");
        
        // Also try to add to properties if it's the main schema
        if (context.Schema.Properties != null && context.Schema.Properties.Count > 0)
        {
            System.Console.WriteLine($"[TestProcessor] Schema {schemaName} has {context.Schema.Properties.Count} properties");
        }
    }
}
