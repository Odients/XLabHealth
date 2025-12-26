using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Enums;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Infrastructure.Services.HealthCheckProviders;

/// <summary>
/// Провайдер для проверки Apache Kafka
/// </summary>
public class KafkaHealthCheckProvider : IHealthCheckProvider
{
    private readonly ILogger<KafkaHealthCheckProvider> _logger;

    public KafkaHealthCheckProvider(ILogger<KafkaHealthCheckProvider> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(Service service, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new HealthCheckResult
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            CheckedAt = startTime,
            Status = HealthStatus.Unknown
        };

        IAdminClient? adminClient = null;

        try
        {
            if (service.Configuration == null || string.IsNullOrEmpty(service.Configuration.Parameters))
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Kafka configuration is not set";
                return result;
            }

            var config = JsonSerializer.Deserialize<KafkaConfig>(service.Configuration.Parameters);
            if (config == null)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Invalid Kafka configuration";
                return result;
            }

            // Формируем конфигурацию для AdminClient
            var adminConfig = new AdminClientConfig();

            if (!string.IsNullOrEmpty(config.BootstrapServers))
            {
                adminConfig.BootstrapServers = config.BootstrapServers;
            }
            else if (!string.IsNullOrEmpty(config.Host))
            {
                var port = config.Port > 0 ? config.Port : 9092;
                adminConfig.BootstrapServers = $"{config.Host}:{port}";
            }
            else
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Kafka BootstrapServers or Host must be specified";
                return result;
            }

            // Настройка таймаутов
            adminConfig.SocketTimeoutMs = service.Timeout;
            adminConfig.SocketKeepaliveEnable = true;

            // Настройка безопасности (SASL/SSL)
            if (!string.IsNullOrEmpty(config.SecurityProtocol))
            {
                if (Enum.TryParse<SecurityProtocol>(config.SecurityProtocol, true, out var securityProtocol))
                {
                    adminConfig.SecurityProtocol = securityProtocol;
                }
            }

            if (!string.IsNullOrEmpty(config.SaslMechanism))
            {
                if (Enum.TryParse<SaslMechanism>(config.SaslMechanism, true, out var saslMechanism))
                {
                    adminConfig.SaslMechanism = saslMechanism;
                }
            }

            if (!string.IsNullOrEmpty(config.SaslUsername))
            {
                adminConfig.SaslUsername = config.SaslUsername;
            }

            if (!string.IsNullOrEmpty(config.SaslPassword))
            {
                adminConfig.SaslPassword = config.SaslPassword;
            }

            if (!string.IsNullOrEmpty(config.SslCaLocation))
            {
                adminConfig.SslCaLocation = config.SslCaLocation;
            }

            if (!string.IsNullOrEmpty(config.SslCertificateLocation))
            {
                adminConfig.SslCertificateLocation = config.SslCertificateLocation;
            }

            if (!string.IsNullOrEmpty(config.SslKeyLocation))
            {
                adminConfig.SslKeyLocation = config.SslKeyLocation;
            }

            if (!string.IsNullOrEmpty(config.SslKeyPassword))
            {
                adminConfig.SslKeyPassword = config.SslKeyPassword;
            }

            // Создаем AdminClient
            adminClient = new AdminClientBuilder(adminConfig).Build();

            // Получаем метаданные кластера для проверки подключения
            var metadata = adminClient.GetMetadata(TimeSpan.FromMilliseconds(service.Timeout));

            var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            result.ResponseTime = responseTime;

            // Проверяем, что кластер доступен
            if (metadata.Brokers.Count == 0)
            {
                result.Status = HealthStatus.Unhealthy;
                result.Message = "Kafka cluster is not available (no brokers found)";
                return result;
            }

            result.Status = HealthStatus.Healthy;
            result.Message = $"Kafka cluster is available ({metadata.Brokers.Count} broker(s))";

            // Собираем дополнительные метрики
            if (config.CheckTopics || config.CheckBrokers)
            {
                try
                {
                    var metrics = new Dictionary<string, object>();

                    if (config.CheckBrokers)
                    {
                        metrics["Brokers"] = metadata.Brokers.Select(b => new
                        {
                            BrokerId = b.BrokerId,
                            Host = b.Host,
                            Port = b.Port
                        }).ToList();
                    }

                    if (config.CheckTopics)
                    {
                        metrics["Topics"] = metadata.Topics.Select(t => new
                        {
                            TopicName = t.Topic,
                            Partitions = t.Partitions.Count
                        }).ToList();
                    }

                    if (metrics.Count > 0)
                    {
                        result.Metadata = JsonSerializer.Serialize(metrics);
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибку сбора метрик, но не прерываем успешную проверку
                    _logger.LogWarning(ex, "Failed to collect extended metrics for Kafka service {ServiceId}", service.Id);
                }
            }
        }
        catch (OperationCanceledException)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = "Kafka health check was cancelled";
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning("Kafka health check cancelled for service {ServiceId}", service.Id);
        }
        catch (KafkaException ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"Kafka connection failed: {ex.Message}";
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Kafka connection error for service {ServiceId}", service.Id);
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = ex.Message;
            result.Exception = ex.ToString();
            result.ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Error checking Kafka health for service {ServiceId}", service.Id);
        }
        finally
        {
            // Освобождаем ресурсы
            if (adminClient != null)
            {
                try
                {
                    adminClient.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Kafka AdminClient for service {ServiceId}", service.Id);
                }
            }
        }

        return result;
    }

    private class KafkaConfig
    {
        public string? BootstrapServers { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; } = 9092;
        public string? SecurityProtocol { get; set; } // Plaintext, Ssl, SaslPlaintext, SaslSsl
        public string? SaslMechanism { get; set; } // Plain, ScramSha256, ScramSha512, Gssapi, OAuthBearer
        public string? SaslUsername { get; set; }
        public string? SaslPassword { get; set; }
        public string? SslCaLocation { get; set; }
        public string? SslCertificateLocation { get; set; }
        public string? SslKeyLocation { get; set; }
        public string? SslKeyPassword { get; set; }
        public bool CheckTopics { get; set; }
        public bool CheckBrokers { get; set; }
    }
}

