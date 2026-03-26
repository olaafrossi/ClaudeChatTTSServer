import { describe, it, expect, vi } from "vitest";
import { synthesizeSpeech } from "./synthesize.js";

const ENDPOINT = "http://localhost:7841/api/tts";

function mockFetch(status: number, body: unknown, ok?: boolean): typeof fetch {
  return vi.fn().mockResolvedValue({
    ok: ok ?? (status >= 200 && status < 300),
    status,
    text: () => Promise.resolve(typeof body === "string" ? body : JSON.stringify(body)),
    json: () => Promise.resolve(body),
  }) as unknown as typeof fetch;
}

describe("synthesizeSpeech", () => {
  it("sends correct request to the endpoint", async () => {
    const fetchFn = mockFetch(200, {
      url: "https://blob.example.com/audio.mp3",
      durationSeconds: 2.5,
      voice: "en-US-AriaNeural",
      characterCount: 11,
    });

    await synthesizeSpeech({ text: "Hello world" }, ENDPOINT, fetchFn);

    expect(fetchFn).toHaveBeenCalledWith(ENDPOINT, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        text: "Hello world",
        voice: "en-US-AriaNeural",
        format: "audio-16khz-128kbitrate-mono-mp3",
      }),
    });
  });

  it("returns formatted result on success", async () => {
    const responseData = {
      url: "https://blob.example.com/audio.mp3",
      durationSeconds: 2.5,
      voice: "en-US-AriaNeural",
      characterCount: 11,
    };
    const fetchFn = mockFetch(200, responseData);

    const result = await synthesizeSpeech({ text: "Hello world" }, ENDPOINT, fetchFn);

    expect(result.isError).toBeUndefined();
    expect(result.content).toHaveLength(1);
    expect(result.content[0].type).toBe("text");
    expect(JSON.parse(result.content[0].text)).toEqual(responseData);
  });

  it("returns error when response is not ok", async () => {
    const fetchFn = mockFetch(500, "Internal Server Error", false);

    const result = await synthesizeSpeech({ text: "Hello" }, ENDPOINT, fetchFn);

    expect(result.isError).toBe(true);
    expect(result.content[0].text).toContain("TTS synthesis failed (500)");
    expect(result.content[0].text).toContain("Internal Server Error");
  });

  it("returns error on 400 bad request", async () => {
    const fetchFn = mockFetch(400, '{"error":"text field is required"}', false);

    const result = await synthesizeSpeech({ text: "" }, ENDPOINT, fetchFn);

    expect(result.isError).toBe(true);
    expect(result.content[0].text).toContain("400");
  });

  it("passes custom voice and format", async () => {
    const fetchFn = mockFetch(200, { url: "https://example.com/a.mp3" });

    await synthesizeSpeech(
      {
        text: "Test",
        voice: "en-GB-SoniaNeural",
        format: "audio-48khz-192kbitrate-mono-mp3",
      },
      ENDPOINT,
      fetchFn
    );

    expect(fetchFn).toHaveBeenCalledWith(ENDPOINT, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        text: "Test",
        voice: "en-GB-SoniaNeural",
        format: "audio-48khz-192kbitrate-mono-mp3",
      }),
    });
  });

  it("uses default voice and format when not specified", async () => {
    const fetchFn = mockFetch(200, { url: "https://example.com/a.mp3" });

    await synthesizeSpeech({ text: "Hi" }, ENDPOINT, fetchFn);

    const call = vi.mocked(fetchFn).mock.calls[0];
    const body = JSON.parse(call[1]!.body as string);
    expect(body.voice).toBe("en-US-AriaNeural");
    expect(body.format).toBe("audio-16khz-128kbitrate-mono-mp3");
  });

  it("handles network errors", async () => {
    const fetchFn = vi.fn().mockRejectedValue(new Error("Network error")) as unknown as typeof fetch;

    await expect(synthesizeSpeech({ text: "Hi" }, ENDPOINT, fetchFn)).rejects.toThrow("Network error");
  });

  it("returns properly formatted JSON in content", async () => {
    const data = { url: "https://example.com/a.mp3", durationSeconds: 1.5 };
    const fetchFn = mockFetch(200, data);

    const result = await synthesizeSpeech({ text: "Test" }, ENDPOINT, fetchFn);

    // Should be pretty-printed with 2-space indent
    expect(result.content[0].text).toBe(JSON.stringify(data, null, 2));
  });
});
