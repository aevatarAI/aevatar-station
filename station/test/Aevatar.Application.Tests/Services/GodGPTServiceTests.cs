using System;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.Services;
using Aevatar.Application.Grains.Common.Options;
using Aevatar.Common.Options;
using Aevatar.GodGPT.Dtos;
using Aevatar.Service;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Services;

public class GodGPTServiceTests
{
    private readonly Mock<IClusterClient> _clusterClientMock;
    private readonly Mock<ILogger<GodGPTService>> _loggerMock;
    private readonly Mock<IOptionsMonitor<StripeOptions>> _stripeOptionsMock;
    private readonly Mock<IOptionsMonitor<ManagerOptions>> _managerOptionsMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GodGPTService _service;

    public GodGPTServiceTests()
    {
        _clusterClientMock = new Mock<IClusterClient>();
        _loggerMock = new Mock<ILogger<GodGPTService>>();
        _stripeOptionsMock = new Mock<IOptionsMonitor<StripeOptions>>();
        _managerOptionsMock = new Mock<IOptionsMonitor<ManagerOptions>>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        
        // Setup default values for options
        _stripeOptionsMock.Setup(x => x.CurrentValue).Returns(new StripeOptions());
        _managerOptionsMock.Setup(x => x.CurrentValue).Returns(new ManagerOptions());
        
        _service = new GodGPTService(
            _clusterClientMock.Object, 
            _loggerMock.Object, 
            _stripeOptionsMock.Object,
            _managerOptionsMock.Object,
            _localizationServiceMock.Object);
    }

    [Fact]
    public void VerifyGooglePlayTransactionAsync_Should_Have_Correct_Signature()
    {
        // Arrange
        var method = _service.GetType().GetMethod("VerifyGooglePlayTransactionAsync");
        
        // Assert
        method.ShouldNotBeNull("VerifyGooglePlayTransactionAsync method should exist");
        method.ReturnType.ShouldBe(typeof(Task<PaymentVerificationResponseDto>), "Should return Task<PaymentVerificationResponseDto>");
        
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2, "Should have 2 parameters");
        parameters[0].ParameterType.ShouldBe(typeof(Guid), "First parameter should be Guid currentUserId");
        parameters[1].ParameterType.ShouldBe(typeof(GooglePlayTransactionVerificationRequestDto), "Second parameter should be GooglePlayTransactionVerificationRequestDto");
    }

    [Fact]
    public void GodGPTService_Should_Not_Have_Old_GooglePay_Methods()
    {
        // Assert that old methods are removed
        var verifyGooglePayMethod = _service.GetType().GetMethod("VerifyGooglePayAsync");
        var verifyGooglePlayMethod = _service.GetType().GetMethod("VerifyGooglePlayAsync");
        
        // These methods should not exist anymore (they're removed)
        verifyGooglePayMethod.ShouldBeNull("VerifyGooglePayAsync method should be removed");
        verifyGooglePlayMethod.ShouldBeNull("VerifyGooglePlayAsync method should be removed");
    }

    [Fact]
    public void GooglePlayTransactionVerificationRequestDto_Should_Have_Correct_Properties()
    {
        // Arrange
        var dto = new GooglePlayTransactionVerificationRequestDto();
        
        // Assert
        dto.ShouldNotBeNull();
        dto.TransactionIdentifier.ShouldNotBeNull("TransactionIdentifier should be initialized");
        dto.TransactionIdentifier.ShouldBe(string.Empty, "TransactionIdentifier should default to empty string");
        
        // Test property assignment
        dto.TransactionIdentifier = "test-transaction-id";
        dto.TransactionIdentifier.ShouldBe("test-transaction-id");
    }

    [Fact]
    public void PaymentVerificationResponseDto_Should_Have_Correct_Properties()
    {
        // Arrange
        var dto = new PaymentVerificationResponseDto();
        
        // Assert
        dto.ShouldNotBeNull();
        dto.IsValid.ShouldBe(false, "IsValid should default to false");
        dto.Message.ShouldBe(string.Empty, "Message should default to empty string");
        dto.OrderId.ShouldBe(string.Empty, "OrderId should default to empty string");
        dto.ProductId.ShouldBe(string.Empty, "ProductId should default to empty string");
        dto.SubscriptionEndDate.ShouldBeNull("SubscriptionEndDate should be nullable");
        dto.PurchaseTimeMillis.ShouldBe(0L, "PurchaseTimeMillis should default to 0");
        dto.ErrorCode.ShouldBe(string.Empty, "ErrorCode should default to empty string");
        
        // Test property assignments
        dto.IsValid = true;
        dto.Message = "Success";
        dto.OrderId = "order-123";
        dto.ProductId = "product-456";
        dto.SubscriptionEndDate = DateTime.UtcNow;
        dto.PurchaseTimeMillis = 1234567890L;
        dto.ErrorCode = "ERROR_001";
        
        dto.IsValid.ShouldBe(true);
        dto.Message.ShouldBe("Success");
        dto.OrderId.ShouldBe("order-123");
        dto.ProductId.ShouldBe("product-456");
        dto.SubscriptionEndDate.ShouldNotBeNull();
        dto.PurchaseTimeMillis.ShouldBe(1234567890L);
        dto.ErrorCode.ShouldBe("ERROR_001");
    }
}
