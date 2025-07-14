using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Quantum;

public class QuantumRenameDto
{
    [Required]
    public Guid SessionId { get; set; }
    [Required]
    public string Title { get; set; }
}