class MCPTestApp {
    constructor() {
        this.selectedServer = null;
        this.selectedTool = null;
        this.servers = [];
        this.tools = [];
        this.init();
    }

    async init() {
        await this.checkHealth();
        await this.loadServers();
        this.setupEventListeners();
    }

    async checkHealth() {
        try {
            const response = await fetch('/api/mcp/health');
            const data = await response.json();
            
            const indicator = document.getElementById('healthIndicator');
            if (response.ok) {
                indicator.textContent = '✅ API Ready';
                indicator.className = 'health-indicator health-ok';
            } else {
                throw new Error('Health check failed');
            }
        } catch (error) {
            const indicator = document.getElementById('healthIndicator');
            indicator.textContent = '❌ API Error';
            indicator.className = 'health-indicator health-error';
            console.error('Health check failed:', error);
        }
    }

    async loadServers() {
        try {
            console.log('Loading servers...');
            const response = await fetch('/api/mcp/servers');
            console.log('Response status:', response.status);
            console.log('Response headers:', response.headers);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const responseText = await response.text();
            console.log('Response text:', responseText);
            
            this.servers = JSON.parse(responseText);
            console.log('Loaded servers:', this.servers.length);
            this.renderServers();
        } catch (error) {
            console.error('Failed to load servers:', error);
            this.showError('Failed to load MCP servers: ' + error.message);
        }
    }

    renderServers() {
        const serverList = document.getElementById('serverList');
        
        if (this.servers.length === 0) {
            serverList.innerHTML = '<div class="server-card">No MCP servers configured</div>';
            return;
        }

        serverList.innerHTML = this.servers.map(server => `
            <div class="server-card" data-server-name="${server.name}">
                <h3>${server.name}</h3>
                <p>${server.description || 'No description available'}</p>
                <p><strong>Command:</strong> ${server.command} ${server.args.join(' ')}</p>
                ${server.requiresEnv ? `
                    <div class="env-indicator">
                        Requires Environment: ${server.envKeys.join(', ')}
                    </div>
                ` : ''}
            </div>
        `).join('');

        // Add click event listeners to server cards
        serverList.querySelectorAll('.server-card').forEach(card => {
            card.addEventListener('click', (e) => {
                const serverName = card.dataset.serverName;
                console.log('Server card clicked:', serverName);
                this.selectServer(serverName);
            });
        });
    }

    async selectServer(serverName) {
        // Update UI selection
        document.querySelectorAll('.server-card').forEach(card => {
            card.classList.remove('selected');
            if (card.dataset.serverName === serverName) {
                card.classList.add('selected');
            }
        });
        
        this.selectedServer = serverName;
        this.selectedTool = null;
        
        // Hide previous sections
        document.getElementById('parametersSection').classList.remove('active');
        document.getElementById('resultsSection').classList.remove('active');
        
        // Load tools for selected server
        await this.loadTools(serverName);
    }

    async loadTools(serverName) {
        try {
            const response = await fetch(`/api/mcp/servers/${serverName}/tools`);
            this.tools = await response.json();
            this.renderTools();
            
            // Show tools section
            document.getElementById('toolsSection').classList.add('active');
        } catch (error) {
            console.error('Failed to load tools:', error);
            this.showError(`Failed to load tools for server ${serverName}`);
        }
    }

    renderTools() {
        const toolsList = document.getElementById('toolsList');
        
        if (this.tools.length === 0) {
            toolsList.innerHTML = '<div class="tool-item">No tools available for this server</div>';
            return;
        }

        toolsList.innerHTML = this.tools.map(tool => `
            <div class="tool-item" onclick="app.selectTool('${tool.name}')">
                <div class="tool-header">
                    <span class="tool-name">${tool.name}</span>
                    <span class="tool-param-count">${Object.keys(tool.parameters).length} parameters</span>
                </div>
                <div class="tool-description">${tool.description || 'No description available'}</div>
            </div>
        `).join('');
    }

    selectTool(toolName) {
        // Update UI selection
        document.querySelectorAll('.tool-item').forEach(item => {
            item.classList.remove('selected');
        });
        
        event.target.closest('.tool-item').classList.add('selected');
        
        this.selectedTool = toolName;
        const tool = this.tools.find(t => t.name === toolName);
        
        this.renderParameters(tool);
        
        // Show parameters section
        document.getElementById('parametersSection').classList.add('active');
        
        // Hide results section
        document.getElementById('resultsSection').classList.remove('active');
    }

