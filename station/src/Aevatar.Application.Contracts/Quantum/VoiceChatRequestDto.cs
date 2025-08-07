using System;
using System.ComponentModel.DataAnnotations;
using GodGPT.GAgents.SpeechChat;

namespace Aevatar.Quantum;

/// <summary>
/// Request DTO for voice chat functionality
/// </summary>
public class VoiceChatRequestDto
{
    /// <summary>
    /// Session identifier
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// Message content
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional region identifier
    /// </summary>
    [MaxLength(10)]
    public string? Region { get; set; }
    
    /// <summary>
    /// Message type - defaults to Text if not specified
    /// </summary>
    public MessageTypeEnum MessageType { get; set; } = MessageTypeEnum.Text;
    
    /// <summary>
    /// Voice language type - defaults to English if not specified
    /// </summary>
    public VoiceLanguageEnum VoiceLanguage { get; set; } = VoiceLanguageEnum.English;
    
    /// <summary>
    /// Duration of the voice message in seconds (provided by frontend)
    /// </summary>
    public double VoiceDurationSeconds { get; set; }
    public Guid UserId{ get; set; }

} 