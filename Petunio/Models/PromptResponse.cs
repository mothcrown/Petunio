using Newtonsoft.Json;

namespace Petunio.Models;

public class PromptResponse
{
    [JsonProperty("prompt_id")]
    public string PromptId { get; set; }
    public int Number { get; set; }
}