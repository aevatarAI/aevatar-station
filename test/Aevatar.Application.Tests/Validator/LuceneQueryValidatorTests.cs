using System.Collections.Generic;
using Aevatar.Query;
using FluentValidation.TestHelper;
using Xunit;

namespace Aevatar.Validator
{
    public class LuceneQueryValidatorTests
    {
        private readonly LuceneQueryValidator _validator;

        public LuceneQueryValidatorTests()
        {
            _validator = new LuceneQueryValidator();
        }

        [Theory]
        [InlineData("_script")]
        [InlineData("some text _script more text")]
        [InlineData("query:_script")]
        public void Should_Have_Error_When_Query_Contains_Script(string query)
        {
            var model = new LuceneQueryDto
            {
                Index = "test-index",
                QueryString = query,
                PageIndex = 0,
                PageSize = 10,
                SortFields = new List<string> { "field1" }
            };

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.QueryString)
                .WithErrorMessage("Script queries are not allowed.");
        }

        [Theory]
        [InlineData("title:hello")]
        [InlineData("name:john AND age:[20 TO 30]")]
        public void Should_Not_Have_Error_When_Query_Does_Not_Contain_Script(string query)
        {
            var model = new LuceneQueryDto
            {
                Index = "test-index",
                QueryString = query,
                PageSize = 10
            };

            var result = _validator.TestValidate(model);

            result.ShouldNotHaveValidationErrorFor(x => x.QueryString);
        }
    }
}