using Microsoft.Extensions.Logging;

namespace Aevatar.Domain.Grains.Authors;

public class NameValidatorGrain : Grain, INameValidator
{
    private readonly ILogger _logger;

    public NameValidatorGrain(ILogger<NameValidatorGrain> logger)
    {
        _logger = logger;
    }
    
    Task<bool> INameValidator.IsValid(string name)
    {
        _logger.LogInformation("""
                               IsValid message received: name = "{Name}"
                               """,
            name);
        
        // Name must contain at least first name and last name to be valid
        return Task.FromResult(name.Contains(' '));
    }
}