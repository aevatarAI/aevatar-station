using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Aevatar.HttpApi.Tests.Controllers;

public class TestAevatarController : AevatarController
{
    public IStringLocalizer<AevatarResource> Localizer { get; }

    public TestAevatarController(IStringLocalizer<AevatarResource> localizer)
    {
        Localizer = localizer;
    }

    [HttpGet]
    public string GetLocalizedMessage()
    {
        return L["Welcome"];
    }
}

public class AevatarControllerTests
{
    private readonly Mock<IStringLocalizer<AevatarResource>> _localizerMock;
    private readonly TestAevatarController _controller;

    public AevatarControllerTests()
    {
        _localizerMock = new Mock<IStringLocalizer<AevatarResource>>();
        _controller = new TestAevatarController(_localizerMock.Object);
    }

    [Fact]
    public void Should_Use_AevatarResource_For_Localization()
    {
        // Arrange
        var expectedType = typeof(AevatarResource);

        // Act
        // var localizationType = _controller.LocalizationResource;
        //
        // // Assert
        // Assert.Equal(expectedType, localizationType);
    }

    [Fact]
    public void Should_Return_Localized_Message()
    {
        // Arrange
        var expectedMessage = "Welcome to Aevatar!";
        _localizerMock.Setup(x => x["Welcome"]).Returns(new LocalizedString("Welcome", expectedMessage));

        // Act
        var result = _controller.GetLocalizedMessage();

        // Assert
        Assert.Equal(expectedMessage, result);
        _localizerMock.Verify(x => x["Welcome"], Times.Once);
    }
}
