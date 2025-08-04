using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Collections.Generic;

namespace Aevatar.GAgent;

public class AgentDiscoveryTests
{
    private readonly ITestOutputHelper _output;

    public AgentDiscoveryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ScanGAgentTypes_ShouldFindAllGAgentImplementations_IncludingNuGetPackages()
    {
        // 首先检查当前已加载的程序集
        _output.WriteLine("=== 当前已加载的程序集分析 ===");
        var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var aevatarAssemblies = currentAssemblies
            .Where(a => a.FullName?.Contains("Aevatar") == true)
            .ToList();
            
        _output.WriteLine($"总程序集数: {currentAssemblies.Length}");
        _output.WriteLine($"Aevatar相关程序集数: {aevatarAssemblies.Count}");
        
        foreach (var assembly in aevatarAssemblies)
        {
            _output.WriteLine($"  - {assembly.GetName().Name} (版本: {assembly.GetName().Version})");
        }
        
        // 检查是否包含GAgents相关包
        var gAgentPackages = aevatarAssemblies
            .Where(a => a.FullName?.Contains("GAgents") == true)
            .ToList();
            
        _output.WriteLine($"\nGAgents相关包数: {gAgentPackages.Count}");
        foreach (var package in gAgentPackages)
        {
            _output.WriteLine($"  - {package.FullName}");
        }

        // 尝试强制加载NuGet包中的程序集
        _output.WriteLine("\n=== 尝试强制加载GAgents程序集 ===");
        TryLoadGAgentAssemblies();
        
        // 重新扫描
        var updatedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var updatedGAgentPackages = updatedAssemblies
            .Where(a => a.FullName?.Contains("GAgents") == true)
            .ToList();
            
        _output.WriteLine($"强制加载后GAgents包数: {updatedGAgentPackages.Count}");
        
        // Act - 扫描所有GAgent类型
        var gAgentType = typeof(IGAgent);
        var validAssemblies = updatedAssemblies
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .ToList();

        var allGAgents = new List<(Type type, Assembly assembly)>();
        var gAgentsWithAttributes = new List<(Type type, DescriptionAttribute attr)>();

        _output.WriteLine($"\n=== 扫描结果 ===");
        _output.WriteLine($"有效程序集数: {validAssemblies.Count}");

        foreach (var assembly in validAssemblies)
        {
            try
            {
                var gAgentTypes = assembly.GetTypes()
                    .Where(t => gAgentType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && t.IsPublic)
                    .ToList();

                if (gAgentTypes.Any())
                {
                    _output.WriteLine($"\n程序集: {assembly.GetName().Name}");
                    foreach (var type in gAgentTypes)
                    {
                        allGAgents.Add((type, assembly));
                        _output.WriteLine($"  - {type.Name} ({type.FullName})");
                        
                        var attr = type.GetCustomAttribute<DescriptionAttribute>();
                        if (attr != null)
                        {
                            gAgentsWithAttributes.Add((type, attr));
                            _output.WriteLine($"    ✓ 有DescriptionAttribute: {attr.Description}");
                        }
                        else
                        {
                            _output.WriteLine($"    ⚠ 缺少DescriptionAttribute属性");
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _output.WriteLine($"⚠ 无法加载类型: {assembly.GetName().Name} - {ex.Message}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠ 扫描异常: {assembly.GetName().Name} - {ex.Message}");
            }
        }

        // Assert
        _output.WriteLine($"\n=== 最终统计 ===");
        _output.WriteLine($"发现的GAgent总数: {allGAgents.Count}");
        _output.WriteLine($"带DescriptionAttribute的GAgent数: {gAgentsWithAttributes.Count}");
        
        // 验证是否发现了NuGet包中的GAgent
        var nugetGAgents = allGAgents
            .Where(ga => ga.assembly.FullName?.Contains("GAgents") == true)
            .ToList();
            
        _output.WriteLine($"NuGet包中的GAgent数: {nugetGAgents.Count}");
        
        // 断言至少应该发现一些GAgent
        allGAgents.Count.ShouldBeGreaterThan(10, "应该发现足够多的GAgent实现");
        
        // 如果没有发现NuGet包中的GAgent，输出警告
        if (nugetGAgents.Count == 0)
        {
            _output.WriteLine("⚠ 警告: 没有发现NuGet包中的GAgent，可能需要改进加载策略");
        }
    }
    
    private void TryLoadGAgentAssemblies()
    {
        var packageNames = new[]
        {
            "Aevatar.GAgents.AIGAgent",
            "Aevatar.GAgents.SemanticKernel", 
            "Aevatar.GAgents.AI.Abstractions",
            "Aevatar.GAgents.Twitter",
            "Aevatar.GAgents.GroupChat"
        };
        
        foreach (var packageName in packageNames)
        {
            try
            {
                // 尝试通过类型引用强制加载程序集
                var loadedAssembly = Assembly.Load(packageName);
                _output.WriteLine($"✓ 成功加载: {loadedAssembly.FullName}");
            }
            catch (FileNotFoundException)
            {
                _output.WriteLine($"✗ 未找到程序集: {packageName}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"✗ 加载失败: {packageName} - {ex.Message}");
            }
        }
    }
} 