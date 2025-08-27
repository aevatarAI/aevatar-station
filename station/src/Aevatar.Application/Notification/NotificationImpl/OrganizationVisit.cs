using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.Notification.Parameters;
using Aevatar.Organizations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.Identity;

namespace Aevatar.Notification.NotificationImpl;

public class OrganizationVisit : NotificationHandlerBase<OrganizationVisitInfo>
{
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _userManager;
    private readonly ILogger<OrganizationVisit> _logger;

    public OrganizationVisit(IOrganizationService organizationService, IdentityUserManager userManager, ILogger<OrganizationVisit> logger)
    {
        _organizationService = organizationService;
        _userManager = userManager;
        _logger = logger;
    }

    public override NotificationTypeEnum Type => NotificationTypeEnum.OrganizationInvitation;

    public override OrganizationVisitInfo ConvertInput(string input)
    {
        return JsonConvert.DeserializeObject<OrganizationVisitInfo>(input);
    }

    public override async Task<string> GetContentForShowAsync(OrganizationVisitInfo input)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var creator = await _userManager.GetByIdAsync(input.Creator);
        var organization = await _organizationService.GetAsync(input.OrganizationId);
        
        stopWatch.Stop();
        _logger.LogInformation($"StopWatch GetContentForShowAsync use time:{stopWatch.ElapsedMilliseconds}");
        
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

    public override async Task HandleRefuseAsync(OrganizationVisitInfo input)
    {
        await _organizationService.RefuseInvitationAsync(input.OrganizationId, input.Vistor);
    }
}