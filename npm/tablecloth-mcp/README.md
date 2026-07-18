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
    "tablecloth": { "command": "npx", "args": ["-y", "tablecloth-mcp"] }
  }
}
```

No .NET runtime required — a platform-native (NativeAOT) binary is fetched as an
optional dependency. Prebuilt for **win32-x64** and **win32-arm64**.

> Also available as a .NET tool: `dnx TableCloth.Mcp` (needs the .NET 10 SDK).

## Tools

`search_services` · `get_service` · `list_categories` · `list_companions` · `generate_wsb` · `launch_sandbox`

`launch_sandbox` requires Windows 11 with the *Windows Sandbox* optional feature. On other
platforms use `generate_wsb` to produce a `.wsb` file to run on a Windows machine.

Catalog + launch assets are consumed from public sources
(`yourtablecloth.app`, GitHub Releases); this package does not bundle them.
