using System;
using System.Threading.Tasks;
using Aevatar.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.HttpApi.Controllers
{
    /// <summary>
    /// 工作流相关API控制器
    /// </summary>
    [ApiController]
    [Route("api/workflow")]
    public class WorkflowController : AbpControllerBase
    {
        private readonly ITextCompletionService _textCompletionService;

        public WorkflowController(ITextCompletionService textCompletionService)
        {
            _textCompletionService = textCompletionService;
        }

        /// <summary>
        /// 生成文本补全
        /// </summary>
        /// <param name="request">补全请求</param>
        /// <returns>补全结果</returns>
        [HttpPost("text-completion/generate")]
        [Authorize]
        public async Task<TextCompletionResponseDto> GenerateTextCompletionAsync([FromBody] TextCompletionRequestDto request)
        {
            if (request == null)
            {
                throw new UserFriendlyException("Request cannot be null");
            }

            return await _textCompletionService.GenerateCompletionsAsync(request);
        }
    }
} 