using InstantBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Audio;

namespace InstantBot.Infrastructure.OpenAi;

public class TtsService(IConfiguration config) : ITtsService
{
    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        var client = new AudioClient("tts-1", config["OpenAI:ApiKey"]!);
        var speech = await client.GenerateSpeechAsync(text, GeneratedSpeechVoice.Alloy, cancellationToken: ct);
        using var ms = new MemoryStream();
        speech.Value.ToStream().CopyTo(ms);
        return ms.ToArray();
    }
}
