using System.Net;
using System.Net.Http.Json;
using ClaudeChatTTSServer.Models;
using ClaudeChatTTSServer.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ClaudeChatTTSServer.Tests;

public class ApiIntegrationTests : IClassFixture<ApiIntegrationTests.TtsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITtsService _mockTts;

    public ApiIntegrationTests(TtsWebApplicationFactory factory)
    {
        _mockTts = factory.MockTtsService;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("healthy", body?.Status);
        Assert.Equal("ClaudeChatTTSServer", body?.Service);
    }

    [Fact]
    public async Task PostTts_ValidRequest_ReturnsOk()
    {
        _mockTts.SynthesizeAsync(Arg.Any<TtsRequest>())
            .Returns(new TtsResponse
            {
                Url = "https://blob.example.com/audio.mp3",
                DurationSeconds = 2.5,
                Voice = "en-US-AriaNeural",
                CharacterCount = 11
            });

        var response = await _client.PostAsJsonAsync("/api/tts", new { text = "Hello world" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TtsResponse>();
        Assert.NotNull(result);
        Assert.Equal("https://blob.example.com/audio.mp3", result.Url);
        Assert.Equal(2.5, result.DurationSeconds);
        Assert.Equal("en-US-AriaNeural", result.Voice);
        Assert.Equal(11, result.CharacterCount);
    }

    [Fact]
    public async Task PostTts_EmptyText_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/tts", new { text = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTts_WhitespaceText_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/tts", new { text = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTts_TextExceedsLimit_ReturnsBadRequest()
    {
        var longText = new string('a', 100_001);
        var response = await _client.PostAsJsonAsync("/api/tts", new { text = longText });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTts_TextAtLimit_ReturnsOk()
    {
        var maxText = new string('a', 100_000);
        _mockTts.SynthesizeAsync(Arg.Any<TtsRequest>())
            .Returns(new TtsResponse
            {
                Url = "https://blob.example.com/audio.mp3",
                DurationSeconds = 600.0,
                Voice = "en-US-AriaNeural",
                CharacterCount = 100_000
            });

        var response = await _client.PostAsJsonAsync("/api/tts", new { text = maxText });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostTts_CustomVoiceAndFormat_PassedToService()
    {
        _mockTts.SynthesizeAsync(Arg.Any<TtsRequest>())
            .Returns(new TtsResponse
            {
                Url = "https://blob.example.com/audio.mp3",
                DurationSeconds = 1.0,
                Voice = "en-GB-SoniaNeural",
                CharacterCount = 4
            });

        var response = await _client.PostAsJsonAsync("/api/tts", new
        {
            text = "Test",
            voice = "en-GB-SoniaNeural",
            format = "audio-48khz-192kbitrate-mono-mp3"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockTts.Received(1).SynthesizeAsync(Arg.Is<TtsRequest>(r =>
            r.Voice == "en-GB-SoniaNeural" &&
            r.Format == "audio-48khz-192kbitrate-mono-mp3"));
    }

    [Fact]
    public async Task PostTts_ServiceThrows_Returns500()
    {
        _mockTts.SynthesizeAsync(Arg.Any<TtsRequest>())
            .ThrowsAsync(new InvalidOperationException("Azure synthesis failed"));

        var response = await _client.PostAsJsonAsync("/api/tts", new { text = "Hello" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task PostTts_DefaultVoiceAndFormat_Applied()
    {
        _mockTts.SynthesizeAsync(Arg.Any<TtsRequest>())
            .Returns(new TtsResponse
            {
                Url = "https://blob.example.com/audio.mp3",
                DurationSeconds = 1.0,
                Voice = "en-US-AriaNeural",
                CharacterCount = 5
            });

        var response = await _client.PostAsJsonAsync("/api/tts", new { text = "Hello" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockTts.Received(1).SynthesizeAsync(Arg.Is<TtsRequest>(r =>
            r.Voice == "en-US-AriaNeural" &&
            r.Format == "audio-16khz-128kbitrate-mono-mp3"));
    }

    private record HealthResponse(string Status, string Service);

    public class TtsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public ITtsService MockTtsService { get; } = Substitute.For<ITtsService>();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("AZURE_SPEECH_KEY", "test-key");
            builder.UseSetting("AZURE_SPEECH_REGION", "test-region");
            builder.UseSetting("AZURE_STORAGE_CONNECTION_STRING",
                "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net");

            builder.ConfigureServices(services =>
            {
                // Remove the real TtsService and replace with mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITtsService));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddSingleton(MockTtsService);
            });
        }
    }
}
