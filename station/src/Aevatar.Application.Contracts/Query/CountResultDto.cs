namespace Aevatar.Query;

/// <summary>
/// Result DTO for count queries, bypassing the 10,000 document limit
/// </summary>
public class CountResultDto
{
    /// <summary>
    /// The exact count of documents matching the query criteria
    /// </summary>
    public long Count { get; set; }
} 