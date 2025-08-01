using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Application.Grains.ChatManager.Dtos;
using Aevatar.Application.Grains.ChatManager.UserBilling;
using Aevatar.Application.Grains.Common.Constants;
using Aevatar.Application.Grains.Common.Options;
using Aevatar.Application.Grains.Invitation;
using Aevatar.Application.Grains.Twitter.Dtos;
using Aevatar.GodGPT.Dtos;
using Aevatar.Service;
using Aevatar.Extensions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using Orleans;
using Stripe;
using Volo.Abp;
using Volo.Abp.Security.Claims;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Invitation")]
[Route("api/godgpt/invitation")]
[Authorize]
public class GodGPTInvitationController : AevatarController
{
    private readonly ILogger<GodGPTPaymentController> _logger;
    private readonly IOptionsMonitor<StripeOptions> _stripeOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IGodGPTService _godGptService;

    public GodGPTInvitationController(IClusterClient clusterClient, ILogger<GodGPTPaymentController> logger,
        IGodGPTService godGptService, IOptionsMonitor<StripeOptions> stripeOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _godGptService = godGptService;
        _stripeOptions = stripeOptions;
    }
    
    [HttpGet("info")]
    public async Task<GetInvitationInfoResponse> GetInvitationInfoAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var invitationInfo = await _godGptService.GetInvitationInfoAsync(currentUserId);
        _logger.LogDebug("[GodGPTInvitationController][GetInvitationInfoAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return invitationInfo;
    }

    [HttpPost("redeem")]
    public async Task<RedeemInviteCodeResponse> RedeemInviteCodeAsync(RedeemInviteCodeRequest input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var response = await _godGptService.RedeemInviteCodeAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTInvitationController][RedeemInviteCodeAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return response;
    }

    [HttpGet("credits/history")]
    public async Task<PagedResultDto<RewardHistoryDto>> GetCreditsHistoryAsync(GetCreditsHistoryInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var response = await _godGptService.GetCreditsHistoryAsync(currentUserId, input);
        _logger.LogDebug("[GodGPTInvitationController][GetCreditsHistoryAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return response;
    }
    
    [HttpGet("twitter/params")]
    public async Task<TwitterAuthParamsDto> GetTwitterAuthParamsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = (Guid)CurrentUser.Id!;
        var response = await _godGptService.GetTwitterAuthParamsAsync(currentUserId);
        _logger.LogDebug("[GodGPTInvitationController][GetTwitterAuthParamsAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return response;
    }

    [HttpPost("twitter/verify")]
    public async Task<TwitterAuthResultDto> TwitterAuthVerifyAsync(TwitterAuthVerifyInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var language = HttpContext.GetGodGPTLanguage();
        var currentUserId = (Guid)CurrentUser.Id!;
        var response = await _godGptService.TwitterAuthVerifyAsync(currentUserId, input, language);
        _logger.LogDebug("[GodGPTInvitationController][TwitterAuthVerifyAsync] userId: {0}, duration: {1}ms",
            currentUserId.ToString(), stopwatch.ElapsedMilliseconds);
        return response;
    }
}