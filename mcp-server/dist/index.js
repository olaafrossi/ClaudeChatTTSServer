import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { createTtsServer } from "./server.js";
const TTS_ENDPOINT = process.env.TTS_ENDPOINT;
if (!TTS_ENDPOINT) {
    console.error("TTS_ENDPOINT environment variable is required");
    process.exit(1);
}
const server = createTtsServer(TTS_ENDPOINT);
const transport = new StdioServerTransport();
await server.connect(transport);
