// 샌드박스 도구(SPEC.md §7, §8). .NET SandboxTools 와 동일한 .wsb 생성/러너 결정/출력.
import { spawn, execFileSync } from "node:child_process";
import { writeFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir, homedir } from "node:os";
import { join } from "node:path";
import { randomUUID } from "node:crypto";
import { getCatalog } from "../catalog.js";
import { strings, wsbTemplate } from "../generated.js";

const SAFE_ID = /^[A-Za-z0-9._-]+$/;

async function resolveIds(serviceIds: string[]): Promise<{ valid: string[]; unknown: string[] }> {
  const doc = await getCatalog();
  const known = new Set(doc.services.map((s) => s.id));
  const valid: string[] = [];
  const unknown: string[] = [];
  for (const raw of serviceIds ?? []) {
    const id = (raw ?? "").trim();
    if (id.length > 0 && SAFE_ID.test(id) && known.has(id)) {
      if (!valid.includes(id)) valid.push(id);
    } else if (id.length > 0) {
      unknown.push(id);
    }
  }
  return { valid, unknown };
}

function buildWsb(validIds: string[]): string {
  // 템플릿(정본)은 shared/wsb-template.xml. 사이트 사전선택만 __SITEIDS__ 로 주입.
  const idsStmt = validIds.length === 0
    ? ""
    : ` $env:TABLECLOTH_SITE_IDS = ''${validIds.join(" ")}'';`;
  return wsbTemplate.split("__SITEIDS__").join(idsStmt);
}

// macSandbox 앱(`macSandbox for Windows.app`, 번들 ID com.rkttu.macsandbox)의 위치를 찾는다.
// Finder 더블클릭처럼 LaunchServices 로 위임하려면 실행 파일이 아니라 .app 번들 경로가 필요하다.
// 표준 설치 경로를 먼저 보고, 없으면 번들 ID 로 mdfind(Spotlight/LaunchServices) 조회해 이름/위치가
// 달라도 탐지한다. 못 찾으면 undefined.
const MACSANDBOX_BUNDLE_ID = "com.rkttu.macsandbox";

function resolveMacSandbox(): string | undefined {
  const candidates = [
    "/Applications/macSandbox for Windows.app",
    "/Applications/MacSandbox.app",
    join(homedir(), "Applications", "macSandbox for Windows.app"),
    join(homedir(), "Applications", "MacSandbox.app"),
  ];
  for (const app of candidates) {
    if (existsSync(app)) return app;
  }
  try {
    const out = execFileSync("mdfind", [`kMDItemCFBundleIdentifier == '${MACSANDBOX_BUNDLE_ID}'`], { encoding: "utf8" });
    for (const line of out.split("\n")) {
      const app = line.trim();
      if (app.length > 0 && existsSync(app)) return app;
    }
  } catch {
    // mdfind 부재/실패 시 무시 — 표준 경로 탐지 결과로 판단.
  }
  return undefined;
}

export async function generateWsb(serviceIds: string[]) {
  const { valid, unknown } = await resolveIds(serviceIds);
  if (valid.length === 0) {
    return {
      error: strings.tools.generate_wsb.errorNoValidIds,
      unknownIds: unknown,
      hint: strings.tools.generate_wsb.hintNoValidIds,
    };
  }
  return {
    siteIds: valid,
    unknownIdsIgnored: unknown.length > 0 ? unknown : undefined,
    wsb: buildWsb(valid),
    usage: strings.tools.generate_wsb.usage,
    securityNote: strings.sandbox.securityNote,
  };
}

export async function launchSandbox(serviceIds: string[]) {
  const { valid, unknown } = await resolveIds(serviceIds);
  if (valid.length === 0) {
    return { launched: false, error: strings.tools.launch_sandbox.errorNoValidIds, unknownIds: unknown, hint: strings.tools.launch_sandbox.hintNoValidIds };
  }

  let runner: string;
  let command: string;
  let args: string[];
  const customRunner = process.env.TABLECLOTH_WSB_RUNNER;
  if (customRunner && customRunner.trim().length > 0) {
    runner = `custom (${customRunner})`;
    command = customRunner;
    args = [];
  } else if (process.platform === "win32") {
    runner = "Windows Sandbox";
    command = "WindowsSandbox.exe";
    args = [];
  } else if (process.platform === "darwin") {
    const mac = resolveMacSandbox();
    if (!mac) {
      return { launched: false, error: strings.tools.launch_sandbox.runnerMacNotFoundError, hint: strings.tools.launch_sandbox.runnerMacNotFoundHint };
    }
    runner = "macSandbox";
    // 직접 exec 대신 LaunchServices 로 위임(Finder 더블클릭과 동일 경로): open -a <앱> <.wsb>.
    command = "open";
    args = ["-a", mac];
  } else {
    return { launched: false, error: strings.tools.launch_sandbox.runnerUnsupportedError, hint: strings.tools.launch_sandbox.runnerUnsupportedHint };
  }

  const wsb = buildWsb(valid);
  const path = join(tmpdir(), `tablecloth-${randomUUID().replaceAll("-", "")}.wsb`);
  await writeFile(path, wsb, "utf8");

  try {
    const child = spawn(command, [...args, path], { detached: true, stdio: "ignore" });
    child.on("error", () => { /* async 실패는 pid 미할당으로 감지. 여기서는 크래시만 방지 */ });
    if (child.pid === undefined) {
      return {
        launched: false,
        runner,
        error: strings.tools.launch_sandbox.runnerLaunchFailedError.replace("{runner}", runner).replace("{message}", "process did not start"),
        hint: strings.tools.launch_sandbox.runnerLaunchFailedHint,
        wsbPath: path,
      };
    }
    child.unref();
    return {
      launched: true,
      runner,
      siteIds: valid,
      unknownIdsIgnored: unknown.length > 0 ? unknown : undefined,
      wsbPath: path,
      note: strings.tools.launch_sandbox.noteTemplate.replace("{runner}", runner),
      securityNote: strings.sandbox.securityNote,
    };
  } catch (e) {
    return {
      launched: false,
      runner,
      error: strings.tools.launch_sandbox.runnerLaunchFailedError.replace("{runner}", runner).replace("{message}", (e as Error).message),
      hint: strings.tools.launch_sandbox.runnerLaunchFailedHint,
      wsbPath: path,
    };
  }
}
