using Newtonsoft.Json;

namespace ReCaptchaJwtAuth.API.Models;

public class GoogleReCaptchaResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("score")]
    public decimal Score { get; set; }

    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    [JsonProperty("error-codes")]
    public List<string> ErrorCodes { get; set; } = new List<string>();
}