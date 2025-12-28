using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services;

/// <summary>
/// Сервис для проверки Google reCAPTCHA v3
/// </summary>
public class RecaptchaService : IRecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecaptchaService> _logger;
    private readonly string? _secretKey;
    private readonly bool _enabled;
    private readonly double _minScore;

    public RecaptchaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RecaptchaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var recaptchaConfig = _configuration.GetSection("Recaptcha");
        _secretKey = recaptchaConfig["SecretKey"];
        _enabled = recaptchaConfig.GetValue<bool>("Enabled", false);
        _minScore = recaptchaConfig.GetValue<double>("MinScore", 0.5);
    }

    public async Task<RecaptchaVerificationResult> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        // Если reCAPTCHA не настроен или отключен, возвращаем успешный результат
        if (!_enabled || string.IsNullOrWhiteSpace(_secretKey))
        {
            _logger.LogDebug("reCAPTCHA is disabled or not configured. Skipping verification.");
            return new RecaptchaVerificationResult
            {
                Success = true,
                Score = 1.0,
                ChallengeTimestamp = DateTime.UtcNow
            };
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("reCAPTCHA token is empty");
            return new RecaptchaVerificationResult
            {
                Success = false,
                Score = 0.0,
                ErrorCodes = new[] { "missing-input-response" }
            };
        }

        try
        {
            // Отправляем запрос к Google reCAPTCHA API
            // Google reCAPTCHA API ожидает POST запрос с form data
            var formData = new List<KeyValuePair<string, string>>
            {
                new("secret", _secretKey!),
                new("response", token)
            };
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to verify reCAPTCHA token. Status code: {StatusCode}", response.StatusCode);
                return new RecaptchaVerificationResult
                {
                    Success = false,
                    Score = 0.0,
                    ErrorCodes = new[] { "network-error" }
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GoogleRecaptchaResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                _logger.LogError("Failed to deserialize reCAPTCHA response");
                return new RecaptchaVerificationResult
                {
                    Success = false,
                    Score = 0.0,
                    ErrorCodes = new[] { "invalid-response" }
                };
            }

            // Проверяем score (для v3 score от 0.0 до 1.0, где 1.0 - это человек, 0.0 - это бот)
            var isScoreValid = result.Score >= _minScore;

            var verificationResult = new RecaptchaVerificationResult
            {
                Success = result.Success && isScoreValid,
                Score = result.Score,
                Action = result.Action,
                ChallengeTimestamp = DateTime.UtcNow,
                Hostname = result.Hostname,
                ErrorCodes = result.ErrorCodes
            };

            if (!verificationResult.Success)
            {
                _logger.LogWarning(
                    "reCAPTCHA verification failed. Success: {Success}, Score: {Score}, MinScore: {MinScore}, Action: {Action}, Errors: {Errors}",
                    result.Success, result.Score, _minScore, result.Action, 
                    result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none");
            }
            else
            {
                _logger.LogDebug(
                    "reCAPTCHA verification successful. Score: {Score}, Action: {Action}",
                    result.Score, result.Action);
            }

            return verificationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying reCAPTCHA token");
            return new RecaptchaVerificationResult
            {
                Success = false,
                Score = 0.0,
                ErrorCodes = new[] { "exception", ex.Message }
            };
        }
    }

    /// <summary>
    /// Модель ответа от Google reCAPTCHA API
    /// </summary>
    private class GoogleRecaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string? Action { get; set; }
        public DateTime ChallengeTimestamp { get; set; }
        public string? Hostname { get; set; }
        public string[]? ErrorCodes { get; set; }
    }
}

