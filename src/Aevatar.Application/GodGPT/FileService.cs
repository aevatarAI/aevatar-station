using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace Aevatar.GodGPT;

public interface IFileService
{
    Task<string> UploadAsync(string chainId, IFormFile file);
    Task<string> UploadFrontEndAsync(string url, string fileName);
    Task<Stream> DownloadImageAsync(string url);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class FileService : ApplicationService, IFileService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly IAwsS3Client _awsS3Client;
    private readonly IUserProvider _userProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileService> _logger;
    private readonly HttpClient _httpClient;

    public FileService(IAwsS3Client awsS3Client, IUserProvider userProvider, IUserBalanceProvider userBalanceProvider, 
        IHttpClientFactory httpClientFactory, ILogger<FileService> logger)
    {
        _awsS3Client = awsS3Client;
        _userBalanceProvider = userBalanceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _userProvider = userProvider;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<string> UploadAsync(string chainId, IFormFile file)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var symbol = CommonConstant.GetVotigramSymbol(chainId);
        var userBalance = await _userBalanceProvider.GetByIdAsync(GuidHelper.GenerateGrainId(address, chainId, symbol));
        if (userBalance == null || userBalance.Amount < 1)
        {
            throw new UserFriendlyException("Nft Not enough.");
        }
        if (file == null || file.Length == 0)
        {
            throw new UserFriendlyException("File is null or empty.");
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new UserFriendlyException("File type is not allowed.");
        }

        await using var stream = file.OpenReadStream();
        var utf8Bytes = await stream.GetAllBytesAsync();
        var url = await _awsS3Client.UpLoadFileAsync(new MemoryStream(utf8Bytes), 
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + CommonConstant.Underline + file.FileName);
        return url;
    }

    public async Task<string> UploadFrontEndAsync(string url, string fileName)
    {
        _logger.LogInformation($"UploadFrontEndAsyncBegin url {url} fileName {fileName}");
        var (baseUrl, extension) = ParseUrl(url);
        await using var stream = await DownloadImageAsync(baseUrl);
        if (stream == null)
        {
            _logger.LogInformation($"UploadFrontEndAsyncDownloadFail url {url} fileName {fileName}");
            return string.Empty;
        }

        await using var memoryStream = await ConvertToWebp(extension, stream);
        if (memoryStream == null)
        {
            _logger.LogInformation($"UploadFrontEndAsyncConvertFail url {url} fileName {fileName}");
            stream.Seek(0, SeekOrigin.Begin); 
            var originResult = await _awsS3Client.UpLoadFileFrontEndAsync(stream, fileName + extension);
            _logger.LogInformation($"UploadFrontEndAsyncOriginResult url {url} result {originResult}");
            return originResult;
        }

        var result = await _awsS3Client.UpLoadFileFrontEndAsync(memoryStream, fileName + ".webp");
        _logger.LogInformation($"UploadFrontEndAsyncResult url {url} result {result}");
        return result;
    }

    public async Task<Stream> DownloadImageAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    private async Task<MemoryStream> ConvertToWebp(string extension, Stream stream)
    {
        try
        {
            var memoryStream = new MemoryStream();
            if (extension == ".webp")
            {
                await stream.CopyToAsync(memoryStream);
            }
            else
            {
                using var image = await Image.LoadAsync(stream);
                await image.SaveAsWebpAsync(memoryStream);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private Tuple<string, string> ParseUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var baseUri = new UriBuilder(uri) { Query = string.Empty }.Uri;
            var extension = Path.GetExtension(baseUri.LocalPath).ToLower();
            return new Tuple<string, string>(baseUri.ToString(), extension);
        }
        catch (Exception)
        {
            return new Tuple<string, string>(string.Empty, string.Empty);
        }
    }
}