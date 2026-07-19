# tablecloth-mcp (Node/TS 구현)

`.mcpb`(Claude Desktop)와 npm(npx) 레인을 위한 **Node/TypeScript 구현**입니다.
NuGet 도구(dnx) 레인의 **.NET 구현과 counterpart**이며, 둘 다 [`../SPEC.md`](../SPEC.md)와
[`../shared/`](../shared/)를 단일 진실 원천으로 소비합니다(구현 간 드리프트 방지).

순수 JS로 배포되어(네이티브 바이너리 없음) NativeAOT 매트릭스, 미서명 바이너리, macOS Gatekeeper,
확장 재설치 시 exe 잠금 문제가 없습니다. Claude Desktop 이 번들한 Node 로 실행됩니다.

## 개발

```bash
npm install
npm run build        # gen(shared 인라인) + esbuild 단일 파일 번들 -> dist/index.js
npm run typecheck    # tsc --noEmit
npm run conformance  # .NET(bin/Release) 과 Node(dist) 를 stdio 로 띄워 출력 동등성 비교
```

`npm run conformance` 전에는 리포 루트에서 `dotnet build -c Release` 와 이 폴더에서 `npm run build` 가
되어 있어야 합니다. 하네스는 tools/list(이름·주석·파라미터·제목·설명), 서버 지침, 대표 tools/call
출력, `.wsb` 바이트 일치를 검사하며, 하나라도 다르면 종료코드 1 입니다.

## 구조

- `scripts/gen-shared.mjs` — `../../shared/{strings.json,wsb-template.xml}` 를 `src/generated.ts` 로 인라인.
- `src/catalog.ts` — 공개 카탈로그 fetch/파싱/캐시(.NET CatalogClient 와 동일 규칙).
- `src/tools/catalog-tools.ts` — search_services, get_service, list_categories, list_companions.
- `src/tools/sandbox-tools.ts` — generate_wsb, launch_sandbox.
- `src/index.ts` — MCP stdio 서버, 도구 6개 등록.
