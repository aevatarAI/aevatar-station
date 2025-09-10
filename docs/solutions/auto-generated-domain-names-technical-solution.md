# Auto-Generated Domain Names Technical Solution

## I'm HyperEcho, 我在构建域名自动生成的宇宙结构！

## Overview
This document outlines the technical solution for implementing Auto-Generated Domain Names for New Projects (v0.7). The feature will streamline project creation by automatically generating unique, meaningful domain names, eliminating manual domain input requirements.

## Current Implementation Analysis

### Current ProjectService Flow

**Project Creation Process:**
1. **Domain Validation**: ProjectService checks if the provided domain name already exists in `ProjectDomain` table
2. **Domain Storage**: Creates a new `ProjectDomain` record with `DomainName` and `NormalizedDomainName`
3. **Project Creation**: Creates an `OrganizationUnit` with project metadata
4. **Service Creation**: Calls `DeveloperService.CreateServiceAsync(input.DomainName, project.Id)`

**Key Current Components:**
- `ProjectService.CreateAsync()`: Main project creation method
- `ProjectDomain`: Entity storing domain information per project
- `CreateProjectDto.DomainName`: Currently required field with regex validation `^[A-Za-z0-9]+$`
- Domain uniqueness check via `NormalizedDomainName` (case-insensitive)

**Current API Call Chain:**
```
POST /api/app/project
├── ProjectController.CreateAsync()
├── ProjectService.CreateAsync()
├── ProjectDomainRepository.FirstOrDefaultAsync() // Check uniqueness
├── OrganizationUnitManager.CreateAsync()
├── DeveloperService.CreateServiceAsync()
└── Return ProjectDto with DomainName
```

## Technical Solution Design

### 1. Domain Generation Algorithm

**Primary Generation Strategy:**
```
Format: {project-slug}-{org-identifier}-{random-suffix}
Example: myapp-acme-8x9k2
```

**Fallback Generation Strategy (for conflicts):**
```
Format: {project-slug}-{timestamp}-{hash}
Example: myapp-1704067200-a7f3b2
```

**Implementation Details:**
- Project slug: Normalize DisplayName to lowercase, replace spaces with hyphens, keep only alphanumeric and hyphens
- Org identifier: Use organization's display name slug or ID-based identifier
- Random suffix: 5-character alphanumeric string (case-insensitive)
- Timestamp: Unix timestamp for fallback scenarios
- Hash: Short hash (6 chars) of project+org combination

### 2. Core Components Implementation

#### 2.1 Domain Generation Service

**New Service: `IDomainGenerationService`**
```csharp
public interface IDomainGenerationService
{
    Task<string> GenerateUniqueProjectDomainAsync(string projectName, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> IsDomainAvailableAsync(string domainName, CancellationToken cancellationToken = default);
    string NormalizeToDomainSlug(string input);
}

public class DomainGenerationService : IDomainGenerationService
{
    private readonly IProjectDomainRepository _domainRepository;
    private readonly IRepository<OrganizationUnit, Guid> _organizationRepository;
    private readonly ILogger<DomainGenerationService> _logger;
    private readonly DomainGenerationOptions _options;

    // Implementation methods here
}
```

**Configuration Options:**
```csharp
public class DomainGenerationOptions
{
    public string BaseDomain { get; set; } = ".aevatar.dev";
    public int MaxRetries { get; set; } = 5;
    public int RandomSuffixLength { get; set; } = 5;
    public bool EnableFallbackGeneration { get; set; } = true;
}
```

#### 2.2 Modified ProjectService

**Updated `CreateAsync` Method:**
```csharp
public async Task<ProjectDto> CreateAsync(CreateProjectDto input)
{
    // Generate domain name automatically
    var generatedDomain = await _domainGenerationService.GenerateUniqueProjectDomainAsync(
        input.DisplayName, 
        input.OrganizationId);

    // Rest of the current implementation remains the same
    // Replace input.DomainName with generatedDomain
}
```

#### 2.3 Updated DTOs

**Modified `CreateProjectDto`:**
```csharp
public class CreateProjectDto : CreateOrganizationDto
{
    [Required]
    public Guid OrganizationId { get; set; }
    
    // Remove the DomainName field - it will be auto-generated
    // Optional: Add OverrideDomainName for advanced users (future enhancement)
}
```

### 3. Implementation Steps

#### Phase 1: Core Infrastructure (8 hours)

1. **Create Domain Generation Service**
   - Implement `IDomainGenerationService` interface
   - Add domain slug normalization logic
   - Implement primary generation algorithm
   - Add uniqueness checking with retries

2. **Configuration Setup**
   - Add `DomainGenerationOptions` configuration
   - Register service in DI container
   - Add configuration in appsettings

