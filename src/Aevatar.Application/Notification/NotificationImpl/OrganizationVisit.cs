using System.Threading.Tasks;
using Aevatar.Notification.Parameters;
using Aevatar.Organizations;
using Newtonsoft.Json;

namespace Aevatar.Notification.NotificationImpl;

public class OrganizationVisit : NotificationHandlerBase<OrganizationVisitInfo>
{
    private readonly IOrganizationService _organizationService;

    public OrganizationVisit(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    public override NotificationTypeEnum Type => NotificationTypeEnum.OrganizationInvitation;

    public override OrganizationVisitInfo ConvertInput(string input)
    {
        return JsonConvert.DeserializeObject<OrganizationVisitInfo>(input);
    }

    public override Task<string> GetContentForShowAsync(OrganizationVisitInfo input)
    {
        return Task.FromResult("you notification message");
    }

    public override async Task HandleAgreeAsync(OrganizationVisitInfo input)
    {
        await _organizationService.SetMemberRoleAsync(input.OrganizationId, new SetOrganizationMemberRoleDto
        {
            UserId = input.UserId,
            RoleId = input.RoleId
        });
    }
}