// using Aevatar.Service;
// using Aevatar.Subscription;
// using Microsoft.Extensions.Logging;
// using Moq;
// using System;
// using System.Collections.Generic;
// using Xunit;
//
// namespace Aevatar.HttpApi.Tests.Controllers
// {
//     public class SubscriptionControllerTests
//     {
//         private readonly SubscriptionAppService _subscriptionAppServiceMock;
//         private readonly Mock<ILogger<SubscriptionController>> _loggerMock;
//         private readonly SubscriptionController _controller;
//
//         public SubscriptionControllerTests()
//         {
//             _subscriptionAppServiceMock = Mock.Of<SubscriptionAppService>(mock =>
//                 mock.GetAvailableEventsAsync(It.IsAny<Guid>()) == Task.FromResult(new List<EventDescriptionDto>()) &&
//                 mock.SubscribeAsync(It.IsAny<CreateSubscriptionDto>()) == Task.FromResult(new SubscriptionDto()) &&
//                 mock.GetSubscriptionAsync(It.IsAny<Guid>()) == Task.FromResult(new SubscriptionDto())
//             );
//
//             _loggerMock = new Mock<ILogger<SubscriptionController>>();
//             _controller = new SubscriptionController(_subscriptionAppServiceMock, _loggerMock.Object);
//         }
//
//         [Fact]
//         public async Task GetAvailableEvents_ShouldReturnEvents()
//         {
//             // Arrange
//             var guid = Guid.NewGuid();
//             var expectedEvents = new List<EventDescriptionDto>();
//
//             Mock.Get(_subscriptionAppServiceMock)
//                 .Setup(x => x.GetAvailableEventsAsync(guid))
//                 .ReturnsAsync(expectedEvents);
//
//             // Act
//             var result = await _controller.GetAvailableEventsAsync(guid);
//
//             // Assert
//             Assert.Equal(expectedEvents, result);
//             Mock.Get(_subscriptionAppServiceMock).Verify(x => x.GetAvailableEventsAsync(guid), Times.Once);
//         }
//
//         [Fact]
//         public async Task Subscribe_ShouldCreateSubscription()
//         {
//             // Arrange
//             var input = new CreateSubscriptionDto();
//             var expectedSubscription = new SubscriptionDto();
//
//             Mock.Get(_subscriptionAppServiceMock)
//                 .Setup(x => x.SubscribeAsync(input))
//                 .ReturnsAsync(expectedSubscription);
//
//             // Act
//             var result = await _controller.SubscribeAsync(input);
//
//             // Assert
//             Assert.Equal(expectedSubscription, result);
//             Mock.Get(_subscriptionAppServiceMock).Verify(x => x.SubscribeAsync(input), Times.Once);
//         }
//
//         [Fact]
//         public async Task CancelSubscription_ShouldCancelSubscription()
//         {
//             // Arrange
//             var subscriptionId = Guid.NewGuid();
//
//             // Act
//             await _controller.CancelSubscriptionAsync(subscriptionId);
//
//             // Assert
//             Mock.Get(_subscriptionAppServiceMock).Verify(x => x.CancelSubscriptionAsync(subscriptionId), Times.Once);
//         }
//
//     }
// }