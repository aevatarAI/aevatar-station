using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;

namespace Aevatar.BlobStorings;

public interface IThumbnailService
{
    Task<List<ThumbnailInfo>> SaveWithThumbnailsAsync(IFormFile file, string fileName);
    Task<List<ThumbnailInfo>> GenerateThumbnailsAsync(Stream imageStream, string baseFileName);
} 

public class ThumbnailService : IThumbnailService, ITransientDependency
{
    private readonly IBlobContainer _blobContainer;
    private readonly ThumbnailOptions _options;
    private readonly BlobStoringOptions _blobStoringOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ThumbnailService> _logger;

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff" };

    public ThumbnailService(
        IBlobContainer blobContainer,
        IOptionsSnapshot<ThumbnailOptions> options,
        IOptionsSnapshot<BlobStoringOptions> blobStoringOptions,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ThumbnailService> logger)
    {
        _blobContainer = blobContainer;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
        _blobStoringOptions = blobStoringOptions.Value;
    }

    public async Task<List<ThumbnailInfo>> SaveWithThumbnailsAsync(IFormFile file, string fileName)
    {
        // Generate thumbnails if it's an image and thumbnails are enabled
        if (!_options.EnableThumbnail || !IsImageFile(file.FileName))
        {
            return new List<ThumbnailInfo>();
        }

        using var imageStream = file.OpenReadStream();
        return await GenerateThumbnailsAsync(imageStream, fileName);
    }

    public async Task<List<ThumbnailInfo>> GenerateThumbnailsAsync(Stream imageStream, string baseFileName)
    {
        var thumbnails = new List<ThumbnailInfo>();
        
        if (!_options.Sizes.Any())
        {
            return thumbnails;
        }

        try
        {
            var thumbnailStopwatch = Stopwatch.StartNew();
            using var image = await Image.LoadAsync(imageStream);
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Process thumbnails in parallel for better performance
            var tasks = _options.Sizes.Select(async size =>
            {
                var thumbnailInfo = await CreateThumbnailAsync(image, size, baseFileName, originalWidth, originalHeight);
                return thumbnailInfo;
            });

            var results = await Task.WhenAll(tasks);
            thumbnails.AddRange(results.Where(t => t != null)!);
            thumbnailStopwatch.Stop();
            _logger.LogDebug("[GodGPTController][BlobSaveAsync] Thumbnail generation completed: Duration={ThumbnailTime}ms",
                thumbnailStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking the main upload
            _logger.LogError(ex, "Error generating thumbnails: {0}", ex.Message);
        }

        return thumbnails;
    }

    private async Task<ThumbnailInfo?> CreateThumbnailAsync(
        Image originalImage, 
        ThumbnailSize size, 
        string baseFileName, 
        int originalWidth,
        int originalHeight)
    {
        try
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
            var extension = Path.GetExtension(baseFileName);
            var fileName = $"{fileNameWithoutExtension}@{size.GetSizeName()}{extension}";
            
            // Calculate dimensions based on resize mode
            var (width, height) = CalculateDimensions(originalWidth, originalHeight, size);
            
            // If thumbnail would be larger than original, save original image directly
            if (width >= originalWidth && height >= originalHeight)
            {
                long originalSize;
                
                using (var originalStream = new MemoryStream())
                {
                    var encoder = GetEncoder();
                    await originalImage.SaveAsync(originalStream, encoder);
                    
                    originalSize = originalStream.Length;
                    
                    // Save to blob storage using a new scope to avoid lifetime issues
                    originalStream.Seek(0, SeekOrigin.Begin);
                    using var scope = _serviceScopeFactory.CreateScope();
                    var blobContainer = scope.ServiceProvider.GetRequiredService<IBlobContainer>();
                    await blobContainer.SaveAsync(fileName, originalStream, true);
                }
                
                return new ThumbnailInfo
                {
                    FileName = fileName,
                    SizeName = size.GetSizeName(),
                    Width = originalWidth,
                    Height = originalHeight,
                    FileSize = originalSize
                };
            }

            int thumbnailWidth, thumbnailHeight;
            long fileSize;
            
            using (var thumbnail = originalImage.Clone(ctx => {}))
            {
                // Apply resizing
                switch (size.ResizeMode)
                {
                    case ResizeMode.Max:
                        thumbnail.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                        }));
                        break;
                        
                    case ResizeMode.Crop:
                        thumbnail.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(size.Width, size.Height),
                            Mode = SixLabors.ImageSharp.Processing.ResizeMode.Crop
                        }));
                        break;
                        
                    case ResizeMode.Stretch:
                        thumbnail.Mutate(x => x.Resize(size.Width, size.Height));
                        break;
                }

                // Save dimensions before thumbnail is disposed
                thumbnailWidth = thumbnail.Width;
                thumbnailHeight = thumbnail.Height;

                // Save thumbnail to memory stream first to get file size
                using (var thumbnailStream = new MemoryStream())
                {
                    var encoder = GetEncoder();
                    await thumbnail.SaveAsync(thumbnailStream, encoder);
                    
                    // Save file size before stream is disposed
                    fileSize = thumbnailStream.Length;
                    
                    // Save to blob storage using a new scope to avoid lifetime issues
                    thumbnailStream.Seek(0, SeekOrigin.Begin);
                    using var scope = _serviceScopeFactory.CreateScope();
                    var blobContainer = scope.ServiceProvider.GetRequiredService<IBlobContainer>();
                    await blobContainer.SaveAsync(fileName, thumbnailStream, true);
                }
            }

            return new ThumbnailInfo
            {
                FileName = fileName,
                SizeName = size.GetSizeName(),
                Width = thumbnailWidth,
                Height = thumbnailHeight,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            // Log error but continue with other thumbnails
            _logger.LogError(ex, "Error creating thumbnail {name}: {message}", size.GetSizeName(), ex.Message);
            return null;
        }
    }

    private (int width, int height) CalculateDimensions(int originalWidth, int originalHeight, ThumbnailSize size)
    {
        switch (size.ResizeMode)
        {
            case ResizeMode.Crop:
            case ResizeMode.Stretch:
                return (size.Width, size.Height);
                
            case ResizeMode.Max:
            default:
                var aspectRatio = (double)originalWidth / originalHeight;
                
                if (originalWidth > originalHeight)
                {
                    var width = Math.Min(size.Width, originalWidth);
                    var height = (int)(width / aspectRatio);
                    return (width, height);
                }
                else
                {
                    var height = Math.Min(size.Height, originalHeight);
                    var width = (int)(height * aspectRatio);
                    return (width, height);
                }
        }
    }

    private IImageEncoder GetEncoder()
    {
        return _options.Format.ToLower() switch
        {
            "webp" => new WebpEncoder { Quality = _options.Quality },
            "png" => new PngEncoder(),
            "jpg" or "jpeg" => new JpegEncoder { Quality = _options.Quality },
            _ => new WebpEncoder { Quality = _options.Quality }
        };
    }

    public bool IsImageFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return ImageExtensions.Contains(extension);
    }
} 