import { createMcpHandler } from "mcp-handler";
import { z } from "zod";

const TTS_ENDPOINT =
  process.env.TTS_ENDPOINT ?? "https://app-claudetts.azurewebsites.net/api/tts";

const AUTH_TOKEN = process.env.MCP_AUTH_TOKEN;

async function synthesizeSpeech(
  params: { text: string; voice: string; format: string },
  endpoint: string
) {
  const response = await fetch(endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(params),
  });

  if (!response.ok) {
    const error = await response.text();
    return {
      content: [
        {
          type: "text" as const,
          text: `TTS synthesis failed (${response.status}): ${error}`,
        },
      ],
      isError: true,
    };
  }

  const result = await response.json();
  return {
    content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
  };
}

function verifyAuth(req: Request): boolean {
  if (!AUTH_TOKEN) return true; // no token configured = open (dev only)
  const authHeader = req.headers.get("authorization");
  if (!authHeader) return false;
  const token = authHeader.replace(/^Bearer\s+/i, "");
  return token === AUTH_TOKEN;
}

const mcpHandler = createMcpHandler(
  (server) => {
    server.tool(
      "synthesize_speech",
      "Convert text to speech using Azure Neural TTS. Returns a downloadable MP3 URL (expires in 1 hour).",
      {
        text: z.string().describe("The text to convert to speech"),
        voice: z
          .string()
          .optional()
          .default("en-US-AriaNeural")
          .describe(
            "Azure Neural TTS voice name, e.g. en-US-AriaNeural, en-US-GuyNeural, en-GB-SoniaNeural"
          ),
        format: z
          .string()
          .optional()
          .default("audio-16khz-128kbitrate-mono-mp3")
          .describe(
            "Audio format: audio-16khz-128kbitrate-mono-mp3, audio-24khz-160kbitrate-mono-mp3, audio-48khz-192kbitrate-mono-mp3"
          ),
      },
      async ({ text, voice, format }) => {
        return synthesizeSpeech({ text, voice, format }, TTS_ENDPOINT);
      }
    );
  },
  {},
  { basePath: "/api", maxDuration: 60 }
);

async function handler(req: Request): Promise<Response> {
  if (!verifyAuth(req)) {
    return new Response(JSON.stringify({ error: "Unauthorized" }), {
      status: 401,
      headers: { "Content-Type": "application/json" },
    });
  }
  return mcpHandler(req);
}

export { handler as GET, handler as POST, handler as DELETE };