    renderParameters(tool) {
        const parametersForm = document.getElementById('parametersForm');
        
        if (!tool.parameters || Object.keys(tool.parameters).length === 0) {
            parametersForm.innerHTML = '<p>This tool has no parameters.</p>';
            return;
        }

        parametersForm.innerHTML = Object.entries(tool.parameters).map(([name, param]) => `
            <div class="parameter-group">
                <label class="parameter-label" for="param_${name}">
                    ${name} 
                    ${param.required ? '<span class="parameter-required">*</span>' : ''}
                    <small>(${param.type})</small>
                </label>
                <input 
                    type="text" 
                    id="param_${name}" 
                    class="parameter-input"
                    placeholder="${param.description || `Enter ${name}`}"
                    ${param.required ? 'required' : ''}
                />
            </div>
        `).join('');
    }

    async callTool() {
        if (!this.selectedServer || !this.selectedTool) {
            this.showError('Please select a server and tool first');
            return;
        }

        // Show loading state
        const button = document.getElementById('callButton');
        const buttonText = document.getElementById('callButtonText');
        const spinner = document.getElementById('callButtonSpinner');
        
        button.disabled = true;
        buttonText.style.display = 'none';
        spinner.style.display = 'inline-block';

        try {
            // Collect parameters
            const tool = this.tools.find(t => t.name === this.selectedTool);
            const toolArgs = {};
            
            if (tool.parameters) {
                for (const [name, param] of Object.entries(tool.parameters)) {
                    const input = document.getElementById(`param_${name}`);
                    const value = input.value.trim();
                    
                    if (param.required && !value) {
                        throw new Error(`Required parameter '${name}' is missing`);
                    }
                    
                    if (value) {
                        // Try to parse as JSON for complex types, otherwise use as string
                        try {
                            toolArgs[name] = JSON.parse(value);
                        } catch {
                            toolArgs[name] = value;
                        }
                    }
                }
            }

            // Call the tool
            const response = await fetch(`/api/mcp/servers/${this.selectedServer}/tools/${this.selectedTool}/call`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(toolArgs)
            });

            const result = await response.json();
            this.showResult(result);

        } catch (error) {
            console.error('Tool call failed:', error);
            this.showResult({
                success: false,
                error: error.message,
                executionTime: new Date().toISOString()
            });
        } finally {
            // Reset loading state
            button.disabled = false;
            buttonText.style.display = 'inline-block';
            spinner.style.display = 'none';
        }
    }

    showResult(result) {
        const resultsSection = document.getElementById('resultsSection');
        const resultsContent = document.getElementById('resultsContent');
        
        const isSuccess = result.success;
        const timestamp = new Date(result.executionTime).toLocaleString();
        
        resultsContent.innerHTML = `
            <div class="${isSuccess ? 'result-success' : 'result-error'}">
                <h3>${isSuccess ? '✅ Success' : '❌ Error'}</h3>
                <p><strong>Executed at:</strong> ${timestamp}</p>
                ${isSuccess ? `
                    <h4>Result:</h4>
                    <div class="result-content">${this.formatResult(result.result)}</div>
                ` : `
                    <h4>Error:</h4>
                    <div class="result-content">${result.error || 'Unknown error'}</div>
                `}
            </div>
        `;
        
        resultsSection.classList.add('active');
        resultsSection.scrollIntoView({ behavior: 'smooth' });
    }

    formatResult(result) {
        if (typeof result === 'string') {
            try {
                const parsed = JSON.parse(result);
                return JSON.stringify(parsed, null, 2);
            } catch {
                return result;
            }
        }
        return JSON.stringify(result, null, 2);
    }

    showError(message) {
        const resultsSection = document.getElementById('resultsSection');
        const resultsContent = document.getElementById('resultsContent');
        
        resultsContent.innerHTML = `
            <div class="result-error">
                <h3>❌ Error</h3>
                <div class="result-content">${message}</div>
            </div>
        `;
        
        resultsSection.classList.add('active');
    }

    setupEventListeners() {
        // Add event listener for Execute Tool button
        const callButton = document.getElementById('callButton');
        if (callButton) {
            callButton.addEventListener('click', () => {
                console.log('Execute Tool button clicked');
                this.callTool();
            });
        }
        
        // Add keyboard shortcut
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && e.ctrlKey) {
                this.callTool();
            }
        });
    }
}

// Initialize the app when the page loads
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, initializing MCP Test App...');
    try {
        const app = new MCPTestApp();
        window.mcpApp = app; // Make it globally accessible for debugging
        window.app = app; // Make it accessible for onclick handlers
    } catch (error) {
        console.error('Failed to initialize MCP Test App:', error);
    }
});

// Fallback initialization if DOMContentLoaded already fired
if (document.readyState === 'loading') {
    // Do nothing, DOMContentLoaded will fire
} else {
    // DOM is already loaded
    console.log('DOM already loaded, initializing MCP Test App...');
    try {
        const app = new MCPTestApp();
        window.mcpApp = app;
        window.app = app; // Make it accessible for onclick handlers
    } catch (error) {
        console.error('Failed to initialize MCP Test App:', error);
    }
}
