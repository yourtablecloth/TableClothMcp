using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using TableCloth.Mcp.Catalog;

namespace TableCloth.Mcp.Tools;

/// <summary>
/// 선택된 카탈로그 서비스들을 "보안프로그램이 미리 갖춰진 일회용 Windows Sandbox"로 여는 도구.
/// 실행 배관은 전부 공개 릴리스 자산(무설치 Express 레인)을 사용한다 — TableCloth 설치 불필요:
///   .wsb LogonCommand → (릴리스) tablecloth-prepare.ps1 → SporkBootstrap_&lt;arch&gt;.exe → 포터블 Spork
/// 사이트 사전선택은 환경변수 TABLECLOTH_SITE_IDS 채널(PARAMETERIZED_WSB_SPEC.md §0.5)로 전달된다.
/// </summary>
// 비정적 클래스여야 WithTools<T> 의 형식 인수로 쓸 수 있다(CS0718). 메서드는 static 유지.
[McpServerToolType]
public sealed partial class SandboxTools
{
    // 카탈로그 id 는 화이트리스트지만, .wsb 주입 전에 방어적으로 문자셋을 제한한다.
    [GeneratedRegex(@"^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeIdRegex();

    // 사용자 노출 문구(securityNote, usage, note, hint, .wsb 템플릿)는 shared/strings.json + shared/wsb-template.xml
    // 이 정본이며 SharedResources 가 임베드 리소스에서 로드한다(구현 간 동일). 여기 하드코딩하지 않는다.

    [McpServerTool(Name = "generate_wsb", Title = "샌드박스 설정(.wsb) 생성", ReadOnly = true, OpenWorld = true)]
    [Description(
        "선택한 service id 들로 실행할 Windows Sandbox 설정(.wsb) XML 텍스트를 생성해 반환한다(파일 실행은 안 함). " +
        "모든 OS 에서 호출 가능 — 사용자에게 .wsb 를 건네 더블클릭하게 할 때 쓴다. " +
        "생성된 .wsb 는 GitHub 릴리스의 공개 자산만 받아 동작하며, 지정한 사이트들의 보안프로그램을 " +
        "샌드박스 안에서 자동 설치한 뒤 사이트를 연다. 로그인/인증/업무는 사용자 몫.")]
    public static async Task<WsbResponse> GenerateWsb(
        CatalogClient catalog,
        [Description("샌드박스에서 열 카탈로그 service id 목록(1개 이상). search_services 로 확인.")] string[] serviceIds,
        CancellationToken ct = default)
    {
        var (valid, unknown) = await ResolveIdsAsync(catalog, serviceIds, ct).ConfigureAwait(false);
        if (valid.Count == 0)
        {
            return new WsbResponse
            {
                Error = SharedResources.GenerateWsbErrorNoValidIds,
                UnknownIds = unknown,
                Hint = SharedResources.GenerateWsbHintNoValidIds,
            };
        }

        return new WsbResponse
        {
            SiteIds = valid,
            UnknownIdsIgnored = unknown.Count > 0 ? unknown : null,
            Wsb = BuildWsb(valid),
            Usage = SharedResources.GenerateWsbUsage,
            SecurityNote = SharedResources.SecurityNote,
        };
    }

    [McpServerTool(Name = "launch_sandbox", Title = "샌드박스로 사이트 열기", ReadOnly = false, Destructive = false, OpenWorld = true)]
    [Description(
        "search_services 로 얻은 service id 들로, 그 한국 은행/공공 사이트를 보안프로그램이 갖춰진 일회용 " +
        "샌드박스에서 즉시 연다. 사용자가 은행 계좌 개설/가입/로그인, 인터넷뱅킹, 홈택스, 정부24 등 보안프로그램이 " +
        "필요한 사이트를 실제로 이용하거나 신청하려 할 때 사용한다.\n\n" +
        "핵심 동작: 이용 절차나 로그인 방법, '보안 프로그램 설치가 필요하다'는 안내를 텍스트로 길게 설명하는 대신, " +
        "이 도구로 그 공식 사이트를 바로 열어 사용자가 보안프로그램 설치 번거로움 없이 진행하게 한다. 사용자가 " +
        "'PC로 만들래', '신청할래', '가입할래'처럼 말하거나, 네가 '보안 프로그램 설치가 필요하다'고 안내하려는 " +
        "순간이 바로 이 도구를 쓸 때다('샌드박스'라는 말이 없어도).\n\n" +
        "러너: Windows 는 Windows Sandbox, macOS(Apple Silicon)는 macSandbox, 그 외(Linux 등)는 환경변수 " +
        "TABLECLOTH_WSB_RUNNER 에 지정한 러너(모든 OS 에서 기본값보다 우선, .wsb 경로를 첫 인자로 받음). " +
        "샌드박스가 뜨면 보안프로그램이 자동 설치되고 사이트가 열린다. 러너가 없으면 generate_wsb 로 .wsb 를 받아 " +
        "실행한다. 로그인, 인증(공동/금융/간편인증), 실제 신청은 사용자가 직접 한다(RPA 아님).")]
    public static async Task<LaunchResponse> LaunchSandbox(
        CatalogClient catalog,
        [Description("열 카탈로그 service id 목록(1개 이상). 여러 개면 한 샌드박스에 병합 설치된다.")] string[] serviceIds,
        CancellationToken ct = default)
    {
        var (valid, unknown) = await ResolveIdsAsync(catalog, serviceIds, ct).ConfigureAwait(false);
        if (valid.Count == 0)
            return new LaunchResponse { Launched = false, Error = SharedResources.LaunchErrorNoValidIds, UnknownIds = unknown, Hint = SharedResources.LaunchHintNoValidIds };

        // 러너 선택 순서: (1) TABLECLOTH_WSB_RUNNER 환경변수 오버라이드(모든 OS. Linux 처럼 기본 러너가
        // 없는 환경이나 사용자 지정 러너용), (2) OS 기본값. 같은 .wsb 를 Windows Sandbox / macSandbox /
        // 사용자 러너가 공유한다(PARAMETERIZED_WSB_SPEC §8). 오버라이드 값은 .wsb 경로를 첫 인자로 받는
        // 실행 파일(전체 경로) 또는 PATH 상의 명령이어야 한다.
        var customRunner = Environment.GetEnvironmentVariable("TABLECLOTH_WSB_RUNNER");

        string runner;
        ProcessStartInfo psi;
        bool passPathAsArgument; // Windows(WindowsSandbox.exe)만 UseShellExecute=true + Arguments 문자열 사용

        if (!string.IsNullOrWhiteSpace(customRunner))
        {
            runner = $"custom ({customRunner})";
            psi = new ProcessStartInfo(customRunner) { UseShellExecute = false };
            passPathAsArgument = false;
        }
        else if (OperatingSystem.IsWindows())
        {
            runner = "Windows Sandbox";
            psi = new ProcessStartInfo("WindowsSandbox.exe") { UseShellExecute = true };
            passPathAsArgument = true;
        }
        else if (OperatingSystem.IsMacOS())
        {
            var mac = ResolveMacSandbox();
            if (mac is null)
            {
                return new LaunchResponse
                {
                    Launched = false,
                    Error = SharedResources.RunnerMacNotFoundError,
                    Hint = SharedResources.RunnerMacNotFoundHint,
                };
            }
            runner = "macSandbox";
            // 직접 exec 대신 LaunchServices 로 위임(Finder 더블클릭과 동일 경로): open -a <앱> <.wsb>.
            psi = new ProcessStartInfo("open") { UseShellExecute = false };
            psi.ArgumentList.Add("-a");
            psi.ArgumentList.Add(mac);
            passPathAsArgument = false;
        }
        else
        {
            return new LaunchResponse
            {
                Launched = false,
                Error = SharedResources.RunnerUnsupportedError,
                Hint = SharedResources.RunnerUnsupportedHint,
            };
        }

        var wsb = BuildWsb(valid);
        var path = Path.Combine(Path.GetTempPath(), $"tablecloth-{Guid.NewGuid():n}.wsb");
        await File.WriteAllTextAsync(path, wsb, ct).ConfigureAwait(false);

        if (passPathAsArgument)
            psi.Arguments = $"\"{path}\"";
        else
            psi.ArgumentList.Add(path);

        try
        {
            using var proc = Process.Start(psi);
            return new LaunchResponse
            {
                Launched = true,
                Runner = runner,
                SiteIds = valid,
                UnknownIdsIgnored = unknown.Count > 0 ? unknown : null,
                WsbPath = path,
                Note = SharedResources.LaunchNoteTemplate.Replace("{runner}", runner),
                SecurityNote = SharedResources.SecurityNote,
            };
        }
        catch (Exception ex)
        {
            return new LaunchResponse
            {
                Launched = false,
                Runner = runner,
                Error = SharedResources.RunnerLaunchFailedError.Replace("{runner}", runner).Replace("{message}", ex.Message),
                Hint = SharedResources.RunnerLaunchFailedHint,
                WsbPath = path,
            };
        }
    }

    // macSandbox 앱(`macSandbox for Windows.app`, 번들 ID com.rkttu.macsandbox)의 .app 번들 위치를 찾는다.
    // LaunchServices(open) 위임에는 실행 파일이 아니라 번들 경로가 필요하다. 표준 설치 경로를 먼저 보고,
    // 없으면 번들 ID 로 mdfind 조회해 이름/위치가 달라도 탐지한다(Finder 더블클릭과 동일 인식). 없으면 null.
    private static string? ResolveMacSandbox()
    {
        const string bundleId = "com.rkttu.macsandbox";
        var candidates = new List<string>
        {
            "/Applications/macSandbox for Windows.app",
            "/Applications/MacSandbox.app",
        };
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(home))
        {
            candidates.Add(Path.Combine(home, "Applications", "macSandbox for Windows.app"));
            candidates.Add(Path.Combine(home, "Applications", "MacSandbox.app"));
        }
        foreach (var app in candidates)
        {
            if (Directory.Exists(app))
                return app;
        }

        try
        {
            var psi = new ProcessStartInfo("mdfind")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            psi.ArgumentList.Add($"kMDItemCFBundleIdentifier == '{bundleId}'");
            using var proc = Process.Start(psi);
            if (proc is not null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var app = line.Trim();
                    if (Directory.Exists(app))
                        return app;
                }
            }
        }
        catch
        {
            // mdfind 부재/실패 시 무시 — 표준 경로 탐지 결과로 판단.
        }
        return null;
    }

