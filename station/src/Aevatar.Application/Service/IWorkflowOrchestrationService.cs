using System.Threading.Tasks;

namespace Aevatar.Service
{
    /// <summary>
    /// 工作流编排服务接口
    /// </summary>
    public interface IWorkflowOrchestrationService
    {
        /// <summary>
        /// 根据用户目标生成工作流配置
        /// </summary>
        /// <param name="userGoal">用户目标描述</param>
        /// <returns>前端可渲染的工作流视图配置</returns>
        Task<AiWorkflowViewConfigDto?> GenerateWorkflowAsync(string userGoal);
    }
} 