using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Interception;

namespace Aevatar.Core.Tests.Interception.TestSubjects
{
    /// <summary>
    /// Basic test class for testing fundamental interception scenarios
    /// </summary>
    public class BasicTestClass
    {
        private readonly ILogger<BasicTestClass> _logger;

        public BasicTestClass(ILogger<BasicTestClass> logger)
        {
            _logger = logger;
        }

        public ILogger<BasicTestClass> Logger => _logger;

        [Interceptor]
        public void VoidMethod()
        {
            // Simple void method
        }

        [Interceptor]
        public string MethodWithReturnValue()
        {
            return "test result";
        }

        [Interceptor]
        public void MethodWithParameters(string input, int count, bool flag)
        {
            // Method with multiple parameters
        }

        [Interceptor]
        public async Task AsyncVoidMethod()
        {
            await Task.Delay(10);
        }

        [Interceptor]
        public async Task<string> AsyncMethodWithReturnValue()
        {
            await Task.Delay(10);
            return "async result";
        }

        [Interceptor]
        public async Task<T> GenericAsyncMethod<T>(T input)
        {
            await Task.Delay(10);
            return input;
        }

        [Interceptor]
        public void MethodThatThrowsException()
        {
            throw new InvalidOperationException("Test exception");
        }

        [Interceptor]
        public async Task AsyncMethodThatThrowsException()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Async test exception");
        }

        [Interceptor]
        public async Task AsyncMethodThatCanBeCanceled(CancellationToken cancellationToken)
        {
            // Use a longer delay and check cancellation token periodically
            for (int i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
            }
            return;
        }

        [Interceptor]
        private void PrivateMethod()
        {
            // Private method to test access level interception
        }

        [Interceptor]
        protected void ProtectedMethod()
        {
            // Protected method to test access level interception
        }

        [Interceptor]
        internal void InternalMethod()
        {
            // Internal method to test access level interception
        }

        public void PublicMethodCallingPrivate()
        {
            PrivateMethod();
        }

        public void PublicMethodCallingProtected()
        {
            ProtectedMethod();
        }

        public void PublicMethodCallingInternal()
        {
            InternalMethod();
        }

        [Interceptor]
        public static string StaticMethod(string input)
        {
            return $"Static: {input}";
        }

        [Interceptor]
        public T GenericMethod<T>(T input)
        {
            return input;
        }

        [Interceptor]
        public T GenericMethodWithClassConstraint<T>(T input) where T : class
        {
            return input;
        }

        [Interceptor]
        public T GenericMethodWithComparableConstraint<T>(T input) where T : IComparable
        {
            return input;
        }

        [Interceptor]
        public T GenericMethodWithNewConstraint<T>() where T : new()
        {
            return new T();
        }

        [Interceptor]
        public TValue GenericMethodWithMultipleTypes<TKey, TValue>(TKey key, TValue value)
        {
            return value;
        }

        [Interceptor]
        public T GenericMethodWithBaseClassConstraint<T>(T input) where T : BasicTestClass
        {
            return input;
        }

        [Interceptor]
        public T GenericMethodWithInterfaceConstraint<T>(T input) where T : IDisposable
        {
            return input;
        }

        [Interceptor]
        public T GenericMethodWithDefaultValue<T>(T input = default(T))
        {
            return input;
        }

        [Interceptor]
        public T GenericMethodWithCollection<T>(IEnumerable<T> items)
        {
            return items.FirstOrDefault();
        }

        [Interceptor]
        public T GenericMethodWithArray<T>(T[] array)
        {
            return array.Length > 0 ? array[0] : default(T);
        }

        [Interceptor]
        public List<T> GenericMethodReturningCollection<T>(T input)
        {
            return new List<T> { input };
        }

        [Interceptor]
        public Dictionary<TKey, TValue> GenericMethodReturningDictionary<TKey, TValue>(TKey key, TValue value)
        {
            return new Dictionary<TKey, TValue> { { key, value } };
        }

        // Numeric types
        [Interceptor]
        public void MethodWithNumericTypes(byte byteValue, sbyte sbyteValue, short shortValue, ushort ushortValue, 
            long longValue, ulong ulongValue, float floatValue, double doubleValue, decimal decimalValue, char charValue)
        {
            // Method to test various numeric parameter types
        }

