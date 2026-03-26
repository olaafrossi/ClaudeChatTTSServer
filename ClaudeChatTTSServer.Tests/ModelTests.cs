using ClaudeChatTTSServer.Models;

namespace ClaudeChatTTSServer.Tests;

public class ModelTests
{
    [Fact]
    public void TtsRequest_DefaultVoice_IsAriaNeural()
    {
        var request = new TtsRequest { Text = "Hello" };
        Assert.Equal("en-US-AriaNeural", request.Voice);
    }

    [Fact]
    public void TtsRequest_DefaultFormat_Is16KhzMp3()
    {
        var request = new TtsRequest { Text = "Hello" };
        Assert.Equal("audio-16khz-128kbitrate-mono-mp3", request.Format);
    }

    [Fact]
    public void TtsRequest_CustomValues_ArePreserved()
    {
        var request = new TtsRequest
        {
            Text = "Test text",
            Voice = "en-GB-SoniaNeural",
            Format = "audio-48khz-192kbitrate-mono-mp3"
        };

        Assert.Equal("Test text", request.Text);
        Assert.Equal("en-GB-SoniaNeural", request.Voice);
        Assert.Equal("audio-48khz-192kbitrate-mono-mp3", request.Format);
    }

    [Fact]
    public void TtsResponse_AllProperties_AreSet()
    {
        var response = new TtsResponse
        {
            Url = "https://example.com/audio.mp3",
            DurationSeconds = 3.14,
            Voice = "en-US-GuyNeural",
            CharacterCount = 42
        };

        Assert.Equal("https://example.com/audio.mp3", response.Url);
        Assert.Equal(3.14, response.DurationSeconds);
        Assert.Equal("en-US-GuyNeural", response.Voice);
        Assert.Equal(42, response.CharacterCount);
    }
}
