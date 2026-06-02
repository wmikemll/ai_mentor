using InstantBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Audio;

namespace InstantBot.Infrastructure.OpenAi;

public class WhisperService(IConfiguration config) : IWhisperService
{
    public async Task<string> TranscribeAsync(Stream audio, string filename, CancellationToken ct = default)
    {
        var client = new AudioClient("whisper-1", config["OpenAI:ApiKey"]!);
        var result = await client.TranscribeAudioAsync(audio, filename, cancellationToken: ct);
        return result.Value.Text;
    }
}
