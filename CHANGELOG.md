# Changelog

이 프로젝트의 버전별 주요 변경 사항을 정리합니다. GitHub Release 노트는 릴리스 시 해당 버전
섹션의 내용으로 자동 생성됩니다. 새 버전을 낼 때는 아래에 `## [X.Y.Z] - YYYY-MM-DD` 섹션을 추가하세요.

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
