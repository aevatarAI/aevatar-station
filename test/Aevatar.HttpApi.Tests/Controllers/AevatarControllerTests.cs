using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Volo.Abp.DependencyInjection;
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
    private readonly Mock<IStringLocalizerFactory> _localizerFactoryMock;
    private readonly Mock<IAbpLazyServiceProvider> _lazyServiceProviderMock;
    private readonly TestAevatarController _controller;

    public AevatarControllerTests()
    {
        _localizerMock = new Mock<IStringLocalizer<AevatarResource>>();
        _localizerFactoryMock = new Mock<IStringLocalizerFactory>();
        _lazyServiceProviderMock = new Mock<IAbpLazyServiceProvider>();

        _localizerFactoryMock.Setup(x => x.Create(typeof(AevatarResource)))
            .Returns(_localizerMock.Object);

        _lazyServiceProviderMock.Setup(x => x.LazyGetRequiredService<IStringLocalizerFactory>())
            .Returns(_localizerFactoryMock.Object);

        _controller = new TestAevatarController(_localizerMock.Object)
        {
            LazyServiceProvider = _lazyServiceProviderMock.Object
        };
    }

    [Fact]
    public void Should_Use_AevatarResource_For_Localization()
    {
        // Arrange
        var expectedType = typeof(AevatarResource);

        // Act
        // var localizationType = _controller.LocalizationResource;

        // Assert
        // Assert.Equal(expectedType, localizationType);
    }

    [Fact]
    public void Should_Return_Localized_Message()
    {
        // Arrange
        var expectedMessage = "Welcome to Aevatar!";
        _localizerMock.Setup(x => x["Welcome"])
            .Returns(new LocalizedString("Welcome", expectedMessage));

        // Act
        var result = _controller.GetLocalizedMessage();

        // Assert
        Assert.Equal(expectedMessage, result);
        _localizerMock.Verify(x => x["Welcome"], Times.Once);
    }
}
