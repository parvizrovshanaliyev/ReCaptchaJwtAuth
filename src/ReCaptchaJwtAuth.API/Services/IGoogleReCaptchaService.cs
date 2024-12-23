using ErrorOr;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ReCaptchaJwtAuth.API.Errors;
using ReCaptchaJwtAuth.API.Models;
using ReCaptchaJwtAuth.API.Settings;

namespace ReCaptchaJwtAuth.API.Services;

public interface IGoogleReCaptchaService
{
    Task<ErrorOr<bool>> VerifyReCaptchaAsync(string token, string action, CancellationToken cancellationToken = default);
}

public class GoogleReCaptchaService : IGoogleReCaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleReCaptchaV3Settings _settings;

    public GoogleReCaptchaService(HttpClient httpClient, IOptions<GoogleReCaptchaV3Settings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<ErrorOr<bool>> VerifyReCaptchaAsync(string token, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return ErrorTypes.InvalidReCaptcha;

        const string verificationUrl = "https://www.google.com/recaptcha/api/siteverify";

        var response = await _httpClient.PostAsync(verificationUrl, new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("secret", _settings.SecretKey),
                new KeyValuePair<string, string>("response", token)
            }), cancellationToken);

        if (!response.IsSuccessStatusCode)
            return ErrorTypes.InvalidReCaptcha;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var verificationResponse = JsonConvert.DeserializeObject<GoogleReCaptchaResponse>(content);

        return verificationResponse?.Success == true &&
               verificationResponse.Action == action &&
               verificationResponse.Score >= _settings.Threshold
            ? true
            : ErrorTypes.InvalidReCaptcha;
    }
}
