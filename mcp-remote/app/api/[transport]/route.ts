import type { AuthInfo } from "@modelcontextprotocol/sdk/server/auth/types.js";
import { createMcpHandler, withMcpAuth } from "mcp-handler";
import { z } from "zod";

const TTS_ENDPOINT = process.env.TTS_ENDPOINT;

if (!TTS_ENDPOINT) {
  throw new Error("TTS_ENDPOINT environment variable is required");
}

const ALLOWED_EMAIL = process.env.ALLOWED_EMAIL;

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

async function verifyToken(
  _req: Request,
  bearerToken?: string
): Promise<AuthInfo | undefined> {
  // No ALLOWED_EMAIL configured = open access (dev mode)
  if (!ALLOWED_EMAIL) {
    return {
      token: "open",
      scopes: [],
      clientId: "anonymous",
      extra: {},
    };
  }

  if (!bearerToken) return undefined;

  try {
    // Validate the Google access token via Google's tokeninfo endpoint
    const res = await fetch(
      `https://oauth2.googleapis.com/tokeninfo?access_token=${encodeURIComponent(bearerToken)}`
    );

    if (!res.ok) return undefined;

    const info = await res.json();

    // Ensure the email is verified and matches the allowed email
    if (!info.email_verified || info.email_verified === "false") {
      return undefined;
    }

    if (info.email?.toLowerCase() !== ALLOWED_EMAIL.toLowerCase()) {
      return undefined;
    }

    return {
      token: bearerToken,
      scopes: (info.scope ?? "").split(" ").filter(Boolean),
      clientId: info.email,
      extra: { email: info.email },
    };
  } catch {
    return undefined;
  }
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
          .default("en-GB-SoniaNeural")
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

const handler = withMcpAuth(mcpHandler, verifyToken, {
  required: !!ALLOWED_EMAIL,
});

export { handler as GET, handler as POST, handler as DELETE };
