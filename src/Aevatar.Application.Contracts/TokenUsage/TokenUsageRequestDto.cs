using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.TokenUsage;

public class TokenUsageRequestDto
{
    [Required]
    public Guid ProjectId { get; set; }
    public string? SystemLLM { get; set; }
    [Required]
    public DateTime StartTime { get; set; }
    [Required]
    public DateTime EndTime { get; set; }

    [Required] public bool StatisticsAsHour { get; set; } = false;

}