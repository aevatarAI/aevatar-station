# Aevatar.AuthServer.Grants

## 📖 Project Overview

`Aevatar.AuthServer.Grants` is a custom OAuth Grant extension system built on ABP Framework and OpenIddict, specifically designed for the Aevatar platform. This project implements a diversified identity authentication solution, supporting both traditional OAuth third-party login and innovative blockchain wallet signature authentication.

### 🌟 Core Features

- **🔐 Multi-Authentication Support**: Supports Google, Apple, GitHub OAuth and blockchain wallet signature authentication
- **🏗️ Extensible Architecture**: Based on strategy pattern, easy to add new authentication providers
- **⛓️ Web3 Integration**: Native support for wallet address authentication and multi-chain compatibility
- **🎯 ABP Framework Integration**: Fully integrated with ABP identity management and permission system
- **🔄 Automatic User Management**: Intelligent user creation, role assignment and account binding
- **✅ Complete Test Coverage**: Provides comprehensive unit test coverage

## 🏛️ Architecture Design

### Core Design Pattern

```
┌─────────────────────────────────────────────────────────────────┐
│                    ExtensionGrantContext                         │
│                 (OpenIddict Grant Context)                      │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                GrantHandlerBase                                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ • HandleAsync(ExtensionGrantContext)                        │ │
│  │ • CreateUserClaimsPrincipalAsync()                          │ │
│  │ • CreateForbidResult()                                      │ │
│  │ • GetResourcesAsync()                                       │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
┌───────▼────┐ ┌─────▼─────┐ ┌─────▼─────┐ ┌──────▼──────┐
│ Signature  │ │  Google   │ │   Apple   │ │   GitHub    │
│   Grant    │ │   Grant   │ │   Grant   │ │    Grant    │
│  Handler   │ │  Handler  │ │  Handler  │ │   Handler   │
└────────────┘ └───────────┘ └───────────┘ └─────────────┘
```

### Module Dependencies

```
AevatarAuthServerGrantsModule
├── AevatarDomainModule
├── AevatarApplicationContractsModule  
├── AbpIdentityAspNetCoreModule
├── AbpIdentityApplicationModule
└── AbpOpenIddictAspNetCoreModule
```

## 🔑 Supported Authentication Methods

### 1. 🔗 Blockchain Wallet Signature Authentication (Signature Grant)

**Grant Type**: `signature`

**Parameters**:
- `pubkey`: Public key
- `signature`: Signature data
- `chain_id`: Chain ID
- `ca_hash`: CA hash value (optional)
- `plain_text`: Plain text for signature

**Authentication Flow**:
1. Validate signature parameter completeness
2. Parse public key and verify signature
3. Extract wallet address as user identifier
4. Automatically create user (if not exists)
5. Assign basic user role
6. Generate JWT Token

### 2. 🔵 Google OAuth Authentication

**Grant Type**: `google`

**Parameters**:
- `id_token`: Google ID Token
- `source`: Client source (optional)

**Authentication Flow**:
1. Validate Google ID Token
2. Extract user email and Subject ID
3. Find or create user account
4. Bind Google login information
5. Generate access token

### 3. 🍎 Apple Sign In Authentication

**Grant Type**: `apple`

**Parameters**:
- `identity_token`: Apple Identity Token
- `source`: Client source

**Authentication Flow**:
1. Validate Apple identity token
2. Parse user identity information
3. Handle user account creation/binding
4. Return authentication result

### 4. 🐙 GitHub OAuth Authentication

**Grant Type**: `github`

**Parameters**:
- `access_token`: GitHub Access Token

**Authentication Flow**:
1. Use Access Token to get GitHub user information
2. Verify user identity
3. Create or update user account
4. Bind GitHub login information

## 🛠️ Technology Stack

- **Framework**: .NET 9.0
- **Architecture**: ABP Framework
- **Authentication**: OpenIddict Server
- **Database**: MongoDB (via ABP Identity)
- **Blockchain**: AElf Types
- **Third-party Integrations**:
  - Google.Apis.Auth
  - Octokit (GitHub API)
  - Custom Apple Provider

## 📦 Project Structure

```
src/Aevatar.AuthServer.Grants/
├── 📁 Options/                    # Configuration options
│   ├── SignatureGrantOptions.cs   # Signature auth config
│   ├── ChainOptions.cs            # Blockchain config
│   └── ChainInfo.cs               # Chain info model
├── 📁 Providers/                  # Authentication providers
│   ├── IWalletLoginProvider.cs    # Wallet login interface
│   ├── WalletLoginProvider.cs     # Wallet login implementation
│   ├── IGoogleProvider.cs         # Google auth interface
│   ├── GoogleProvider.cs          # Google auth implementation
│   ├── IAppleProvider.cs          # Apple auth interface
│   ├── AppleProvider.cs           # Apple auth implementation
│   ├── IGithubProvider.cs         # GitHub auth interface
│   └── GithubProvider.cs          # GitHub auth implementation
├── 📄 GrantHandlerBase.cs         # Grant handler base class
├── 📄 SignatureGrantHandler.cs     # Signature auth handler
├── 📄 GoogleGrantHandler.cs        # Google auth handler
├── 📄 AppleGrantHandler.cs         # Apple auth handler
├── 📄 GithubGrantHandler.cs        # GitHub auth handler
└── 📄 AevatarAuthServerGrantsModule.cs # ABP module definition
```

