# 🎬 Video Generation Demo

This demo showcases the Aevatar Video Generation Agent capabilities, demonstrating AI-powered video generation from text prompts and images using BytePlus ModelArk integration.

## 🚀 Features

- **Text-to-Video Generation**: Create videos from descriptive text prompts
- **Image-to-Video Generation**: Animate static images with AI-powered motion
- **Customizable Options**: Control duration, resolution, style, and motion parameters
- **Performance Testing**: Benchmark concurrent video generation requests
- **Interactive Console**: User-friendly interface with rich formatting
- **Real-time Status**: Track video generation progress and results

## 📋 Prerequisites

1. **.NET 9.0 SDK** - Required for running the application
2. **BytePlus ModelArk API Key** - For video generation services
3. **Orleans Cluster** - Automatically set up by the demo application

## ⚙️ Configuration

1. **Update API Configuration**:
   Edit `appsettings.json` and replace `your-byteplus-api-key-here` with your actual BytePlus ModelArk API key:

   ```json
   {
     "SystemLLMConfigs": {
       "BytePlusVideoGeneration": {
         "ProviderEnum": "BytePlus",
         "ModelIdEnum": "BytePlusVideoGeneration",
         "ModelName": "seedance-1-0-pro-250528",
         "Endpoint": "https://ark.ap-southeast.bytepluses.com",
         "ApiKey": "your-actual-api-key-here",
         "NetworkTimeoutInSeconds": 300
       }
     },
     "VideoGeneration": {
       "DefaultOptions": {
         "Duration": 5,
         "AspectRatio": "16:9",
         "Resolution": "1080p",
         "FrameRate": 24,
         "Style": "realistic",
         "MotionStrength": 0.7,
         "MotionType": "smooth",
         "ImageInfluence": 0.8
       }
     }
   }
   ```

   > **Note**: This demo uses the centralized `SystemLLMConfigs` for AI service configuration (API keys, endpoints) and `VideoGeneration.DefaultOptions` for video-specific parameters. The VideoGenerationGAgent inherits configuration management from `AIGAgentBase`.

2. **Optional Settings**:
   - Add additional AI service configurations to `SystemLLMConfigs`
   - Customize video generation defaults in `VideoGeneration.DefaultOptions`
   - Configure logging levels and Orleans cluster settings
   - Adjust network timeout and retry policies

## 🏃‍♂️ Running the Demo

1. **Navigate to the demo directory**:
   ```bash
   cd aevatar-station/station/samples/VideoGenerationDemo
   ```

2. **Run the application**:
   ```bash
   dotnet run
   ```

3. **Follow the interactive prompts** to explore different features:
   - Generate videos from text descriptions
   - Animate images with custom prompts
   - View agent capabilities
   - Run performance benchmarks

## 🎯 Demo Scenarios

### Text-to-Video Generation

Create videos from descriptive prompts:

- **Nature Scenes**: "A serene ocean wave at sunset with golden reflections"
- **Urban Environments**: "A bustling city street at night with neon lights"
- **Fantasy Themes**: "A magical forest with glowing fireflies and ancient trees"
- **Sci-Fi Concepts**: "A futuristic spaceship traveling through a colorful nebula"

### Image-to-Video Generation

Animate static images with motion:

- **Landscape Animation**: Upload a landscape photo and add "gentle wind movement"
- **Portrait Animation**: Add "subtle facial expressions" to portrait images
- **Architecture Animation**: Create "dynamic lighting changes" for building photos
- **Abstract Animation**: Apply "flowing motion patterns" to abstract art

### Customization Options

Fine-tune your video generation:

- **Duration**: 1-60 seconds
- **Resolution**: 720p, 1080p, 4K
- **Aspect Ratio**: 16:9, 9:16, 1:1, 4:3
- **Style**: realistic, artistic, cartoon, cinematic, abstract
- **Motion Strength**: 0.1-1.0 (subtle to dramatic)

## 📊 Performance Testing

The demo includes a performance testing feature that:

- Runs multiple concurrent video generation requests
- Measures response times and success rates
- Provides detailed performance metrics
- Helps identify optimal load characteristics

## 🏗️ Architecture Overview

```
VideoGenerationDemo
├── Console Application (Spectre.Console UI)
├── Orleans TestCluster (In-Memory)
├── VideoGenerationGAgent (AI Agent)
├── BytePlus ModelArk Integration
└── Performance Monitoring
```

## 🔧 Troubleshooting

### Common Issues

1. **API Key Missing/Invalid**:
   - Verify your BytePlus ModelArk API key in `SystemLLMConfigs.BytePlusVideoGeneration.ApiKey` within `appsettings.json`
   - Check API key permissions and quota
   - Ensure `ProviderEnum` is set to "BytePlus" and `ModelIdEnum` is set to "BytePlusVideoGeneration"

2. **Orleans Cluster Startup Failed**:
   - Ensure no other Orleans applications are running on the same ports
   - Check firewall settings for local network access

3. **Video Generation Timeout**:
   - Increase timeout settings in configuration
   - Try simpler prompts or shorter durations

4. **Performance Issues**:
   - Reduce concurrent request count in performance tests
   - Monitor system resources (CPU, memory, network)

### Debug Mode

Run with detailed logging:

```bash
dotnet run --configuration Debug
```

## 📁 Project Structure

```
VideoGenerationDemo/
├── Program.cs                 # Main application entry point
├── VideoGenerationDemo.csproj # Project configuration
├── appsettings.json           # Application settings
└── README.md                 # This documentation
```

## 🔗 Related Components

- **VideoGenerationGAgent**: Core AI agent implementation inheriting from `AIGAgentBase`
- **SystemLLMConfigs**: Centralized AI service configuration system
- **AiAgentHelper**: Utility functions for AI operations
- **BytePlus ModelArk**: Video generation service integration
- **Orleans Framework**: Distributed computing foundation

## 🚀 Next Steps

After exploring the demo:

1. **Integrate into Applications**: Use the VideoGenerationGAgent in your own projects
2. **Extend Functionality**: Add custom video processing workflows
3. **Scale Deployment**: Deploy to production Orleans clusters
4. **Monitor Performance**: Implement production monitoring and alerting

## 📞 Support

For questions or issues:

- Check the main Aevatar documentation
- Review Orleans framework guides
- Consult BytePlus ModelArk API documentation
- Contact the development team for specific integration questions

---

**Happy Video Generation! 🎬✨**
