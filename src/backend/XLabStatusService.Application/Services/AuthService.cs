using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Application.Services;

/// <summary>
/// Сервис для аутентификации и авторизации
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILoginAttemptRepository _loginAttemptRepository;
    private readonly IBlockedIpRepository _blockedIpRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginAttemptRepository loginAttemptRepository,
        IBlockedIpRepository blockedIpRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _loginAttemptRepository = loginAttemptRepository;
        _blockedIpRepository = blockedIpRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto, string? ipAddress, CancellationToken cancellationToken = default)
    {
        // Получаем настройки защиты от брутфорса
        var bruteForceConfig = _configuration.GetSection("BruteForceProtection");
        var maxFailedAttempts = bruteForceConfig.GetValue<int>("MaxFailedAttempts", 5);
        var lockoutWindowMinutes = bruteForceConfig.GetValue<int>("LockoutWindowMinutes", 15);
        var autoBlockIp = bruteForceConfig.GetValue<bool>("AutoBlockIp", true);
        var autoBlockAfterAttempts = bruteForceConfig.GetValue<int>("AutoBlockIpAfterAttempts", 10);

        var lockoutWindowStart = DateTime.UtcNow.AddMinutes(-lockoutWindowMinutes);

        // Проверяем, не заблокирован ли IP-адрес
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var isBlocked = await _blockedIpRepository.IsBlockedAsync(ipAddress, cancellationToken);
            if (isBlocked)
            {
                _logger.LogWarning("Login attempt from blocked IP address: {IpAddress}", ipAddress);
                await LogLoginAttemptAsync(ipAddress, loginDto.Username, false, "IP address is blocked", cancellationToken);
                throw new UnauthorizedAccessException("Your IP address has been blocked.");
            }

            // Проверяем количество неудачных попыток с IP
            var failedAttemptsFromIp = await _loginAttemptRepository.GetFailedAttemptsCountByIpAsync(
                ipAddress, lockoutWindowStart, cancellationToken);

            if (failedAttemptsFromIp >= maxFailedAttempts)
            {
                _logger.LogWarning(
                    "Login attempt blocked due to too many failed attempts from IP: {IpAddress} (Attempts: {Attempts})",
                    ipAddress, failedAttemptsFromIp);

                await LogLoginAttemptAsync(ipAddress, loginDto.Username, false, 
                    "Too many failed attempts from IP", cancellationToken);

                throw new UnauthorizedAccessException(
                    $"Too many failed login attempts. Please try again after {lockoutWindowMinutes} minutes.");
            }

            // Автоматическая блокировка IP при превышении лимита
            if (autoBlockIp && failedAttemptsFromIp >= autoBlockAfterAttempts)
            {
                var isAlreadyBlocked = await _blockedIpRepository.IsBlockedAsync(ipAddress, cancellationToken);
                if (!isAlreadyBlocked)
                {
                    await _blockedIpRepository.AddAsync(ipAddress, cancellationToken);
                    _logger.LogWarning("IP address automatically blocked due to brute force attempts: {IpAddress}", ipAddress);
                }
            }
        }

        // Проверяем количество неудачных попыток для пользователя
        var failedAttemptsForUser = await _loginAttemptRepository.GetFailedAttemptsCountByUsernameAsync(
            loginDto.Username, lockoutWindowStart, cancellationToken);

        if (failedAttemptsForUser >= maxFailedAttempts)
        {
            _logger.LogWarning(
                "Login attempt blocked due to too many failed attempts for username: {Username} (Attempts: {Attempts})",
                loginDto.Username, failedAttemptsForUser);

            await LogLoginAttemptAsync(ipAddress, loginDto.Username, false, 
                "Too many failed attempts for username", cancellationToken);

            throw new UnauthorizedAccessException(
                $"Too many failed login attempts. Please try again after {lockoutWindowMinutes} minutes.");
        }

        // Проверяем пользователя
        var user = await _userRepository.GetByUsernameAsync(loginDto.Username, cancellationToken);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Failed login attempt: User not found or inactive - Username: {Username}, IP: {IpAddress}",
                loginDto.Username, ipAddress);

            await LogLoginAttemptAsync(ipAddress, loginDto.Username, false, 
                user == null ? "User not found" : "User inactive", cancellationToken);

            return null;
        }

        // Проверка пароля
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Failed login attempt: Invalid password - Username: {Username}, IP: {IpAddress}",
                loginDto.Username, ipAddress);

            await LogLoginAttemptAsync(ipAddress, loginDto.Username, false, 
                "Invalid password", cancellationToken);

            return null;
        }

        // Успешный вход
        _logger.LogInformation("Successful login - Username: {Username}, IP: {IpAddress}", 
            loginDto.Username, ipAddress);

        await LogLoginAttemptAsync(ipAddress, loginDto.Username, true, null, cancellationToken);

        // Обновляем время последнего входа
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Генерируем токены
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 30) * 60,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    private async Task LogLoginAttemptAsync(string? ipAddress, string? username, bool isSuccessful, 
        string? failureReason, CancellationToken cancellationToken)
    {
        try
        {
            var attempt = new LoginAttempt
            {
                IpAddress = ipAddress,
                Username = username,
                IsSuccessful = isSuccessful,
                AttemptedAt = DateTime.UtcNow,
                FailureReason = failureReason
            };

            await _loginAttemptRepository.CreateAsync(attempt, cancellationToken);
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не прерываем процесс входа
            _logger.LogError(ex, "Failed to log login attempt");
        }
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        // Генерируем новые токены
        var accessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, cancellationToken);

        // Отзываем старый refresh token
        token.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 30) * 60,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        await _refreshTokenRepository.RevokeTokenAsync(refreshToken, cancellationToken);
        return true;
    }

    private string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var key = Encoding.UTF8.GetBytes(secretKey);
        var expiresIn = jwtSettings.GetValue<int>("AccessTokenExpirationMinutes", 30);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresIn),
            Issuer = jwtSettings["Issuer"] ?? "XLabStatusService",
            Audience = jwtSettings["Audience"] ?? "XLabStatusService",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);

        var expiresIn = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expiresIn),
            IsRevoked = false
        };

        return await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);
    }
}

