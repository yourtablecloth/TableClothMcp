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

    [McpServerTool(Name = "generate_wsb")]
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
                Error = "유효한 service id 가 없습니다.",
                UnknownIds = unknown,
                Hint = "search_services 로 정확한 id(대소문자 구분)를 확인하세요.",
            };
        }

        return new WsbResponse
        {
            SiteIds = valid,
            UnknownIdsIgnored = unknown.Count > 0 ? unknown : null,
            Wsb = BuildWsb(valid),
            Usage = "이 XML 을 .wsb 파일로 저장해 더블클릭하면 Windows Sandbox 가 뜨고, 지정한 사이트들이 보안프로그램과 함께 준비됩니다. (Windows 11 + Windows Sandbox 기능 필요)",
        };
    }

    [McpServerTool(Name = "launch_sandbox")]
    [Description(
        "선택한 service id 들로 Windows Sandbox 를 즉시 실행한다(Windows 전용). " +
        ".wsb 를 임시 생성해 WindowsSandbox.exe 로 띄운다. 샌드박스 안에서 해당 사이트들의 보안프로그램이 " +
        "자동 설치되고 사이트가 열린다. 로그인/인증/실제 업무는 사용자가 직접 진행한다(RPA 아님).")]
    public static async Task<LaunchResponse> LaunchSandbox(
        CatalogClient catalog,
        [Description("열 카탈로그 service id 목록(1개 이상). 여러 개면 한 샌드박스에 병합 설치된다.")] string[] serviceIds,
        CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new LaunchResponse
            {
                Launched = false,
                Error = "launch_sandbox 는 Windows(Windows Sandbox 기능 필요)에서만 동작합니다.",
                Hint = "다른 OS 에서는 generate_wsb 로 .wsb 텍스트를 받아 Windows 사용자에게 전달하세요.",
            };
        }

        var (valid, unknown) = await ResolveIdsAsync(catalog, serviceIds, ct).ConfigureAwait(false);
        if (valid.Count == 0)
            return new LaunchResponse { Launched = false, Error = "유효한 service id 가 없습니다.", UnknownIds = unknown, Hint = "search_services 로 확인하세요." };

        var wsb = BuildWsb(valid);
        var path = Path.Combine(Path.GetTempPath(), $"tablecloth-{Guid.NewGuid():n}.wsb");
        await File.WriteAllTextAsync(path, wsb, ct).ConfigureAwait(false);

        try
        {
            using var proc = Process.Start(new ProcessStartInfo("WindowsSandbox.exe", $"\"{path}\"")
            {
                UseShellExecute = true,
            });
            return new LaunchResponse
            {
                Launched = true,
                SiteIds = valid,
                UnknownIdsIgnored = unknown.Count > 0 ? unknown : null,
                WsbPath = path,
                Note = "샌드박스가 뜨면 준비(보안프로그램 설치)에 수 분 걸릴 수 있습니다. 인증/로그인은 사용자가 직접 진행하세요.",
            };
        }
        catch (Exception ex)
        {
            return new LaunchResponse
            {
                Launched = false,
                Error = $"WindowsSandbox.exe 실행 실패: {ex.Message}",
                Hint = "Windows 11 에서 'Windows Sandbox' 선택적 기능이 설치/활성화돼 있어야 합니다.",
                WsbPath = path,
            };
        }
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

    // 무설치 Express .wsb 템플릿(PARAMETERIZED_WSB_SPEC.md §0.5 "간소화된 기본형").
    // 유일한 주입점은 TABLECLOTH_SITE_IDS(사이트 사전선택). 나머지는 전부 릴리스 고정 URL.
    //
    // 비보간(non-interpolated) raw string 을 쓰고 __SITEIDS__ 만 치환한다 → PowerShell 중괄호를
    // 이스케이프할 필요가 없다. LogonCommand 원문은 릴리스 자산 no-install-spork.wsb 와 동일.
    private const string WsbTemplate =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <Configuration>
          <Networking>Enable</Networking>
          <vGPU>Disable</vGPU>
          <LogonCommand>
            <Command>powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell.exe -WindowStyle Normal -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command','$Host.UI.RawUI.WindowTitle = ''TableCloth Setup''; Write-Host '' Getting TableCloth ready...'' -ForegroundColor Cyan; if (-not (Resolve-DnsName -Name github.com -QuickTimeout -ErrorAction SilentlyContinue)) { Get-NetAdapter | Where-Object Status -eq ''Up'' | Set-DnsClientServerAddress -ServerAddresses 8.8.8.8,1.1.1.1 }; [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072;__SITEIDS__ try { iex ((New-Object Net.WebClient).DownloadString(''https://github.com/yourtablecloth/TableCloth/releases/latest/download/tablecloth-prepare.ps1'')) } catch { Write-Host ('' Failed: '' + $_.Exception.Message) -ForegroundColor Red; $null = Read-Host '' Press Enter to close'' }'"</Command>
          </LogonCommand>
        </Configuration>
        """;

    private static string BuildWsb(IReadOnlyList<string> validIds)
    {
        // valid 는 이미 SafeIdRegex 통과 → 공백 join 안전(작은따옴표/XML 특수문자 없음).
        // .wsb 안에서는 PowerShell 작은따옴표 문자열이라 리터럴 ' 는 '' 로 이스케이프된다.
        var idsStmt = validIds.Count == 0
            ? string.Empty
            : " $env:TABLECLOTH_SITE_IDS = ''" + string.Join(' ', validIds) + "'';";

        return WsbTemplate.Replace("__SITEIDS__", idsStmt);
    }
}
