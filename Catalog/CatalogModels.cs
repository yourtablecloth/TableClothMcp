namespace TableCloth.Mcp.Catalog;

// 공개 Catalog.xml (https://yourtablecloth.app/TableClothCatalog/Catalog.xml) 의 부분 모델.
// 스키마: <TableClothCatalog> → <Companions>/<Companion>, <InternetServices>/<Service>
//         <Service> → <Packages>/<Package>, <EdgeExtension>, <SearchKeywords>, <CompatNotes>

public sealed record CatalogPackage(string Name, string Url, string Arguments);

public sealed record CatalogEdgeExtension(string Name, string ExtensionId, string CrxUrl);

public sealed record CatalogService(
    string Id,
    string DisplayName,
    string? DisplayNameEn,
    string Category,
    string Url,
    IReadOnlyList<CatalogPackage> Packages,
    IReadOnlyList<CatalogEdgeExtension> EdgeExtensions,
    string? SearchKeywords,
    string? CompatNotes)
{
    // 키워드 필터용 haystack (소문자). Id·표시명(한/영)·카테고리·URL·패키지명·검색키워드 포함.
    public string SearchHaystack { get; } = string.Join(' ', new[]
        {
            Id, DisplayName, DisplayNameEn ?? string.Empty, Category, Url,
            string.Join(' ', Packages.Select(p => p.Name)),
            SearchKeywords ?? string.Empty,
        }).ToLowerInvariant();
}

public sealed record CatalogCompanion(
    string Id,
    string DisplayName,
    string? DisplayNameEn,
    string Url,
    string? Arguments)
{
    public string SearchHaystack { get; } =
        $"{Id} {DisplayName} {DisplayNameEn} {Url}".ToLowerInvariant();
}

public sealed record CatalogDocument(
    IReadOnlyList<CatalogService> Services,
    IReadOnlyList<CatalogCompanion> Companions);
