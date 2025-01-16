using System.Collections.Generic;

namespace Aevatar.CQRS.Dto;

public class ChatLogPageResultDto
{
    public long TotalRecordCount { get; set; }
    public List<AIChatLogIndexDto> Data{ get; set; }
}