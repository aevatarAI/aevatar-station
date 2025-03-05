using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Query;

public class LuceneQueryDto 
{
    [Required]
    public string Index { get; set; }  

    [Required]
    public string QueryString { get; set; } 

    public int From { get; set; } = 0; 
    public int Size { get; set; } = 10;
    public List<string> SortFields { get; set; } = new List<string>();  
}