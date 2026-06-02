namespace InstantBot.Application.Interfaces;

public interface ITtsService
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default);
}
