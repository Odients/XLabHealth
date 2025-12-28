namespace XLabStatusService.Core.Interfaces;

/// <summary>
/// Интерфейс для проверки Google reCAPTCHA v3
/// </summary>
public interface IRecaptchaService
{
    /// <summary>
    /// Проверяет токен reCAPTCHA v3
    /// </summary>
    /// <param name="token">Токен reCAPTCHA, полученный от клиента</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат проверки с score и success статусом</returns>
    Task<RecaptchaVerificationResult> VerifyAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Результат проверки reCAPTCHA
/// </summary>
public class RecaptchaVerificationResult
{
    public bool Success { get; set; }
    public double Score { get; set; }
    public string? Action { get; set; }
    public DateTime ChallengeTimestamp { get; set; }
    public string? Hostname { get; set; }
    public string[]? ErrorCodes { get; set; }
}

