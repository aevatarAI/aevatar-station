using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Interception;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Register the interceptor at module level
[module: Interceptor]

namespace InterceptorDemo
{
    public class Program
    {
        private static ILogger<Program> _logger;

        static async Task Main(string[] args)
        {
            bool useJsonFormat = args.Length > 0 && args[0] == "--json";
            
            // Setup logging - JSON format for ES-friendly output or standard console
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                if (useJsonFormat)
                {
                    // Configure Serilog for JSON structured logging
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Console(new CompactJsonFormatter())
                        .Enrich.FromLogContext()
                        .CreateLogger();
                    
                    builder.AddSerilog(Log.Logger);
                }
                else
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                }
            });
            _logger = loggerFactory.CreateLogger<Program>();

            RequestContext.Set("traceId", "test-trace-id-01");

            if (useJsonFormat)
            {
                Console.WriteLine("=== JSON Structured Logging Demo for Elasticsearch ===\n");
                
                // Only test workflow functionality for cleaner JSON output
                await TestWorkflowInterceptor();
            }
            else
            {
                Console.WriteLine("=== Enhanced Interceptor Method Tracing Demo ===\n");

                // Test basic methods
                var demo = new DemoClass("Test Instance");
                await TestBasicMethods(demo);

                // Test Orleans-like patterns
                var grainDemo = new OrleansGrainDemo();
                await TestOrleansPatterns(grainDemo);

                // Test static and utility methods
                await TestStaticAndUtilityMethods();

                // Test property accessors
                await TestPropertyAccessors(demo);

                // Test event handling
                await TestEventHandling(demo);

                // Test generic methods
                await TestGenericMethods(demo);

                // Test extension methods
                await TestExtensionMethods();

                // Test non-public methods
                await TestNonPublicMethods(demo);

                // Test workflow interceptor functionality
                await TestWorkflowInterceptor();
            }

            Console.WriteLine("\nAll tests completed successfully!");
        }

        private static async Task TestBasicMethods(DemoClass demo)
        {
            Console.WriteLine("=== Testing Basic Methods ===\n");

            // Test synchronous method
            Console.WriteLine("Testing synchronous method...");
            demo.SyncMethod("Hello World");
            Console.WriteLine();

            // Test asynchronous method
            Console.WriteLine("Testing asynchronous method...");
            await demo.AsyncMethod("Async Hello");
            Console.WriteLine();

            // Test method with parameters
            Console.WriteLine("Testing method with parameters...");
            demo.MethodWithParams("Test", 3);
            Console.WriteLine();

            // Test method with return value
            Console.WriteLine("Testing method with return value...");
            var result = demo.MethodWithReturnValue(5, 7);
            Console.WriteLine($"Result: {result}\n");

            // Test exception handling
            Console.WriteLine("Testing exception handling...");
            try
            {
                demo.MethodWithException(null);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Caught expected exception: {ex.Message}");
            }
        }

        private static async Task TestOrleansPatterns(OrleansGrainDemo grainDemo)
        {
            Console.WriteLine("=== Testing Orleans-like Patterns ===\n");

            // Test Orleans lifecycle methods
            Console.WriteLine("Testing Orleans lifecycle methods...");
            await grainDemo.OnActivateAsync(CancellationToken.None);
            // Note: OnDeactivateAsync requires specific Orleans runtime context, skipping for demo
            Console.WriteLine();

            // Test stream processing methods
            Console.WriteLine("Testing stream processing methods...");
            await grainDemo.ProcessStreamEvent("StreamEvent1");
            await grainDemo.ProcessStreamEvent("StreamEvent2");
            Console.WriteLine();

            // Test event handling methods
            Console.WriteLine("Testing event handling methods...");
            await grainDemo.HandleEvent(new TestEvent { Name = "TestEvent1" });
            await grainDemo.HandleEvent(new TestEvent { Name = "TestEvent2" });
            Console.WriteLine();

            // Test Orleans grain methods
            Console.WriteLine("Testing Orleans grain methods...");
            await grainDemo.GetDescriptionAsync();
            await grainDemo.ActivateAsync();
            Console.WriteLine();
        }

        private static async Task TestStaticAndUtilityMethods()
        {
            Console.WriteLine("=== Testing Static and Utility Methods ===\n");

            // Test static methods
            Console.WriteLine("Testing static methods...");
            var staticResult = DemoClass.StaticMethod("Static Test");
            Console.WriteLine($"Static result: {staticResult}");
            Console.WriteLine();

            // Test factory methods
            Console.WriteLine("Testing factory methods...");
            var factoryInstance = DemoClass.CreateInstance("Factory Created");
            Console.WriteLine($"Factory instance: {factoryInstance.Name}");
            Console.WriteLine();

            // Test utility methods
            Console.WriteLine("Testing utility methods...");
            var utilityResult = await DemoClass.UtilityMethodAsync("Utility Test");
            Console.WriteLine($"Utility result: {utilityResult}");
            Console.WriteLine();
        }

        private static async Task TestPropertyAccessors(DemoClass demo)
        {
            Console.WriteLine("=== Testing Property Accessors ===\n");

            // Test property getters
            Console.WriteLine("Testing property getters...");
            var name = demo.Name;
            var count = demo.Count;
            Console.WriteLine($"Name: {name}, Count: {count}");
            Console.WriteLine();

            // Test property setters
            Console.WriteLine("Testing property setters...");
            demo.Name = "Updated Name";
            demo.Count = 42;
            Console.WriteLine($"Updated - Name: {demo.Name}, Count: {demo.Count}");
            Console.WriteLine();

            // Test computed properties
            Console.WriteLine("Testing computed properties...");
            var computed = demo.ComputedValue;
            Console.WriteLine($"Computed value: {computed}");
            Console.WriteLine();
        }

        private static async Task TestEventHandling(DemoClass demo)
        {
            Console.WriteLine("=== Testing Event Handling ===\n");

            // Subscribe to events
            demo.TestEvent += async (sender, e) =>
            {
                Console.WriteLine($"Event handler called: {e.Message}");
                await Task.Delay(10); // Simulate async work
            };

            // Trigger events
            Console.WriteLine("Testing event triggering...");
            demo.TriggerEvent("Event 1");
            demo.TriggerEvent("Event 2");
            Console.WriteLine();
        }

        private static async Task TestGenericMethods(DemoClass demo)
        {
            Console.WriteLine("=== Testing Generic Methods ===\n");

            // Test generic methods with different types
            Console.WriteLine("Testing generic methods...");
            var stringResult = await demo.GenericMethodAsync<string>("String Type");
            var intResult = await demo.GenericMethodAsync<int>(42);
            var doubleResult = await demo.GenericMethodAsync<double>(3.14);
            
            Console.WriteLine($"Generic results - String: {stringResult}, Int: {intResult}, Double: {doubleResult}");
            Console.WriteLine();
        }

        private static async Task TestExtensionMethods()
        {
            Console.WriteLine("=== Testing Extension Methods ===\n");

            // Test extension methods
            Console.WriteLine("Testing extension methods...");
            var demo = new DemoClass("Extension Test");
            var extendedResult = await demo.ExtensionMethodAsync("Extension");
            Console.WriteLine($"Extension method result: {extendedResult}");
            Console.WriteLine();
        }

        private static async Task TestNonPublicMethods(DemoClass demo)
        {
            Console.WriteLine("=== Testing Non-Public Methods ===\n");

            // Test public method calling private method
            Console.WriteLine("Testing public method calling private method...");
            await demo.PublicMethodCallsPrivate();
            Console.WriteLine();

            // Test public method calling protected method
            Console.WriteLine("Testing public method calling protected method...");
            await demo.PublicMethodCallsProtected();
            Console.WriteLine();

            // Test public method calling internal method
            Console.WriteLine("Testing public method calling internal method...");
            await demo.PublicMethodCallsInternal();
            Console.WriteLine();
        }

        private static async Task TestWorkflowInterceptor()
        {
            Console.WriteLine("=== Testing Workflow Interceptor ===\n");

            var workflowDemo = new WorkflowDemo();

            // Test Order Processing workflow
            Console.WriteLine("Testing Order Processing Workflow...");
            await workflowDemo.ProcessOrderWorkflow("ORDER-123", "user@example.com", 99.99m);
            Console.WriteLine();

            // Test User Onboarding workflow
            Console.WriteLine("Testing User Onboarding Workflow...");
            await workflowDemo.UserOnboardingWorkflow("john.doe@example.com", "John Doe");
            Console.WriteLine();

            // Test workflow with exception
            Console.WriteLine("Testing Workflow Exception Handling...");
            try
            {
                await workflowDemo.WorkflowWithException("invalid-data");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught expected workflow exception: {ex.Message}");
            }
            Console.WriteLine();

            // Test nested workflow calls
            Console.WriteLine("Testing Nested Workflow Calls...");
            await workflowDemo.ParentWorkflow("nested-test");
            Console.WriteLine();
        }
    }

    public class DemoClass
    {
        private readonly ILogger<DemoClass> _logger;
        private string _name;
        private int _count;

        // Test constructor
        [Interceptor]
        public DemoClass(string name)
        {
            _name = name;
            _count = 0;
            
            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<DemoClass>();
            
            Console.WriteLine($"Constructor called with name: {name}");
        }

        // Properties with getters/setters
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public int Count
        {
            get => _count;
            set => _count = value;
        }

        public string ComputedValue => $"Computed: {_name} ({_count})";

        // Events
        public event EventHandler<TestEventArgs>? TestEvent;

        [Interceptor]
        public void TriggerEvent(string message)
        {
            TestEvent?.Invoke(this, new TestEventArgs { Message = message });
        }

        // Basic methods
        [Interceptor]
        public void SyncMethod(string input)
        {
            Console.WriteLine($"Processing input: {input}");
            Task.Delay(100).Wait();
            Console.WriteLine($"Result: Processed: {input}");
        }

        [Interceptor]
        public async Task AsyncMethod(string input)
        {
            Console.WriteLine($"Processing input asynchronously: {input}");
            await Task.Delay(200);
            Console.WriteLine($"Result: Async processed: {input}");
        }

        [Interceptor]
        public void MethodWithParams(string input, int count)
        {
            Console.WriteLine($"Processing input: {input}, count: {count}");
            Task.Delay(150).Wait();
            Console.WriteLine($"Result: Processed: {input} x{count}");
        }

        [Interceptor]
        public int MethodWithReturnValue(int a, int b)
        {
            Console.WriteLine($"Calculating {a} + {b} = {a + b}");
            Task.Delay(100).Wait();
            return a + b;
        }

        [Interceptor]
        public void MethodWithException(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be null or empty", nameof(input));
            }

            Console.WriteLine($"Processing input: {input}");
        }

        // Generic methods
        [Interceptor]
        public async Task<T> GenericMethodAsync<T>(T input)
        {
            Console.WriteLine($"Processing generic input of type {typeof(T).Name}: {input}");
            await Task.Delay(50);
            return input;
        }

        // Static methods
        [Interceptor]
        public static string StaticMethod(string input)
        {
            Console.WriteLine($"Static method processing: {input}");
            return $"Static: {input}";
        }

        [Interceptor]
        public static DemoClass CreateInstance(string name)
        {
            Console.WriteLine($"Factory method creating instance with name: {name}");
            return new DemoClass(name);
        }

        [Interceptor]
        public static async Task<string> UtilityMethodAsync(string input)
        {
            Console.WriteLine($"Utility method processing: {input}");
            await Task.Delay(50);
            return $"Utility: {input}";
        }

        // Test methods for non-public method interception
        [Interceptor]
        public async Task PublicMethodCallsPrivate()
        {
            Console.WriteLine("Public method calling private method...");
            await PrivateMethodAsync("Private Test");
        }

        [Interceptor]
        public async Task PublicMethodCallsProtected()
        {
            Console.WriteLine("Public method calling protected method...");
            await ProtectedMethodAsync("Protected Test");
        }

        [Interceptor]
        public async Task PublicMethodCallsInternal()
        {
            Console.WriteLine("Public method calling internal method...");
            await InternalMethodAsync("Internal Test");
        }

        // Private method with interceptor
        [Interceptor]
        private async Task PrivateMethodAsync(string input)
        {
            Console.WriteLine($"Private method processing: {input}");
            await Task.Delay(50);
            Console.WriteLine($"Private method completed: {input}");
        }

        // Protected method with interceptor
        [Interceptor]
        protected async Task ProtectedMethodAsync(string input)
        {
            Console.WriteLine($"Protected method processing: {input}");
            await Task.Delay(50);
            Console.WriteLine($"Protected method completed: {input}");
        }

        // Internal method with interceptor
        [Interceptor]
        internal async Task InternalMethodAsync(string input)
        {
            Console.WriteLine($"Internal method processing: {input}");
            await Task.Delay(50);
            Console.WriteLine($"Internal method completed: {input}");
        }
    }

    // Real Orleans grain demo inheriting from Orleans base
    public class OrleansGrainDemo : Grain, IGrainWithGuidKey
    {
        private readonly ILogger<OrleansGrainDemo> _logger;
        private bool _isActivated = false;

        public OrleansGrainDemo()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<OrleansGrainDemo>();
        }

        // Orleans lifecycle methods
        [Interceptor]
        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Orleans grain activating...");
            await Task.Delay(100);
            _isActivated = true;
            Console.WriteLine("Orleans grain activated");
        }

        [Interceptor]
        public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            Console.WriteLine("Orleans grain deactivating...");
            await Task.Delay(100);
            _isActivated = false;
            Console.WriteLine("Orleans grain deactivated");
        }

        // Stream processing methods
        [Interceptor]
        public async Task ProcessStreamEvent(string eventData)
        {
            Console.WriteLine($"Processing stream event: {eventData}");
            await Task.Delay(50);
            Console.WriteLine($"Stream event processed: {eventData}");
        }

        // Event handling methods
        [Interceptor]
        public async Task HandleEvent(TestEvent testEvent)
        {
            Console.WriteLine($"Handling event: {testEvent.Name}");
            await Task.Delay(50);
            Console.WriteLine($"Event handled: {testEvent.Name}");
        }

        // Orleans grain interface methods
        [Interceptor]
        public async Task<string> GetDescriptionAsync()
        {
            Console.WriteLine("Getting grain description...");
            await Task.Delay(50);
            return "OrleansGrainDemo - Test Grain";
        }

        [Interceptor]
        public async Task ActivateAsync()
        {
            Console.WriteLine("Activating grain...");
            await Task.Delay(50);
            Console.WriteLine("Grain activated");
        }
    }

    // Test event classes
    public class TestEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestEvent
    {
        public string Name { get; set; } = string.Empty;
    }

    // Extension methods
    public static class DemoClassExtensions
    {
        [Interceptor]
        public static async Task<string> ExtensionMethodAsync(this DemoClass demo, string input)
        {
            Console.WriteLine($"Extension method processing: {input}");
            await Task.Delay(50);
            return $"Extension: {input}";
        }
    }

    /// <summary>
    /// Demonstrates workflow interceptor functionality with different workflow scenarios
    /// </summary>
    public class WorkflowDemo
    {
        /// <summary>
        /// Simulates a complete order processing workflow with multiple steps
        /// </summary>
        public async Task ProcessOrderWorkflow(string orderId, string customerEmail, decimal amount)
        {
            Console.WriteLine($"Starting order processing workflow for order: {orderId}");
            
            // Set workflow context for all steps
            await WorkflowContext.ExecuteInWorkflowAsync($"order-proc-{orderId}", "OrderProcessing", async () =>
            {
                // Step 1: Validate Order
                var isValidOrder = await ValidateOrder(orderId, amount);
                if (!isValidOrder) return;
                
                // Step 2: Process Payment
                var paymentResult = await ProcessPayment(orderId, amount, customerEmail);
                if (paymentResult == null) return;
                
                // Step 3: Fulfill Order
                await FulfillOrder(orderId, paymentResult.TransactionId);
            });
            
            Console.WriteLine($"Order processing workflow completed for order: {orderId}");
        }
        
        /// <summary>
        /// Simulates user onboarding workflow
        /// </summary>
        public async Task UserOnboardingWorkflow(string email, string fullName)
        {
            Console.WriteLine($"Starting user onboarding workflow for: {email}");
            
            // Set workflow context for all steps
            var userId = await WorkflowContext.ExecuteInWorkflowAsync($"user-onboard-{email}", "UserOnboarding", async () =>
            {
                // Step 1: Create User Account
                var id = await CreateUserAccount(email, fullName);
                
                // Step 2: Send Welcome Email
                await SendWelcomeEmail(email, fullName);
                
                // Step 3: Setup User Preferences
                await SetupUserPreferences(id);
                
                return id;
            });
            
            Console.WriteLine($"User onboarding workflow completed for: {email}");
        }
        
        /// <summary>
        /// Demonstrates workflow exception handling
        /// </summary>
        public async Task WorkflowWithException(string data)
        {
            Console.WriteLine($"Starting workflow that will throw exception with data: {data}");
            
            await WorkflowContext.ExecuteInWorkflowAsync("data-validation-workflow", "DataProcessing", async () =>
            {
                await ProcessDataWithValidation(data);
            });
        }
        
        /// <summary>
        /// Demonstrates nested workflow calls
        /// </summary>
        public async Task ParentWorkflow(string inputData)
        {
            Console.WriteLine($"Starting parent workflow with data: {inputData}");
            
            await WorkflowContext.ExecuteInWorkflowAsync("nested-parent-workflow", "NestedProcessing", async () =>
            {
                // Call child workflow steps
                await ChildWorkflowStepA(inputData);
                await ChildWorkflowStepB(inputData + "-processed");
            });
            
            Console.WriteLine("Parent workflow completed");
        }
        
        // Order Processing Workflow Steps
        [Interceptor(IsWorkflowStep = true)]
        private async Task<bool> ValidateOrder(string orderId, decimal amount)
        {
            Console.WriteLine($"Validating order {orderId} with amount ${amount}");
            await Task.Delay(100);
            
            if (amount <= 0)
            {
                Console.WriteLine("Invalid order amount");
                return false;
            }
            
            Console.WriteLine("Order validation successful");
            return true;
        }
        
        [Interceptor(IsWorkflowStep = true)]
        private async Task<PaymentResult?> ProcessPayment(string orderId, decimal amount, string customerEmail)
        {
            Console.WriteLine($"Processing payment for order {orderId}: ${amount}");
            await Task.Delay(200);
            
            var result = new PaymentResult
            {
                TransactionId = $"TXN-{Guid.NewGuid().ToString("N")[..8]}",
                Amount = amount,
                Status = "Completed",
                ProcessedAt = DateTime.UtcNow
            };
            
            Console.WriteLine($"Payment processed successfully: {result.TransactionId}");
            return result;
        }
        
        [Interceptor(IsWorkflowStep = true)]
        private async Task FulfillOrder(string orderId, string transactionId)
        {
            Console.WriteLine($"Fulfilling order {orderId} with transaction {transactionId}");
            await Task.Delay(150);
            Console.WriteLine("Order fulfillment completed");
        }
        
        // User Onboarding Workflow Steps
        [Interceptor(IsWorkflowStep = true)]
        private async Task<string> CreateUserAccount(string email, string fullName)
        {
            Console.WriteLine($"Creating user account for {email}");
            await Task.Delay(120);
            
            var userId = $"USER-{Guid.NewGuid().ToString("N")[..8]}";
            Console.WriteLine($"User account created with ID: {userId}");
            return userId;
        }
        
        [Interceptor(IsWorkflowStep = true)]
        private async Task SendWelcomeEmail(string email, string fullName)
        {
            Console.WriteLine($"Sending welcome email to {email}");
            await Task.Delay(80);
            Console.WriteLine("Welcome email sent successfully");
        }
        
        [Interceptor(IsWorkflowStep = true)]
        private async Task SetupUserPreferences(string userId)
        {
            Console.WriteLine($"Setting up preferences for user {userId}");
            await Task.Delay(60);
            Console.WriteLine("User preferences configured");
        }
        
        // Exception Handling Workflow
        [Interceptor(IsWorkflowStep = true)]
        private async Task ProcessDataWithValidation(string data)
        {
            Console.WriteLine($"Processing data: {data}");
            await Task.Delay(50);
            
            if (data == "invalid-data")
            {
                throw new InvalidOperationException("Data validation failed: invalid format detected");
            }
            
            Console.WriteLine("Data processed successfully");
        }
        
        // Nested Workflow Steps
        [Interceptor(IsWorkflowStep = true)]
        private async Task ChildWorkflowStepA(string inputData)
        {
            Console.WriteLine($"Executing child workflow step A with: {inputData}");
            await Task.Delay(75);
            Console.WriteLine("Child workflow step A completed");
        }
        
        [Interceptor(IsWorkflowStep = true)]
        private async Task ChildWorkflowStepB(string processedData)
        {
            Console.WriteLine($"Executing child workflow step B with: {processedData}");
            await Task.Delay(90);
            Console.WriteLine("Child workflow step B completed");
        }
    }

    /// <summary>
    /// Data models for workflow demo
    /// </summary>
    public class PaymentResult
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
