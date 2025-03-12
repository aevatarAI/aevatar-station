using System.Threading.Tasks;

namespace Aevatar.Notification.NotificationImpl;

public class OrganizationVisit:NotificationHandlerBase<OrganizationVisitInfoDto>
{
    public override NotificationTypeEnum Type => NotificationTypeEnum.OrganizationInvitation;
    
    public override OrganizationVisitInfoDto ConvertInput(string input)
    {
        throw new System.NotImplementedException();
    }

    public override Task<string> GetContentForShowAsync(OrganizationVisitInfoDto input)
    {
        throw new System.NotImplementedException();
    }

    public override Task HandleAgreeAsync(OrganizationVisitInfoDto input)
    {
        throw new System.NotImplementedException();
    }
}