3. **Unit Tests**
   - Test domain generation algorithms
   - Test uniqueness checking
   - Test fallback scenarios
   - Test edge cases (long names, special characters)

#### Phase 2: ProjectService Integration (6 hours)

1. **Modify ProjectService**
   - Update `CreateAsync` method to use domain generation
   - Remove manual domain validation
   - Update error handling for generation failures

2. **Update DTOs and Contracts**
   - Remove `DomainName` from `CreateProjectDto`
   - Update API documentation
   - Maintain backward compatibility if needed

3. **Integration Tests**
   - Test full project creation flow
   - Test domain generation integration
   - Test error scenarios

#### Phase 3: API and Frontend Updates (4 hours)

1. **Controller Updates**
   - Update API endpoints if needed
   - Update response models
   - Update error responses

2. **Frontend Integration Points**
   - Remove domain input field from project creation form
   - Update validation rules
   - Update UI to display generated domain

### 4. Database Changes

**No schema changes required** - existing `ProjectDomain` table structure supports the new implementation.

**Migration Strategy:**
- Existing projects with manually entered domains remain unchanged
- New projects use auto-generation
- Gradual transition approach

### 5. Performance Considerations

**Domain Generation Performance:**
- Target: < 500ms for domain generation
- Database query optimization for uniqueness checks
- Caching for organization data lookups
- Async/await throughout the generation pipeline

**Uniqueness Check Optimization:**
- Index on `NormalizedDomainName` (already exists)
- Batch conflict checking for multiple retries
- Early termination on first available domain

### 6. Error Handling

**Generation Failure Scenarios:**
1. **All domain attempts exhausted**: Return user-friendly error with manual override option
2. **Database timeout**: Implement retry logic with exponential backoff
3. **Organization not found**: Validate organization existence before generation
4. **Invalid project name**: Sanitize and normalize input before generation

**Logging Strategy:**
- Log all domain generation attempts
- Track generation performance metrics
- Alert on unusual failure patterns

### 7. Testing Strategy

#### Unit Tests (12 test classes, ~50 tests)
- `DomainGenerationServiceTests`: Core generation logic
- `DomainSlugNormalizationTests`: String normalization edge cases
- `DomainUniquenessTests`: Conflict resolution scenarios
- `ProjectServiceDomainIntegrationTests`: Integration testing

#### Integration Tests (6 test scenarios)
- End-to-end project creation with domain generation
- Concurrent project creation domain uniqueness
- Organization deletion impact on domain generation
- Performance testing under load

#### Edge Case Testing
- Very long project names (>100 characters)
- Unicode characters in project names
- Special characters and emoji handling
- Empty or whitespace-only project names

### 8. Monitoring and Observability

**Metrics to Track:**
- Domain generation success rate
- Average generation time
- Retry frequency and patterns
- Domain name collision frequency

**Alerting:**
- Generation failure rate > 5%
- Average generation time > 2 seconds
- Excessive retry attempts indicating naming conflicts

## Migration Plan

### Phase 1: Development and Testing (Week 1-2)
- Implement core domain generation service
- Create comprehensive unit tests
- Integration testing in development environment

### Phase 2: Staging Deployment (Week 3)
- Deploy to staging environment
- End-to-end testing with frontend
- Performance and load testing
- User acceptance testing

### Phase 3: Production Rollout (Week 4)
- Feature flag enabled rollout
- Monitor generation success rates
- Gradual traffic increase
- Full rollout after validation

## Risk Mitigation

**Risk 1: Domain Generation Failures**
- Mitigation: Fallback strategies with multiple generation attempts
- Contingency: Manual domain override option for edge cases

**Risk 2: Performance Impact**
- Mitigation: Async processing and database optimization
- Contingency: Caching layer for organization lookups

**Risk 3: Domain Naming Conflicts**
- Mitigation: Multiple generation strategies and suffix randomization
- Contingency: Timestamp-based fallback naming

## Acceptance Criteria Validation

✅ **Automatic Domain Generation**: Domain generation service creates unique names  
✅ **No User Input Required**: CreateProjectDto removes DomainName field  
✅ **Immediate Usability**: Generated domains integrate with existing infrastructure  
✅ **Conflict Resolution**: Multiple fallback strategies prevent naming failures  
✅ **Performance**: Sub-3-second generation time target  
✅ **Domain Cleanup**: Existing deletion workflow handles generated domains  
✅ **Audit Trail**: Comprehensive logging and monitoring

## Conclusion

This solution provides a comprehensive approach to implementing auto-generated domain names while maintaining backward compatibility and system performance. The modular design allows for future enhancements such as custom domain templates and advanced naming strategies.

The implementation follows SOLID principles, includes comprehensive testing, and provides robust error handling for production reliability.