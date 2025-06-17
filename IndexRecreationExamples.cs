using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.Extensions.Logging;

namespace Aevatar.Examples;

/// <summary>
/// Examples of Index Recreation Issues and Index Mapping Conflicts
/// Based on the Aevatar project's Elasticsearch implementation
/// </summary>
public class IndexRecreationExamples
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<IndexRecreationExamples> _logger;

    public IndexRecreationExamples(ElasticsearchClient client, ILogger<IndexRecreationExamples> logger)
    {
        _client = client;
        _logger = logger;
    }

    #region INDEX RECREATION ISSUES

    /// <summary>
    /// Example 1: Problematic Index Recreation - Current Implementation Issue
    /// Based on ElasticIndexingService.CheckExistOrCreateStateIndex method
    /// </summary>
    public async Task<string> DemonstrateIndexRecreationIssue()
    {
        var issues = new List<string>();
        var indexName = "user-state-index";

        try
        {
            // Issue 1: Create index with initial schema
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Properties<UserStateV1>(p => p
                        .Text(f => f.Name)
                        .LongNumber(f => f.Age)
                    )
                )
            );

            // Insert some data
            await _client.IndexAsync(new UserStateV1 { Name = "John", Age = 25 }, 
                i => i.Index(indexName).Id("1"));

            // Issue 2: Application restarts with modified state class
            // The current CheckExistOrCreateStateIndex only checks if index exists
            // It doesn't validate if the mapping matches the current state structure
            
            var exists = await _client.Indices.ExistsAsync(indexName);
            if (exists.Exists)
            {
                issues.Add("Index exists but schema validation is skipped");
                
                // Attempt to index new structure - this will cause issues
                try
                {
                    await _client.IndexAsync(new UserStateV2 
                    { 
                        Name = "Jane", 
                        Age = "twenty-five", // Now string instead of number
                        Email = "jane@example.com" // New field
                    }, i => i.Index(indexName).Id("2"));
                }
                catch (Exception ex)
                {
                    issues.Add($"Data insertion failed due to type mismatch: {ex.Message}");
                }
            }

            return string.Join("; ", issues);
        }
        catch (Exception ex)
        {
            return $"Index recreation demonstration failed: {ex.Message}";
        }
        finally
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    /// <summary>
    /// Example 2: Schema Evolution Problem
    /// Shows what happens when state classes evolve over time
    /// </summary>
    public async Task<Dictionary<string, object>> DemonstrateSchemaEvolutionIssues()
    {
        var results = new Dictionary<string, object>();
        var indexName = "product-evolution-index";

        try
        {
            // Original schema
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Properties<ProductStateV1>(p => p
                        .Text(f => f.ProductId)
                        .DoubleNumber(f => f.Price)
                        .Text(f => f.Category)
                    )
                )
            );

            // Insert original data
            await _client.IndexAsync(new ProductStateV1
            {
                ProductId = "PROD-001",
                Price = 99.99,
                Category = "Electronics"
            }, i => i.Index(indexName).Id("1"));

            results["OriginalDataInserted"] = "Success";

            // Evolution 1: Change ProductId to GUID (should be keyword)
            try
            {
                await _client.IndexAsync(new
                {
                    ProductId = Guid.NewGuid().ToString(),
                    Price = 149.99,
                    Category = "Electronics"
                }, i => i.Index(indexName).Id("2"));
                
                results["GuidProductIdInserted"] = "Success - but not optimized for exact matching";
            }
            catch (Exception ex)
            {
                results["GuidProductIdError"] = ex.Message;
            }

            // Evolution 2: Add new fields
            try
            {
                await _client.IndexAsync(new
                {
                    ProductId = "PROD-003",
                    Price = 199.99,
                    Category = "Electronics",
                    Specifications = new { Weight = "1.5kg", Color = "Black" },
                    Tags = new[] { "gaming", "premium" }
                }, i => i.Index(indexName).Id("3"));
                
                results["NewFieldsAdded"] = "Success - dynamic mapping creates inconsistent types";
            }
            catch (Exception ex)
            {
                results["NewFieldsError"] = ex.Message;
            }

            // Evolution 3: Change price to integer cents
            try
            {
                await _client.IndexAsync(new
                {
                    ProductId = "PROD-004",
                    Price = 29999, // Price in cents
                    Category = "Electronics"
                }, i => i.Index(indexName).Id("4"));
                
                results["PriceAsIntInserted"] = "Success - but semantic meaning lost";
            }
            catch (Exception ex)
            {
                results["PriceAsIntError"] = ex.Message;
            }

            return results;
        }
        catch (Exception ex)
        {
            results["Error"] = ex.Message;
            return results;
        }
        finally
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    #endregion

    #region INDEX MAPPING CONFLICTS

    /// <summary>
    /// Example 3: Type Mapping Conflicts
    /// Based on the CreateIndexAsync method's simple type mapping
    /// </summary>
    public async Task<List<string>> DemonstrateTypeMappingConflicts()
    {
        var conflicts = new List<string>();
        var indexName = "type-conflict-index";

        try
        {
            // Create index with specific field types
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Properties<StateWithMixedTypes>(p => p
                        .Text(f => f.Id)           // Initially text
                        .LongNumber(f => f.Count)  // Initially long
                        .Boolean(f => f.IsActive)  // Initially boolean
                        .Date(f => f.Timestamp)    // Initially date
                    )
                )
            );

            // Insert valid data
            await _client.IndexAsync(new StateWithMixedTypes
            {
                Id = "STATE-001",
                Count = 100,
                IsActive = true,
                Timestamp = DateTime.Now
            }, i => i.Index(indexName).Id("1"));

            // Conflict 1: Try to insert number as text field
            try
            {
                await _client.IndexAsync(new
                {
                    Id = 12345, // Number to text field
                    Count = 200,
                    IsActive = true,
                    Timestamp = DateTime.Now
                }, i => i.Index(indexName).Id("2"));
            }
            catch (Exception ex)
            {
                conflicts.Add($"Number to text conflict: {ex.Message}");
            }

            // Conflict 2: Try to insert text as number field
            try
            {
                await _client.IndexAsync(new
                {
                    Id = "STATE-002",
                    Count = "two hundred", // Text to number field
                    IsActive = true,
                    Timestamp = DateTime.Now
                }, i => i.Index(indexName).Id("3"));
            }
            catch (Exception ex)
            {
                conflicts.Add($"Text to number conflict: {ex.Message}");
            }

            // Conflict 3: Try to insert string as boolean field
            try
            {
                await _client.IndexAsync(new
                {
                    Id = "STATE-003",
                    Count = 300,
                    IsActive = "yes", // String to boolean field
                    Timestamp = DateTime.Now
                }, i => i.Index(indexName).Id("4"));
            }
            catch (Exception ex)
            {
                conflicts.Add($"String to boolean conflict: {ex.Message}");
            }

            // Conflict 4: Try to insert invalid date format
            try
            {
                await _client.IndexAsync(new
                {
                    Id = "STATE-004",
                    Count = 400,
                    IsActive = false,
                    Timestamp = "not-a-date" // Invalid date format
                }, i => i.Index(indexName).Id("5"));
            }
            catch (Exception ex)
            {
                conflicts.Add($"Invalid date format conflict: {ex.Message}");
            }

            return conflicts;
        }
        catch (Exception ex)
        {
            conflicts.Add($"Overall error: {ex.Message}");
            return conflicts;
        }
        finally
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    /// <summary>
    /// Example 4: Complex Object Mapping Conflicts
    /// Shows issues with nested objects and arrays
    /// </summary>
    public async Task<Dictionary<string, object>> DemonstrateComplexMappingConflicts()
    {
        var results = new Dictionary<string, object>();
        var indexName = "complex-conflict-index";

        try
        {
            // Create index allowing dynamic mapping
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Dynamic(DynamicMapping.True)
                    .Properties<ComplexState>(p => p
                        .Text(f => f.Name)
                    )
                )
            );

            // First document establishes dynamic mapping
            await _client.IndexAsync(new
            {
                Name = "Complex State 1",
                Metadata = new
                {
                    Version = 1,        // Inferred as long
                    Active = true,      // Inferred as boolean
                    Tags = new[] { "tag1", "tag2" } // Inferred as text array
                },
                Properties = new Dictionary<string, object>
                {
                    ["prop1"] = "value1",  // Inferred as text
                    ["prop2"] = 42         // Inferred as long
                }
            }, i => i.Index(indexName).Id("1"));

            results["FirstDocumentInserted"] = "Success - establishes dynamic mapping";

            // Conflict 1: Change nested object structure
            try
            {
                await _client.IndexAsync(new
                {
                    Name = "Complex State 2",
                    Metadata = "simple string", // Object to string conflict
                    Properties = new Dictionary<string, object>
                    {
                        ["prop1"] = 123,        // Text to number conflict
                        ["prop2"] = "string"    // Number to text conflict
                    }
                }, i => i.Index(indexName).Id("2"));
            }
            catch (Exception ex)
            {
                results["NestedObjectConflict"] = ex.Message;
            }

            // Conflict 2: Array type conflicts
            try
            {
                await _client.IndexAsync(new
                {
                    Name = "Complex State 3",
                    Metadata = new
                    {
                        Version = "v2.0",   // Long to text conflict
                        Active = "maybe",   // Boolean to text conflict
                        Tags = 42           // Array to number conflict
                    }
                }, i => i.Index(indexName).Id("3"));
            }
            catch (Exception ex)
            {
                results["ArrayTypeConflict"] = ex.Message;
            }

            return results;
        }
        catch (Exception ex)
        {
            results["Error"] = ex.Message;
            return results;
        }
        finally
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    #endregion

    #region SOLUTIONS AND BEST PRACTICES

    /// <summary>
    /// Example 5: Proper Index Recreation with Validation
    /// Shows how to safely handle index recreation
    /// </summary>
    public async Task<bool> SafeIndexRecreation(string indexName, Type stateType)
    {
        try
        {
            var versionedIndexName = $"{indexName}_v{GetSchemaVersion(stateType)}";
            
            // Check if the correct version exists
            var exists = await _client.Indices.ExistsAsync(versionedIndexName);
            if (exists.Exists)
            {
                // Validate schema compatibility
                var isCompatible = await ValidateSchemaCompatibility(versionedIndexName, stateType);
                if (isCompatible)
                {
                    return true; // Index is good to use
                }
                
                // Schema mismatch - perform migration
                await PerformIndexMigration(indexName, stateType);
                return true;
            }

            // Create new versioned index
            return await CreateVersionedIndex(versionedIndexName, stateType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Safe index recreation failed for {IndexName}", indexName);
            return false;
        }
    }

    /// <summary>
    /// Example 6: Conflict-Free Mapping Strategy
    /// </summary>
    public async Task<bool> CreateConflictFreeMapping(string indexName)
    {
        try
        {
            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1))
                .Mappings(m => m
                    .Dynamic(DynamicMapping.Strict) // Prevent unexpected fields
                    .Properties<FlexibleState>(p => p
                        // Multi-field mapping for flexibility
                        .Text(f => f.Name, t => t
                            .Fields(fields => fields
                                .Keyword("exact") // For exact matching
                                .Text("analyzed") // For full-text search
                            )
                        )
                        // Support multiple numeric formats
                        .Text(f => f.FlexibleNumber, t => t
                            .Fields(fields => fields
                                .LongNumber("as_long")
                                .DoubleNumber("as_double")
                                .Keyword("as_keyword")
                            )
                        )
                        // Flexible date handling
                        .Date(f => f.Timestamp, d => d
                            .Format("strict_date_optional_time||epoch_millis||yyyy-MM-dd HH:mm:ss")
                        )
                        // Controlled nested object
                        .Object(f => f.Metadata, o => o
                            .Properties<MetadataStructure>(mp => mp
                                .Text(mf => mf.StringValue)
                                .LongNumber(mf => mf.NumberValue)
                                .Boolean(mf => mf.BoolValue)
                                .Date(mf => mf.DateValue)
                            )
                        )
                    )
                )
            );

            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create conflict-free mapping for {IndexName}", indexName);
            return false;
        }
    }

    #endregion

    #region HELPER METHODS

    private async Task<bool> ValidateSchemaCompatibility(string indexName, Type stateType)
    {
        try
        {
            var mapping = await _client.Indices.GetMappingAsync(indexName);
            // Implementation would validate current mapping against expected schema
            // This is simplified for example purposes
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> PerformIndexMigration(string baseIndexName, Type stateType)
    {
        var oldVersion = GetSchemaVersion(stateType) - 1;
        var newVersion = GetSchemaVersion(stateType);
        var oldIndexName = $"{baseIndexName}_v{oldVersion}";
        var newIndexName = $"{baseIndexName}_v{newVersion}";

        try
        {
            // Create new index with updated schema
            await CreateVersionedIndex(newIndexName, stateType);

            // Reindex with data transformation
            var reindexResponse = await _client.ReindexAsync(r => r
                .Source(s => s.Index(oldIndexName))
                .Destination(d => d.Index(newIndexName))
                .Script(sc => sc.Source("ctx._source.migrated = true;"))
            );

            // Update alias
            await _client.Indices.UpdateAliasesAsync(a => a
                .Actions(actions => actions
                    .Remove(rem => rem.Index(oldIndexName).Alias(baseIndexName))
                    .Add(add => add.Index(newIndexName).Alias(baseIndexName))
                )
            );

            return reindexResponse.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index migration failed from {OldIndex} to {NewIndex}", 
                oldIndexName, newIndexName);
            return false;
        }
    }

    private async Task<bool> CreateVersionedIndex(string indexName, Type stateType)
    {
        // Implementation would create index based on state type
        // This is simplified for example purposes
        var response = await _client.Indices.CreateAsync(indexName, c => c
            .Mappings(m => m.Dynamic(DynamicMapping.Strict))
        );
        
        return response.IsValidResponse;
    }

    private int GetSchemaVersion(Type stateType)
    {
        // Implementation would determine schema version based on type
        // Could use attributes, reflection, or configuration
        return 1;
    }

    #endregion
}

