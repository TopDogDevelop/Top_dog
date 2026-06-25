#!/usr/bin/env node
/**
 * Minimal stdio -> Unity MCP HTTP bridge for Cursor.
 * Forwards one JSON-RPC line to http://127.0.0.1:8080/ (plain POST, no Streamable HTTP).
 */
import readline from "node:readline";

const ENDPOINT = process.env.UNITY_MCP_URL ?? "http://127.0.0.1:8080/";

function writeJsonRpcError(id, message) {
  const payload = {
    jsonrpc: "2.0",
    id: id ?? null,
    error: { code: -32603, message },
  };
  process.stdout.write(`${JSON.stringify(payload)}\n`);
}

async function forwardRequest(line) {
  const trimmed = line.trim();
  if (!trimmed) {
    return;
  }

  let requestId = null;
  try {
    const parsed = JSON.parse(trimmed);
    requestId = parsed?.id ?? null;
  } catch {
    writeJsonRpcError(null, "Invalid JSON input");
    return;
  }

  try {
    const response = await fetch(ENDPOINT, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: trimmed,
    });

    const text = await response.text();
    if (!response.ok) {
      writeJsonRpcError(requestId, `Unity MCP HTTP ${response.status}: ${text}`);
      return;
    }

    process.stdout.write(`${text.trim()}\n`);
  } catch (error) {
    writeJsonRpcError(
      requestId,
      `Unity MCP unreachable at ${ENDPOINT}. Start Window -> Unity MCP -> Start Server. ${error}`,
    );
  }
}

const rl = readline.createInterface({ input: process.stdin, terminal: false });
for await (const line of rl) {
  await forwardRequest(line);
}
