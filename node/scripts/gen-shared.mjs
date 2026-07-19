// shared/strings.json + shared/wsb-template.xml 을 src/generated.ts 로 인라인한다.
// .NET 이 EmbeddedResource 로 하는 것과 동일하게, Node 는 빌드 시 shared/ 를 번들에 인라인해
// 단일 진실 원천을 소비한다(SPEC.md). 번들러/경로 해석에 의존하지 않아 npm/.mcpb 모두 안전.
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const sharedDir = join(here, "..", "..", "shared");

const strings = readFileSync(join(sharedDir, "strings.json"), "utf8").trimEnd();
const wsb = readFileSync(join(sharedDir, "wsb-template.xml"), "utf8");

const out = `// AUTO-GENERATED from ../../shared/ by scripts/gen-shared.mjs. DO NOT EDIT.
/* eslint-disable */
export const strings = ${strings} as const;
export const wsbTemplate = ${JSON.stringify(wsb)};
`;

const srcDir = join(here, "..", "src");
mkdirSync(srcDir, { recursive: true });
writeFileSync(join(srcDir, "generated.ts"), out);
console.error("gen-shared: wrote src/generated.ts");
