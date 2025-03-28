using System;
using System.Collections.Generic;
using NJsonSchema;
using NJsonSchema.Validation;

namespace Aevatar.Schema;

public interface ISchemaProvider
{
    JsonSchema GetTypeSchema(Type type);

    Dictionary<string, string> ConvertValidateError(ICollection<ValidationError> errors);
}