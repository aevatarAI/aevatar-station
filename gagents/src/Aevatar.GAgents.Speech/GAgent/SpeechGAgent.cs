using System;
using System.IO;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Speech.Events;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Aevatar.GAgents.Speech.GAgent;

[GenerateSerializer]
public class SpeechGAgentState : StateBase
{
    [Id(0)] public string SubscriptionKey { get; set; }
    [Id(1)] public string Region { get; set; }
    [Id(2)] public string RecognitionLanguage { get; set; }
    [Id(3)] public string SynthesisLanguage { get; set; }
    [Id(4)] public string SynthesisVoiceName { get; set; }
}

[GenerateSerializer]
public class SpeechStateLogEvent : StateLogEventBase<SpeechStateLogEvent>
{

}

[GenerateSerializer]
public class SpeechGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string? SubscriptionKey { get; set; }
    [Id(1)] public string? Region { get; set; }
    [Id(2)] public string? RecognitionLanguage { get; set; }
    [Id(3)] public string? SynthesisLanguage { get; set; }
    [Id(4)] public string? SynthesisVoiceName { get; set; }
}

[GAgent]
public class SpeechGAgent : GAgentBase<SpeechGAgentState, SpeechStateLogEvent, EventBase, SpeechGAgentConfiguration>,
    ISpeechGAgent
{
    private SpeechConfig _speechConfig;
    private SpeechSynthesizer _synthesizer;
    private readonly SpeechOptions _speechOptions;

    public SpeechGAgent(IOptionsSnapshot<SpeechOptions> speechOptions)
    {
        _speechOptions = speechOptions.Value;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for TTS & STT.");
    }

    protected override async Task PerformConfigAsync(SpeechGAgentConfiguration configuration)
    {
        RaiseEvent(new ConfigSpeechStateLogEvent
        {
            SubscriptionKey = configuration.SubscriptionKey ?? _speechOptions.SubscriptionKey,
            Region = configuration.Region ?? _speechOptions.Region,
            RecognitionLanguage = configuration.RecognitionLanguage ?? _speechOptions.RecognitionLanguage,
            SynthesisLanguage = configuration.SynthesisLanguage ?? _speechOptions.SynthesisLanguage,
            SynthesisVoiceName = configuration.SynthesisVoiceName ?? _speechOptions.SynthesisVoiceName
        });
        await ConfirmEvents();
        await base.PerformConfigAsync(configuration);
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestSTTEvent eventData)
    {
        var text = await SpeechToTextAsync(eventData.AudioData);
        await PublishAsync(new ResponseSTTEvent
        {
            Text = text
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(RequestTTSEvent eventData)
    {
        var audioData = await TextToSpeechAsync(eventData.Text);
        await PublishAsync(new ResponseTTSEvent
        {
            AudioData = audioData
        });
    }

    private bool EnsureConfiguration()
    {
        if (State.SubscriptionKey.IsNullOrEmpty())
        {
            Logger.LogError("[{grainId}] Not configured.", this.GetGrainId().ToString());
            return false;
        }

        _speechConfig = SpeechConfig.FromSubscription(State.SubscriptionKey, State.Region);
        _speechConfig.SpeechRecognitionLanguage = State.RecognitionLanguage;
        _speechConfig.SpeechSynthesisLanguage = State.SynthesisLanguage;
        _speechConfig.SpeechSynthesisVoiceName = State.SynthesisVoiceName;
        _synthesizer = new SpeechSynthesizer(_speechConfig);

        return true;
    }

    protected override void GAgentTransitionState(SpeechGAgentState state,
        StateLogEventBase<SpeechStateLogEvent> @event)
    {
        switch (@event)
        {
            case ConfigSpeechStateLogEvent configSpeechStateLogEvent:
                State.SubscriptionKey = configSpeechStateLogEvent.SubscriptionKey;
                State.Region = configSpeechStateLogEvent.Region;
                State.RecognitionLanguage = configSpeechStateLogEvent.RecognitionLanguage;
                State.SynthesisLanguage = configSpeechStateLogEvent.SynthesisLanguage;
                State.SynthesisVoiceName = configSpeechStateLogEvent.SynthesisVoiceName;
                break;
        }
    }

    [GenerateSerializer]
    public class ConfigSpeechStateLogEvent : StateLogEventBase<SpeechStateLogEvent>
    {
        [Id(0)] public required string SubscriptionKey { get; set; }
        [Id(1)] public required string Region { get; set; }
        [Id(2)] public required string RecognitionLanguage { get; set; }
        [Id(3)] public required string SynthesisLanguage { get; set; }
        [Id(4)] public required string SynthesisVoiceName { get; set; }
    }

    public async Task<string> SpeechToTextAsync(byte[] audioData)
    {
        if (!EnsureConfiguration()) return string.Empty;

        var tempFilePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFilePath, audioData);

            using var audioConfig = AudioConfig.FromWavFileInput(tempFilePath);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();
            return result.Text;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during speech recognition.");
            return string.Empty;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    public async Task<byte[]> TextToSpeechAsync(string text)
    {
        if (!EnsureConfiguration()) return [];

        using var result = await _synthesizer.SpeakTextAsync(text);
        return result.AudioData;
    }
}