## ⚙️ Configuration

### appsettings.json Configuration Example

```json
{
  "Signature": {
    "Timeout": 300,
    "AllowedChains": ["AELF", "tDVV", "tDVW"]
  },
  "Chains": {
    "AELF": {
      "ChainId": "AELF",
      "Endpoint": "https://aelf-mainnet.io"
    },
    "tDVV": {
      "ChainId": "tDVV", 
      "Endpoint": "https://aelf-testnet.io"
    }
  },
  "Google": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret"
  },
  "Apple": {
    "ClientId": "your-apple-client-id",
    "TeamId": "your-apple-team-id",
    "KeyId": "your-apple-key-id",
    "PrivateKey": "your-apple-private-key"
  },
  "GitHub": {
    "ClientId": "your-github-client-id",
    "ClientSecret": "your-github-client-secret"
  }
}
```

## 🚀 Usage Examples

### Module Registration

```csharp
[DependsOn(typeof(AevatarAuthServerGrantsModule))]
public class YourAppModule : AbpModule
{
    // Module will automatically register all grant handlers
}
```

### Client Authentication Requests

#### Wallet Signature Authentication
```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=signature&
pubkey=04a1b2c3...&
signature=30450221...&
chain_id=AELF&
plain_text=Login%20to%20Aevatar&
ca_hash=optional_ca_hash
```

#### Google Authentication
```http  
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=google&
id_token=eyJhbGciOiJSUzI1NiIs...&
source=web
```

## 🧩 Extending New Authentication Providers

### 1. Create Authentication Provider Interface
```csharp
public interface ICustomProvider
{
    Task<CustomUserInfo> ValidateTokenAsync(string token);
}
```

### 2. Implement Authentication Provider
```csharp
public class CustomProvider : ICustomProvider, ITransientDependency
{
    public async Task<CustomUserInfo> ValidateTokenAsync(string token)
    {
        // Implement custom authentication logic
    }
}
```

### 3. Create Grant Handler
```csharp
public class CustomGrantHandler : GrantHandlerBase, ITransientDependency
{
    public override string Name => "custom";
    
    private readonly ICustomProvider _customProvider;
    
    public CustomGrantHandler(ICustomProvider customProvider)
    {
        _customProvider = customProvider;
    }
    
    public override async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var token = context.Request.GetParameter("token").ToString();
        var userInfo = await _customProvider.ValidateTokenAsync(token);
        
        // Handle user creation and authentication logic
        // ...
        
        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
}
```

### 4. Register to Module
```csharp
options.Grants.Add("custom", 
    new CustomGrantHandler(serviceProvider.GetRequiredService<ICustomProvider>()));
```

## 🧪 Testing

The project includes complete unit test coverage:

```bash
# Run all tests
dotnet test

# Run specific tests
dotnet test --filter "ClassName=GoogleGrantHandlerTests"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Test File Structure
```
test/Aevatar.AuthServer.Grants.Tests/
├── GoogleGrantHandlerTests.cs      # Google auth tests
├── AppleGrantHandlerTests.cs       # Apple auth tests
├── GithubGrantHandlerTests.cs      # GitHub auth tests
└── SignatureGrantHandlerTests.cs   # Signature auth tests
```

## 🔒 Security Considerations

- **Signature Verification**: All blockchain signatures undergo strict cryptographic verification
- **Token Validation**: Third-party OAuth tokens are validated through official APIs
- **Error Handling**: Unified error handling mechanism that doesn't leak sensitive information
- **User Isolation**: Users from different authentication sources are securely isolated through LoginInfo
- **Permission Control**: Integrated with ABP permission system for fine-grained access control

## 📝 Development Guidelines

1. **Inherit Base Class**: All grant handlers must inherit from `GrantHandlerBase`
2. **Dependency Injection**: Use ABP's dependency injection system to register services
3. **Exception Handling**: Use `CreateForbidResult` to return standard error responses
4. **Logging**: Add structured logging at key checkpoints
5. **Unit Testing**: New features must include corresponding unit tests

## 🤝 Contributing

1. Fork the project
2. Create a feature branch: `git checkout -b feature/new-provider`
3. Commit your changes: `git commit -am 'Add new authentication provider'`
4. Push to the branch: `git push origin feature/new-provider`
5. Submit a Pull Request

## 📄 License

This project follows the license file in the project root directory.

## 🔗 Related Links

- [ABP Framework Documentation](https://docs.abp.io/)
- [OpenIddict Documentation](https://documentation.openiddict.com/)
