// Conformance 하네스(SPEC.md §10): .NET 구현과 Node 구현을 각각 stdio 로 띄워
// tools/list(이름/주석/파라미터/제목/설명)와 대표 tools/call 출력, 공유 문자열 일치를 비교한다.
// 사용법: node test/conformance.mjs
//   .NET:  dotnet <repo>/bin/Release/net10.0/tablecloth-mcp.dll
//   Node:  node <repo>/node/dist/index.js
// 불일치가 하나라도 있으면 종료코드 1.
import { spawn } from "node:child_process";
import { createInterface } from "node:readline";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const shared = JSON.parse(readFileSync(join(repo, "shared", "strings.json"), "utf8"));

function makeClient(cmd, args) {
  const child = spawn(cmd, args, { stdio: ["pipe", "pipe", "ignore"] });
  const rl = createInterface({ input: child.stdout });
  const waiters = new Map();
  const buffered = new Map();
  rl.on("line", (line) => {
    line = line.trim();
    if (!line) return;
    let d;
    try { d = JSON.parse(line); } catch { return; }
    if (d.id === undefined || d.id === null) return;
    if (waiters.has(d.id)) { waiters.get(d.id)(d); waiters.delete(d.id); }
    else buffered.set(d.id, d);
  });
  const send = (o) => child.stdin.write(JSON.stringify(o) + "\n");
  const wait = (id, ms = 30000) => new Promise((resolve, reject) => {
    if (buffered.has(id)) return resolve(buffered.get(id));
    const t = setTimeout(() => reject(new Error(`timeout waiting id ${id} from ${cmd}`)), ms);
    waiters.set(id, (d) => { clearTimeout(t); resolve(d); });
  });
  return { child, send, wait, close: () => { try { child.stdin.end(); child.kill(); } catch { } } };
}

async function collect(cmd, args) {
  const c = makeClient(cmd, args);
  c.send({ jsonrpc: "2.0", id: 0, method: "initialize", params: { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "conf", version: "0" } } });
  const init = await c.wait(0);
  c.send({ jsonrpc: "2.0", method: "notifications/initialized" });
  const reqs = [
    [1, "tools/list", undefined],
    [2, "tools/call", { name: "generate_wsb", arguments: { serviceIds: ["Hometax"] } }],
    [3, "tools/call", { name: "search_services", arguments: { query: "은행" } }],
    [4, "tools/call", { name: "list_categories", arguments: {} }],
    [5, "tools/call", { name: "get_service", arguments: { id: "Hometax" } }],
    [6, "tools/call", { name: "list_companions", arguments: {} }],
  ];
  for (const [id, method, params] of reqs) c.send({ jsonrpc: "2.0", id, method, params });
  const body = async (id) => JSON.parse((await c.wait(id)).result.content[0].text);
  const out = {
    instructions: init.result.instructions,
    tools: (await c.wait(1)).result.tools,
    genWsb: await body(2),
    search: await body(3),
    cats: await body(4),
    getSvc: await body(5),
    companions: await body(6),
  };
  c.close();
  return out;
}

// ---- helpers ----
const canon = (v) => JSON.stringify(sortKeys(v));
function sortKeys(v) {
  if (Array.isArray(v)) return v.map(sortKeys);
  if (v && typeof v === "object") return Object.fromEntries(Object.keys(v).sort().map((k) => [k, sortKeys(v[k])]));
  return v;
}
const normTool = (t) => ({
  name: t.name, title: t.title, description: t.description,
  ro: t.annotations?.readOnlyHint, dx: t.annotations?.destructiveHint, ow: t.annotations?.openWorldHint,
  atitle: t.annotations?.title,
  params: Object.keys(t.inputSchema?.properties ?? {}).sort(),
  required: (t.inputSchema?.required ?? []).slice().sort(),
});
const catMap = (r) => Object.fromEntries((r.categories ?? []).map((c) => [c.category, c.count]));
const ids = (arr) => (arr ?? []).map((x) => x.id).sort();
const tnorm = (s) => (s ?? "").trim();

