using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// Agent描述信息DTO，用于工作流编排时传递Agent的详细信息
    /// </summary>
    public class AgentDescriptionInfo
    {
        /// <summary>
        /// Agent的唯一标识符
        /// </summary>
        [Required]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Agent的显示名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Agent的分类（如：Data, Analysis, AI, Blockchain等）
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 一级描述 - 简短的功能概述
        /// </summary>
        public string L1Description { get; set; } = string.Empty;

        /// <summary>
        /// 二级描述 - 详细的功能描述
        /// </summary>
        public string L2Description { get; set; } = string.Empty;

        /// <summary>
        /// Agent的能力列表
        /// </summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// Agent的标签列表，用于分类和搜索
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
} 