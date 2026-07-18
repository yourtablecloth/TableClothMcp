// Assemble the npm publish tree from NativeAOT build artifacts.
//
//   node scripts/assemble-npm.mjs <version>
//
// Inputs  : artifacts/aot-<rid>/tablecloth-mcp.exe   (from the release workflow)
//           npm/tablecloth-mcp/**                     (launcher package source)
// Output  : dist/npm/tablecloth-mcp/                  (launcher, version + optionalDependencies stamped)
//           dist/npm/tablecloth-mcp-<platform>-<cpu>/ (one per platform, contains the native binary)

import { mkdirSync, cpSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';

const version = process.argv[2];
if (!version || !/^\d+\.\d+\.\d+/.test(version)) {
  console.error('usage: node scripts/assemble-npm.mjs <version X.Y.Z>');
  process.exit(1);
}

// 새 플랫폼을 추가하려면 여기에 rid/os/cpu 한 줄만 넣고, release.yml 의 aot 매트릭스에도 같은 rid 를 추가한다.
const platforms = [
  { rid: 'win-x64', os: 'win32', cpu: 'x64', exe: 'tablecloth-mcp.exe' },
  { rid: 'win-arm64', os: 'win32', cpu: 'arm64', exe: 'tablecloth-mcp.exe' },
];

const outRoot = 'dist/npm';
const optionalDependencies = {};

for (const p of platforms) {
  const pkgName = `tablecloth-mcp-${p.os}-${p.cpu}`;
  const artifact = join('artifacts', `aot-${p.rid}`, p.exe);
  if (!existsSync(artifact)) {
    console.error(`missing artifact for ${p.rid}: ${artifact}`);
    process.exit(1);
  }

  const dir = join(outRoot, pkgName);
  mkdirSync(dir, { recursive: true });
  cpSync(artifact, join(dir, p.exe));
  // 바이너리를 담은 패키지도 각각 라이선스를 동봉한다(AGPL 배포 준수).
  cpSync('LICENSE-AGPL', join(dir, 'LICENSE-AGPL'));
  cpSync('LICENSE-COMMERCIAL', join(dir, 'LICENSE-COMMERCIAL'));
  writeFileSync(join(dir, 'package.json'), JSON.stringify({
    name: pkgName,
    version,
    description: `TableCloth MCP native binary for ${p.os}-${p.cpu}`,
    license: 'AGPL-3.0-or-later',
    os: [p.os],
    cpu: [p.cpu],
    files: [p.exe, 'LICENSE-AGPL', 'LICENSE-COMMERCIAL'],
  }, null, 2) + '\n');

  optionalDependencies[pkgName] = version;
  console.log(`assembled ${pkgName}`);
}

// Launcher package: copy source, stamp version + optionalDependencies.
const launcherSrc = 'npm/tablecloth-mcp';
const launcherOut = join(outRoot, 'tablecloth-mcp');
mkdirSync(join(launcherOut, 'bin'), { recursive: true });
cpSync(join(launcherSrc, 'bin', 'cli.js'), join(launcherOut, 'bin', 'cli.js'));
cpSync(join(launcherSrc, 'README.md'), join(launcherOut, 'README.md'));
cpSync('LICENSE-AGPL', join(launcherOut, 'LICENSE-AGPL'));
cpSync('LICENSE-COMMERCIAL', join(launcherOut, 'LICENSE-COMMERCIAL'));

const manifest = JSON.parse(readFileSync(join(launcherSrc, 'package.json'), 'utf8'));
manifest.version = version;
manifest.optionalDependencies = optionalDependencies;
writeFileSync(join(launcherOut, 'package.json'), JSON.stringify(manifest, null, 2) + '\n');
console.log(`assembled tablecloth-mcp (launcher) v${version} -> ${Object.keys(optionalDependencies).length} platform deps`);
