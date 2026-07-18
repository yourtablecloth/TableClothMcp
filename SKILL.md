---
name: tablecloth
description: >-
  대화 중 한국 공공(e-Gov)이나 금융 서비스, 정책을 사용자가 실제로 신청, 가입, 접속, 이용하려 할 때
  사용한다(예: "그거 신청할래", "가입할래", "접속해서 처리해야겠다"). 사용자가 "샌드박스"라고 말하지
  않아도 행동 의도면 TableCloth MCP 도구로 해당 공식 사이트를 보안프로그램이 갖춰진 안전한 일회용
  샌드박스에서 연다. 단순 정보나 뉴스 검색에는 쓰지 않는다. Use this when the user wants to actually
  apply for or access a Korean government or financial service that came up in the conversation.
---

# TableCloth: 한국 공공/금융 서비스 안전 실행

한국의 공공(e-Gov)과 금융 사이트는 대개 보안프로그램 설치를 요구해 접속이 번거롭다. 이 스킬은
사용자가 그런 서비스를 실제로 이용하려는 순간을 포착해, 보안프로그램이 미리 갖춰진 일회용 샌드박스에서
해당 공식 사이트를 열어 준다. TableCloth MCP 서버의 도구를 쓴다.

## 언제 발동하나

정부 정책, 금융 상품, 시사 현안을 이야기하다가 사용자가 실제 행동 의도를 보일 때다.

- "그거 신청할래", "가입할래", "접속해서 처리할래", "이거 해야겠다" 처럼 실제 이용이나 신청 의도.
- 사용자가 "샌드박스"라는 말을 하지 않아도 위 의도면 이 스킬로 연결한다.

쓰지 않는 경우: 단순히 정보를 묻거나("이 정책이 뭐야?", "조건이 어떻게 돼?") 뉴스를 찾는 경우는
일반 웹 검색을 쓴다. 이 스킬은 실제로 가서 이용하는 행동에만 쓴다.

## 어떻게 하나

1. 대화에서 사용자가 이용하려는 서비스나 정책을 파악한다.
2. `search_services`로 그 서비스를 찾아 카탈로그 `id`를 얻는다(예: "연말정산"이면 홈택스).
   - 후보가 여럿이면 어떤 것인지 사용자에게 짧게 확인한다.
   - 검색 결과가 없으면 카탈로그에 없는 서비스이므로, 공식 사이트 링크만 안내하고 멈춘다.
3. 실행 전에 한 줄로 확인한다. 예: "홈택스를 안전 샌드박스로 열까요?" (샌드박스는 다소 무거우니
   의도치 않은 실행을 막는다.)
4. `launch_sandbox`에 그 `id`를 넘겨 샌드박스로 공식 사이트를 연다.
5. 로그인과 인증(공동/금융/간편인증), 실제 신청과 조회는 사용자가 직접 하도록 안내한다.
   이 스킬은 대신 로그인하거나 업무를 자동화하지 않는다.

## TableCloth 도구가 없을 때 (설치)

이 스킬은 TableCloth MCP 서버가 클라이언트에 등록돼 있어야 그 도구(`search_services`,
`launch_sandbox` 등)를 쓸 수 있다. 도구가 보이지 않으면 아래 방법을 사용자에게 안내하되, 직접 실행하지
말고 사용자가 실행하게 한다. MCP 서버는 세션 시작 시점에 연결되므로, 세션 도중에 추가해도 그 세션에서는
도구가 바로 보이지 않는다. 등록 후 새 세션을 시작해야 한다.

Claude Code:

```bash
# npx (Node 사용자, .NET 불필요). npx 는 캐시된 옛 버전을 쓸 수 있어 @latest 를 붙인다.
claude mcp add --transport stdio --scope user tablecloth -- npx -y tablecloth-mcp@latest

# 또는 dnx (.NET 10 SDK 사용자). dnx 는 버전 미명시 시 실행마다 최신을 해석한다.
claude mcp add --transport stdio --scope user tablecloth -- dnx TableCloth.Mcp --yes
```

Claude Desktop: `claude_desktop_config.json`의 `mcpServers`에 아래를 넣고 앱을 재시작한다.

```jsonc
{ "mcpServers": { "tablecloth": { "command": "npx", "args": ["-y", "tablecloth-mcp@latest"] } } }
```

등록은 사용자의 환경을 바꾸는 일이라 임의로 실행하지 않는다. 위 명령이나 설정을 제시하고, 사용자가
직접 등록한 뒤 세션(또는 앱)을 새로 시작하도록 안내한다.

## 업데이트

새 버전은 클라이언트가 서버 프로세스를 다시 띄울 때 반영된다. Claude Desktop은 앱 재시작, Claude Code는
새 세션이 필요하다. 카탈로그(사이트와 정책)는 런타임에 라이브로 받아오므로 서버 버전을 올리지 않아도
갱신된다. 사용자가 최신 도구 동작을 못 받는 것 같으면 클라이언트를 재시작하도록 안내한다.

## 제약

- `launch_sandbox`의 실제 실행은 Windows(Windows Sandbox)나 Apple Silicon Mac(macSandbox),
  또는 `TABLECLOTH_WSB_RUNNER`를 지정한 환경에서 된다. 그 외에는 `generate_wsb`로 .wsb를 받아 지원
  러너에서 실행한다.
- 카탈로그에 없는 서비스는 실행 대상이 아니다.
- 인증이 필요한 동작은 자동화하지 않는다. 모바일이나 간편인증을 전제로 사용자가 직접 인증한다.
