using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.AppleAuth;

public class AppleAuthCallbackDto : IValidatableObject
{
    public string Code { get; set; }
    public string Id_token { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Id_token) && string.IsNullOrWhiteSpace(Code))
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "id_token" }
            );
        }
    }
}