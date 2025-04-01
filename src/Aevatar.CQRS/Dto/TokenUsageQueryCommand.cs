using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Aevatar.CQRS.Dto;

public class TokenUsageQueryCommand : IRequest<Tuple<long, List<string>>?>
{
    [Required] public Guid ProjectId { get; set; }
    public string? SystemLLM { get; set; }
    [Required] public DateTime StartTime { get; set; }
    [Required] public DateTime EndTime { get; set; }

    [Required] public bool StatisticsAsHour { get; set; } = false;
}