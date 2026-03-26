using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ClaudeChatTTSServer.Models;
using Microsoft.CognitiveServices.Speech;

namespace ClaudeChatTTSServer.Services;

public sealed class TtsService : ITtsService
{
    private const string ContainerName = "tts-audio";

    private readonly SpeechConfig _speechConfig;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<TtsService> _logger;

    public TtsService(SpeechConfig speechConfig, BlobServiceClient blobServiceClient, ILogger<TtsService> logger)
    {
        _speechConfig = speechConfig;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<TtsResponse> SynthesizeAsync(TtsRequest request)
    {
        _speechConfig.SpeechSynthesisVoiceName = request.Voice;
        _speechConfig.SetSpeechSynthesisOutputFormat(ParseOutputFormat(request.Format));

        using var synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig: null);

        _logger.LogInformation("Synthesizing {CharCount} characters with voice {Voice}",
            request.Text.Length, request.Voice);

        var result = await synthesizer.SpeakTextAsync(request.Text);

        if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            throw new InvalidOperationException(
                $"TTS synthesis canceled: {cancellation.Reason} — {cancellation.ErrorDetails}");
        }

        var audioData = result.AudioData;
        var durationSeconds = result.AudioDuration.TotalSeconds;

        var blobName = $"{Guid.NewGuid():N}.mp3";
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(audioData);
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "audio/mpeg" });

        var sasUrl = GenerateSasUrl(blobClient);

        _logger.LogInformation("Uploaded {BlobName}, duration {Duration:F1}s, size {Size} bytes",
            blobName, durationSeconds, audioData.Length);

        return new TtsResponse
        {
            Url = sasUrl,
            DurationSeconds = durationSeconds,
            Voice = request.Voice,
            CharacterCount = request.Text.Length
        };
    }

    private static string GenerateSasUrl(BlobClient blobClient)
    {
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    private static SpeechSynthesisOutputFormat ParseOutputFormat(string format) => format switch
    {
        "audio-16khz-128kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3,
        "audio-24khz-160kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio24Khz160KBitRateMonoMp3,
        "audio-48khz-192kbitrate-mono-mp3" => SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3,
        "riff-16khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm,
        "riff-24khz-16bit-mono-pcm" => SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm,
        _ => SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3
    };
}
