using System.ComponentModel;
using ModelContextProtocol.Server;
using TableCloth.Mcp.Catalog;

namespace TableCloth.Mcp.Tools;

/// <summary>
/// 카탈로그 조회/검색 도구. 사용자의 상황·질의를 "어떤 사이트(서비스)를 열면 되는가"로
/// 좁히는 발견(discovery) 레이어. 실제 로그인/업무 자동화는 하지 않는다.
/// </summary>
// 비정적 클래스여야 WithTools<T> 의 형식 인수로 쓸 수 있다(CS0718). 메서드는 static 유지.
[McpServerToolType]
public sealed class CatalogTools
{
    private static readonly char[] Separators = { ' ', ',', '\t', '\n', '\r' };

    [McpServerTool(Name = "search_services")]
    [Description(
        "한국 인터넷 뱅킹/금융/공공(e-Gov) 사이트 카탈로그를 키워드로 검색한다. " +
        "표시명(한/영)·카테고리·URL·필요 보안 패키지명·검색 키워드를 대상으로 대소문자 무시 OR 매칭한다. " +
        "반환된 service id 를 launch_sandbox/generate_wsb 에 넣어 해당 사이트를 보안프로그램이 갖춰진 " +
        "일회용 샌드박스로 열 수 있다. (예: '홈택스', '연말정산', '국민은행', 'hometax')")]
    public static async Task<SearchResponse> SearchServices(
        CatalogClient catalog,
        [Description("검색어. 공백/쉼표로 여러 토큰 가능(OR 매칭).")] string query,
        [Description("카테고리로 추가 필터(선택). 예: Banking, Financing, CreditCard, Government, Other.")] string? category = null,
        [Description("최대 반환 개수(기본 15).")] int limit = 15,
        CancellationToken ct = default)
    {
        var doc = await catalog.GetAsync(ct: ct).ConfigureAwait(false);
        var tokens = (query ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant()).Distinct().ToArray();

        IEnumerable<CatalogService> pool = doc.Services;
        if (!string.IsNullOrWhiteSpace(category))
            pool = pool.Where(s => string.Equals(s.Category, category, StringComparison.OrdinalIgnoreCase));

        var results = pool
            .Select(s => (svc: s, score: tokens.Length == 0 ? 1 : tokens.Count(t => s.SearchHaystack.Contains(t))))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.svc.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(x => new SearchItemDto
            {
                Id = x.svc.Id,
                DisplayName = x.svc.DisplayName,
                DisplayNameEn = x.svc.DisplayNameEn,
                Category = x.svc.Category,
                Url = x.svc.Url,
                RequiredPackages = x.svc.Packages.Select(p => p.Name).ToArray(),
                CompatWarning = string.IsNullOrWhiteSpace(x.svc.CompatNotes) ? null : x.svc.CompatNotes,
            })
            .ToList();

        return new SearchResponse
        {
            Query = query ?? string.Empty,
            Category = category,
            TotalServices = doc.Services.Count,
            Matched = results.Count,
            Results = results,
            Note = "id 를 launch_sandbox(serviceIds)에 넣어 실행하세요. 로그인/인증/실제 업무는 사용자가 직접 진행합니다.",
        };
    }

    [McpServerTool(Name = "get_service")]
    [Description("특정 service id 의 상세 정보(필요 보안 패키지 전체, Edge 확장, 호환성 주의사항, 아이콘 URL)를 반환한다.")]
    public static async Task<ServiceResponse> GetService(
        CatalogClient catalog,
        [Description("카탈로그 service id (search_services 결과의 id).")] string id,
        CancellationToken ct = default)
    {
        var doc = await catalog.GetAsync(ct: ct).ConfigureAwait(false);
        var s = doc.Services.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        if (s is null)
            return new ServiceResponse { Error = $"service id '{id}' 를 카탈로그에서 찾지 못했습니다.", Hint = "search_services 로 정확한 id 를 확인하세요. (대소문자 구분)" };

        return new ServiceResponse
        {
            Id = s.Id,
            DisplayName = s.DisplayName,
            DisplayNameEn = s.DisplayNameEn,
            Category = s.Category,
            Url = s.Url,
            IconUrl = catalog.IconUrlFor(s.Id),
            Packages = s.Packages.Select(p => new PackageDto(p.Name, p.Url, p.Arguments)).ToList(),
            EdgeExtensions = s.EdgeExtensions.Select(e => new EdgeExtensionDto(e.Name, e.ExtensionId, e.CrxUrl)).ToList(),
            SearchKeywords = s.SearchKeywords,
            CompatNotes = s.CompatNotes,
        };
    }

    [McpServerTool(Name = "list_categories")]
    [Description("카탈로그의 서비스 카테고리 목록과 각 개수를 반환한다. 탐색 범위를 좁힐 때 사용.")]
    public static async Task<CategoriesResponse> ListCategories(CatalogClient catalog, CancellationToken ct = default)
    {
        var doc = await catalog.GetAsync(ct: ct).ConfigureAwait(false);
        var groups = doc.Services
            .GroupBy(s => s.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CategoryCountDto(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        return new CategoriesResponse { TotalServices = doc.Services.Count, Categories = groups };
    }

    [McpServerTool(Name = "list_companions")]
    [Description("보조 프로그램(공용 소프트웨어: PDF 뷰어, 프린터 유틸 등) 목록을 반환한다(선택적 키워드 필터). " +
                 "이들은 사이트별 필수 보안프로그램과 달리 공식 다운로드 페이지 안내용이다.")]
    public static async Task<CompanionsResponse> ListCompanions(
        CatalogClient catalog,
        [Description("이름/URL 부분 일치 필터(선택).")] string? query = null,
        CancellationToken ct = default)
    {
        var doc = await catalog.GetAsync(ct: ct).ConfigureAwait(false);
        var q = (query ?? string.Empty).ToLowerInvariant().Trim();
        var items = doc.Companions
            .Where(c => q.Length == 0 || c.SearchHaystack.Contains(q))
            .Select(c => new CompanionItemDto { Id = c.Id, DisplayName = c.DisplayName, DisplayNameEn = c.DisplayNameEn, Url = c.Url })
            .ToList();
        return new CompanionsResponse { Matched = items.Count, Companions = items };
    }
}
