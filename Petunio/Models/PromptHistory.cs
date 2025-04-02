using System.Text.Json.Serialization;

namespace Petunio.Models;

public class PromptHistory
{
    [JsonPropertyName("outputs")]
    public Outputs? Outputs { get; set; }
}

public class Outputs
{
    [JsonPropertyName("9")]
    public Container? Container { get; set; }
}

public class Container
{
    [JsonPropertyName("images")]
    public List<PromptImage>? Images { get; set; }
}

public class PromptImage
{
    [JsonPropertyName("filename")]
    public string? FileName { get; set; }
}