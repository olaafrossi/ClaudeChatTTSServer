using Azure.Storage.Blobs;
using ClaudeChatTTSServer.Models;
using ClaudeChatTTSServer.Services;
using Microsoft.CognitiveServices.Speech;

namespace ClaudeChatTTSServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var speechKey = builder.Configuration["AZURE_SPEECH_KEY"]
            ?? throw new InvalidOperationException("AZURE_SPEECH_KEY not configured");
        var speechRegion = builder.Configuration["AZURE_SPEECH_REGION"]
            ?? throw new InvalidOperationException("AZURE_SPEECH_REGION not configured");
        var storageConn = builder.Configuration["AZURE_STORAGE_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING not configured");

        builder.Services.AddSingleton(_ => SpeechConfig.FromSubscription(speechKey, speechRegion));
        builder.Services.AddSingleton(_ => new BlobServiceClient(storageConn));
        builder.Services.AddSingleton<ITtsService, TtsService>();

        var app = builder.Build();

        app.MapPost("/api/tts", async (TtsRequest request, ITtsService tts) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest(new { error = "\"text\" field is required and cannot be empty" });

            if (request.Text.Length > 100_000)
                return Results.BadRequest(new { error = "Text exceeds 100,000 character limit" });

            try
            {
                var result = await tts.SynthesizeAsync(request);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Synthesis failed", statusCode: 500);
            }
        });

        app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "ClaudeChatTTSServer" }));

        app.Run();
    }
}