        // Date/Time types
        [Interceptor]
        public void MethodWithDateTimeTypes(DateTime dateTime, DateTimeOffset dateTimeOffset, TimeSpan timeSpan, 
            DateOnly dateOnly, TimeOnly timeOnly)
        {
            // Method to test date/time parameter types
        }

        // Nullable types
        [Interceptor]
        public void MethodWithNullableTypes(int? nullableInt, string? nullableString, bool? nullableBool, 
            DateTime? nullableDateTime, Guid? nullableGuid)
        {
            // Method to test nullable parameter types
        }

        // Enum types
        [Interceptor]
        public void MethodWithEnumTypes(DayOfWeek dayOfWeek, ConsoleColor consoleColor, TestEnum testEnum)
        {
            // Method to test enum parameter types
        }

        // Struct types
        [Interceptor]
        public void MethodWithStructTypes(Guid guid, TimeSpan timeSpan, TestStruct testStruct)
        {
            // Method to test struct parameter types
        }

        // Collection types (specific)
        [Interceptor]
        public void MethodWithSpecificCollections(byte[] byteArray, int[] intArray, List<string> stringList, 
            Dictionary<int, string> intStringDict, HashSet<int> intHashSet, Queue<string> stringQueue, Stack<bool> boolStack)
        {
            // Method to test specific collection parameter types
        }

        // Interface types (non-generic base interfaces)
        [Interceptor]
        public void MethodWithInterfaceTypes(System.Collections.IEnumerable nonGenericEnumerable, System.Collections.ICollection nonGenericCollection, 
            System.Collections.IList nonGenericList, ITestInterface testInterface)
        {
            // Method to test non-generic interface parameter types
        }

        // Interface types (generic)
        [Interceptor]
        public void MethodWithGenericInterfaceTypes(IEnumerable<string> genericEnumerable, ICollection<int> genericCollection, 
            IList<string> genericList, ITestInterface testInterface)
        {
            // Method to test generic interface parameter types
        }

        // Delegate types
        [Interceptor]
        public void MethodWithDelegateTypes(Action<string> stringAction, Func<int, string> intToStringFunc, 
            TestDelegate testDelegate)
        {
            // Method to test delegate parameter types
        }

        // Special .NET types
        [Interceptor]
        public void MethodWithSpecialTypes(object obj, dynamic dynamicObj, IntPtr intPtr, UIntPtr uintPtr)
        {
            // Method to test special .NET parameter types
        }

        // Mixed complex types
        [Interceptor]
        public void MethodWithMixedComplexTypes(List<Dictionary<string, int?>> complexList, 
            Dictionary<Guid, List<DateTime?>> complexDict, Tuple<string, int, bool> tuple)
        {
            // Method to test complex mixed parameter types
        }
    }

    // Test enum for enum parameter testing
    public enum TestEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    // Test struct for struct parameter testing
    public struct TestStruct
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public TestStruct(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    // Test interface for interface parameter testing
    public interface ITestInterface
    {
        string GetValue();
    }

    // Test class implementing the interface
    public class TestInterfaceImpl : ITestInterface
    {
        public string GetValue() => "test value";
    }

    // Test delegate for delegate parameter testing
    public delegate string TestDelegate(int input);

    /// <summary>
    /// Test class for base class constraint testing
    /// </summary>
    public class DerivedTestClass : BasicTestClass
    {
        public DerivedTestClass(ILogger<BasicTestClass> logger) : base(logger)
        {
        }
        
        public string AdditionalProperty { get; set; } = "derived";
    }

    /// <summary>
    /// Test class with constructor interception
    /// </summary>
    public class ConstructorTestClass
    {
        public string Name { get; }
        public int Value { get; }

        [Interceptor]
        public ConstructorTestClass(string name)
        {
            Name = name;
        }

        [Interceptor]
        public ConstructorTestClass(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>
    /// Extension methods for testing
    /// </summary>
    public static class TestExtensions
    {
        [Interceptor]
        public static string ExtensionMethod(this BasicTestClass obj, string input)
        {
            return $"Extended: {input}";
        }

        [Interceptor]
        public static async Task<string> AsyncExtensionMethod(this BasicTestClass obj, string input)
        {
            await Task.Delay(10);
            return $"Async Extended: {input}";
        }
    }
}
