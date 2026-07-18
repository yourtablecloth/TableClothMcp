# tablecloth-mcp

MCP server that finds Korean e-Gov / internet-banking / finance sites and opens them in a
clean, disposable **Windows Sandbox** with the required security software preinstalled.

**Scope:** discovery + safe launch only. It does **not** log in, authenticate, or automate the
actual task (no RPA) — that stays with the user. Its value is removing the security-software
setup frustration and helping you find the right site.

## Use with an MCP client (npx)

```jsonc
{
  "mcpServers": {
    "tablecloth": { "command": "npx", "args": ["-y", "tablecloth-mcp@latest"] }
  }
}
```

No .NET runtime required: a platform-native (NativeAOT) binary is fetched as an
optional dependency. Prebuilt for **win32-x64**, **win32-arm64**, **darwin-arm64**,
**linux-x64**, **linux-arm64**.

> Also available as a .NET tool: `dnx TableCloth.Mcp` (needs the .NET 10 SDK).

### Updates

The MCP server is spawned by the client at startup and is not hot-swapped, so a new version
applies after the client restarts (restart the Claude Desktop app, or start a new Claude Code
session). `npx` can reuse a cached older version, so `@latest` is recommended; `dnx` resolves the
latest version on each run. The site/policy catalog is fetched live at runtime, so catalog changes
apply without a server version bump.

## Tools

`search_services` · `get_service` · `list_categories` · `list_companions` · `generate_wsb` · `launch_sandbox`

`launch_sandbox` picks a runner per OS: **Windows** → Windows Sandbox (Windows 11 optional feature);
**macOS** → [macSandbox](https://github.com/yourtablecloth/macSandbox) (Apple Silicon, macOS 26).
On other platforms use `generate_wsb` to produce a `.wsb` for a supported runner. Search and
`generate_wsb` work everywhere.

Catalog + launch assets are consumed from public sources
(`yourtablecloth.app`, GitHub Releases); this package does not bundle them.
