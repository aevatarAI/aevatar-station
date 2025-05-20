using System.Threading.Tasks;
using Aevatar.Notification.Parameters;
using Aevatar.Organizations;
using Newtonsoft.Json;
using Volo.Abp.Identity;

namespace Aevatar.Notification.NotificationImpl;

public class OrganizationVisit : NotificationHandlerBase<OrganizationVisitInfo>
{
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _userManager;

    public OrganizationVisit(IOrganizationService organizationService, IdentityUserManager userManager)
    {
        _organizationService = organizationService;
        _userManager = userManager;
    }

    public override NotificationTypeEnum Type => NotificationTypeEnum.OrganizationInvitation;

    public override OrganizationVisitInfo ConvertInput(string input)
    {
        return JsonConvert.DeserializeObject<OrganizationVisitInfo>(input);
    }

    public override async Task<string> GetContentForShowAsync(OrganizationVisitInfo input)
    {
        var creator = await _userManager.GetByIdAsync(input.Creator);
        var organization = await _organizationService.GetAsync(input.OrganizationId);
        return $"{creator!.Name} has invited you to join {organization.DisplayName}";
    }

    public override async Task HandleAgreeAsync(OrganizationVisitInfo input)
    {
        await _organizationService.SetMemberRoleAsync(input.OrganizationId, new SetOrganizationMemberRoleDto
        {
            UserId = input.Vistor,
            RoleId = input.RoleId
        });
    }
}