let fails = 0;
const check = (name, ok, detail) => { console.log(`  ${ok ? "PASS" : "FAIL"}  ${name}`); if (!ok) { fails++; if (detail) console.log("        " + detail); } };

const [net, node] = await Promise.all([
  collect("dotnet", [join(repo, "bin", "Release", "net10.0", "tablecloth-mcp.dll")]),
  collect(process.execPath, [join(repo, "node", "dist", "index.js")]),
]);

console.log("\n[1] tools/list parity (.NET vs Node)");
const netTools = Object.fromEntries(net.tools.map((t) => [t.name, normTool(t)]));
const nodeTools = Object.fromEntries(node.tools.map((t) => [t.name, normTool(t)]));
check("tool name set equal", canon(Object.keys(netTools).sort()) === canon(Object.keys(nodeTools).sort()),
  `net=${Object.keys(netTools).sort()} node=${Object.keys(nodeTools).sort()}`);
for (const name of Object.keys(shared.tools)) {
  check(`tool '${name}' normalized equal`, canon(netTools[name]) === canon(nodeTools[name]),
    `net=${canon(netTools[name])}\n        node=${canon(nodeTools[name])}`);
}

console.log("\n[2] title/description match shared/strings.json (both impls)");
for (const [name, spec] of Object.entries(shared.tools)) {
  check(`'${name}' title==shared (net,node)`, netTools[name]?.title === spec.title && nodeTools[name]?.title === spec.title);
  check(`'${name}' description==shared (net,node)`, netTools[name]?.description === spec.description && nodeTools[name]?.description === spec.description);
}

console.log("\n[3] server instructions match shared (both impls)");
check("instructions == shared", net.instructions === shared.server.instructions && node.instructions === shared.server.instructions);

console.log("\n[4] generate_wsb output parity");
check(".wsb byte-equal", net.genWsb.wsb === node.genWsb.wsb);
check("securityNote==shared (both)", net.genWsb.securityNote === shared.sandbox.securityNote && node.genWsb.securityNote === shared.sandbox.securityNote);
check("usage==shared (both)", net.genWsb.usage === shared.tools.generate_wsb.usage && node.genWsb.usage === shared.tools.generate_wsb.usage);
check("siteIds equal", canon(net.genWsb.siteIds) === canon(node.genWsb.siteIds));

console.log("\n[5] list_categories parity");
check("totalServices equal", net.cats.totalServices === node.cats.totalServices, `net=${net.cats.totalServices} node=${node.cats.totalServices}`);
check("category counts equal", canon(catMap(net.cats)) === canon(catMap(node.cats)), `net=${JSON.stringify(catMap(net.cats))}\n        node=${JSON.stringify(catMap(node.cats))}`);

console.log("\n[6] search_services('은행') parity");
check("matched equal", net.search.matched === node.search.matched, `net=${net.search.matched} node=${node.search.matched}`);
check("totalServices equal", net.search.totalServices === node.search.totalServices);
check("result id set equal", canon(ids(net.search.results)) === canon(ids(node.search.results)),
  `net=${ids(net.search.results)}\n        node=${ids(node.search.results)}`);

console.log("\n[7] get_service('Hometax') parity");
const gs = (o) => ({ id: o.id, displayName: o.displayName, displayNameEn: o.displayNameEn, category: o.category, url: o.url, iconUrl: o.iconUrl, packages: o.packages, edgeExtensions: o.edgeExtensions, searchKeywords: tnorm(o.searchKeywords), compatNotes: tnorm(o.compatNotes) });
check("service fields equal", canon(gs(net.getSvc)) === canon(gs(node.getSvc)),
  `net=${canon(gs(net.getSvc))}\n        node=${canon(gs(node.getSvc))}`);

console.log("\n[8] list_companions parity");
check("matched equal", net.companions.matched === node.companions.matched, `net=${net.companions.matched} node=${node.companions.matched}`);
check("companion id set equal", canon(ids(net.companions.companions)) === canon(ids(node.companions.companions)));

console.log(`\n${fails === 0 ? "ALL CONFORMANCE CHECKS PASSED" : `${fails} CHECK(S) FAILED`}`);
process.exit(fails === 0 ? 0 : 1);
