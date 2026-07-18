# TableCloth MCP (standalone)

식탁보(TableCloth)의 **발견 + 안전 실행환경 인계** 레이어를 MCP 서버로 제공한다.
상황/질의를 받아 **지금 필요한 한국 공공(e-Gov)/금융 사이트를 찾아주고**, 보안프로그램 설치 지옥 없이
**깨끗한 일회용 Windows Sandbox 로 그 사이트를 열어**준다.

> 가치 범위는 "보안프로그램으로 인한 frustration 감소 + 사이트 찾기"까지다.
> 로그인/인증/실제 업무 자동화(RPA)는 **하지 않는다** — 그건 사용자의 몫.

## 이 서버는 TableCloth 리포지터리에 의존하지 않는다

런타임에 **공개 자산만** 소비한다:

| 용도 | 소스 |
| --- | --- |
| 카탈로그(사이트/보안패키지 목록) | `https://yourtablecloth.app/TableClothCatalog/Catalog.xml` |
| 아이콘 | `https://yourtablecloth.app/TableClothCatalog/images/<id>.png` |
| 샌드박스 실행 자산 | GitHub Release `latest/download` 의 `tablecloth-prepare.ps1` / `SporkBootstrap_<arch>.exe` / `Spork_<arch>_Portable.zip` |

생성되는 `.wsb` 는 무설치 Express 레인(`PARAMETERIZED_WSB_SPEC.md §0.5`)을 따르며,
사이트 사전선택은 `TABLECLOTH_SITE_IDS` 환경변수 채널로 전달된다.

## 도구(tools)

| 도구 | 설명 |
| --- | --- |
| `search_services(query, category?, limit?)` | 사이트를 키워드로 검색(표시명 한/영·URL·보안패키지명·검색키워드) |
| `get_service(id)` | 특정 서비스 상세(필요 보안패키지 전체, 호환성 주의, 아이콘 URL) |
| `list_categories()` | 카테고리별 개수 |
| `list_companions(query?)` | 보조 프로그램(공용 SW) 목록 |
| `generate_wsb(serviceIds[])` | 실행용 `.wsb` XML 텍스트 생성(모든 OS). 사용자에게 건네 더블클릭 |
| `launch_sandbox(serviceIds[])` | Windows 에서 즉시 샌드박스 실행(WindowsSandbox.exe) |

## 배포/실행 레인

### 레인 A — dnx / .NET tool (기본, 구현됨)

NuGet 에 `TableCloth.Mcp` 툴 패키지로 게시하고, 클라이언트는 `dnx` 로 설치 없이 실행한다.
클라이언트에 **.NET 10 SDK** 필요.

```jsonc
{
  "mcpServers": {
    "tablecloth": { "command": "dnx", "args": ["TableCloth.Mcp", "--yes"] }
  }
}
```

로컬 개발/게시 전:

```bash
dotnet run --project TableCloth.Mcp.csproj     # 직접 실행
dotnet pack -c Release                          # bin/Release/TableCloth.Mcp.<ver>.nupkg
```

### 레인 B — npx / NativeAOT 바이너리

Node 생태계 도달 + .NET 미설치 사용자를 위해, 플랫폼별 **NativeAOT** 바이너리를 npm 패키지로 감싸
`npx @tablecloth/mcp` 로 실행한다(플랫폼별 바이너리를 고르는 JS shim + `optionalDependencies`).

```jsonc
{ "mcpServers": { "tablecloth": { "command": "npx", "args": ["-y", "@tablecloth/mcp"] } } }
```

**AOT 호환은 코드상 완료·검증됨:**
- 도구 반환을 익명 객체 → **선언형 record + 소스젠 `AppJsonContext`(JsonSerializerContext)** 로 전환.
- 도구 등록을 리플렉션 스캔(`WithToolsFromAssembly`, `[RequiresUnreferencedCode]`) → 제네릭
  **`WithTools<T>(options)`** 로 전환(도구 클래스는 비정적이어야 함 — CS0718).
