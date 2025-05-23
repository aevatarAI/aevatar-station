using Aevatar.Core.Abstractions;
using Aevatar.SignalR.GAgents;
using Moq;
using Moq.Language.Flow;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace Aevatar.SignalR.Tests.Extensions
{
    public static class MockExtensions
    {
        public static Mock<IGAgent> SetupRegisterAsync(this Mock<IGAgent> mock, Func<IGAgent, Task> callback)
        {
            // Setup a callback handler for when RegisterAsync is called
            mock.Setup(m => m.RegisterAsync(It.IsAny<IGAgent>()))
                .Callback<IGAgent>(agent => callback(agent).Wait())
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static Mock<ISignalRGAgent> SetupRegisterAsync(this Mock<ISignalRGAgent> mock, Func<IGAgent, Task> callback)
        {
            // Setup a callback handler for when RegisterAsync is called
            mock.Setup(m => m.RegisterAsync(It.IsAny<IGAgent>()))
                .Callback<IGAgent>(agent => callback(agent).Wait())
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static Mock<ISignalRGAgent> SetupPublishEventAsync(this Mock<ISignalRGAgent> mock, Func<EventBase, string, Task> callback)
        {
            // Setup a callback handler for when PublishEventAsync is called
            mock.Setup(m => m.PublishEventAsync(It.IsAny<EventBase>(), It.IsAny<string>()))
                .Callback<EventBase, string>((evt, connId) => callback(evt, connId).Wait())
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static Mock<ISignalRGAgent> SetupAddConnectionIdAsync(this Mock<ISignalRGAgent> mock, Func<string, bool, Task> callback)
        {
            // Setup a callback handler for when AddConnectionIdAsync is called
            mock.Setup(m => m.AddConnectionIdAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, bool>((connId, fireAndForget) => callback(connId, fireAndForget).Wait())
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static Mock<ISignalRGAgent> SetupRemoveConnectionIdAsync(this Mock<ISignalRGAgent> mock, Func<string, Task> callback)
        {
            // Setup a callback handler for when RemoveConnectionIdAsync is called
            mock.Setup(m => m.RemoveConnectionIdAsync(It.IsAny<string>()))
                .Callback<string>(connId => callback(connId).Wait())
                .Returns(Task.CompletedTask);

            return mock;
        }
    }
} 