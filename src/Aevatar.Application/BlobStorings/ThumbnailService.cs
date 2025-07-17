using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

public class ThumbnailService : IThumbnailService, ITransientDependency
{
    private readonly IBlobContainer _blobContainer;
    private readonly ThumbnailOptions _options;
    private readonly BlobStoringOptions _blobStoringOptions;
    
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff" };

    public ThumbnailService(
        IBlobContainer blobContainer,
        IOptionsSnapshot<ThumbnailOptions> options,
        IOptionsSnapshot<BlobStoringOptions> blobStoringOptions)
    {
        _blobContainer = blobContainer;
        _options = options.Value;
        _blobStoringOptions = blobStoringOptions.Value;
    }

    public async Task<SaveBlobResponse> SaveWithThumbnailsAsync(IFormFile file, string fileName)
    {
        var response = new SaveBlobResponse
        {
            OriginalFileName = fileName,
            OriginalSize = file.Length
        };

        // Save original file
        using var originalStream = file.OpenReadStream();
        await _blobContainer.SaveAsync(fileName, originalStream, true);

        // Generate thumbnails if it's an image and thumbnails are enabled
        if (_options.EnableThumbnail && IsImageFile(file.FileName))
        {
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var fileExtension = Path.GetExtension(fileName);
            
            using var imageStream = file.OpenReadStream();
            response.Thumbnails = await GenerateThumbnailsAsync(imageStream, baseFileName, fileExtension);
        }

        return response;
    }

    public async Task<List<ThumbnailInfo>> GenerateThumbnailsAsync(Stream imageStream, string baseFileName, string fileExtension)
    {
        var thumbnails = new List<ThumbnailInfo>();
        
        if (!_options.Sizes.Any())
        {
            return thumbnails;
        }

        try
        {
            using var image = await Image.LoadAsync(imageStream);
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Process thumbnails in parallel for better performance
            var tasks = _options.Sizes.Select(async size =>
            {
                var thumbnailInfo = await CreateThumbnailAsync(image, size, baseFileName, fileExtension, originalWidth, originalHeight);
                return thumbnailInfo;
            });

            var results = await Task.WhenAll(tasks);
            thumbnails.AddRange(results.Where(t => t != null)!);
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking the main upload
            // TODO: Add proper logging
            Console.WriteLine($"Error generating thumbnails: {ex.Message}");
        }

        return thumbnails;
    }

    private async Task<ThumbnailInfo?> CreateThumbnailAsync(
        Image originalImage, 
        ThumbnailSize size, 
        string baseFileName, 
        string fileExtension,
        int originalWidth,
        int originalHeight)
    {
        try
        {
            var thumbnailFileName = $"{baseFileName}_{size.Name}{GetThumbnailExtension()}";
            
            // Calculate dimensions based on resize mode
            var (width, height) = CalculateDimensions(originalWidth, originalHeight, size);
            
            // Skip if thumbnail would be larger than original
            if (width >= originalWidth && height >= originalHeight)
            {
                return null;
            }

            using var thumbnail = originalImage.Clone(ctx => {});
            
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

            // Save thumbnail to memory stream first to get file size
            using var thumbnailStream = new MemoryStream();
            var encoder = GetEncoder();
            await thumbnail.SaveAsync(thumbnailStream, encoder);
            
            // Save to blob storage
            thumbnailStream.Seek(0, SeekOrigin.Begin);
            await _blobContainer.SaveAsync(thumbnailFileName, thumbnailStream, true);

            return new ThumbnailInfo
            {
                FileName = thumbnailFileName,
                SizeName = size.Name,
                Width = thumbnail.Width,
                Height = thumbnail.Height,
                FileSize = thumbnailStream.Length
            };
        }
        catch (Exception ex)
        {
            // Log error but continue with other thumbnails
            Console.WriteLine($"Error creating thumbnail {size.Name}: {ex.Message}");
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

    private string GetThumbnailExtension()
    {
        return _options.Format.ToLower() switch
        {
            "webp" => ".webp",
            "png" => ".png", 
            "jpg" or "jpeg" => ".jpg",
            _ => ".webp"
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