- `EnableAotAnalyzer/TrimAnalyzer/SingleFileAnalyzer` 켠 빌드 **경고 0**, ILC 네이티브 코드 생성 **성공**
  (`obj/…/native/*.obj` 산출). 최종 링크만 C++ 빌드 도구를 요구.

**네이티브 바이너리 만들기(표준 NativeAOT 전제: C++ 빌드 도구 필요):**

```bash
# VS "Desktop development with C++" 워크로드가 있는 환경(로컬 개발자 프롬프트 / CI)에서:
dotnet publish -r win-x64   -c Release -p:PublishAot=true -p:PackAsTool=false
dotnet publish -r win-arm64 -c Release -p:PublishAot=true -p:PackAsTool=false
# 산출: bin/Release/net10.0/<rid>/publish/tablecloth-mcp.exe (self-contained 단일 네이티브)
```

이후 각 RID exe 를 npm 패키지(`@tablecloth/mcp` + 플랫폼별 optionalDependency)로 감싸 게시하면
`npx` 로 .NET 설치 없이 실행된다. (RID 별 빌드/링크는 CI에서 수행 권장 — 부트스트래퍼와 동일하게.)

## 릴리스 (동일 태그 → 두 채널 동시)

버전 태그 하나(`vX.Y.Z`)를 밀면 [`.github/workflows/release.yml`](.github/workflows/release.yml)가
**dnx(NuGet)와 npx(npm)를 같은 버전으로 동시에** 게시한다.

1. `Version`을 정하고(태그에서 파생), csproj `<Version>`도 맞춘다.
2. `git tag vX.Y.Z && git push origin vX.Y.Z`
3. 워크플로우:
   - `nuget` 잡: `dotnet pack` → `TableCloth.Mcp` 를 nuget.org 로 push (dnx 레인)
   - `aot` 잡(매트릭스 win-x64/win-arm64): NativeAOT 게시 → 바이너리 아티팩트
   - `npm` 잡: `scripts/assemble-npm.mjs`로 `tablecloth-mcp`(런처)+플랫폼 패키지 조립 → npmjs.org publish (npx 레인)

**필요 시크릿:** `NUGET_API_KEY`, `NPM_TOKEN`. (수동 실행은 Actions → Release → Run workflow, `version` 입력)

**이름 선점 확인:** nuget `TableCloth.Mcp`, npm `tablecloth-mcp` / `tablecloth-mcp-win32-x64` / `-arm64` 가
사용 가능한지(또는 소유 중인지) 최초 게시 전에 확인할 것.

## 예시 흐름 (연말정산)

1. `search_services("연말정산")` 또는 `search_services("홈택스")` → 홈택스 등 후보 + `id`
2. `launch_sandbox(["<홈택스 id>"])` → 보안프로그램이 갖춰진 일회용 샌드박스가 뜨고 사이트가 열림
3. 인증서/간편인증·연말정산간소화 조회·발급은 **사용자가 직접** 진행

## 제약

- `launch_sandbox` 는 Windows 11 + "Windows Sandbox" 선택적 기능 필요. 그 외 OS 는 `generate_wsb` 사용.
- 인증이 필요한 액션은 자동화하지 않는다(무설치 레인은 호스트 파일 접근이 없어 파일 인증서 반입 불가 → 모바일/간편인증 전제).
- 카탈로그에 없는 서비스는 실행 대상이 아니다(제보는 식탁보 카탈로그로).

## 라이선스

본 프로젝트 TableCloth 와 동일한 **듀얼 라이선스**다:

- **AGPL-3.0-or-later** ([LICENSE-AGPL](LICENSE-AGPL)) — 오픈소스 이용.
- **Commercial** ([LICENSE-COMMERCIAL](LICENSE-COMMERCIAL)) — AGPL 의무를 원치 않는 상업적 이용은
  개발자(Jung Hyun, Nam / rkttu at rkttu dot com)와 협의.

참고: 이 서버는 stdio 로컬 프로세스로 동작하고 MCP 클라이언트와 프로토콜(별도 프로세스)로만 통신하므로,
호출하는 쪽에 AGPL 의무가 전파되지 않는다. AGPL 바이너리(npm/NuGet)를 공개 배포하므로 소스는 공개
저장소로 제공된다.
