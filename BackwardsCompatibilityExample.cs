using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.Examples;

/// <summary>
/// Example demonstrating backwards compatibility scenarios with Orleans serialization and Elasticsearch
/// </summary>
public class BackwardsCompatibilityExample
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<BackwardsCompatibilityExample> _logger;

    public BackwardsCompatibilityExample(ElasticsearchClient client, ILogger<BackwardsCompatibilityExample> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates what happens with the current Elasticsearch implementation
    /// when adding new properties with Orleans serialization IDs
    /// </summary>
    public async Task<Dictionary<string, object>> DemonstrateBackwardsCompatibility()
    {
        var results = new Dictionary<string, object>();
        var indexName = "user-state-backwards-compat";

        try
        {
            // Step 1: Create index with original state structure
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Properties<UserStateV1>(p => p
                        .Text(f => f.Name)
                        .LongNumber(f => f.Count)
                    )
                )
            );

            // Step 2: Index original data
            await _client.IndexAsync(new UserStateV1
            {
                Name = "John Doe",
                Count = 100
            }, i => i.Index(indexName).Id("1"));

            results["OriginalDataIndexed"] = "Success";

            // Step 3: Simulate application restart with evolved state class
            // Current implementation: CheckExistOrCreateStateIndex will find existing index and skip creation
            var exists = await _client.Indices.ExistsAsync(indexName);
            if (exists.Exists)
            {
                results["IndexExistsCheck"] = "Index exists - no recreation attempted";
                
                // Step 4: Try to index evolved state data
                try
                {
                    // This will work because Elasticsearch allows dynamic mapping
                    await _client.IndexAsync(new
                    {
                        Name = "Jane Smith",
                        Count = 200,
                        Description = "Updated user", // New field - will be dynamically mapped
                        LastUpdated = DateTime.UtcNow,  // New field - will be dynamically mapped
                        IsActive = true                 // New field - will be dynamically mapped
                    }, i => i.Index(indexName).Id("2"));

                    results["EvolvedDataIndexed"] = "Success - new fields added via dynamic mapping";
                }
                catch (Exception ex)
                {
                    results["EvolvedDataError"] = ex.Message;
                }

                // Step 5: Check the current mapping to see what was inferred
                var mappingResponse = await _client.Indices.GetMappingAsync(indexName);
                if (mappingResponse.IsValidResponse)
                {
                    results["DynamicMappingResult"] = "New fields mapped dynamically";
                    // In real scenario, you'd examine mappingResponse.Indices[indexName].Mappings
                }
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

    /// <summary>
    /// Demonstrates potential problems with backwards compatibility
    /// </summary>
    public async Task<List<string>> DemonstrateBackwardsCompatibilityProblems()
    {
        var problems = new List<string>();
        var indexName = "problematic-backwards-compat";

        try
        {
            // Create index with initial mapping
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Dynamic(Elastic.Clients.Elasticsearch.IndexManagement.DynamicMapping.False) // Strict mapping
                    .Properties<UserStateV1>(p => p
                        .Text(f => f.Name)
                        .LongNumber(f => f.Count)
                    )
                )
            );

            // Try to index evolved state with strict mapping
            try
            {
                await _client.IndexAsync(new
                {
                    Name = "Jane Smith",
                    Count = 200,
                    Description = "This will fail", // New field not in mapping
                    LastUpdated = DateTime.UtcNow   // New field not in mapping
                }, i => i.Index(indexName).Id("1"));
            }
            catch (Exception ex)
            {
                problems.Add($"Strict mapping rejects new fields: {ex.Message}");
            }

            // Problem 2: Type conflicts with dynamic mapping
            await _client.Indices.DeleteAsync(indexName);
            await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m.Dynamic(Elastic.Clients.Elasticsearch.IndexManagement.DynamicMapping.True))
            );

            // First document establishes dynamic mapping
            await _client.IndexAsync(new
            {
                Name = "John",
                Count = 100,
                Version = 1 // Will be mapped as long
            }, i => i.Index(indexName).Id("1"));

            // Second document with incompatible type
            try
            {
                await _client.IndexAsync(new
                {
                    Name = "Jane",
                    Count = 200,
                    Version = "2.0" // Conflict: string to long field
                }, i => i.Index(indexName).Id("2"));
            }
            catch (Exception ex)
            {
                problems.Add($"Type conflict with dynamic mapping: {ex.Message}");
            }

            return problems;
        }
        catch (Exception ex)
        {
            problems.Add($"Overall error: {ex.Message}");
            return problems;
        }
        finally
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    /// <summary>
    /// Demonstrates the recommended approach for backwards compatible index evolution
    /// </summary>
    public async Task<bool> RecommendedBackwardsCompatibleApproach(string baseIndexName)
    {
        try
        {
            var schemaVersion = GetCurrentSchemaVersion();
            var versionedIndexName = $"{baseIndexName}_v{schemaVersion}";
            var aliasName = baseIndexName;

            // Check if current version index exists
            var exists = await _client.Indices.ExistsAsync(versionedIndexName);
            if (exists.Exists)
            {
                _logger.LogInformation("Index {IndexName} already exists with correct schema version", versionedIndexName);
                return true;
            }

            // Create new versioned index with evolved schema
            var createResponse = await _client.Indices.CreateAsync(versionedIndexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1))
                .Mappings(m => m
                    .Dynamic(Elastic.Clients.Elasticsearch.IndexManagement.DynamicMapping.Strict)
                    .Properties<UserStateV2>(p => p
                        .Text(f => f.Name, t => t
                            .Fields(fields => fields
                                .Keyword("exact") // Multi-field for exact matching
                            )
                        )
                        .LongNumber(f => f.Count)
                        .Text(f => f.Description)        // New field
                        .Date(f => f.LastUpdated)        // New field
                        .Boolean(f => f.IsActive)        // New field
                    )
                )
            );

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError("Failed to create versioned index: {Error}", createResponse.ElasticsearchServerError?.Error);
                return false;
            }

            // Update alias to point to new version
            await _client.Indices.UpdateAliasesAsync(a => a
                .Actions(actions => actions
                    .Add(add => add.Index(versionedIndexName).Alias(aliasName))
                )
            );

            _logger.LogInformation("Successfully created versioned index {IndexName} with alias {Alias}", 
                versionedIndexName, aliasName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backwards compatible index for {IndexName}", baseIndexName);
            return false;
        }
    }

    /// <summary>
    /// Simulates how the current ElasticIndexingService.CreateIndexAsync would handle evolved state
    /// </summary>
    public async Task<bool> SimulateCurrentImplementationWithEvolvedState<T>(string indexName) where T : StateBase
    {
        try
        {
            // This mirrors the current CreateIndexAsync<T> method logic
            var createIndexResponse = await _client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Properties<T>(props =>
                    {
                        var type = typeof(T);
                        foreach (var property in type.GetProperties())
                        {
                            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                            var propType = property.PropertyType;

                            // This is the current mapping logic from ElasticIndexingService
                            if (propType == typeof(string))
                            {
                                props.Text(propertyName);
                            }
                            else if (propType == typeof(short) || propType == typeof(int) || propType == typeof(long))
                            {
                                props.LongNumber(propertyName);
                            }
                            else if (propType == typeof(float))
                            {
                                props.FloatNumber(propertyName);
                            }
                            else if (propType == typeof(double) || propType == typeof(decimal))
                            {
                                props.DoubleNumber(propertyName);
                            }
                            else if (propType == typeof(DateTime))
                            {
                                props.Date(propertyName);
                            }
                            else if (propType == typeof(bool))
                            {
                                props.Boolean(propertyName);
                            }
                            else if (propType == typeof(Guid))
                            {
                                props.Keyword(propertyName, k => k.ToString());
                            }
                            else
                            {
                                props.Text(propertyName);
                            }
                        }

                        // Add CTime as a Date field (from current implementation)
                        props.Date("CTime");
                    })
                )
            );

            return createIndexResponse.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate current implementation for {IndexName}", indexName);
            return false;
        }
    }

    private int GetCurrentSchemaVersion()
    {
        // In a real implementation, this would determine version based on:
        // - Assembly version
        // - Schema change tracking
        // - Configuration
        return 2;
    }
}

#region EXAMPLE STATE CLASSES FOR BACKWARDS COMPATIBILITY

/// <summary>
/// Original state class (version 1)
/// </summary>
[GenerateSerializer]
public class UserStateV1 : StateBase
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public int Count { get; set; }
}

/// <summary>
/// Evolved state class (version 2) - backwards compatible with Orleans serialization
/// </summary>
[GenerateSerializer]
public class UserStateV2 : StateBase
{
    // Original properties maintain same IDs for backwards compatibility
    [Id(0)] public string Name { get; set; }
    [Id(1)] public int Count { get; set; }
    
    // New properties get new IDs for Orleans backwards compatibility
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    [Id(4)] public bool IsActive { get; set; } = true;
}

/// <summary>
/// Example of a problematic evolution (what NOT to do)
/// </summary>
[GenerateSerializer]
public class UserStateProblematic : StateBase
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Count { get; set; } // PROBLEM: Changed from int to string but kept same ID
    [Id(2)] public string Description { get; set; }
}

#endregion 