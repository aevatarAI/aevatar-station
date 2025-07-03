using Aevatar.Query;
using FluentValidation;

namespace Aevatar.Validator;

public class LuceneQueryValidator : AbstractValidator<LuceneQueryDto>
{
    public LuceneQueryValidator()
    {
        RuleFor(x => x.QueryString)
            .Must(NotContainsScript).WithMessage("Script queries are not allowed.");
    }
    
    private bool NotContainsScript(string query)
    {
        return !query.Contains("_script");
    }
}