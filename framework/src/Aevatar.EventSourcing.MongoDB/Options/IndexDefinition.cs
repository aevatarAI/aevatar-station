using MongoDB.Driver;

namespace Aevatar.EventSourcing.MongoDB.Options;

/// <summary>
/// Represents a complete MongoDB index definition with keys and options.
/// </summary>
public class IndexDefinition
{
    /// <summary>
    /// The name of the index.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The list of keys that make up this index.
    /// </summary>
    public List<IndexKey> Keys { get; set; } = new List<IndexKey>();

    /// <summary>
    /// MongoDB-specific index options.
    /// </summary>
    public CreateIndexOptions? Options { get; set; }

    /// <summary>
    /// Initializes a new instance of the IndexDefinition class.
    /// </summary>
    public IndexDefinition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the IndexDefinition class with specified name and keys.
    /// </summary>
    /// <param name="name">The name of the index.</param>
    /// <param name="keys">The keys that make up this index.</param>
    public IndexDefinition(string name, params IndexKey[] keys)
    {
        Name = name;
        Keys = keys.ToList();
    }

    /// <summary>
    /// Initializes a new instance of the IndexDefinition class with specified name, keys, and options.
    /// </summary>
    /// <param name="name">The name of the index.</param>
    /// <param name="keys">The keys that make up this index.</param>
    /// <param name="options">The MongoDB index options.</param>
    public IndexDefinition(string name, IndexKey[] keys, CreateIndexOptions options)
    {
        Name = name;
        Keys = keys.ToList();
        Options = options;
    }

    /// <summary>
    /// Creates a simple ascending index on a single field.
    /// </summary>
    /// <param name="name">The name of the index.</param>
    /// <param name="fieldName">The field name to index.</param>
    /// <returns>A new IndexDefinition.</returns>
    public static IndexDefinition CreateAscending(string name, string fieldName)
    {
        return new IndexDefinition(name, new IndexKey(fieldName, SortDirection.Ascending));
    }

    /// <summary>
    /// Creates a compound index with multiple fields.
    /// </summary>
    /// <param name="name">The name of the index.</param>
    /// <param name="fieldNames">The field names to include in the compound index.</param>
    /// <returns>A new IndexDefinition.</returns>
    public static IndexDefinition CreateCompound(string name, params string[] fieldNames)
    {
        var keys = fieldNames.Select(f => new IndexKey(f, SortDirection.Ascending)).ToArray();
        return new IndexDefinition(name, keys);
    }

    public override string ToString()
    {
        var keyString = string.Join(", ", Keys.Select(k => k.ToString()));
        return $"{Name}: {{ {keyString} }}";
    }
} 