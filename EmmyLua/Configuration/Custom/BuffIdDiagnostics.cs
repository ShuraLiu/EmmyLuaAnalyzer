using System.Text.Json.Serialization;

namespace EmmyLua.Configuration.Custom;

public class BuffIdConfigFileInfo
{
    [JsonPropertyName("path")]
    public string Path {get;set;} = string.Empty;

    [JsonPropertyName("sheet")]
    public string Sheet {get;set;} = string.Empty;

    [JsonPropertyName("column")]
    public string Column {get;set;} = string.Empty;
}

public class BuffIdFunctionInfo
{
    [JsonPropertyName("name")]
    public string FuncName { get; set; } = string.Empty;

    [JsonPropertyName("argPos")]
    public List<int> ArgPos { get; set; } = [];
}

public class BuffIdDiagnostics
{
    [JsonPropertyName("configFile")]
    public BuffIdConfigFileInfo BuffIdConfigFileInfo { get; set; } = new();

    [JsonPropertyName("designerScriptPaths")]
    public List<string> DesignerScriptPaths { get; set; } = [];

    [JsonPropertyName("functions")]
    public List<BuffIdFunctionInfo> Functions { get; set; } = [];
}
