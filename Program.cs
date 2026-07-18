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
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<CatalogTools>(toolJsonOptions)
    .WithTools<SandboxTools>(toolJsonOptions);

await builder.Build().RunAsync();
