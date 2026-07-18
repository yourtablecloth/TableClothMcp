#!/usr/bin/env node
'use strict';

// tablecloth-mcp launcher: resolve the platform-specific NativeAOT binary
// (shipped as an optionalDependency) and run it, forwarding stdio for the
// MCP stdio transport. No .NET runtime required.

const { spawnSync } = require('node:child_process');
const { chmodSync } = require('node:fs');

function resolveBinary() {
  const platform = process.platform; // 'win32' | 'darwin' | 'linux'
  const arch = process.arch;         // 'x64' | 'arm64' | ...
  const pkg = `tablecloth-mcp-${platform}-${arch}`;
  const exe = platform === 'win32' ? 'tablecloth-mcp.exe' : 'tablecloth-mcp';
  try {
    return require.resolve(`${pkg}/${exe}`);
  } catch {
    return null;
  }
}

const bin = resolveBinary();
if (!bin) {
  process.stderr.write(
    `tablecloth-mcp: no prebuilt native binary for ${process.platform}-${process.arch}.\n` +
    `Currently shipped: win32-x64, win32-arm64.\n` +
    `Alternatively run the .NET tool (needs .NET 10 SDK):  dnx TableCloth.Mcp\n`
  );
  process.exit(1);
}

// npm 이 실행 비트를 잃은 경우를 대비해 unix 에서 방어적으로 +x (실패는 무시).
if (process.platform !== 'win32') {
  try { chmodSync(bin, 0o755); } catch { /* read-only install etc. */ }
}

const child = spawnSync(bin, process.argv.slice(2), { stdio: 'inherit' });
if (child.error) {
  process.stderr.write(`tablecloth-mcp: failed to start ${bin}: ${child.error.message}\n`);
  process.exit(1);
}
process.exit(child.status === null ? 1 : child.status);
