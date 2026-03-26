namespace ClaudeChatTTSServer.Models;

public sealed class TtsRequest
{
    public required string Text { get; init; }
    public string Voice { get; init; } = "en-US-AriaNeural";
    public string Format { get; init; } = "audio-16khz-128kbitrate-mono-mp3";
}
