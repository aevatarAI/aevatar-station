Write-Host "Testing Dynamic AI with MCP and GAgent Tools..." -ForegroundColor Green

# Wait for application to start
Write-Host "Waiting for application to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Step 1: Initialize agent
Write-Host "`nStep 1: Initializing agent..." -ForegroundColor Yellow
$initResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/initialize" -Method POST -ContentType "application/json" -Body '{"systemLLM":"DeepSeek"}'
$initResult = $initResponse.Content | ConvertFrom-Json
$agentId = $initResult.agentId
Write-Host "Agent created: $agentId" -ForegroundColor Cyan

# Step 2: Configure MCP servers
Write-Host "`nStep 2: Configuring MCP servers..." -ForegroundColor Yellow
$mcpConfig = @{
    agentId = $agentId
    servers = @(
        @{
            serverName = "filesystem"
            command = "npx"
            args = @("-y", "@modelcontextprotocol/server-filesystem", "/tmp", $HOME)
        }
    )
} | ConvertTo-Json -Depth 5

try {
    $mcpResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/configure-servers" -Method POST -ContentType "application/json" -Body $mcpConfig
    $mcpResult = $mcpResponse.Content | ConvertFrom-Json
    Write-Host "MCP Configuration result: $($mcpResult.message)" -ForegroundColor Cyan
    
    if ($mcpResult.availableTools) {
        Write-Host "Available MCP tools:" -ForegroundColor Yellow
        foreach ($server in $mcpResult.availableTools.PSObject.Properties) {
            Write-Host "  Server: $($server.Name)" -ForegroundColor Cyan
            foreach ($tool in $server.Value) {
                Write-Host "    - $($tool.name): $($tool.description)" -ForegroundColor Gray
            }
        }
    }
} catch {
    Write-Host "Failed to configure MCP servers: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Configure GAgent tools
Write-Host "`nStep 3: Configuring GAgent tools..." -ForegroundColor Yellow
$gagentConfig = @{
    agentId = $agentId
    selectedGAgents = @("mathgagent/math", "timeconvertergagent/timeconverter")
} | ConvertTo-Json
$gagentResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/configure-gagent-tools" -Method POST -ContentType "application/json" -Body $gagentConfig
$gagentResult = $gagentResponse.Content | ConvertFrom-Json
Write-Host "GAgent configuration result: $($gagentResult.message)" -ForegroundColor Cyan

# Step 4: Get agent info
Write-Host "`nStep 4: Getting agent info..." -ForegroundColor Yellow
$infoResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/dynamicaimcp/agent-info?agentId=$agentId" -Method GET
$infoResult = $infoResponse.Content | ConvertFrom-Json
Write-Host "Total tools available: $($infoResult.totalTools)" -ForegroundColor Cyan
Write-Host "MCP tools: $($infoResult.mcpTools.Count)" -ForegroundColor Cyan
Write-Host "GAgent functions: $($infoResult.registeredGAgentFunctions -join ', ')" -ForegroundColor Cyan

Write-Host "`nTest completed!" -ForegroundColor Green
