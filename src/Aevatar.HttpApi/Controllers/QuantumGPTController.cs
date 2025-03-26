using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Aevatar.Controllers;

[Route("api/quantum")]
public class QuantumGPTController  : AevatarController
{
    [HttpGet("chats")]
    public async Task Chats()
    {
       return;
    }
    
    [HttpGet("chats/{id}")]
    public async Task ChatsById(string id)
    {
        return;
    }
    
    [HttpPost("chat/{id}/new-prompt")]
    public async Task ChatNewPrompt(string id)
    {
        return;
    }
    
    
}