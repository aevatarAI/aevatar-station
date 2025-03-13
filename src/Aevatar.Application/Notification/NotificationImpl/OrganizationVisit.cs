using System.Threading.Tasks;
using Aevatar.Notification.Parameters;
using Newtonsoft.Json;

namespace Aevatar.Notification.NotificationImpl;

public class OrganizationVisit : NotificationHandlerBase<OrganizationVisitInfo>
{
    public override NotificationTypeEnum Type => NotificationTypeEnum.OrganizationInvitation;

    public override OrganizationVisitInfo ConvertInput(string input)
    {
        return JsonConvert.DeserializeObject<OrganizationVisitInfo>(input);
    }

    public override Task<string> GetContentForShowAsync(OrganizationVisitInfo input)
    {
        return Task.FromResult("you notification message");
    }

    public override Task HandleAgreeAsync(OrganizationVisitInfo input)
    {
        return Task.CompletedTask;
    }
}