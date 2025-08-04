using System.Collections.Generic;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// Agent描述信息 - 用于工作流编排的Agent元数据
    /// </summary>
    public class AgentDescriptionInfo
    {
        /// <summary>
        /// Agent唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Agent名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// L1简短描述（100-150字符），用于快速语义匹配
        /// </summary>
        public string L1Description { get; set; } = string.Empty;
        
        /// <summary>
        /// L2详细描述（300-500字符），用于精确能力分析
        /// </summary>
        public string L2Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Agent分类
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Agent能力列表
        /// </summary>
        public List<string> Capabilities { get; set; } = new();
        
        /// <summary>
        /// Agent标签列表
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
} 