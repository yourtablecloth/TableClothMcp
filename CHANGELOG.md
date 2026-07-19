# Changelog

이 프로젝트의 버전별 주요 변경 사항을 정리합니다. GitHub Release 노트는 릴리스 시 해당 버전
섹션의 내용으로 자동 생성됩니다. 새 버전을 낼 때는 아래에 `## [X.Y.Z] - YYYY-MM-DD` 섹션을 추가하세요.

## [0.3.0] - 2026-07-19

### Changed
- 레인별 런타임 컷오버(SPEC.md 3단계). `.mcpb`(Claude Desktop)와 npm(npx) 을 **Node 순수 JS** 로
  게시하도록 릴리스 파이프라인을 재구성했습니다. NativeAOT 5-플랫폼 매트릭스와 npm 플랫폼 바이너리
  패키지를 제거했고, `.mcpb` 는 Claude Desktop 번들 Node 로 실행되는 자기완결 JS 번들이 되었습니다
  (크로스플랫폼, 네이티브 바이너리 없음 → 미서명/Gatekeeper/확장 재설치 시 exe 잠금 이슈 해소).
  NuGet 도구(dnx)는 C# 로 유지합니다.
- CI 에 conformance 게이트를 추가했습니다. 매 빌드에서 .NET 과 Node 구현을 stdio 로 띄워
  tools/list·대표 출력·공유 문자열·`.wsb` 일치를 검증합니다.

### Removed
- 구 npm 런처/플랫폼 바이너리 패키징(`scripts/assemble-npm.mjs`, `npm/`). 순수 JS 패키지로 대체.
  구 플랫폼 패키지(`tablecloth-mcp-<os>-<arch>`)는 `npm deprecate` 로 정리 예정.

## [0.2.0] - 2026-07-19

### Changed
- 구현 간 드리프트를 구조적으로 막기 위해 **단일 진실 원천**을 도입했습니다. 모든 프롬프트/문자열과
  `.wsb` 템플릿을 `shared/strings.json` 과 `shared/wsb-template.xml` 로 외부화하고, .NET 구현이 이를
  어셈블리에 임베드해 런타임에 소비하도록 리팩토링했습니다(동작/출력 동일).
