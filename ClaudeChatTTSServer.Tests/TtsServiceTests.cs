using System.Reflection;
using ClaudeChatTTSServer.Services;
using Microsoft.CognitiveServices.Speech;

namespace ClaudeChatTTSServer.Tests;

public class TtsServiceTests
{
    private static SpeechSynthesisOutputFormat InvokeParseOutputFormat(string format)
    {
        var method = typeof(TtsService).GetMethod("ParseOutputFormat",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (SpeechSynthesisOutputFormat)method.Invoke(null, [format])!;
    }

    [Theory]
    [InlineData("audio-16khz-128kbitrate-mono-mp3", SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3)]
    [InlineData("audio-24khz-160kbitrate-mono-mp3", SpeechSynthesisOutputFormat.Audio24Khz160KBitRateMonoMp3)]
    [InlineData("audio-48khz-192kbitrate-mono-mp3", SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3)]
    [InlineData("riff-16khz-16bit-mono-pcm", SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm)]
    [InlineData("riff-24khz-16bit-mono-pcm", SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm)]
    public void ParseOutputFormat_KnownFormat_ReturnsCorrectEnum(string format, SpeechSynthesisOutputFormat expected)
    {
        var result = InvokeParseOutputFormat(format);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("unknown-format")]
    [InlineData("")]
    [InlineData("wav")]
    public void ParseOutputFormat_UnknownFormat_ReturnsDefault(string format)
    {
        var result = InvokeParseOutputFormat(format);
        Assert.Equal(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3, result);
    }
}
