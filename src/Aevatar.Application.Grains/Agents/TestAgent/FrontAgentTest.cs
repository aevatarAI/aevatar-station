using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core;
using Aevatar.Core.Abstractions;

namespace Aevatar.Application.Grains.Agents.TestAgent;

public class FrontAgentTest : GAgentBase<FrontAgentState, FrontTestEvent, EventBase, FrontInitConfig>, IFrontAgentTest
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("this is used for front test");
    }
}

public interface IFrontAgentTest : IGAgent
{
}

[GenerateSerializer]
public class FrontAgentState : StateBase
{
}

[GenerateSerializer]
public class FrontTestEvent : StateLogEventBase<FrontTestEvent>
{
}

[GenerateSerializer]
public class FrontInitConfig : ConfigurationBase
{
    [Id(0)] [Required] public string Name { get; set; }

    [Id(1)] [Required] public List<int> StudentIds { get; set; }

    [Id(2)] [Required] public JobType JobType { get; set; }

    [Required]
    [RegularExpression(@"^https?://.*")]
    [Description("the url of school")]
    [Id(3)]
    public string Url { get; set; }

    [Id(4)] public string Memo { get; set; }
}

public enum JobType
{
    Teacher,
    Professor,
    Dean
}