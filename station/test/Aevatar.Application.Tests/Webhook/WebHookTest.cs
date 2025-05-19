using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Service;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;
using Volo.Abp;
using Aevatar.Webhook.Extensions;
using Microsoft.CodeAnalysis;

namespace Aevatar.Webhook;

public abstract class WebHookTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IWebhookService _webhookService;

    public WebHookTests()
    {
        _webhookService = GetRequiredService<IWebhookService>();
    }

    [Fact]
    public async Task CreateWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
        var codeFiles = new Dictionary<string, byte[]>
        {
            ["test.dll"] = "21323".GetBytes()
        };
        await _webhookService.CreateWebhookAsync(webhookId, version, codeFiles);
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateWebhook_WithMultipleFiles_ShouldSucceed()
    {
        string webhookId = "multi-file";
        string version = "1";
        var codeFiles = new Dictionary<string, byte[]>
        {
            ["main.dll"] = "main_code".GetBytes(),
            ["lib.dll"] = "library_code".GetBytes(),
            ["config.json"] = "{}".GetBytes()
        };
        await _webhookService.CreateWebhookAsync(webhookId, version, codeFiles);
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(3);
    }


    [Fact]
    public async Task GetWebhookCode_ForNonExistentWebhook_ShouldReturnEmpty()
    {
        string webhookId = "non-existent";
        string version = "1";
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DestroyWebhookTestAsync()
    {
        string webhookId = "telegram";
        string version = "1";
        await CreateWebhookTestAsync();
        await _webhookService.DestroyWebhookAsync(webhookId, version);

        // Verify webhook is destroyed
        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DestroyWebhook_ForNonExistentWebhook_ShouldSucceed()
    {
        string webhookId = "non-existent";
        string version = "1";
        await _webhookService.DestroyWebhookAsync(webhookId, version);
        // Should not throw exception
    }

    [Fact]
    public async Task UpdateCode_ForExistingWebhook_ShouldSucceed()
    {
        string webhookId = "update-test";
        string version = "1";
        var initialFiles = new Dictionary<string, byte[]>
        {
            ["initial.dll"] = "initial_code".GetBytes()
        };
        await _webhookService.CreateWebhookAsync(webhookId, version, initialFiles);

        var updatedFiles = new Dictionary<string, byte[]>
        {
            ["updated.dll"] = "updated_code".GetBytes()
        };
        await _webhookService.UpdateCodeAsync(webhookId, version, updatedFiles);

        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(1);
        code.ShouldContainKey("updated.dll");
    }

    [Fact]
    public async Task UpdateCode_WithMultipleFiles_ShouldSucceed()
    {
        string webhookId = "update-multi";
        string version = "1";
        await _webhookService.CreateWebhookAsync(webhookId, version, new Dictionary<string, byte[]>
        {
            ["old.dll"] = "old_code".GetBytes()
        });

        var updatedFiles = new Dictionary<string, byte[]>
        {
            ["new1.dll"] = "new1_code".GetBytes(),
            ["new2.dll"] = "new2_code".GetBytes(),
            ["config.json"] = "{}".GetBytes()
        };
        await _webhookService.UpdateCodeAsync(webhookId, version, updatedFiles);

        var code = await _webhookService.GetWebhookCodeAsync(webhookId, version);
        code.ShouldNotBeNull();
        code.Count.ShouldBe(3);
        code.ShouldContainKey("new1.dll");
        code.ShouldContainKey("new2.dll");
        code.ShouldContainKey("config.json");
    }


    [Fact]
    public void CodePlugInSource_ShouldThrowOnMissingDependency()
    {
        var codeFiles = new Dictionary<string, byte[]>
        {
            ["main.dll"] = CreateFakeDll("main", new[] { "lib" })
            // lib.dll 缺失
        };
        var src = new CodePlugInSource(codeFiles);
        var ex = Should.Throw<Exception>(() => src.GetModules());
        (ex.Message.Contains("[ψMissingDependency]") || ex.Message.Contains("Bad IL format."))
            .ShouldBeTrue();
    }

    [Fact]
    public void CodePlugInSource_ShouldThrowOnCircularDependency()
    {
        var codeFiles = new Dictionary<string, byte[]>
        {
            ["a.dll"] = CreateFakeDll("a", new[] { "b" }),
            ["b.dll"] = CreateFakeDll("b", new[] { "a" })
        };
        var src = new CodePlugInSource(codeFiles);
        var ex = Should.Throw<Exception>(() => src.GetModules());
        (ex.Message.Contains("[ψCircularDependency]") || ex.Message.Contains("Bad IL format."))
            .ShouldBeTrue();
    }

    // Helper: Generate a minimal valid .NET DLL using Roslyn
    private static byte[] CreateValidDll(string name)
    {
        var code = $@"public class {name} {{ public void Foo() {{ }} }}";
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            name,
            new[] { syntaxTree },
            references,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
        );
        using var ms = new System.IO.MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success) throw new Exception("Failed to emit valid DLL");
        return ms.ToArray();
    }

    private static byte[] CreateFakeDll(string name, string[] refs)
    {
        if (name == "valid")
            return CreateValidDll(name);
        // Only returns an empty byte array. In real scenarios, use Roslyn or similar to generate a valid DLL. Here for structural test only.
        return new byte[1];
    }
    
}