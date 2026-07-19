using System.Text.Json;

namespace TableCloth.Mcp;

/// <summary>
/// shared/strings.json 과 shared/wsb-template.xml 을 어셈블리에 임베드해 런타임에 읽는다.
/// 이 파일들이 프롬프트/문자열과 .wsb 템플릿의 단일 진실 원천이며 .NET/Node 구현이 동일하게 소비한다(SPEC.md).
/// JsonDocument DOM 만 쓰므로 NativeAOT 안전(리플렉션 직렬화 없음).
///
/// 주의: 도구의 Description/Title/파라미터 설명은 attribute 상수라 여기서 로드할 수 없다.
/// 그 문자열은 CatalogTools/SandboxTools 의 attribute 에 남고, conformance 테스트가 strings.json 과의 일치를 강제한다.
/// </summary>
internal static class SharedResources
{
    // server
    public static readonly string ServerInstructions;
    // .wsb 정본 템플릿(치환점 __SITEIDS__)
    public static readonly string WsbTemplate;
    // sandbox 공용
    public static readonly string SecurityNote;
    // search_services
    public static readonly string SearchResultNote;
    // get_service
    public static readonly string GetServiceErrorNotFound; // {id}
    public static readonly string GetServiceHintNotFound;
    // generate_wsb
    public static readonly string GenerateWsbUsage;
    public static readonly string GenerateWsbErrorNoValidIds;
    public static readonly string GenerateWsbHintNoValidIds;
    // launch_sandbox
    public static readonly string LaunchNoteTemplate; // {runner}
    public static readonly string LaunchErrorNoValidIds;
    public static readonly string LaunchHintNoValidIds;
    public static readonly string RunnerMacNotFoundError;
    public static readonly string RunnerMacNotFoundHint;
    public static readonly string RunnerUnsupportedError;
    public static readonly string RunnerUnsupportedHint;
    public static readonly string RunnerLaunchFailedError; // {runner}, {message}
    public static readonly string RunnerLaunchFailedHint;

    static SharedResources()
    {
        WsbTemplate = ReadResource("wsb-template.xml");

        using var doc = JsonDocument.Parse(ReadResource("strings.json"));
        var root = doc.RootElement;
        var tools = root.GetProperty("tools");

        ServerInstructions = Str(root.GetProperty("server"), "instructions");
        SecurityNote = Str(root.GetProperty("sandbox"), "securityNote");

        var ss = tools.GetProperty("search_services");
        SearchResultNote = Str(ss, "resultNote");

        var gs = tools.GetProperty("get_service");
        GetServiceErrorNotFound = Str(gs, "errorNotFound");
        GetServiceHintNotFound = Str(gs, "hintNotFound");

        var gw = tools.GetProperty("generate_wsb");
        GenerateWsbUsage = Str(gw, "usage");
        GenerateWsbErrorNoValidIds = Str(gw, "errorNoValidIds");
        GenerateWsbHintNoValidIds = Str(gw, "hintNoValidIds");

        var ls = tools.GetProperty("launch_sandbox");
        LaunchNoteTemplate = Str(ls, "noteTemplate");
        LaunchErrorNoValidIds = Str(ls, "errorNoValidIds");
        LaunchHintNoValidIds = Str(ls, "hintNoValidIds");
        RunnerMacNotFoundError = Str(ls, "runnerMacNotFoundError");
        RunnerMacNotFoundHint = Str(ls, "runnerMacNotFoundHint");
        RunnerUnsupportedError = Str(ls, "runnerUnsupportedError");
        RunnerUnsupportedHint = Str(ls, "runnerUnsupportedHint");
        RunnerLaunchFailedError = Str(ls, "runnerLaunchFailedError");
        RunnerLaunchFailedHint = Str(ls, "runnerLaunchFailedHint");
    }

    private static string Str(JsonElement obj, string name) =>
        obj.GetProperty(name).GetString()
            ?? throw new InvalidOperationException($"strings.json: '{name}' is null");

    private static string ReadResource(string logicalName)
    {
        var asm = typeof(SharedResources).Assembly;
        using var stream = asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"embedded resource not found: {logicalName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