- 언어 중립 설계 명세 `SPEC.md` 를 추가했습니다. 도구/카탈로그/`.wsb`/샌드박스 실행 계약과, 배포 레인별
  런타임 전략(`.mcpb`+npm 은 Node, NuGet 도구는 C#), conformance 검증 전략을 규정합니다.
- 이 재편에 맞춰 버전 라인을 **0.2.0** 으로 올렸습니다. Node 구현과 conformance 하네스는 이 명세를
  따라 후속(2단계)으로 추가합니다.

## [0.1.8] - 2026-07-19

### Added
- `generate_wsb`와 `launch_sandbox` 응답에 `securityNote` 필드를 추가했습니다. 생성/실행되는 `.wsb`가
  공식 HTTPS 스크립트를 일회용 샌드박스 안에서만 실행하고 호스트에 영향이 없으며 자격증명을 다루지
  않는다는 동작을 명시해, 어시스턴트가 이 명령을 악성 다운로더로 오탐하거나 사용자 불안을 유발하는 것을
  줄입니다. 관련 known issue와 근본 개선(명령 최소화 + 해시 검증)은 TROUBLESHOOTING.md와 #1에서 추적합니다.

## [0.1.7] - 2026-07-19

### Added
- TableCloth 로고를 아이콘으로 넣었습니다. `.mcpb` 번들 매니페스트의 `icon` 필드와 NuGet 패키지
  아이콘에 512x512 PNG로 반영되어, Claude 확장 목록과 nuget.org 리스팅에 표시됩니다.

## [0.1.6] - 2026-07-19

### Added
- Claude Connectors Directory(데스크톱 확장) 제출 준비를 마쳤습니다. 모든 도구에 `title`과
  `readOnly`/`destructive` annotation을 추가하고, 개인정보처리방침을 갖췄습니다(PRIVACY.md,
  README의 Privacy Policy 섹션, `.mcpb` manifest의 `privacy_policies`).

## [0.1.5] - 2026-07-19

### Added
- Claude Desktop 확장(.mcpb) 번들을 릴리스 때 자동으로 만들어 GitHub Release에 첨부합니다. Node나 .NET
  없이 원클릭으로 설치되는 자기완결 네이티브 바이너리 번들입니다(Windows, macOS Apple Silicon).
- 공식 MCP Registry에 서버 메타데이터(server.json)를 게시하는 잡을 추가했습니다(GitHub OIDC 인증,
  시크릿 불필요). npm 패키지에 mcpName을 넣어 레지스트리 검증을 통과하게 했습니다.

## [0.1.4] - 2026-07-19

### Changed
- 도구가 더 능동적으로 호출되도록 `search_services`와 `launch_sandbox`의 설명을 대폭 강화했습니다.
  은행 계좌 개설이나 인터넷뱅킹 로그인처럼 보안프로그램이 필요한 한국 사이트를 이용하려는 순간에는
  절차를 설명하는 대신 이 도구로 사이트를 열도록, "언제 쓰고 언제 쓰지 않는지"를 명시했습니다.
  Claude Desktop은 MCP 서버의 instructions 필드를 사용하지 않으므로, 도구 설명이 서버 쪽에서 유일하게
  효과적인 신호입니다. 다만 서버만으로 모호한 의도의 능동 호출을 완전히 보장할 수는 없어, 확실히 하려면
  사용자나 프로젝트 지침을 함께 두는 것을 권합니다.

## [0.1.3] - 2026-07-19

### Added
- 대화 중 한국 공공/금융 서비스나 정책을 실제로 신청하거나 이용하려는 의도를 감지하면, 사용자가
  "샌드박스"를 명시하지 않아도 도구로 연결되도록 도구 설명을 의도 기반으로 다듬고 MCP 서버
  instructions를 추가했습니다.
- GitHub Release 노트를 CHANGELOG.md의 해당 버전 섹션으로 채우는 루틴을 추가했습니다.
- README에 최신 릴리스 태그 배지와 자동 연결을 위한 사용 팁을 추가했습니다.

### Changed
- README의 릴리스 안내에서 제작자에게만 해당하는 부트스트랩 토큰과 IP allowlist 설명을 걷어내고
  읽는 사람에게 필요한 내용으로 정리했습니다.

## [0.1.2] - 2026-07-18

### Added
- `launch_sandbox`에 `TABLECLOTH_WSB_RUNNER` 환경변수 오버라이드를 추가해 리눅스 등에서도 사용자
  지정 러너로 자동 실행할 수 있게 했습니다.
- 태그 푸시 시 5개 플랫폼 네이티브 바이너리와 nupkg를 첨부한 GitHub Release를 생성하는 잡을 추가했습니다.

## [0.1.1] - 2026-07-18

### Changed
- README를 정리하고 npm/NuGet 버전 배지와 패키지 매니저별 설치 가이드를 추가했습니다.
- npm 게시를 순수 OIDC Trusted Publishing으로 전환했습니다.

### Fixed
- npm provenance 검증을 위해 플랫폼 패키지 package.json에 `repository` 필드를 추가했습니다.

## [0.1.0] - 2026-07-18

### Added
- 최초 릴리스. 카탈로그 검색과 안전 샌드박스 실행 도구(`search_services`, `get_service`,
  `list_categories`, `list_companions`, `generate_wsb`, `launch_sandbox`).
- 하나의 태그에서 dnx(NuGet)와 npx(npm) 두 채널을 동시에 게시하는 릴리스 파이프라인.
- Windows와 macOS(Apple Silicon)용 크로스플랫폼 NativeAOT 바이너리.
