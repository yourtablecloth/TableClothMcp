# TableCloth MCP

[![Release](https://img.shields.io/github/v/release/yourtablecloth/TableClothMcp?logo=github&label=release)](https://github.com/yourtablecloth/TableClothMcp/releases/latest)
[![npm](https://img.shields.io/npm/v/tablecloth-mcp?logo=npm&label=npm)](https://www.npmjs.com/package/tablecloth-mcp)
[![NuGet](https://img.shields.io/nuget/v/TableCloth.Mcp?logo=nuget&label=NuGet)](https://www.nuget.org/packages/TableCloth.Mcp)
[![License](https://img.shields.io/badge/license-AGPL--3.0%20or%20Commercial-blue)](LICENSE-AGPL)

식탁보(TableCloth)의 "발견과 안전 실행환경 인계" 레이어를 MCP 서버로 제공합니다. 사용자의 상황이나
질의를 받아 지금 필요한 한국 공공(e-Gov)이나 금융 사이트를 찾아주고, 보안프로그램 설치의 번거로움 없이
깨끗한 일회용 Windows Sandbox로 그 사이트를 열어 줍니다.

이 서버가 하는 일은 보안프로그램 때문에 생기는 불편을 줄이고 알맞은 사이트를 찾아 주는 데까지입니다.
로그인이나 인증, 실제 업무 자동화(RPA)는 하지 않으며, 그 부분은 사용자가 직접 진행합니다.

## 설치

Claude Desktop이라면 원클릭 확장(.mcpb)이 가장 간편하고, 그 외 클라이언트는 MCP 설정에 npx나 dnx 실행을
넣으면 됩니다. 모두 동일한 서버입니다.

### Claude Desktop 확장 (.mcpb, 원클릭)

[GitHub Release](https://github.com/yourtablecloth/TableClothMcp/releases/latest)에서
`tablecloth-mcp-<버전>.mcpb`를 받아 Claude Desktop에 끌어다 놓거나 더블클릭하면 설치됩니다. Node나 .NET
없이 자기완결 네이티브 바이너리로 동작합니다(Windows, macOS Apple Silicon). MCP 설정을 직접 편집할 필요가
없습니다. 참고로 macOS 바이너리는 아직 서명/공증 전이라 Gatekeeper가 실행을 막을 수 있고(허용 필요),
macOS에서의 `launch_sandbox`는 macSandbox가 필요합니다.

### npx로 실행 (Node 사용자, .NET 불필요)

플랫폼에 맞는 네이티브(NativeAOT) 바이너리가 optionalDependency로 자동 설치됩니다. `@latest`를 붙인 이유는
아래 [업데이트](#업데이트) 참고입니다. npx는 버전을 명시하지 않으면 캐시된 옛 버전을 계속 쓸 수 있어,
새 버전을 잘 받도록 `@latest`를 권합니다.

```jsonc
{
  "mcpServers": {
    "tablecloth": { "command": "npx", "args": ["-y", "tablecloth-mcp@latest"] }
  }
}
```

전역으로 설치해서 쓸 수도 있습니다.

```bash
npm install -g tablecloth-mcp
tablecloth-mcp
```

### dnx로 실행 (.NET 10 SDK 사용자)

NuGet 툴 패키지를 별도 설치 없이 실행합니다.

```jsonc
{
  "mcpServers": {
    "tablecloth": { "command": "dnx", "args": ["TableCloth.Mcp", "--yes"] }
  }
}
```

전역 도구로 설치해서 쓸 수도 있습니다.

```bash
dotnet tool install -g TableCloth.Mcp
tablecloth-mcp
```

프리빌드 네이티브 바이너리는 win32-x64, win32-arm64, darwin-arm64, linux-x64, linux-arm64를 제공합니다.
macOS는 macSandbox와 마찬가지로 Apple Silicon만 지원하며, Intel Mac에서는 dnx 방식을 쓰면 됩니다.
검색과 `generate_wsb`는 모든 OS에서 동작하고, `launch_sandbox`의 자동 실행은 러너가 있는
OS(Windows, macOS)에서만 됩니다.

## 업데이트

MCP 서버는 클라이언트가 시작할 때 프로세스로 떠서 실행되고, 실행 중에는 새 버전으로 교체되지 않습니다.
그래서 새 버전은 **클라이언트를 다시 시작해 서버를 새로 띄울 때** 반영됩니다. Claude Desktop은 앱을
재시작해야 하고, Claude Code는 세션을 새로 시작하면 됩니다.

- 무엇이 자동으로 갱신되나: 카탈로그(사이트, 정책, 보안패키지 목록)는 서버가 런타임에 라이브로 받아오므로,
  새 서비스나 정책 추가 같은 변경은 서버 버전을 올리지 않아도 바로 반영됩니다. 서버 버전 교체가 필요한 건
  도구 동작 변경이나 버그 수정 같은 코드 변경뿐입니다.
- npx: 버전 없이 쓰면 캐시된 옛 버전을 계속 쓸 수 있어 `tablecloth-mcp@latest`를 권합니다. 그래도 npm
  캐시 영향이 남을 수 있으니, 확실히 최신으로 받으려면 클라이언트 재시작 전에 `npm cache clean --force`를
  한 번 해도 됩니다.
- dnx: 버전을 명시하지 않으면 실행할 때마다 최신 버전을 해석해 받습니다. 갱신 적시성이 중요하면 dnx 쪽이
  더 유리합니다.
- 결정론이 필요하면(기업 배포 등) 버전을 고정하고(`tablecloth-mcp@0.1.3`, dnx는 `--version`) 의도적으로
  올리는 방법도 있습니다. 갱신은 수동이 되지만 예측 가능합니다.

## 도구

| 도구 | 설명 |
| --- | --- |
| `search_services(query, category?, limit?)` | 표시명(한국어와 영어), URL, 보안패키지명, 검색 키워드를 대상으로 사이트를 검색합니다 |
| `get_service(id)` | 특정 서비스의 상세 정보(필요한 보안패키지 전체, 호환성 주의사항, 아이콘 URL)를 반환합니다 |
| `list_categories()` | 카테고리별 개수를 반환합니다 |
| `list_companions(query?)` | 보조 프로그램(공용 소프트웨어) 목록을 반환합니다 |
| `generate_wsb(serviceIds[])` | 실행용 `.wsb` XML 텍스트를 생성합니다(모든 OS). 지원 러너에서 실행합니다 |
| `launch_sandbox(serviceIds[])` | 즉시 샌드박스를 실행합니다. Windows는 Windows Sandbox, macOS(Apple Silicon)는 [macSandbox](https://github.com/yourtablecloth/macSandbox), 그 외(Linux 등)는 `TABLECLOTH_WSB_RUNNER` 환경변수로 지정한 러너를 씁니다 |

## 예시 흐름 (연말정산)

1. `search_services("연말정산")` 또는 `search_services("홈택스")`로 홈택스 등 후보와 `id`를 얻습니다.
2. `launch_sandbox(["<홈택스 id>"])`를 호출하면 보안프로그램이 갖춰진 일회용 샌드박스가 뜨고 사이트가 열립니다.
3. 인증서나 간편인증, 연말정산간소화 조회와 발급은 사용자가 직접 진행합니다.

## TableCloth 리포지터리에 의존하지 않습니다

런타임에 공개된 자산만 소비합니다.

| 용도 | 소스 |
| --- | --- |
| 카탈로그(사이트와 보안패키지 목록) | `https://yourtablecloth.app/TableClothCatalog/Catalog.xml` |
| 아이콘 | `https://yourtablecloth.app/TableClothCatalog/images/<id>.png` |
| 샌드박스 실행 자산 | GitHub Release `latest/download`의 `tablecloth-prepare.ps1`, `SporkBootstrap_<arch>.exe`, `Spork_<arch>_Portable.zip` |

생성되는 `.wsb`는 무설치 Express 방식(`PARAMETERIZED_WSB_SPEC.md` 0.5절)을 따르며, 사이트 사전선택은
`TABLECLOTH_SITE_IDS` 환경변수 채널로 전달됩니다.

## 사용 팁: TableCloth 스킬로 자동 연결하기

정부 정책이나 금융 상품, 시사 현안을 이야기하다가 "그거 신청할래" 정도로만 말해도 이 서버로 연결되게
하려면, 저장소의 [`SKILL.md`](SKILL.md)를 Agent Skill로 설치하는 방법을 권합니다. 이 스킬은 사용자의
행동 의도(신청, 가입, 접속, 이용)를 포착해 "샌드박스"라는 말이 없어도 TableCloth 도구로 공식 사이트를
안전한 샌드박스에서 열도록 안내합니다. 다른 검색 MCP(예: 웹 검색)와 함께 쓰면 정보 탐색은 그쪽으로 가고
실제 신청과 이용은 이 서버로 갈라집니다.

설치는 `SKILL.md`를 `~/.claude/skills/tablecloth/SKILL.md`에 두면 됩니다. 폴더 이름 `tablecloth`가 스킬
이름이 됩니다. Claude Desktop이나 claude.ai에서는 각 앱의 스킬 설정에서 관리합니다.

도구 설명과 서버 instructions에도 같은 의도가 심어져 있어 스킬 없이도 어느 정도 연결되지만, 스킬을
더하면 더 안정적으로 라우팅됩니다. `launch_sandbox`의 실제 실행 환경은 아래 제약을 참고하세요.

## 제약

- `launch_sandbox`의 자동 실행 러너는 Windows에서는 Windows 11의 "Windows Sandbox" 선택적 기능,
  macOS에서는 [macSandbox](https://github.com/yourtablecloth/macSandbox)(Apple Silicon, macOS 26)입니다.
  리눅스처럼 기본 러너가 없는 OS에서는 `TABLECLOTH_WSB_RUNNER` 환경변수에 `.wsb` 경로를 첫 인자로 받는
  러너 명령(예: QEMU 기반 스크립트)을 지정하면 자동으로 실행합니다. 이 환경변수는 모든 OS에서 기본값보다
  우선하므로 사용자 지정 러너를 붙일 때도 씁니다. 지정이 없으면 `generate_wsb`로 `.wsb`를 받아 실행하면 됩니다.
- 인증이 필요한 동작은 자동화하지 않습니다. 무설치 방식은 호스트 파일 접근이 없어 파일 인증서를 들일 수
  없으므로 모바일이나 간편인증을 전제로 합니다.
- 카탈로그에 없는 서비스는 실행 대상이 아닙니다. 제보는 식탁보 카탈로그로 하면 됩니다.

## 릴리스

버전 태그(`vX.Y.Z`)를 밀면 [`.github/workflows/release.yml`](.github/workflows/release.yml)가 같은
버전으로 여러 곳에 함께 게시합니다.

- npm에 런처 `tablecloth-mcp`와 5개 플랫폼 패키지를 올립니다.
- NuGet에 `TableCloth.Mcp` 툴 패키지를 올립니다.
- GitHub Release에 5개 플랫폼 네이티브 바이너리, nupkg, 그리고 Claude Desktop 확장 `.mcpb`를 첨부합니다.
- 공식 [MCP Registry](https://registry.modelcontextprotocol.io)에 서버 메타데이터(`server.json`)를 게시합니다.
  GitHub OIDC로 인증하므로 시크릿이 필요 없고, VS Code나 Cursor 같은 다른 클라이언트가 이 색인으로 서버를
  발견합니다.

npm과 NuGet은 OIDC Trusted Publishing으로 게시되어 장기 토큰을 저장하지 않으며, npm 패키지에는
provenance가 자동으로 붙습니다. 변경 이력은 [CHANGELOG.md](CHANGELOG.md)에 정리하고, 새 버전을 낼 때는
거기에 `## [X.Y.Z]` 섹션을 추가한 뒤 태그를 밀면 그 내용이 GitHub Release 노트가 됩니다.

## 개발: 네이티브 바이너리 빌드

NativeAOT는 타깃 OS에서 빌드해야 합니다. Windows는 MSVC, macOS는 Xcode Command Line Tools, Linux는
clang과 zlib1g-dev가 필요합니다.

```bash
dotnet publish -r win-x64     -c Release -p:PublishAot=true -p:PackAsTool=false
dotnet publish -r win-arm64   -c Release -p:PublishAot=true -p:PackAsTool=false
dotnet publish -r osx-arm64   -c Release -p:PublishAot=true -p:PackAsTool=false
dotnet publish -r linux-x64   -c Release -p:PublishAot=true -p:PackAsTool=false
dotnet publish -r linux-arm64 -c Release -p:PublishAot=true -p:PackAsTool=false
```

RID별 빌드와 링크는 각 OS 러너에서 수행하며, [`release.yml`](.github/workflows/release.yml)의 `aot`
매트릭스가 이를 자동화합니다. 도구 반환은 선언형 record와 소스 생성 `JsonSerializerContext`를 쓰고,
도구 등록은 제네릭 `WithTools<T>`로 하여 AOT에서 리플렉션 없이 동작합니다.

## 라이선스

본 프로젝트 TableCloth와 동일하게 듀얼 라이선스입니다.

- AGPL-3.0-or-later ([LICENSE-AGPL](LICENSE-AGPL))로 오픈소스 이용이 가능합니다.
- 상업적 이용으로 AGPL 의무를 원치 않으면 개발자(Jung Hyun, Nam, rkttu at rkttu dot com)와 협의하는
  Commercial 라이선스([LICENSE-COMMERCIAL](LICENSE-COMMERCIAL))를 쓰면 됩니다.

이 서버는 stdio 로컬 프로세스로 동작하고 MCP 클라이언트와는 별도 프로세스로 프로토콜을 통해서만
통신하므로, 호출하는 쪽에 AGPL 의무가 전파되지 않습니다.
