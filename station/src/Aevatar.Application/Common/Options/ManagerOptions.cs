using System.Collections.Generic;
using Orleans;

namespace Aevatar.Common.Options;

public class ManagerOptions
{
    [Id(0)] public List<string> ManagerIds { get; set; } = new List<string>();
}