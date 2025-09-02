using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;

namespace Aevatar.GAgents.Speech;

public class SpeechService : ISpeechService
{
    private readonly SpeechConfig _speechConfig;
    private readonly SpeechSynthesizer _synthesizer;

    public SpeechService(IOptionsSnapshot<SpeechOptions> speechOptions)
    {
        _speechConfig = SpeechConfig.FromSubscription(speechOptions.Value.SubscriptionKey, speechOptions.Value.Region);
        _speechConfig.SpeechRecognitionLanguage = "zh-CN";
        _speechConfig.SpeechSynthesisLanguage = "zh-CN";
        _speechConfig.SpeechSynthesisVoiceName = "zh-CN-XiaoxiaoNeural";
        _synthesizer = new SpeechSynthesizer(_speechConfig);
    }

    public async Task<string> SpeechToTextAsync(byte[] audioData)
    {
        var tempFilePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(tempFilePath, audioData);

            using var audioConfig = AudioConfig.FromWavFileInput(tempFilePath);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            var result = await recognizer.RecognizeOnceAsync();
            return result.Text;
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
        using var result = await _synthesizer.SpeakTextAsync(text);
        return result.AudioData;
    }
}