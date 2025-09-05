using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.Analytics;
using Aevatar.Dtos;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Aevatar.Controllers;

/// <summary>
/// Apple attribution report receiving controller
/// Implements NSAdvertisingAttributionReportEndpoint receiving endpoint
/// Supports Private Click Measurement (PCM) and SKAdNetwork postbacks
/// </summary>
[RemoteService]
[ControllerName("AppleADNetwork")]
[Route("api/apple/ad-network")]
public class AppleADNetworkController : AevatarController
{
    private readonly IAppleSignatureVerificationService _signatureVerificationService;
    private readonly IDeviceMappingService _deviceMappingService;
    private readonly ILogger<AppleADNetworkController> _logger;

    public AppleADNetworkController(
        IAppleSignatureVerificationService signatureVerificationService,
        IGoogleAnalyticsService googleAnalyticsService,
        IDeviceMappingService deviceMappingService,
        ILogger<AppleADNetworkController> logger)
    {
        _signatureVerificationService = signatureVerificationService;
        _deviceMappingService = deviceMappingService;
        _logger = logger;
    }

    /// <summary>
    /// Register device mapping for Firebase Analytics attribution
    /// iOS clients call this endpoint to establish IDFV -> Firebase Instance ID mapping
    /// </summary>
    /// <param name="request">Device mapping registration data</param>
    /// <returns>Registration result</returns>
    [Authorize]
    [HttpPost("register-device-mapping")]
    public async Task<IActionResult> RegisterDeviceMappingAsync(DeviceMappingRegistrationDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("[AppleADNetworkController][RegisterDeviceMappingAsync] Received device mapping registration request: IDFV={Idfv}, BundleId={BundleId}",
                request.Idfv, request.BundleId);

            // Validate input
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[AppleADNetworkController][RegisterDeviceMappingAsync] Invalid model state for IDFV: {Idfv}", request.Idfv);
                return BadRequest(ModelState);
            }

            // Delegate business logic to service
            var response = await _deviceMappingService.RegisterDeviceMappingAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning("[AppleADNetworkController][RegisterDeviceMappingAsync] Registration failed: {Message}", response.Message);
                return BadRequest(response);
            }

            _logger.LogInformation("[AppleADNetworkController][RegisterDeviceMappingAsync] Device mapping registration completed: IDFV={Idfv}, Success={Success}, Duration={Duration}ms",
                request.Idfv, response.Success, stopwatch.ElapsedMilliseconds);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppleADNetworkController][RegisterDeviceMappingAsync] Unexpected error during device mapping registration for IDFV: {Idfv}", request.Idfv);
            
            return StatusCode(500, new DeviceMappingRegistrationResponseDto
            {
                Success = false,
                Message = "Internal server error occurred during registration"
            });
        }
    }

    /// <summary>
    /// Apple attribution report receiving endpoint
    /// Receives POST requests from iOS NSAdvertisingAttributionReportEndpoint
    /// </summary>
    /// <param name="report">Attribution report data</param>
    /// <returns>Processing result</returns>
    [HttpPost("postback")]
    public async Task<IActionResult> ReceiveAttributionReportAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Log all request headers
            var headersInfo = string.Join(", ", HttpContext.Request.Headers.Select(h => $"{h.Key}:{h.Value}"));
            _logger.LogDebug("[AppleAttributionController][ReceiveAttributionReportAsync] REQUEST HEADERS: {Headers}", headersInfo);
            
            // Read request body directly - simple and effective approach
            using var streamReader = new StreamReader(Request.Body, Encoding.UTF8);
            var requestJson = await streamReader.ReadToEndAsync();
            
            // Log raw request body
            _logger.LogDebug("[AppleAttributionController][ReceiveAttributionReportAsync] RAW BODY: {RawBody}", requestJson);
            
            // Check if body is empty
            if (string.IsNullOrWhiteSpace(requestJson))
            {
                _logger.LogWarning("[AppleAttributionController][ReceiveAttributionReportAsync] Request body is empty");
                return BadRequest(new { status = "error", message = "Request body is empty" });
            }
            
            // Convert to strongly typed object
            AppleAttributionReportDto report;
            try
            {
                report = JsonSerializer.Deserialize<AppleAttributionReportDto>(requestJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                })!;
                
                // Additional null check after deserialization
                if (report == null)
                {
                    _logger.LogError("[AppleAttributionController][ReceiveAttributionReportAsync] Deserialization returned null");
                    return BadRequest(new { status = "error", message = "Invalid JSON format or structure" });
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "[AppleAttributionController][ReceiveAttributionReportAsync] JSON deserialization failed: {JsonBody}", requestJson);
                return BadRequest(new { status = "error", message = "Invalid JSON format", details = jsonEx.Message });
            }
            
            _logger.LogInformation("[AppleAttributionController][ReceiveAttributionReportAsync] Received Apple attribution report: TransactionId={TransactionId}, AdNetworkId={AdNetworkId}, AppId={AppId}",
                report.TransactionId, report.AdNetworkId, report.AppId);

            // 2. Verify Apple signature
            var verificationResult = await _signatureVerificationService.VerifySignatureAsync(report);
            
            if (!verificationResult.IsValid)
            {
                _logger.LogWarning("[AppleAttributionController][ReceiveAttributionReportAsync] Signature verification failed for transaction {TransactionId}: {ErrorMessage}",
                    report.TransactionId, verificationResult.ErrorMessage);
                
                // For signature verification failure, we still return 200 OK to avoid Apple resending
                // But log the issue
                return Ok(new { status = "received", verified = false, message = "Signature verification failed" });
            }

            _logger.LogInformation("[AppleAttributionController][ReceiveAttributionReportAsync] Signature verification successful for transaction {TransactionId}, Version={Version}, IsWinning={IsWinning}",
                report.TransactionId, verificationResult.Version, verificationResult.IsWinningAttribution);

            // 3. Forward to Firebase Analytics (background async processing)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _signatureVerificationService.ForwardToFirebaseAnalyticsAsync(report, verificationResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AppleAttributionController][ReceiveAttributionReportAsync] Background Firebase forwarding failed for transaction: {TransactionId}", 
                        report.TransactionId);
                }
            });

            _logger.LogInformation("[AppleAttributionController][ReceiveAttributionReportAsync] Apple attribution report processed successfully: TransactionId={TransactionId}, Duration={Duration}ms",
                report.TransactionId, stopwatch.ElapsedMilliseconds);

            // 4. Return success response (Apple requires 200 OK)
            return Ok(new 
            { 
                status = "received", 
                verified = true, 
                transactionId = report.TransactionId,
                version = verificationResult.Version,
                isWinning = verificationResult.IsWinningAttribution
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppleAttributionController][ReceiveAttributionReportAsync] Error processing Apple attribution report");
                
            // Return 200 OK even for errors to avoid Apple resending
            return Ok(new { status = "error", message = "Internal processing error" });
        }
    }
}


