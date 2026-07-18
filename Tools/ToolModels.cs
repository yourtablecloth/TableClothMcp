namespace TableCloth.Mcp.Tools;

// 도구 반환 DTO. NativeAOT(리플렉션 직렬화 불가) 대비로 익명 객체 대신 선언형 record 를 쓰고,
// AppJsonContext(소스젠)로 직렬화한다. 성공/오류를 한 타입에서 표현하려고 오류 필드는 nullable.

public sealed record SearchItemDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? DisplayNameEn { get; init; }
    public required string Category { get; init; }
    public required string Url { get; init; }
    public required IReadOnlyList<string> RequiredPackages { get; init; }
    public string? CompatWarning { get; init; }
}

public sealed record SearchResponse
{
    public required string Query { get; init; }
    public string? Category { get; init; }
    public required int TotalServices { get; init; }
    public required int Matched { get; init; }
    public required IReadOnlyList<SearchItemDto> Results { get; init; }
    public required string Note { get; init; }
}

public sealed record PackageDto(string Name, string Url, string Arguments);

public sealed record EdgeExtensionDto(string Name, string ExtensionId, string CrxUrl);

public sealed record ServiceResponse
{
    public string? Id { get; init; }
    public string? DisplayName { get; init; }
    public string? DisplayNameEn { get; init; }
    public string? Category { get; init; }
    public string? Url { get; init; }
    public string? IconUrl { get; init; }
    public IReadOnlyList<PackageDto>? Packages { get; init; }
    public IReadOnlyList<EdgeExtensionDto>? EdgeExtensions { get; init; }
    public string? SearchKeywords { get; init; }
    public string? CompatNotes { get; init; }
    public string? Error { get; init; }
    public string? Hint { get; init; }
}

public sealed record CategoryCountDto(string Category, int Count);

public sealed record CategoriesResponse
{
    public required int TotalServices { get; init; }
    public required IReadOnlyList<CategoryCountDto> Categories { get; init; }
}

public sealed record CompanionItemDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? DisplayNameEn { get; init; }
    public required string Url { get; init; }
}

public sealed record CompanionsResponse
{
    public required int Matched { get; init; }
    public required IReadOnlyList<CompanionItemDto> Companions { get; init; }
}

public sealed record WsbResponse
{
    public IReadOnlyList<string>? SiteIds { get; init; }
    public IReadOnlyList<string>? UnknownIdsIgnored { get; init; }
    public string? Wsb { get; init; }
    public string? Usage { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string>? UnknownIds { get; init; }
    public string? Hint { get; init; }
}

public sealed record LaunchResponse
{
    public required bool Launched { get; init; }
    public string? Runner { get; init; }
    public IReadOnlyList<string>? SiteIds { get; init; }
    public IReadOnlyList<string>? UnknownIdsIgnored { get; init; }
    public string? WsbPath { get; init; }
    public string? Note { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string>? UnknownIds { get; init; }
    public string? Hint { get; init; }
}
