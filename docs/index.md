---
---

<p align="center">
  <img src="icon.png" alt="TableCloth" width="112">
</p>

<p align="center">
  <a href="https://github.com/yourtablecloth/TableClothMcp/releases/latest"><img src="https://img.shields.io/github/v/release/yourtablecloth/TableClothMcp?label=release" alt="release"></a>
  <a href="https://www.npmjs.com/package/tablecloth-mcp"><img src="https://img.shields.io/npm/v/tablecloth-mcp?label=npm" alt="npm"></a>
  <a href="https://www.nuget.org/packages/TableCloth.Mcp"><img src="https://img.shields.io/nuget/v/TableCloth.Mcp?label=nuget" alt="nuget"></a>
  <img src="https://img.shields.io/badge/license-AGPL--3.0%20or%20Commercial-blue" alt="license">
</p>

한국의 은행이나 공공(e-Gov) 사이트는 대개 보안프로그램 설치를 요구해서 접속이 번거롭습니다.
**TableCloth MCP**는 대화형 AI가 그런 서비스를 이용하려는 순간을 알아채고, 보안프로그램이 미리 갖춰진
깨끗한 **일회용 Windows Sandbox**(또는 macSandbox)로 해당 공식 사이트를 열어 줍니다.
사이트를 찾아 안전하게 여는 데까지만 돕고, 로그인/인증/실제 업무는 사용자가 직접 진행합니다.

## 이런 순간에 씁니다

사용자가 "샌드박스"라고 말하지 않아도, 아래 같은 의도를 감지하면 AI가 알아서 공식 사이트를 찾아
일회용 샌드박스로 열어 줍니다.

- "국민은행 인터넷뱅킹 PC로 하려고 해"
- "홈택스에서 연말정산 하려는데"
- "정부24에서 서류 하나 떼야 해"
- "보안프로그램을 또 깔라고 하네"

기존 PC를 어지럽히지 않고, 매번 깨끗한 환경에서 필요한 보안프로그램만 갖춘 채로 공식 사이트를 엽니다.
샌드박스를 닫으면 흔적이 남지 않습니다.

## 제공하는 도구

| 도구 | 하는 일 | 성격 |
| --- | --- | --- |
| `search_services` | 한국 은행/금융/공공 사이트를 찾아 서비스 id 를 얻습니다 | 읽기 전용 |
| `get_service` | 특정 서비스의 상세(필요 보안패키지 등)를 반환합니다 | 읽기 전용 |
| `list_categories` | 카테고리별 개수를 반환합니다 | 읽기 전용 |
| `list_companions` | 보조 프로그램 목록을 반환합니다 | 읽기 전용 |
| `generate_wsb` | 실행용 `.wsb` 설정 텍스트를 생성합니다 | 읽기 전용 |
| `launch_sandbox` | 선택한 사이트를 보안프로그램이 갖춰진 일회용 샌드박스로 엽니다 | 로컬 실행 |

## 설치

### Claude Desktop 확장 (.mcpb)

[GitHub Releases](https://github.com/yourtablecloth/TableClothMcp/releases/latest)에서
`tablecloth-mcp.mcpb`를 받아 Claude Desktop 창에 끌어다 놓거나,
Settings > Extensions > Advanced settings > Install Extension 에서 선택합니다.
Claude Desktop 이 번들한 Node 로 실행되는 순수 JS 번들이라 별도 런타임 설치가 필요 없습니다(Windows, macOS).
macOS 에서 `launch_sandbox` 는 Apple Silicon 과 최신 macOS 의 [macSandbox](https://yourtablecloth.app/macSandbox/) 가 필요합니다.

### npx (Node)

Node 가 있으면 아래 한 줄로 등록합니다.

```bash
claude mcp add tablecloth -- npx -y tablecloth-mcp@latest
```

### dnx (.NET)

.NET 10 SDK 가 있으면 아래 한 줄로 등록합니다.

```bash
claude mcp add tablecloth -- dnx TableCloth.Mcp
```

등록 후 Claude Desktop 을 재시작하면 도구가 활성화됩니다.

## 어떻게 동작하나요

이 서버는 **발견과 안전 실행**까지만 담당합니다.

1. 공개 카탈로그(`yourtablecloth.app`)에서 요청한 서비스의 공식 주소와 필요한 보안패키지를 찾습니다.
2. 그 정보를 담은 일회용 샌드박스 설정(`.wsb`)을 만들고 Windows Sandbox(또는 macSandbox)를 띄웁니다.
3. 샌드박스 안에서 보안프로그램이 갖춰진 채로 공식 사이트가 열립니다.

로그인, 인증서, 금융 거래 같은 실제 업무는 사용자가 샌드박스 안에서 직접 진행합니다.
서버는 그 과정을 대신하지 않으며(자동화/RPA 아님), 자격증명을 보거나 다루지 않습니다.

## 개인정보

이 서버는 개인정보나 대화 내용을 수집/저장/전송하지 않습니다. 네트워크 요청은 공개 카탈로그와
GitHub Release 자산을 읽는 용도로만 나갑니다. 자세한 내용은
[개인정보처리방침](https://github.com/yourtablecloth/TableClothMcp/blob/main/PRIVACY.md)을 참고하세요.

## 라이선스

이 프로젝트는 이중 라이선스입니다.

- 오픈소스: **AGPL-3.0-or-later**
- 상용: 별도 **상용 라이선스**([LICENSE-COMMERCIAL](https://github.com/yourtablecloth/TableClothMcp/blob/main/LICENSE-COMMERCIAL))

## 링크

- [GitHub 저장소](https://github.com/yourtablecloth/TableClothMcp)
- [문제 해결 (Troubleshooting)](https://github.com/yourtablecloth/TableClothMcp/blob/main/TROUBLESHOOTING.md)
- [릴리스](https://github.com/yourtablecloth/TableClothMcp/releases)
- [npm 패키지](https://www.npmjs.com/package/tablecloth-mcp)
- [NuGet 패키지](https://www.nuget.org/packages/TableCloth.Mcp)
- [macSandbox (macOS 러너)](https://yourtablecloth.app/macSandbox/)
- [TableCloth 본 프로젝트](https://yourtablecloth.app)
