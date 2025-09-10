using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Aevatar.Core.Tests.Interception.TestSubjects;

namespace Aevatar.Core.Tests.Interception.Unit
{
    /// <summary>
    /// Tests for generic type handling in interception.
    /// Uses TestMockLoggerProvider instead of console capture to avoid race conditions.
    /// </summary>
    [Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
    public class GenericTypeTests : IClassFixture<TraceContextFixture>, IDisposable
    {
        private readonly BasicTestClass _testClass;
        private readonly TestMockLoggerProvider _mockLoggerProvider;
        private readonly ILogger<BasicTestClass> _logger;
        private readonly TraceContextFixture _fixture;

        public GenericTypeTests(TraceContextFixture fixture)
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
        public void GenericMethodWithClassConstraint_ShouldWorkWithReferenceTypes()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithClassConstraint("test string");

            // Assert
            result.Should().Be("test string");
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithClassConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test string"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithClassConstraint"));
        }

        [Fact]
        public void GenericMethodWithClassConstraint_ShouldWorkWithCustomObjects()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var customObj = new { Name = "test", Value = 42 };
            var result = _testClass.GenericMethodWithClassConstraint(customObj);

            // Assert
            result.Should().Be(customObj);
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithClassConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = "));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithClassConstraint"));
        }

        [Fact]
        public void GenericMethodWithComparableConstraint_ShouldWorkWithComparableTypes()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithComparableConstraint(42);

            // Assert
            result.Should().Be(42);
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithComparableConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = 42"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithComparableConstraint"));
        }

        [Fact]
        public void GenericMethodWithComparableConstraint_ShouldWorkWithStrings()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithComparableConstraint("test");

            // Assert
            result.Should().Be("test");
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithComparableConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithComparableConstraint"));
        }

        [Fact]
        public void GenericMethodWithNewConstraint_ShouldCreateNewInstance()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithNewConstraint<object>();

            // Assert
            result.Should().NotBeNull();
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithNewConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithNewConstraint"));
        }

        [Fact]
        public void GenericMethodWithMultipleTypes_ShouldWorkWithDifferentTypes()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithMultipleTypes("key", 42);

            // Assert
            result.Should().Be(42);
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithMultipleTypes"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter key = key"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter value = 42"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithMultipleTypes"));
        }

        [Fact]
        public void GenericMethodWithMultipleTypes_ShouldWorkWithComplexTypes()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithMultipleTypes(DateTime.Now, new List<string> { "item1", "item2" });

            // Assert
            result.Should().NotBeNull();
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithMultipleTypes"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithMultipleTypes"));
        }

        [Fact]
        public void GenericMethodWithBaseClassConstraint_ShouldWorkWithDerivedTypes()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var derived = new DerivedTestClass(_logger);
            var result = _testClass.GenericMethodWithBaseClassConstraint(derived);

            // Assert
            result.Should().Be(derived);
            result.AdditionalProperty.Should().Be("derived");
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithBaseClassConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithBaseClassConstraint"));
        }

        [Fact]
        public void GenericMethodWithInterfaceConstraint_ShouldWorkWithDisposableTypes()
        {
            _fixture.ResetTraceContext();
        
        // Act
            using var disposable = new TestDisposable();
            var result = _testClass.GenericMethodWithInterfaceConstraint(disposable);

            // Assert
            result.Should().Be(disposable);
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithInterfaceConstraint"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithInterfaceConstraint"));
        }

        [Fact]
        public void GenericMethodWithDefaultValue_ShouldWorkWithExplicitValue()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithDefaultValue("explicit value");

            // Assert
            result.Should().Be("explicit value");
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithDefaultValue"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = explicit value"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithDefaultValue"));
        }

        [Fact]
        public void GenericMethodWithDefaultValue_ShouldWorkWithDefaultValue()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodWithDefaultValue<string>();

            // Assert
            result.Should().BeNull(); // default(string) is null
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithDefaultValue"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithDefaultValue"));
        }

        [Fact]
        public void GenericMethodWithCollection_ShouldWorkWithList()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var list = new List<string> { "item1", "item2", "item3" };
            var result = _testClass.GenericMethodWithCollection(list);

            // Assert
            result.Should().Be("item1"); // FirstOrDefault returns first item
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithCollection"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithCollection"));
        }

        [Fact]
        public void GenericMethodWithCollection_ShouldWorkWithEmptyCollection()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var emptyList = new List<int>();
            var result = _testClass.GenericMethodWithCollection(emptyList);

            // Assert
            result.Should().Be(0); // default(int) is 0
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithCollection"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithCollection"));
        }

        [Fact]
        public void GenericMethodWithArray_ShouldWorkWithStringArray()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var array = new[] { "first", "second", "third" };
            var result = _testClass.GenericMethodWithArray(array);

            // Assert
            result.Should().Be("first");
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithArray"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithArray"));
        }

        [Fact]
        public void GenericMethodWithArray_ShouldWorkWithEmptyArray()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var emptyArray = new int[0];
            var result = _testClass.GenericMethodWithArray(emptyArray);

            // Assert
            result.Should().Be(0); // default(int) is 0
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithArray"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithArray"));
        }

        [Fact]
        public void GenericMethodReturningCollection_ShouldReturnListWithInput()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var result = _testClass.GenericMethodReturningCollection("test item");

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().Be("test item");
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodReturningCollection"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter input = test item"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodReturningCollection"));
        }

        [Fact]
        public void GenericMethodReturningDictionary_ShouldReturnDictionaryWithKeyValue()
        {
            _fixture.ResetTraceContext();
            
        // Act
            var result = _testClass.GenericMethodReturningDictionary("key1", 42);

            // Assert
            result.Should().HaveCount(1);
            result["key1"].Should().Be(42);
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodReturningDictionary"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter key = key1"));
            logs.Should().Contain(line => line.Contains("TRACE: Parameter value = 42"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodReturningDictionary"));
        }

        [Fact]
        public void GenericMethodWithComplexGenericTypes_ShouldWorkWithNestedGenerics()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var complexList = new List<Dictionary<string, int>>
            {
                new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
                new Dictionary<string, int> { { "c", 3 } }
            };
            var result = _testClass.GenericMethodWithCollection(complexList);

            // Assert
            result.Should().NotBeNull();
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethodWithCollection"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethodWithCollection"));
        }

        [Fact]
        public void GenericMethodWithTuple_ShouldWorkWithValueTuple()
        {
            _fixture.ResetTraceContext();
        
        // Act
            var tuple = (Name: "test", Value: 42, Flag: true);
            var result = _testClass.GenericMethod(tuple);

            // Assert
            result.Should().Be(tuple);
            result.Name.Should().Be("test");
            result.Value.Should().Be(42);
            result.Flag.Should().BeTrue();
            var logs = _mockLoggerProvider.Logs.ToArray();
            logs.Should().Contain(line => line.Contains("TRACE: Entering GenericMethod"));
            logs.Should().Contain(line => line.Contains("TRACE: Exiting GenericMethod"));
        }

        private class TestDisposable : IDisposable
        {
            public void Dispose()
            {
                // Test implementation
            }
        }
    }
}
