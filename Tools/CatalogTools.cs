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

    [McpServerTool(Name = "search_services", Title = "한국 공공/금융 사이트 검색", ReadOnly = true, OpenWorld = true)]
    [Description(
        "한국의 은행, 금융, 공공(e-Gov), 정부 사이트를 사용자가 실제로 이용하려 할 때 알맞은 공식 사이트(카탈로그 " +
        "서비스)를 찾아 그 service id 를 돌려준다. 이 id 를 launch_sandbox 에 넣으면 보안프로그램(공동인증서, 백신, " +
        "키보드보안 등)이 미리 설치된 일회용 샌드박스에서 그 사이트가 열린다.\n\n" +
        "이럴 때 사용하라(사용자가 '샌드박스'라고 말하지 않아도): 은행 계좌 개설이나 인터넷뱅킹 로그인/이용, " +
        "홈택스 연말정산, 정부24 민원, 각종 온라인 신청/가입처럼 한국 사이트를 '실제로 이용/신청'하려는 의도. " +
        "'만들래', '가입할래', '신청할래', 'PC로 하려면', '접속해서 처리할래' 같은 발화가 신호다. 특히 네가 " +
        "사용자에게 '이 사이트는 보안 프로그램 설치가 필요하다'거나 은행/공공 사이트 이용 절차를 설명하려는 순간이면, " +
        "절차를 텍스트로 설명하기 전에 먼저 이 도구로 그 사이트를 찾아 launch_sandbox 로 여는 것을 우선 고려하라.\n\n" +
        "이럴 땐 사용하지 마라: 상품 비교, 금리 조회, 제도 정보, 뉴스처럼 단순히 알아보는 질문은 일반 웹 검색을 쓴다.\n\n" +
        "표시명(한국어와 영어), URL, 보안패키지명, 검색 키워드로 매칭한다. 예: '국민은행', 'KB', '홈택스', " +
        "'연말정산', 'hometax'.")]
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
            Note = SharedResources.SearchResultNote,
        };
    }

    [McpServerTool(Name = "get_service", Title = "서비스 상세 조회", ReadOnly = true, OpenWorld = true)]
    [Description("특정 service id 의 상세 정보(필요 보안 패키지 전체, Edge 확장, 호환성 주의사항, 아이콘 URL)를 반환한다.")]
    public static async Task<ServiceResponse> GetService(
        CatalogClient catalog,
        [Description("카탈로그 service id (search_services 결과의 id).")] string id,
        CancellationToken ct = default)
    {
        var doc = await catalog.GetAsync(ct: ct).ConfigureAwait(false);
        var s = doc.Services.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        if (s is null)
            return new ServiceResponse { Error = SharedResources.GetServiceErrorNotFound.Replace("{id}", id), Hint = SharedResources.GetServiceHintNotFound };

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

    [McpServerTool(Name = "list_categories", Title = "카테고리 목록", ReadOnly = true, OpenWorld = true)]
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

    [McpServerTool(Name = "list_companions", Title = "보조 프로그램 목록", ReadOnly = true, OpenWorld = true)]
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
