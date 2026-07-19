# TableCloth MCP — 설계 명세 (SPEC)

이 문서는 TableCloth MCP 서버의 **언어 중립 계약**입니다. `.NET` 구현과 `Node` 구현은
서로의 코드가 아니라 **이 문서와 `shared/` 리소스**를 기준으로 만들어지고 검증됩니다.
목적은 두 구현 사이의 드리프트(불일치)를 구조적으로 막는 것입니다.

문자열/프롬프트는 이 문서가 아니라 [`shared/strings.json`](shared/strings.json) 이 정본입니다.
이 문서는 "동작 계약"을, `shared/` 는 "문구"를 담습니다.

## 1. 범위

- **한다**: 한국 공공(e-Gov)/금융 서비스를 발견(검색/조회)하고, 보안프로그램이 갖춰진 **일회용 샌드박스로 안전하게 연다.**
- **안 한다**: 로그인/인증/이체/실제 업무 자동화(RPA), 자격증명 취급.
- TableCloth/Spork 데스크톱 앱이나 그 소스에 **의존하지 않는다.** 런타임에 공개 자산만 소비한다.

## 2. 배포 레인과 런타임 매핑

| 레인 | 런타임 | 근거 |
| --- | --- | --- |
| `.mcpb` (Claude Desktop) | **Node/TS** | 호스트가 Node 런타임을 번들 → 자기잠금 바이너리 없음, 무전제, Gatekeeper/AOT매트릭스 회피 |
| npm (`npx`) | **Node/TS** | 순수 JS 패키지(바이너리 없음) → 전파 지연/미서명 블롭 문제 제거 |
| NuGet 도구 (`dnx`) | **C#/.NET** | .NET 사용자용 네이티브 도구. dnx 는 사용자 .NET 호스트가 실행 → 잠금 비이슈 |

두 구현은 **행동/입출력이 동일**해야 한다. 유일하게 다른 것은 런타임과 패키징이다.

## 3. 공유 리소스 (`shared/`)

- `shared/strings.json` — 서버 지침, 도구 title/description/파라미터 설명, note/hint, `securityNote`.
- `shared/wsb-template.xml` — `.wsb` 정본 템플릿(치환점 `__SITEIDS__` 1개).

소비 규칙은 [`shared/README.md`](shared/README.md) 참조. 요약: 문자열은 `shared/` 에서만 수정하고,
C# 의 attribute 상수(도구 Description/Title/파라미터 설명)는 런타임 로드가 불가하므로 코드에 두되
**conformance 테스트가 `strings.json` 과의 일치를 강제**한다.

## 4. 서버 계약

- 전송: **stdio**. `stdout` 은 JSON-RPC 전용, **모든 로그는 stderr**.
- 서버 지침: `strings.json` 의 `server.instructions` 를 그대로 `ServerInstructions` 로 노출.
- 프로토콜: MCP(예: `2025-06-18`). 서버 이름 `tablecloth-mcp`.

## 5. 도구 계약 (6개)

주석: `RO`=readOnly, `OW`=openWorld, `Dx`=destructive. 모든 도구 `openWorld=true`.
문구(title/description/파라미터 설명/note/hint)는 `strings.json` 의 대응 키를 쓴다.

| name | 주석 | 입력 | 출력(주요 필드) |
| --- | --- | --- | --- |
| `search_services` | RO | `query: string`(필수), `category?: string`, `limit?: int=15`(1~50 clamp) | `{query, category?, totalServices, matched, results[]: {id, displayName, displayNameEn?, category, url, requiredPackages[], compatWarning?}, note}` |
| `get_service` | RO | `id: string`(필수) | `{id, displayName, displayNameEn?, category, url, iconUrl, packages[]: {name,url,arguments}, edgeExtensions[]: {name,extensionId,crxUrl}, searchKeywords?, compatNotes?}` 또는 `{error, hint}` |
| `list_categories` | RO | 없음 | `{totalServices, categories[]: {category, count}}` (count 내림차순) |
| `list_companions` | RO | `query?: string` | `{matched, companions[]: {id, displayName, displayNameEn?, url}}` |
| `generate_wsb` | RO | `serviceIds: string[]`(1개+) | `{siteIds[], unknownIdsIgnored?[], wsb, usage, securityNote}` 또는 `{error, unknownIds[], hint}` |
| `launch_sandbox` | RW, Dx=false | `serviceIds: string[]`(1개+) | `{launched:true, runner, siteIds[], unknownIdsIgnored?[], wsbPath, note, securityNote}` 또는 `{launched:false, error, hint, unknownIds?[]}` |

검색 매칭 규칙: `query` 를 공백/쉼표/탭/개행으로 토큰화(소문자, 중복 제거) → 각 서비스의
검색 대상 텍스트(표시명 한/영 + URL + 보안패키지명 + 검색키워드)에 포함되는 토큰 수를 점수로
내림차순, 동점은 표시명 오름차순. `category` 는 대소문자 무시 완전일치 필터.

id 문자셋 방어: `.wsb` 주입 전 `^[A-Za-z0-9._-]+$` 만 허용.

