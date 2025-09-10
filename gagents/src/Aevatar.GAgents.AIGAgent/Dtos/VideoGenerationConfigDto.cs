using Orleans;
using Aevatar.GAgents.AI.Common;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AIGAgent.Dtos;

[GenerateSerializer]
public class VideoGenerationConfigDto : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("Create a high-quality video from the following description")]
    public string Instructions { get; set; } = "Create a high-quality video from the following description";

    [Id(1)]
    [DefaultValues("text-to-video", "image-to-video")]
    public string GenerationType { get; set; } = "text-to-video";

    [Id(2)]
    [DefaultValues(5, 10)]
    public int Duration { get; set; } = 5; // seconds

    [Id(3)]
    [DefaultValues("16:9", "9:16", "1:1", "4:3")]
    public string AspectRatio { get; set; } = "16:9";

    [Id(4)]
    [DefaultValues("1080p", "720p", "480p")]
    public string Resolution { get; set; } = "720p";

    [Id(5)]
    [DefaultValues(24)]
    public int FrameRate { get; set; } = 24;

    [Id(6)]
    [DefaultValues("cinematic", "realistic", "animated", "artistic")]
    public string Style { get; set; } = "cinematic";

    [Id(7)]
    [DefaultValues(0.3f, 0.5f, 0.7f, 1.0f)]
    public float MotionStrength { get; set; } = 0.7f;

    [Id(8)]
    [DefaultValues("smooth", "dynamic", "cinematic", "natural")]
    public string MotionType { get; set; } = "smooth";

    [Id(9)]
    [DefaultValues(0.5f, 0.8f, 1.0f)]
    public float ImageInfluence { get; set; } = 0.8f;

    [Id(10)]
    [DefaultValues("")]
    public string ImageUrl { get; set; } = string.Empty;

    [Id(11)]
    [DefaultValues(true, false)]
    public bool AutoReturnResult { get; set; } = true;
}