#region EXAMPLE STATE CLASSES

public class UserStateV1
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class UserStateV2
{
    public string Name { get; set; }
    public string Age { get; set; } // Changed to string
    public string Email { get; set; } // New field
}

public class ProductStateV1
{
    public string ProductId { get; set; }
    public double Price { get; set; }
    public string Category { get; set; }
}

public class StateWithMixedTypes
{
    public string Id { get; set; }
    public long Count { get; set; }
    public bool IsActive { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ComplexState
{
    public string Name { get; set; }
    public object Metadata { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}

public class FlexibleState
{
    public string Name { get; set; }
    public string FlexibleNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public MetadataStructure Metadata { get; set; }
}

public class MetadataStructure
{
    public string StringValue { get; set; }
    public long NumberValue { get; set; }
    public bool BoolValue { get; set; }
    public DateTime DateValue { get; set; }
}

#endregion

/// <summary>
/// Example error scenarios and their solutions
/// </summary>
public static class IndexErrorScenarios
{
    public static readonly Dictionary<string, string> CommonErrors = new()
    {
        ["mapper_parsing_exception"] = "Field type conflicts - attempting to index wrong data type",
        ["strict_dynamic_mapping_exception"] = "New field in strict mapping mode",
        ["version_conflict_engine_exception"] = "Concurrent updates to same document",
        ["index_already_exists_exception"] = "Attempting to create existing index",
        ["resource_already_exists_exception"] = "Index creation race condition",
        ["illegal_argument_exception"] = "Invalid mapping configuration"
    };

    public static readonly Dictionary<string, string> Solutions = new()
    {
        ["Use explicit mapping"] = "Define field types explicitly instead of relying on dynamic mapping",
        ["Implement versioning"] = "Use versioned indices with aliases for schema evolution",
        ["Use multi-field mapping"] = "Support multiple data types for same logical field",
        ["Implement proper migration"] = "Zero-downtime reindexing for schema changes",
        ["Add conflict detection"] = "Validate schema compatibility before operations",
        ["Use index templates"] = "Consistent mapping across similar indices"
    };
} 