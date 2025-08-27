using System.Collections.Generic;
using Newtonsoft.Json;
using Volo.Abp;

namespace Aevatar.Exceptions;

public sealed class ParameterValidateException(Dictionary<string, string> validateErrors) : BusinessException("5003",
    JsonConvert.SerializeObject(validateErrors));