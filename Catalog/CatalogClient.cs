using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace TableCloth.Mcp.Catalog;

/// <summary>
/// 공개 카탈로그(yourtablecloth.app)를 받아 파싱하고 메모리에 캐시한다.
/// TableCloth 소스에 의존하지 않고 XML 을 직접 파싱한다.
/// </summary>
public sealed class CatalogClient(HttpClient http, ILogger<CatalogClient> logger)
{
    public const string CatalogUrl = "https://yourtablecloth.app/TableClothCatalog/Catalog.xml";
    public const string IconBaseUrl = "https://yourtablecloth.app/TableClothCatalog/images";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CatalogDocument? _cached;
    private DateTimeOffset _fetchedAt;

    public string IconUrlFor(string serviceId) => $"{IconBaseUrl}/{serviceId}.png";

    public async Task<CatalogDocument> GetAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        // TTL 내이고 강제갱신이 아니면 캐시 반환. (Date/시간은 서버 프로세스 clock 사용)
        if (!forceRefresh && _cached is not null && DateTimeOffset.UtcNow - _fetchedAt < CacheTtl)
            return _cached;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _cached is not null && DateTimeOffset.UtcNow - _fetchedAt < CacheTtl)
                return _cached;

            logger.LogInformation("Fetching catalog from {Url}", CatalogUrl);
            var xml = await http.GetStringAsync(CatalogUrl, ct).ConfigureAwait(false);
            _cached = Parse(xml);
            _fetchedAt = DateTimeOffset.UtcNow;
            logger.LogInformation("Catalog loaded: {Services} services, {Companions} companions",
                _cached.Services.Count, _cached.Companions.Count);
            return _cached;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal static CatalogDocument Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root
            ?? throw new InvalidDataException("Catalog.xml has no root element.");

        var companions = root.Descendants("Companion")
            .Select(c => new CatalogCompanion(
                Id: (Attr(c, "Id") ?? string.Empty).Trim(),
                DisplayName: Attr(c, "DisplayName") ?? string.Empty,
                DisplayNameEn: Attr(c, "en-US-DisplayName"),
                Url: Attr(c, "Url") ?? string.Empty,
                Arguments: Attr(c, "Arguments")))
            .Where(c => !string.IsNullOrWhiteSpace(c.DisplayName))
            .ToList();

        var services = (root.Element("InternetServices")?.Elements("Service") ?? Enumerable.Empty<XElement>())
            .Select(ParseService)
            .Where(s => !string.IsNullOrWhiteSpace(s.Id))
            .ToList();

        return new CatalogDocument(services, companions);
    }

    private static CatalogService ParseService(XElement s)
    {
        var packages = s.Descendants("Package")
            .Select(p => new CatalogPackage(
                Attr(p, "Name") ?? string.Empty,
                Attr(p, "Url") ?? string.Empty,
                Attr(p, "Arguments") ?? string.Empty))
            .ToList();

        var extensions = s.Descendants("EdgeExtension")
            .Select(e => new CatalogEdgeExtension(
                Attr(e, "Name") ?? string.Empty,
                Attr(e, "ExtensionId") ?? string.Empty,
                Attr(e, "CrxUrl") ?? string.Empty))
            .ToList();

        return new CatalogService(
            Id: (Attr(s, "Id") ?? string.Empty).Trim(),
            DisplayName: Attr(s, "DisplayName") ?? string.Empty,
            DisplayNameEn: Attr(s, "en-US-DisplayName"),
            Category: Attr(s, "Category") ?? "Other",
            Url: Attr(s, "Url") ?? string.Empty,
            Packages: packages,
            EdgeExtensions: extensions,
            SearchKeywords: s.Element("SearchKeywords")?.Value,
            CompatNotes: s.Element("CompatNotes")?.Value);
    }

    private static string? Attr(XElement e, string name) => e.Attribute(name)?.Value;
}
