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

MCP 클라이언트(Claude Desktop, Claude Code 등)의 설정에 아래 방식 중 하나를 넣으면 됩니다. 두 방식은
동일한 서버이며, 실행 경로만 다릅니다.

### npx로 실행 (Node 사용자, .NET 불필요)

플랫폼에 맞는 네이티브(NativeAOT) 바이너리가 optionalDependency로 자동 설치됩니다.

```jsonc
{
  "mcpServers": {
    "tablecloth": { "command": "npx", "args": ["-y", "tablecloth-mcp"] }
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

## 사용 팁: "샌드박스" 없이 의도만으로 연결하기

정부 정책이나 금융 상품, 시사 현안을 이야기하다가 "그거 신청할래" 정도로만 말해도 이 서버로 연결되게
하려면, Claude Desktop의 커스텀 지침(또는 프로젝트 지침)에 아래 문장을 넣어 두면 좋습니다.

> 내가 한국 공공/금융 서비스나 정책을 실제로 신청하거나 이용하려는 뜻을 비치면(예: "신청할래", "가입할래",
> "접속해서 처리할래"), 내가 "샌드박스"라고 말하지 않아도 TableCloth 도구로 해당 공식 사이트를 안전한
> 샌드박스에서 열어줘. 단순 정보나 뉴스 검색은 기존 검색 도구를 써줘.

도구 설명과 서버 instructions에도 같은 의도가 심어져 있어 지침 없이도 어느 정도 연결되지만, 위 지침을
더하면 더 안정적으로 라우팅됩니다. 다른 검색 MCP(예: 웹 검색)와 함께 붙여 두면 정보 탐색은 그쪽으로 가고
실제 신청과 이용은 이 서버로 갈라집니다. `launch_sandbox`의 실제 실행은 Windows(Windows Sandbox)나
Apple Silicon Mac(macSandbox), 또는 `TABLECLOTH_WSB_RUNNER`를 지정한 환경에서 됩니다.

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
버전으로 세 곳에 함께 게시합니다.

- npm에 런처 `tablecloth-mcp`와 5개 플랫폼 패키지를 올립니다.
- NuGet에 `TableCloth.Mcp` 툴 패키지를 올립니다.
- GitHub Release에 5개 플랫폼 네이티브 바이너리와 nupkg를 첨부합니다. 패키지 매니저를 거치지 않고
  바이너리를 바로 받고 싶을 때 씁니다.

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
