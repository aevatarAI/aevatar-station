using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Aevatar.GodGPT.Options;
using Amazon;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GodGPT;

public interface IAwsS3Client
{
    Task<string> UpLoadFileAsync(Stream steam, string fileName);
    Task<string> UpLoadFileFrontEndAsync(Stream steam, string fileName);
    Task<string> UpLoadBase64FileAsync(string base64Image, string fileName);
}

public class AwsS3Client : IAwsS3Client, ITransientDependency
{
    private readonly ILogger<AwsS3Client> _logger;
    private readonly IOptionsMonitor<AwsS3Option> _awsS3Option;
    private AmazonS3Client _amazonS3Client;
    private AmazonS3Client _amazonS3ClientFrontEnd;

    public AwsS3Client(IOptionsMonitor<AwsS3Option> awsS3Option, ILogger<AwsS3Client> logger)
    {
        _awsS3Option = awsS3Option;
        _logger = logger;
        InitAmazonS3Client();
        InitAmazonS3ClientFrontEnd();
    }

    private void InitAmazonS3Client()
    {
        var accessKeyId = _awsS3Option.CurrentValue.AccessKey;
        var secretKey = _awsS3Option.CurrentValue.SecretKey;
        var serviceUrl = _awsS3Option.CurrentValue.ServiceURL;
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            RegionEndpoint = RegionEndpoint.APNortheast1
        };
        _amazonS3Client = new AmazonS3Client(accessKeyId, secretKey, config);
    }
    
    private void InitAmazonS3ClientFrontEnd()
    {
        var accessKeyId = _awsS3Option.CurrentValue.AccessKeyFrontEnd;
        var secretKey = _awsS3Option.CurrentValue.SecretKeyFrontEnd;
        var serviceUrl = _awsS3Option.CurrentValue.ServiceURL;
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            RegionEndpoint = RegionEndpoint.APNortheast1
        };
        _amazonS3ClientFrontEnd = new AmazonS3Client(accessKeyId, secretKey, config);
    }

    public async Task<string> UpLoadFileFrontEndAsync(Stream steam, string fileName)
    {
        try
        {
            var putObjectRequest = new PutObjectRequest
            {
                InputStream = steam,
                BucketName = _awsS3Option.CurrentValue.BucketNameFrontEnd,
                Key = _awsS3Option.CurrentValue.S3KeyFrontEnd + "/" + fileName,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = "image/webp",
                Headers =
                {
                    ContentDisposition = "inline" 
                }
            };
            var putObjectResponse = await _amazonS3ClientFrontEnd.PutObjectAsync(putObjectRequest);
            return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
                ? $"https://{_awsS3Option.CurrentValue.BucketNameFrontEnd}.s3.amazonaws.com/{putObjectRequest.Key}"
                : string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public async Task<string> UpLoadBase64FileAsync(string base64Image, string fileName)
    {
        // base64Image = base64Image.TrimStart("data:image/png;base64".ToCharArray());
        var vals = base64Image.Split(CommonConstant.Comma);
        AssertHelper.NotEmpty(vals, "Invalid image base64");
        var imageBytes = Convert.FromBase64String(vals[vals.Length - 1]);
        using var ms = new MemoryStream(imageBytes);
        return await UpLoadFileAsync(ms, fileName);
    }

    public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.CurrentValue.BucketName,
            Key = _awsS3Option.CurrentValue.S3Key + "/" + fileName,
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
            ? $"https://{_awsS3Option.CurrentValue.BucketName}.s3.amazonaws.com/{putObjectRequest.Key}"
            : string.Empty;
    }
}