## 6. 카탈로그 계약

- 소스: `https://yourtablecloth.app/TableClothCatalog/Catalog.xml` (XML).
- 아이콘 URL: `https://yourtablecloth.app/TableClothCatalog/images/<id>.png`.
- 파싱: `<TableClothCatalog>` → `<InternetServices>/<Service>`(→`<Packages>/<Package>`, Edge 확장), `<Companions>/<Companion>`.
- 캐시: 프로세스 내 메모리, TTL 30분. 요청 시 지연 로드.
- HTTP: 타임아웃 30초, `User-Agent: tablecloth-mcp/<ver>`.

## 7. `.wsb` 생성 계약

- 템플릿: `shared/wsb-template.xml` 로드 후 `__SITEIDS__` 를 사이트 사전선택 구문으로 치환.
- 사이트 주입 채널: 환경변수 `TABLECLOTH_SITE_IDS`(PARAMETERIZED_WSB_SPEC.md §0.5). 치환 구문 예:
  ` $env:TABLECLOTH_SITE_IDS = ''<id1> <id2>'';` (id 없으면 빈 문자열).
- 실행 자산은 전부 GitHub 릴리스 공개 URL(`tablecloth-prepare.ps1` 등). 무설치 Express 레인.
- `securityNote`: 응답에 항상 포함. 문구는 `strings.json` 의 `sandbox.securityNote`.
- 알려진 이슈/하드닝: 명령이 원격 스크립트 실행 형태라 오탐될 수 있음 → [#1](https://github.com/yourtablecloth/TableClothMcp/issues/1).

## 8. 샌드박스 실행 계약 (`launch_sandbox`)

러너 결정 순서:

1. `TABLECLOTH_WSB_RUNNER`(모든 OS, 최우선) — `UseShellExecute=false`, `.wsb` 경로를 인자 리스트로.
2. Windows → `WindowsSandbox.exe` — `UseShellExecute=true`, `.wsb` 경로를 **인용된 Arguments 문자열**로.
3. macOS → `macSandbox`(`/Applications/MacSandbox.app/Contents/MacOS/MacSandbox` 또는 PATH) — 인자 리스트.
4. 그 외(Linux 등) → 러너 없음 안내(`runnerUnsupported*`).

`.wsb` 는 시스템 임시 폴더에 `tablecloth-<guid>.wsb` 로 쓴 뒤 러너에 전달. 프로세스는
**시작 후 즉시 반환**(WaitForExit 금지 — 매다는 동작 방지).

## 9. 구현 전략

### .NET (C#) — dnx 레인
- MCP C# SDK, 소스젠 JSON(AOT 안전). 도구는 `[McpServerTool]` + `[Description]` attribute.
- 런타임 문자열/템플릿은 `shared/*` 를 어셈블리에 임베드(`EmbeddedResource`)해 `SharedResources` 가 로드.
- `.NET tool`(PackAsTool)로 배포. **NativeAOT는 npm/mcpb 를 Node 가 가져가면 불필요** → 제거 대상.

### Node (TypeScript) — mcpb + npm 레인
- `@modelcontextprotocol/sdk`, stdio. `shared/strings.json` 직접 import, `shared/wsb-template.xml` 읽기.
- 도구 정의는 `strings.json` 의 title/description/파라미터 설명을 그대로 사용.
- 순수 JS 로 배포(바이너리 없음). `.mcpb` 는 Claude Desktop 번들 Node 로 실행.

## 10. 검증 전략 (conformance)

두 구현을 각각 stdio 로 띄워 아래를 **동일성 비교**한다(공용 하네스):

1. `tools/list` — 도구 집합, 이름, 주석(readOnly/destructive/openWorld), 입력 스키마(파라미터/필수/기본값),
   **title/description/파라미터 설명이 `shared/strings.json` 과 정확히 일치**.
2. 대표 입력의 `tools/call` 출력 JSON 동일:
   - `search_services("은행")`, `list_categories()`, `get_service("Hometax")`, `generate_wsb(["Hometax"])`
   - `generate_wsb` 의 `wsb` 는 `shared/wsb-template.xml` + 주입 구문과 바이트 일치(개행 정규화 허용).
3. `securityNote` 등 note/hint 가 `strings.json` 과 일치.

카탈로그는 라이브 데이터라, 동일성 비교는 **같은 시점 스냅샷**을 두 구현에 주입하거나 필드 구조/불변 항목
위주로 비교한다. CI 에서 두 구현 모두에 대해 실행한다.

## 11. 버전/릴리스

- **버전은 두 구현 lockstep.** 한 태그(`vX.Y.Z`)로 함께 게시.
- 이 재편(공유 리소스 도입 + 레인/런타임 정리)부터 **0.2.0** 으로 올린다.
- 현재 상태: 0.2.0 에서 `.NET` 구현이 `shared/` 를 소비하도록 전환(1단계). `Node` 구현과
  conformance 하네스는 이 문서를 따라 후속(2단계)으로 추가한다.