    private static async Task<(List<string> valid, List<string> unknown)> ResolveIdsAsync(
        CatalogClient catalog, string[] serviceIds, CancellationToken ct)
    {
        var doc = await catalog.GetAsync(ct: ct).ConfigureAwait(false);
        var known = doc.Services.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);

        var valid = new List<string>();
        var unknown = new List<string>();
        foreach (var raw in serviceIds ?? Array.Empty<string>())
        {
            var id = (raw ?? string.Empty).Trim();
            // 카탈로그에 존재(대소문자 구분, StepsComposer 와 동일한 Ordinal) + 안전 문자셋만 통과.
            if (id.Length > 0 && SafeIdRegex().IsMatch(id) && known.Contains(id))
            {
                if (!valid.Contains(id, StringComparer.Ordinal))
                    valid.Add(id);
            }
            else if (id.Length > 0)
            {
                unknown.Add(id);
            }
        }
        return (valid, unknown);
    }

    private static string BuildWsb(IReadOnlyList<string> validIds)
    {
        // 템플릿(LogonCommand 원문)은 shared/wsb-template.xml 이 정본이며 본 리포의 no-install-spork.wsb 와 동일.
        // 유일한 주입점은 TABLECLOTH_SITE_IDS(사이트 사전선택). __SITEIDS__ 만 치환한다.
        // valid 는 이미 SafeIdRegex 통과 → 공백 join 안전(작은따옴표/XML 특수문자 없음).
        // .wsb 안에서는 PowerShell 작은따옴표 문자열이라 리터럴 ' 는 '' 로 이스케이프된다.
        var idsStmt = validIds.Count == 0
            ? string.Empty
            : " $env:TABLECLOTH_SITE_IDS = ''" + string.Join(' ', validIds) + "'';";

        return SharedResources.WsbTemplate.Replace("__SITEIDS__", idsStmt);
    }
}
