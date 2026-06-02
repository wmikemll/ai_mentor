namespace InstantBot.Application.Interfaces;

public interface IWhisperService
{
    Task<string> TranscribeAsync(Stream audioStream, string filename, CancellationToken ct = default);
}
