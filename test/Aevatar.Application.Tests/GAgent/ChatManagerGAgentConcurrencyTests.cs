using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Aevatar.Application.Grains.Agents.ChatManager.Chat;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.AIGAgent.Dtos;
using Moq;
using Shouldly;
using Xunit;
using Orleans.TestKit;
using Orleans.Runtime;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Concurrency;

namespace Aevatar.Application.Tests.GAgent
{
    public class ChatManagerGAgentConcurrencyTests : AevatarApplicationTestBase, IDisposable
    {
        private readonly TestKitSilo _silo;
        private readonly Mock<IConfigurationGAgent> _mockConfigGAgent;
        private readonly Mock<IGodChat> _mockGodChat;
        private readonly Guid _godChatId;

        public ChatManagerGAgentConcurrencyTests()
        {
            _silo = new TestKitSilo();
            
            // 配置测试时使用的依赖Mock
            _mockConfigGAgent = new Mock<IConfigurationGAgent>();
            _mockConfigGAgent.Setup(x => x.GetSystemLLM()).ReturnsAsync("OpenAI");
            _mockConfigGAgent.Setup(x => x.GetPrompt()).ReturnsAsync("Test prompt");
            _mockConfigGAgent.Setup(x => x.GetStreamingModeEnabled()).ReturnsAsync(true);
            
            // 使用TestServiceProvider来添加服务Mock
            _silo.ServiceProvider.AddServiceProbe(_mockConfigGAgent);
            
            _godChatId = Guid.NewGuid();
            _mockGodChat = new Mock<IGodChat>();
            _mockGodChat.Setup(x => x.ConfigAsync(It.IsAny<ChatConfigDto>())).Returns(Task.CompletedTask);
            _mockGodChat.Setup(x => x.GodChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExecutionPromptSettings>()))
                .ReturnsAsync("Test response");
                
            // 使用工厂方法来在创建特定ID的grain时返回mock
            _silo.AddProbe<IGodChat>(id => {
                if (id.ToString() == _godChatId.ToString()) {
                    return _mockGodChat.Object;
                }
                return new Mock<IGodChat>().Object;
            });
        }
        
        [Fact]
        public async Task ReadOnlyMethods_ShouldExecuteConcurrently()
        {
            // Arrange
            var chatManager = await _silo.CreateGrainAsync<ChatGAgentManager>(Guid.NewGuid());
            
            // 添加一些测试会话数据
            var sessionId1 = Guid.NewGuid();
            var sessionId2 = Guid.NewGuid();
            
            // 使用非公开方法触发状态变更
            var field = typeof(ChatGAgentManager).GetField("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var state = field.GetValue(chatManager) as ChatManagerGAgentState;
            state.SessionInfoList.Add(new SessionInfo { SessionId = sessionId1, Title = "Session 1" });
            state.SessionInfoList.Add(new SessionInfo { SessionId = sessionId2, Title = "Session 2" });
            
            // Act
            // 并发调用只读方法
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(chatManager.GetSessionListAsync());
            }
            
            await Task.WhenAll(tasks);
            sw.Stop();
            
            // Assert
            // 验证并发执行速度是否合理（如果是顺序执行会很慢）
            sw.ElapsedMilliseconds.ShouldBeLessThan(1000); // 并发执行应该远低于1秒
        }
        
        [Fact]
        public async Task StreamingChat_ShouldNotBlockOtherRequests()
        {
            // Arrange
            var chatManager = await _silo.CreateGrainAsync<ChatGAgentManager>(Guid.NewGuid());
            
            // 模拟流式处理时的长时间运行操作
            _mockGodChat.Setup(x => x.GodStreamChatAsync(
                    It.IsAny<string>(), 
                    It.IsAny<bool>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<ExecutionPromptSettings>()))
                .Returns(async () => {
                    // 模拟长时间处理
                    await Task.Delay(2000);
                    return string.Empty;
                });
            
            // Act - 发送流式处理请求
            var streamChatTask = chatManager.HandleEventAsync(new RequestStreamGodChatEvent
            {
                SessionId = _godChatId,
                SystemLLM = "OpenAI",
                Content = "Test content"
            });
            
            // 不等待流式处理完成，立即发送其他请求
            await Task.Delay(100); // 给处理一点时间启动
            
            var sw = Stopwatch.StartNew();
            var sessionListTask = chatManager.GetSessionListAsync();
            
            // 这个应该能迅速完成，不会被流式处理阻塞
            await sessionListTask;
            sw.Stop();
            
            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(500); // 应该很快就能完成，不会被流处理阻塞
            
            // 等待流式处理完成，避免测试结束时有未完成的任务
            await streamChatTask;
        }
        
        [Fact]
        public async Task MultipleRequests_ShouldExecuteEfficiently()
        {
            // Arrange
            var chatManager = await _silo.CreateGrainAsync<ChatGAgentManager>(Guid.NewGuid());
            
            // 设置模拟的响应延迟
            _mockGodChat.Setup(x => x.GodChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExecutionPromptSettings>()))
                .Returns(async () => {
                    await Task.Delay(200); // 每个请求花费200ms
                    return "Response";
                });
            
            // Act
            var sw = Stopwatch.StartNew();
            
            // 并发发送10个不同的请求，这些请求应该能够重入执行
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(chatManager.HandleEventAsync(new RequestGodChatEvent
                {
                    SessionId = _godChatId,
                    SystemLLM = "OpenAI",
                    Content = $"Content {i}"
                }));
            }
            
            await Task.WhenAll(tasks);
            sw.Stop();
            
            // Assert
            // 由于使用了[Reentrant]特性，应该能够并发处理这些请求
            // 如果是顺序执行，需要10*200=2000ms
            // 并发执行应该大幅减少时间
            sw.ElapsedMilliseconds.ShouldBeLessThan(1000);
        }
        
        public override void Dispose()
        {
            // 实现IDisposable接口，清理资源
            _silo.Dispose();
            base.Dispose();
        }
        
        void IDisposable.Dispose()
        {
            // 清理资源并调用基类的Dispose方法
            _silo?.Dispose();
            base.Dispose();
        }
    }
} 