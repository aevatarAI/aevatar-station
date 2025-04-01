using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Orleans.TestKit.Tests.Tests;

public class EventSourceTest : TestKitBase
{
    [Fact]
    public async Task ExceptionLogTests()
    {
        Silo.ProtocolServices.ProtocolError("test message", false);
        var exception = Assert.Throws<OrleansException>(() => Silo.ProtocolServices.ProtocolError("test message", true));
        exception.Message.ShouldContain("test message");
        
        // can print error log
        Silo.ProtocolServices.CaughtException("test message", new Exception());
        Silo.ProtocolServices.CaughtUserCodeException("", "", new Exception());
        Silo.ProtocolServices.Log(LogLevel.Debug,"", null);
        
        Silo.ProtocolServices.GrainId.ShouldBe(new GrainId());
        Silo.ProtocolServices.MyClusterId.ShouldBe("Unknown");
    }
}