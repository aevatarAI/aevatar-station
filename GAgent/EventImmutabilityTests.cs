using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.ChatManager;
using Orleans.TestKit;
using Shouldly;
using Xunit;
using Orleans.Concurrency;
using Orleans.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Application.Tests.GAgent
{
    public class EventImmutabilityTests : AevatarApplicationTestBase, IDisposable
    {
        private readonly TestKitSilo _silo;
        private readonly Serializer _serializer;

        public EventImmutabilityTests()
        {
            _silo = new TestKitSilo();
            
            // 获取Orleans序列化器
            _serializer = _silo.ServiceProvider.GetRequiredService<Serializer>();
        }
        
        [Fact]
        public void EventClasses_ShouldHaveImmutableAttribute()
        {
            // Arrange - 检查事件类是否已应用[Immutable]特性
            var eventTypes = new Type[]
            {
                typeof(RequestCreateGodChatEvent),
                typeof(ResponseCreateGod),
                typeof(RequestGodChatEvent),
                typeof(ResponseGodChat),
                typeof(RequestStreamGodChatEvent),
                typeof(ResponseStreamGodChat),
                typeof(RequestGodSessionListEvent),
                typeof(ResponseGodSessionList),
                typeof(RequestSessionChatHistoryEvent),
                typeof(ResponseSessionChatHistory),
                typeof(RequestDeleteSessionEvent),
                typeof(ResponseDeleteSession),
                typeof(RequestRenameSessionEvent),
                typeof(ResponseRenameSession),
                typeof(RequestClearAllEvent),
                typeof(ResponseClearAll)
            };
            
            // Act & Assert
            foreach (var eventType in eventTypes)
            {
                var immutableAttribute = eventType.GetCustomAttributes(typeof(Orleans.Concurrency.ImmutableAttribute), false);
                immutableAttribute.ShouldNotBeEmpty($"{eventType.Name} should have [Immutable] attribute");
            }
        }
        
        // ... 其余代码保持不变 ...
    }
} 