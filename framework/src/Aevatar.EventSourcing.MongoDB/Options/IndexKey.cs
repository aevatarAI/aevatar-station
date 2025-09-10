using MongoDB.Driver;

namespace Aevatar.EventSourcing.MongoDB.Options;

/// <summary>
/// Represents a single key in a MongoDB index definition.
/// </summary>
public class IndexKey
{
    /// <summary>
    /// The field name to index.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The sort direction for this field in the index.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Initializes a new instance of the IndexKey class.
    /// </summary>
    public IndexKey()
    {
    }

    /// <summary>
    /// Initializes a new instance of the IndexKey class with specified field and direction.
    /// </summary>
    /// <param name="fieldName">The field name to index.</param>
    /// <param name="direction">The sort direction for this field.</param>
    public IndexKey(string fieldName, SortDirection direction = SortDirection.Ascending)
    {
        FieldName = fieldName;
        Direction = direction;
    }

    public override string ToString()
    {
        return $"{FieldName}:{(Direction == SortDirection.Ascending ? "1" : "-1")}";
    }
}

/// <summary>
/// Enumeration for sort directions in MongoDB indexes.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending sort direction.
    /// </summary>
    Ascending = 1,
    
    /// <summary>
    /// Descending sort direction.
    /// </summary>
    Descending = -1
} 