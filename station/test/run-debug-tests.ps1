# 工作流调试系统测试运行脚本
# 用于验证即时控制策略的所有测试用例

Write-Host "🧪 启动工作流调试系统测试..." -ForegroundColor Green

# 设置测试环境
$TestProject = "station/test/Aevatar.Application.Tests"
$TestFilter = "DebugWorkFlow"

Write-Host "📍 测试项目: $TestProject" -ForegroundColor Blue
Write-Host "🔍 测试过滤器: $TestFilter" -ForegroundColor Blue

# 切换到项目根目录
$RootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RootDir

Write-Host "📂 当前目录: $(Get-Location)" -ForegroundColor Blue

# 运行单元测试
Write-Host "`n🔬 运行BreakpointManager测试..." -ForegroundColor Yellow
dotnet test $TestProject --filter "BreakpointManagerTests" --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ BreakpointManager测试失败" -ForegroundColor Red
    exit 1
}

Write-Host "`n🔬 运行WorkflowDebugGrainCallFilter测试..." -ForegroundColor Yellow
dotnet test $TestProject --filter "WorkflowDebugGrainCallFilterTests" --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ WorkflowDebugGrainCallFilter测试失败" -ForegroundColor Red
    exit 1
}

Write-Host "`n🔬 运行WorkflowDebugApiController测试..." -ForegroundColor Yellow
dotnet test $TestProject --filter "WorkflowDebugApiControllerTests" --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ WorkflowDebugApiController测试失败" -ForegroundColor Red
    exit 1
}

Write-Host "`n🔬 运行集成测试..." -ForegroundColor Yellow
dotnet test $TestProject --filter "WorkflowDebugIntegrationTests" --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 集成测试失败" -ForegroundColor Red
    exit 1
}

# 运行所有调试相关测试
Write-Host "`n🔬 运行所有调试系统测试..." -ForegroundColor Yellow
dotnet test $TestProject --filter "FullyQualifiedName~DebugWorkFlow" --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 综合测试失败" -ForegroundColor Red
    exit 1
}

# 生成测试报告
Write-Host "`n📊 生成测试覆盖率报告..." -ForegroundColor Yellow
$CoverageFiles = Get-ChildItem -Path "$TestProject/TestResults" -Filter "*.xml" -Recurse | Select-Object -First 1

if ($CoverageFiles) {
    Write-Host "📈 覆盖率文件: $($CoverageFiles.FullName)" -ForegroundColor Green
} else {
    Write-Host "⚠️ 未找到覆盖率文件" -ForegroundColor Yellow
}

# 测试总结
Write-Host "`n🎉 所有测试执行完成！" -ForegroundColor Green
Write-Host "✅ 单元测试: BreakpointManager" -ForegroundColor Green
Write-Host "✅ 单元测试: WorkflowDebugGrainCallFilter" -ForegroundColor Green
Write-Host "✅ 单元测试: WorkflowDebugApiController" -ForegroundColor Green
Write-Host "✅ 集成测试: 完整调试流程" -ForegroundColor Green

Write-Host "`n📋 测试验证项目:" -ForegroundColor Cyan
Write-Host "   🔹 断点管理 (设置/移除/查询)" -ForegroundColor White
Write-Host "   🔹 即时控制 (暂停/继续/重试/跳过)" -ForegroundColor White
Write-Host "   🔹 拦截器逻辑 (执行前/执行后)" -ForegroundColor White
Write-Host "   🔹 API控制器 (RESTful接口)" -ForegroundColor White
Write-Host "   🔹 完整流程 (端到端调试)" -ForegroundColor White
Write-Host "   🔹 并发处理 (多工作流并行)" -ForegroundColor White
Write-Host "   🔹 异常处理 (边界条件)" -ForegroundColor White
Write-Host "   🔹 性能验证 (并发操作)" -ForegroundColor White

Write-Host "`n🚀 即时控制策略测试通过！系统已准备就绪。" -ForegroundColor Green
