using Aevatar.Application.Grains.Tests.Core;
using Xunit;
using Aevatar.Application.Grains.Agents.ChatManager;

namespace Aevatar.Application.Grains.Tests.Agents
{
    public class ChatManagerTests : GodGPTTestBase
    {
        [Fact]
        public async Task Should_Create_Chat_Session()
        {
            // 获取ChatManager Grain
            var grain = GrainFactory.GetGrain<IChatManagerGAgent>(Guid.NewGuid());
            
            // 创建会话
            var session = await grain.CreateSessionAsync("DefaultLLM", "测试提示词");
            
            // 验证会话创建成功
            Assert.NotEqual(Guid.Empty, session);
        }

        [Fact]
        public async Task Should_Process_Message()
        {
            // 获取ChatManager Grain
            var grain = GrainFactory.GetGrain<IChatManagerGAgent>(Guid.NewGuid());
            
            // 创建会话
            var sessionId = await grain.CreateSessionAsync("DefaultLLM", "测试提示词");
            
            // 处理消息
            var response = await grain.ChatWithSessionAsync(sessionId, "DefaultLLM", "测试消息");
            
            // 验证响应
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Should_Maintain_Session_State()
        {
            // 获取ChatManager Grain
            var grain = GrainFactory.GetGrain<IChatManagerGAgent>(Guid.NewGuid());
            
            // 创建会话
            var sessionId = await grain.CreateSessionAsync("DefaultLLM", "测试提示词");
            
            // 验证会话状态
            Assert.True(await grain.IsUserSessionAsync(sessionId));
            
            // 处理消息
            var response = await grain.ChatWithSessionAsync(sessionId, "DefaultLLM", "测试消息");
            Assert.NotNull(response);
            
            // 再次验证会话状态
            Assert.True(await grain.IsUserSessionAsync(sessionId));
        }
    }
} 