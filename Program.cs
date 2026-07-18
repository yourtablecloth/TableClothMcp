using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using TableCloth.Mcp.Catalog;
using TableCloth.Mcp.Tools;

// TableCloth MCP server (stdio).
//
// 설계 원칙: 이 서버는 TableCloth/Spork 데스크톱 앱 및 그 소스에 의존하지 않는다.
// 카탈로그와 샌드박스 실행 자산을 "공개 URL"에서만 받아 소비하는 별도 lane 이다.
// (PARAMETERIZED_WSB_SPEC.md 의 consumer ② "독립형 MCP 서버" 역할)
//
// 가치 범위: "지금 필요한 한국 공공/금융 서비스를 찾아주고, 보안프로그램 설치 없이
// 깨끗한 일회용 샌드박스로 그 사이트를 열어준다"까지. 로그인/인증/실제 업무 자동화(RPA)는
// 의도적으로 하지 않는다 — 그건 사용자의 몫.

var builder = Host.CreateApplicationBuilder(args);

// stdio 전송은 stdout 을 프로토콜 채널로 쓴다. 모든 로그는 stderr 로 보낸다(안 그러면 프로토콜 깨짐).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton(_ => new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30),
    DefaultRequestHeaders = { { "User-Agent", "tablecloth-mcp/0.1" } },
});
builder.Services.AddSingleton<CatalogClient>();

// NativeAOT 대비: 리플렉션 스캔(WithToolsFromAssembly)은 [RequiresUnreferencedCode] 라 쓰지 않는다.
// 제네릭 WithTools<T> + 소스젠 컨텍스트(AppJsonContext)를 SDK 기본 옵션 앞쪽에 얹어 도구 입출력을 직렬화한다.
var toolJsonOptions = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);
toolJsonOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);

builder.Services
    .AddMcpServer(options =>
    {
        // 서버 수준 지침: 클라이언트(예: Claude Desktop)가 모델에 힌트로 노출한다.
        // 사용자가 "샌드박스"를 명시하지 않아도 행동 의도(신청/가입/접속/이용)면 이 서버로 라우팅되도록 유도.
        options.ServerInstructions =
            "TableCloth 는 한국 공공(e-Gov)과 금융 서비스를 보안프로그램이 갖춰진 안전한 일회용 샌드박스에서 열어 준다. " +
            "사용자가 대화 중 특정 한국 공공/금융 서비스나 정책을 실제로 신청, 가입, 접속, 이용하려는 의도를 보이면 " +
            "(예: '신청할래', '가입할래', '이거 해야겠다', '접속해서 처리할래'), 사용자가 '샌드박스'라는 말을 하지 않아도 " +
            "search_services 로 해당 서비스를 찾아 launch_sandbox 로 연다. 단순 정보나 뉴스 검색은 일반 웹 검색 도구를 쓰고, " +
            "이 서버의 도구는 실제 이용/신청 행동에 쓴다. 로그인, 인증, 실제 업무는 사용자가 직접 진행한다(RPA 아님).";
    })
    .WithStdioServerTransport()
    .WithTools<CatalogTools>(toolJsonOptions)
    .WithTools<SandboxTools>(toolJsonOptions);

await builder.Build().RunAsync();
