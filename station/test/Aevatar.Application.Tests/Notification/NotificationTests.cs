using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Organizations;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;
using Volo.Abp.Identity;
using Volo.Abp.Domain.Repositories;
using Aevatar.Notification.Parameters;
using Volo.Abp.Users;

namespace Aevatar.Notification;

public abstract class NotificationTests<TStartupModule> : AevatarApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IOrganizationService _organizationService;
    private readonly IdentityUserManager _userManager;
    private readonly Guid _creator = Guid.Parse("fb63293b-fdde-4730-b10a-e95c373797c2");
    private readonly Guid _receiveId = Guid.Parse("da63293b-fdde-4730-b10a-e95c37379703");
    private readonly CancellationToken _cancellation;
    private readonly Guid _roleId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public NotificationTests()
    {
        _notificationRepository = GetRequiredService<INotificationRepository>();
        _organizationService = GetRequiredService<IOrganizationService>();
        _currentUser = GetRequiredService<ICurrentUser>();
        _cancellation = new CancellationToken();
        _userManager = GetRequiredService<IdentityUserManager>();
        _notificationService = GetRequiredService<INotificationService>();
    }

    [Fact]
    public virtual async Task CreatNotification_Test()
    {
        var response =await CreatNotificationAsync();
        response.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task WithdrawAsync_Test()
    {
        var notificationInfo = await _notificationRepository.InsertAsync(new NotificationInfo
        {
            Type = NotificationTypeEnum.OrganizationInvitation,
            Input = new Dictionary<string, object>(),
            Content = "",
            Receiver = _receiveId,
            Status = NotificationStatusEnum.None,
            CreationTime = DateTime.Now,
            CreatorId = _creator,
        },true, _cancellation);
        var response = await _notificationService.WithdrawAsync(_creator, notificationInfo.Id);
        response.ShouldBeTrue();
    }

    [Fact]
    public async Task ResponseAsync_Test()
    {
        await CreatNotificationAsync();
        var notification = await _notificationRepository.FirstAsync(cancellationToken: _cancellation);
        
        var response = await _notificationService.Response(notification.Id, notification.Receiver, NotificationStatusEnum.Agree);

        response.ShouldBeTrue();
    }
    
    private async Task<Guid> CreatNotificationAsync()
    {
        var owner = new IdentityUser(_currentUser.Id!.Value, "owner", "owner@email.io");
        await _userManager.CreateAsync(owner);
        // Create test users
        var creator = new IdentityUser(
            _creator,
            "test@creator.com",
            "test@creator.com")
        {
            Name = "Test Creator"
        };

        var receiver = new IdentityUser(
            _receiveId,
            "test@receiver.com",
            "test@receiver.com")
        {
            Name = "Test Receiver"
        };

        await _userManager.CreateAsync(creator, "1q2w3E*");
        await _userManager.CreateAsync(receiver, "1q2w3E*");

        var organizationDto = await _organizationService.CreateAsync(new CreateOrganizationDto
        {
            DisplayName = "Test Organization"
        });

        await _organizationService.SetMemberAsync(organizationDto.Id, new SetOrganizationMemberDto
        {
            Email = receiver.Email,
            Join = true,
            RoleId = _roleId
        });

        var organizationVisitInfo = new OrganizationVisitInfo
        {
            Creator = creator.Id,
            OrganizationId = organizationDto.Id,
            RoleId = _roleId,
            Vistor = receiver.Id
        };
        var input = JsonConvert.SerializeObject(organizationVisitInfo);

        return await _notificationService.CreateAsync(NotificationTypeEnum.OrganizationInvitation, creator.Id,
            receiver.Id, input);
    }
}