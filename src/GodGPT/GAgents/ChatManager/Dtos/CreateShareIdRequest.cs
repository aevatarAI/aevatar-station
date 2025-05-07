using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Grains.Agents.ChatManager.Dtos;

public class CreateShareIdRequest
{
    [Required]
    public Guid SessionId { get; set; }
}

public class CreateShareIdResponse
{
    public string ShareId { get; set; }
}