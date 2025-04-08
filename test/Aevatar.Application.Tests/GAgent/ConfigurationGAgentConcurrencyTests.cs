using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager.ConfigAgent;
using Moq;
using Shouldly;
using Xunit;
using Orleans.TestKit;

namespace Aevatar.Application.Tests.GAgent
{
    public class ConfigurationGAgentConcurrencyTests : AevatarApplicationTestBase
    {
        private readonly TestKitSilo _silo;

        public ConfigurationGAgentConcurrencyTests()
        {
            _silo = new TestKitSilo();
        }

        [Fact]
        public async Task ReadOnlyMethods_ShouldExecuteConcurrently()
        {
            // Arrange
            var configGrain = await _silo.CreateGrainAsync<ConfigurationGAgent>(Guid.NewGuid());
            
            // 使用非公开方法设置状态
            var field = typeof(ConfigurationGAgent).GetField("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var state = field.GetValue(configGrain) as ConfigurationState;
            state.SystemLLM = "OpenAI";
            state.Prompt = "Test prompt";
            state.StreamingModeEnabled = true;
            
            // Act
            // 并发调用只读方法
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();
            
            // 创建大量并发请求
            for (int i = 0; i < 100; i++)
            {
                if (i % 3 == 0)
                {
                    tasks.Add(configGrain.GetSystemLLM());
                }
                else if (i % 3 == 1)
                {
                    tasks.Add(configGrain.GetPrompt());
                }
                else
                {
                    tasks.Add(configGrain.GetStreamingModeEnabled());
                }
            }
            
            await Task.WhenAll(tasks);
            sw.Stop();
            
            // Assert - 验证并发执行速度
            sw.ElapsedMilliseconds.ShouldBeLessThan(1000); // 并发执行应该远低于1秒
        }
        
        [Fact]
        public async Task MixedReadWriteOperations_ShouldHandleConcurrencyCorrectly()
        {
            // Arrange
            var configGrain = await _silo.CreateGrainAsync<ConfigurationGAgent>(Guid.NewGuid());
            
            // 设置初始状态
            var field = typeof(ConfigurationGAgent).GetField("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var state = field.GetValue(configGrain) as ConfigurationState;
            state.SystemLLM = "OpenAI";
            state.Prompt = "Initial prompt";
            
            // Act - 模拟混合读写操作
            var sw = Stopwatch.StartNew();
            
            // 启动一个写操作（会花费一些时间）
            var updateTask = configGrain.UpdateSystemPromptAsync("New long prompt that takes time to process...");
            
            // 不等待写操作完成，立即启动一系列只读操作
            await Task.Delay(10); // 给写操作一点时间开始
            var readTasks = new List<Task>();
            
            for (int i = 0; i < 20; i++)
            {
                readTasks.Add(configGrain.GetSystemLLM());
                readTasks.Add(configGrain.GetPrompt());
                readTasks.Add(configGrain.GetStreamingModeEnabled());
            }
            
            // 等待所有读操作完成
            await Task.WhenAll(readTasks);
            var readTime = sw.ElapsedMilliseconds;
            
            // 等待写操作完成
            await updateTask;
            sw.Stop();
            
            // Assert
            // 读操作应该不被写操作阻塞（由于[ReadOnly]和[Reentrant]特性）
            readTime.ShouldBeLessThan(500); // 读操作应该很快完成
            
            // 验证最终状态
            var finalPrompt = await configGrain.GetPrompt();
            finalPrompt.ShouldBe("New long prompt that takes time to process...");
        }
        
        [Fact]
        public async Task EventHandlers_ShouldExecuteReentrant()
        {
            // Arrange
            var configGrain = await _silo.CreateGrainAsync<ConfigurationGAgent>(Guid.NewGuid());
            
            // Act
            var sw = Stopwatch.StartNew();
            
            // 发送多个事件
            var tasks = new List<Task>();
            tasks.Add(configGrain.HandleEventAsync(new SetLLMEvent { LLM = "GPT-4" }));
            tasks.Add(configGrain.HandleEventAsync(new SetPromptEvent { Prompt = "Prompt 1" }));
            tasks.Add(configGrain.HandleEventAsync(new SetStreamingModeEnabledEvent { StreamingModeEnabled = true }));
            tasks.Add(configGrain.HandleEventAsync(new SetLLMEvent { LLM = "Claude" }));
            tasks.Add(configGrain.HandleEventAsync(new SetPromptEvent { Prompt = "Prompt 2" }));
            
            await Task.WhenAll(tasks);
            sw.Stop();
            
            // Assert
            // 由于[Reentrant]特性，这些操作应该能够高效处理
            sw.ElapsedMilliseconds.ShouldBeLessThan(1000);
            
            // 验证最终状态反映了最后的修改
            (await configGrain.GetSystemLLM()).ShouldBe("Claude");
            (await configGrain.GetPrompt()).ShouldBe("Prompt 2");
            (await configGrain.GetStreamingModeEnabled()).ShouldBeTrue();
        }
        
        public void Dispose()
        {
            _silo.Dispose();
        }
    }
} 