using System.Text.Json.Serialization;

namespace TableCloth.Mcp.Tools;

// System.Text.Json 소스 생성 컨텍스트. NativeAOT 에서 리플렉션 없이 도구 반환 DTO 를 직렬화한다.
// Program.cs 가 이 컨텍스트를 MCP 도구 직렬화 옵션(McpJsonUtilities.DefaultOptions 기반)에 앞쪽으로 얹는다.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(ServiceResponse))]
[JsonSerializable(typeof(CategoriesResponse))]
[JsonSerializable(typeof(CompanionsResponse))]
[JsonSerializable(typeof(WsbResponse))]
[JsonSerializable(typeof(LaunchResponse))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
