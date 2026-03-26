import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { synthesizeSpeech } from "./synthesize.js";

const TTS_ENDPOINT =
  process.env.TTS_ENDPOINT ?? "http://localhost:7841/api/tts";

const server = new McpServer({
  name: "claude-tts",
  version: "1.0.0",
});

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

const transport = new StdioServerTransport();
await server.connect(transport);
