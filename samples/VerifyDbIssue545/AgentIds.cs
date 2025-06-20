using System.Collections.Generic;

namespace VerifyDbIssue545
{
    public class AgentIds
    {
        public List<string> SubAgentIds { get; set; } = new List<string>();
        public string PubAgentId { get; set; } = string.Empty;
    }
} 