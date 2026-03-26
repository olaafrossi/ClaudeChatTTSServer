export default function Home() {
  return (
    <main style={{ padding: "2rem", fontFamily: "system-ui" }}>
      <h1>Claude TTS MCP Server</h1>
      <p>
        This is a remote MCP server for Azure Neural Text-to-Speech. Connect to
        it in Claude via <code>/api/mcp</code>.
      </p>
    </main>
  );
}
