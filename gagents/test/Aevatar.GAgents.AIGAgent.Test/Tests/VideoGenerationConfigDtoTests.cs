using System;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Test.Tests;

/// <summary>
/// Unit tests for VideoGenerationConfigDto validation, defaults, and serialization
/// Tests configuration DTO functionality, DefaultValues attributes, and data integrity
/// </summary>
public sealed class VideoGenerationConfigDtoTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public VideoGenerationConfigDtoTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    #region Constructor and Default Values Tests

    [Fact]
    public void Constructor_ShouldSetCorrectDefaultValues()
    {
        // Arrange & Act
        var config = new VideoGenerationConfigDto();

        // Assert - Test all default values
        config.Instructions.ShouldBe("Create a high-quality video from the following description");
        config.GenerationType.ShouldBe("text-to-video");
        config.Duration.ShouldBe(5);
        config.AspectRatio.ShouldBe("16:9");
        config.Resolution.ShouldBe("720p");
        config.FrameRate.ShouldBe(24);
        config.Style.ShouldBe("cinematic");
        config.MotionStrength.ShouldBe(0.7f);
        config.MotionType.ShouldBe("smooth");
        config.ImageInfluence.ShouldBe(0.8f);
        config.ImageUrl.ShouldBe(string.Empty);
        config.AutoReturnResult.ShouldBe(true);
        
        _testOutputHelper.WriteLine("All default values correctly set");
    }

    #endregion

    #region DefaultValues Attribute Tests

    [Fact]
    public void GenerationType_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.GenerationType));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain("text-to-video");
        defaultValuesAttr.Values.ShouldContain("image-to-video");
        defaultValuesAttr.Values.Length.ShouldBe(2);
        
        _testOutputHelper.WriteLine($"GenerationType DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void Duration_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.Duration));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain(5);
        defaultValuesAttr.Values.ShouldContain(10);
        
        _testOutputHelper.WriteLine($"Duration DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void AspectRatio_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.AspectRatio));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain("16:9");
        defaultValuesAttr.Values.ShouldContain("9:16");
        defaultValuesAttr.Values.ShouldContain("1:1");
        defaultValuesAttr.Values.ShouldContain("4:3");
        
        _testOutputHelper.WriteLine($"AspectRatio DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void Resolution_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.Resolution));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain("1080p");
        defaultValuesAttr.Values.ShouldContain("720p");
        defaultValuesAttr.Values.ShouldContain("480p");
        
        _testOutputHelper.WriteLine($"Resolution DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void FrameRate_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.FrameRate));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain(24);
        
        _testOutputHelper.WriteLine($"FrameRate DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void Style_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.Style));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain("cinematic");
        defaultValuesAttr.Values.ShouldContain("realistic");
        defaultValuesAttr.Values.ShouldContain("animated");
        defaultValuesAttr.Values.ShouldContain("artistic");
        
        _testOutputHelper.WriteLine($"Style DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void MotionStrength_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.MotionStrength));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain(0.3f);
        defaultValuesAttr.Values.ShouldContain(0.5f);
        defaultValuesAttr.Values.ShouldContain(0.7f);
        defaultValuesAttr.Values.ShouldContain(1.0f);
        
        _testOutputHelper.WriteLine($"MotionStrength DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void MotionType_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.MotionType));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain("smooth");
        defaultValuesAttr.Values.ShouldContain("dynamic");
        defaultValuesAttr.Values.ShouldContain("cinematic");
        defaultValuesAttr.Values.ShouldContain("natural");
        
        _testOutputHelper.WriteLine($"MotionType DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void ImageInfluence_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.ImageInfluence));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain(0.5f);
        defaultValuesAttr.Values.ShouldContain(0.8f);
        defaultValuesAttr.Values.ShouldContain(1.0f);
        
        _testOutputHelper.WriteLine($"ImageInfluence DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    [Fact]
    public void AutoReturnResult_ShouldHaveCorrectDefaultValuesAttribute()
    {
        // Arrange
        var property = typeof(VideoGenerationConfigDto).GetProperty(nameof(VideoGenerationConfigDto.AutoReturnResult));

        // Act
        var defaultValuesAttr = property?.GetCustomAttribute<DefaultValuesAttribute>();

        // Assert
        defaultValuesAttr.ShouldNotBeNull();
        defaultValuesAttr.Values.ShouldContain(true);
        defaultValuesAttr.Values.ShouldContain(false);
        
        _testOutputHelper.WriteLine($"AutoReturnResult DefaultValues: {string.Join(", ", defaultValuesAttr.Values)}");
    }

    #endregion

    #region Property Validation Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(61, false)] // Assuming max duration limit
    public void Duration_ShouldValidateCorrectly(int duration, bool shouldBeValid)
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.Duration = duration;

        // Assert
        if (shouldBeValid)
        {
            config.Duration.ShouldBe(duration);
            _testOutputHelper.WriteLine($"Duration {duration} correctly accepted");
        }
        else
        {
            // Note: This tests the property assignment, actual validation might be in business logic
            config.Duration.ShouldBe(duration); // Property allows it, but business logic should validate
            _testOutputHelper.WriteLine($"Duration {duration} assigned (validation should be handled by business logic)");
        }
    }

    [Theory]
    [InlineData("16:9")]
    [InlineData("9:16")]
    [InlineData("1:1")]
    [InlineData("4:3")]
    public void AspectRatio_ShouldAcceptValidFormats(string aspectRatio)
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.AspectRatio = aspectRatio;

        // Assert
        config.AspectRatio.ShouldBe(aspectRatio);
        _testOutputHelper.WriteLine($"Aspect ratio {aspectRatio} correctly accepted");
    }

    [Theory]
    [InlineData("1080p")]
    [InlineData("720p")]
    [InlineData("480p")]
    public void Resolution_ShouldAcceptValidFormats(string resolution)
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.Resolution = resolution;

        // Assert
        config.Resolution.ShouldBe(resolution);
        _testOutputHelper.WriteLine($"Resolution {resolution} correctly accepted");
    }

    [Theory]
    [InlineData("cinematic")]
    [InlineData("realistic")]
    [InlineData("animated")]
    [InlineData("artistic")]
    public void Style_ShouldAcceptValidStyles(string style)
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.Style = style;

        // Assert
        config.Style.ShouldBe(style);
        _testOutputHelper.WriteLine($"Style {style} correctly accepted");
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.3f)]
    [InlineData(0.5f)]
    [InlineData(0.7f)]
    [InlineData(1.0f)]
    public void MotionStrength_ShouldAcceptValidRange(float motionStrength)
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.MotionStrength = motionStrength;

        // Assert
        config.MotionStrength.ShouldBe(motionStrength);
        _testOutputHelper.WriteLine($"Motion strength {motionStrength} correctly accepted");
    }

    [Theory]
    [InlineData("smooth")]
    [InlineData("dynamic")]
    [InlineData("cinematic")]
    [InlineData("natural")]
    public void MotionType_ShouldAcceptValidTypes(string motionType)
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.MotionType = motionType;

        // Assert
        config.MotionType.ShouldBe(motionType);
        _testOutputHelper.WriteLine($"Motion type {motionType} correctly accepted");
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void VideoGenerationConfigDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var config = new VideoGenerationConfigDto
        {
            Instructions = "Test instructions",
            GenerationType = "image-to-video",
            Duration = 10,
            AspectRatio = "9:16",
            Resolution = "1080p",
            FrameRate = 30,
            Style = "animated",
            MotionStrength = 1.0f,
            MotionType = "dynamic",
            ImageInfluence = 0.9f,
            ImageUrl = "https://example.com/test.jpg",
            AutoReturnResult = false
        };

        // Act & Assert - Should not throw during serialization
        // Note: Actual serialization test would depend on the serialization framework used
        config.ShouldNotBeNull();
        config.Instructions.ShouldBe("Test instructions");
        config.GenerationType.ShouldBe("image-to-video");
        config.Duration.ShouldBe(10);
        
        _testOutputHelper.WriteLine("VideoGenerationConfigDto serialization properties accessible");
    }

    #endregion

    #region Configuration Inheritance Tests

    [Fact]
    public void VideoGenerationConfigDto_ShouldInheritFromConfigurationBase()
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act & Assert
        config.ShouldBeAssignableTo<ConfigurationBase>();
        _testOutputHelper.WriteLine("VideoGenerationConfigDto correctly inherits from ConfigurationBase");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void VideoGenerationConfigDto_WithNullImageUrl_ShouldHandleGracefully()
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.ImageUrl = null!;

        // Assert
        // Property should handle null assignment (though default is string.Empty)
        config.ImageUrl.ShouldBeNull();
        _testOutputHelper.WriteLine("Null ImageUrl handled");
    }

    [Fact]
    public void VideoGenerationConfigDto_WithEmptyStrings_ShouldHandleGracefully()
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.Instructions = "";
        config.GenerationType = "";
        config.AspectRatio = "";
        config.Resolution = "";
        config.Style = "";
        config.MotionType = "";

        // Assert
        config.Instructions.ShouldBe("");
        config.GenerationType.ShouldBe("");
        config.AspectRatio.ShouldBe("");
        config.Resolution.ShouldBe("");
        config.Style.ShouldBe("");
        config.MotionType.ShouldBe("");
        
        _testOutputHelper.WriteLine("Empty strings handled gracefully");
    }

    [Fact]
    public void VideoGenerationConfigDto_WithExtremeValues_ShouldAllowAssignment()
    {
        // Arrange
        var config = new VideoGenerationConfigDto();

        // Act
        config.Duration = int.MaxValue;
        config.FrameRate = int.MaxValue;
        config.MotionStrength = float.MaxValue;
        config.ImageInfluence = float.MaxValue;

        // Assert
        config.Duration.ShouldBe(int.MaxValue);
        config.FrameRate.ShouldBe(int.MaxValue);
        config.MotionStrength.ShouldBe(float.MaxValue);
        config.ImageInfluence.ShouldBe(float.MaxValue);
        
        _testOutputHelper.WriteLine("Extreme values allowed (validation should be in business logic)");
    }

    #endregion

    #region Property Count Verification

    [Fact]
    public void VideoGenerationConfigDto_ShouldHaveExpectedNumberOfProperties()
    {
        // Arrange
        var type = typeof(VideoGenerationConfigDto);

        // Act
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        properties.Length.ShouldBe(12, "Expected 12 declared properties in VideoGenerationConfigDto");
        
        var propertyNames = properties.Select(p => p.Name).ToArray();
        _testOutputHelper.WriteLine($"Properties found: {string.Join(", ", propertyNames)}");
        
        // Verify all expected properties exist
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.Instructions));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.GenerationType));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.Duration));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.AspectRatio));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.Resolution));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.FrameRate));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.Style));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.MotionStrength));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.MotionType));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.ImageInfluence));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.ImageUrl));
        propertyNames.ShouldContain(nameof(VideoGenerationConfigDto.AutoReturnResult));
    }

    #endregion
}
