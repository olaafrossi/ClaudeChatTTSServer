namespace ClaudeChatTTSServer.Models;

public sealed class TtsResponse
{
    public required string Url { get; init; }
    public double DurationSeconds { get; init; }
    public required string Voice { get; init; }
    public int CharacterCount { get; init; }
}
