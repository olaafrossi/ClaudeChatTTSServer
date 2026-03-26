using ClaudeChatTTSServer.Models;

namespace ClaudeChatTTSServer.Services;

public interface ITtsService
{
    Task<TtsResponse> SynthesizeAsync(TtsRequest request);
}
