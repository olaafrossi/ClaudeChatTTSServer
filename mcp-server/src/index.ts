import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { createTtsServer } from "./server.js";

const TTS_ENDPOINT =
  process.env.TTS_ENDPOINT ?? "http://localhost:7841/api/tts";

const server = createTtsServer(TTS_ENDPOINT);
const transport = new StdioServerTransport();
await server.connect(transport);
