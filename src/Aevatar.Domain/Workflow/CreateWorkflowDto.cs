using System.ComponentModel.DataAnnotations;

namespace Aevatar.Workflow;

public class CreateWorkflowDto
{
    [Required]
    public string WorkflowName { get; set; }
    
    [Required]
    public AIConfigDto AiConfig { get; set; }
    
    [Required]
    public TwitterConfigDto TwitterConfig { get; set; }
}

public class AIConfigDto
{
    [Required]
    public string ApiKey { get; set; }
    
    public string Model { get; set; } = "gpt-4";
    
    public int MaxTokens { get; set; } = 1000;
    
    public double Temperature { get; set; } = 0.7;
}

public class TwitterConfigDto
{
    [Required]
    public string ConsumerKey { get; set; }
    
    [Required]
    public string ConsumerSecret { get; set; }
    
    [Required]
    public string BearerToken { get; set; }
    
    [Required]
    public string EncryptionPassword { get; set; }
    
    public int ReplyLimit { get; set; } = 10;
    
    // Twitter Account Binding Fields for BindTwitterAccountGEvent
    public string UserName { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public string TokenSecret { get; set; }
} 