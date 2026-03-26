export async function synthesizeSpeech(params, endpoint, fetchFn = fetch) {
    const { text, voice = "en-US-AriaNeural", format = "audio-16khz-128kbitrate-mono-mp3" } = params;
    const response = await fetchFn(endpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text, voice, format }),
    });
    if (!response.ok) {
        const error = await response.text();
        return {
            content: [
                {
                    type: "text",
                    text: `TTS synthesis failed (${response.status}): ${error}`,
                },
            ],
            isError: true,
        };
    }
    const result = await response.json();
    return {
        content: [
            {
                type: "text",
                text: JSON.stringify(result, null, 2),
            },
        ],
    };
}
