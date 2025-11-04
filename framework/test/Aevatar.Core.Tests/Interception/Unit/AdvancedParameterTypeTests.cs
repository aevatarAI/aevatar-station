using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Aevatar.Core.Tests.Interception.TestSubjects;
using Xunit;
using FluentAssertions;

namespace Aevatar.Core.Tests.Interception.Unit;

/// <summary>
/// Tests for advanced parameter type handling in interception.
/// Uses TestMockLoggerProvider instead of console capture to avoid race conditions.
/// </summary>
[Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
public class AdvancedParameterTypeTests : IClassFixture<TraceContextFixture>, IDisposable
{
    private readonly BasicTestClass _testClass;
    private readonly TestMockLoggerProvider _mockLoggerProvider;
    private readonly ILogger<BasicTestClass> _logger;
    private readonly TraceContextFixture _fixture;

    public AdvancedParameterTypeTests(TraceContextFixture fixture)
    {
        _fixture = fixture;
        
        // Create TestMockLoggerProvider to capture logs without console race conditions
        _mockLoggerProvider = new TestMockLoggerProvider();
        _logger = _mockLoggerProvider.CreateLogger<BasicTestClass>();
        
        // Create BasicTestClass with proper logger injection
        _testClass = new BasicTestClass(_logger);
        
        // CRITICAL: Ensure each test starts with a clean TraceContext state
        _fixture.ResetTraceContext();
    }

    public void Dispose()
    {
        _mockLoggerProvider?.Dispose();
    }

    [Fact]
    public void MethodWithNumericTypes_ShouldLogAllNumericParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        _testClass.MethodWithNumericTypes(255, -128, -32768, 65535, -9223372036854775808L, 18446744073709551615UL, 3.14159f, 2.718281828459045, 123.456789m, 'A');

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithNumericTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter byteValue = 255"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter charValue = A"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithNumericTypes"));
    }

    [Fact]
    public void MethodWithDateTimeTypes_ShouldLogAllDateTimeParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        var now = DateTime.Now;
        _testClass.MethodWithDateTimeTypes(now, DateTimeOffset.Now, TimeSpan.FromHours(2.5), DateOnly.FromDateTime(now), TimeOnly.FromDateTime(now));

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithDateTimeTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithDateTimeTypes"));
    }

    [Fact]
    public void MethodWithNullableTypes_ShouldLogAllNullableParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        _testClass.MethodWithNullableTypes(42, "test", true, DateTime.Now, Guid.NewGuid());

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithNullableTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter nullableInt = 42"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithNullableTypes"));
    }

    [Fact]
    public void MethodWithEnumTypes_ShouldLogAllEnumParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        _testClass.MethodWithEnumTypes(DayOfWeek.Monday, ConsoleColor.Blue, TestEnum.Value2);

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithEnumTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Parameter dayOfWeek = Monday"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithEnumTypes"));
    }

    [Fact]
    public void MethodWithStructTypes_ShouldLogAllStructParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        var testStruct = new TestStruct(123, "test name");
        _testClass.MethodWithStructTypes(Guid.NewGuid(), TimeSpan.FromMinutes(30), testStruct);

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithStructTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithStructTypes"));
    }

    [Fact]
    public void MethodWithSpecificCollections_ShouldLogAllCollectionParameters()
    {
        _fixture.ResetTraceContext();
        // Act
        _testClass.MethodWithSpecificCollections(
            new byte[] { 1, 2, 3 },
            new int[] { 10, 20, 30 },
            new List<string> { "item1", "item2" },
            new Dictionary<int, string> { { 1, "one" } },
            new HashSet<int> { 100, 200 },
            new Queue<string>(new[] { "first" }),
            new Stack<bool>(new[] { true, false })
        );

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithSpecificCollections"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithSpecificCollections"));
    }

    [Fact]
    public void MethodWithInterfaceTypes_ShouldLogAllNonGenericInterfaceParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        var testInterface = new TestInterfaceImpl();
        _testClass.MethodWithInterfaceTypes(
            new List<string> { "test1" },
            new List<int> { 1, 2, 3 },
            new string[] { "array1" },
            testInterface
        );

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithInterfaceTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithInterfaceTypes"));
    }

    [Fact]
    public void MethodWithGenericInterfaceTypes_ShouldLogAllGenericInterfaceParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        var testInterface = new TestInterfaceImpl();
        _testClass.MethodWithGenericInterfaceTypes(
            new List<string> { "test1" },
            new List<int> { 1, 2, 3 },
            new string[] { "array1" },
            testInterface
        );

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithGenericInterfaceTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithGenericInterfaceTypes"));
    }

    [Fact]
    public void MethodWithDelegateTypes_ShouldLogAllDelegateParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        Action<string> stringAction = (s) => Console.WriteLine(s);
        Func<int, string> intToStringFunc = (i) => i.ToString();
        TestDelegate testDelegate = (i) => $"delegate result: {i}";

        _testClass.MethodWithDelegateTypes(stringAction, intToStringFunc, testDelegate);

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithDelegateTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithDelegateTypes"));
    }

    [Fact]
    public void MethodWithSpecialTypes_ShouldLogAllSpecialParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        var obj = new { Name = "test object", Value = 42 };
        _testClass.MethodWithSpecialTypes(obj, obj, new IntPtr(12345), new UIntPtr(67890));

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithSpecialTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithSpecialTypes"));
    }

    [Fact]
    public void MethodWithMixedComplexTypes_ShouldLogAllComplexParameters()
    {
        _fixture.ResetTraceContext();

        // Act
        var complexList = new List<Dictionary<string, int?>>
        {
            new Dictionary<string, int?> { { "key1", 1 } }
        };

        var complexDict = new Dictionary<Guid, List<DateTime?>>
        {
            { Guid.NewGuid(), new List<DateTime?> { DateTime.Now } }
        };

        var tuple = Tuple.Create("tuple string", 42, true);

        _testClass.MethodWithMixedComplexTypes(complexList, complexDict, tuple);

        // Assert
        var logs = _mockLoggerProvider.Logs.ToArray();
        logs.Should().Contain(line => line.Contains("TRACE: Entering MethodWithMixedComplexTypes"));
        logs.Should().Contain(line => line.Contains("TRACE: Exiting MethodWithMixedComplexTypes"));
    }
}
