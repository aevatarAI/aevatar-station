using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Query;

public class LuceneQueryDto 
{
    [Required]
    public string Index { get; set; }

    public string QueryString { get; set; } = "";

    public int PageIndex { get; set; } = 0; 
    public int PageSize { get; set; } = 10;
    public List<string> SortFields { get; set; } = new List<string>();  
}