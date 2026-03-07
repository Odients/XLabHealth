import { useState, useEffect } from 'react';
import { toast } from 'react-toastify';
import { ServiceType, ServiceCreateDto, ServiceUpdateDto, ServiceDto, ServiceConfigurationDto } from '@/types';
import './ServiceForm.css';

interface ServiceFormProps {
  service?: ServiceDto;
  onSubmit: (data: ServiceCreateDto | ServiceUpdateDto) => void;
  onCancel: () => void;
  isLoading?: boolean;
}

interface HttpConfig {
  expectedStatusCode?: number;
  headers?: Record<string, string>;
  parseModules?: boolean;
  criticalModules?: string[];
}

interface DatabaseConfig {
  connectionString?: string;
  testQuery?: string;
  checkDatabaseSize?: boolean;
  checkActiveConnections?: boolean;
  checkPerformance?: boolean;
  checkTableInfo?: boolean;
  checkBackupInfo?: boolean;
  checkServerInfo?: boolean;
  checkResourceUsage?: boolean;
}

interface RedisConfig {
  redisConnection?: string;
  useSsl?: boolean;
  checkMemoryUsage?: boolean;
  checkConnectedClients?: boolean;
}

interface WindowsServiceConfig {
  serviceName?: string;
  machineName?: string;
  checkStartType?: boolean;
  expectedStartType?: string;
}

const ServiceForm = ({ service, onSubmit, onCancel, isLoading }: ServiceFormProps) => {
  // Маппинг строковых значений типа сервиса в enum
  const parseServiceType = (type: string | number | null | undefined): ServiceType => {
    if (type === null || type === undefined) {
      return ServiceType.Custom;
    }
    
    // Если это уже число, проверяем валидность
    if (typeof type === 'number') {
      if (type >= 0 && type <= 6) {
        return type as ServiceType;
      }
      return ServiceType.Custom;
    }
    
    // Преобразуем строку в enum
    // Сначала проверяем точное совпадение (для camelCase и PascalCase)
    const exactMatch: Record<string, ServiceType> = {
      'windowsService': ServiceType.WindowsService,
      'WindowsService': ServiceType.WindowsService,
      'http': ServiceType.Http,
      'Http': ServiceType.Http,
      'https': ServiceType.Http,
      'Https': ServiceType.Http,
      'tcp': ServiceType.Tcp,
      'Tcp': ServiceType.Tcp,
      'database': ServiceType.Database,
      'Database': ServiceType.Database,
      'redis': ServiceType.Redis,
      'Redis': ServiceType.Redis,
      'kafka': ServiceType.Kafka,
      'Kafka': ServiceType.Kafka,
      'custom': ServiceType.Custom,
      'Custom': ServiceType.Custom,
    };
    
    // Проверяем точное совпадение
    if (exactMatch[type]) {
      return exactMatch[type];
    }
    
    // Затем проверяем lowercase версию
    const typeLower = type.toLowerCase();
    const typeMap: Record<string, ServiceType> = {
      'http': ServiceType.Http,
      'https': ServiceType.Http,
      'tcp': ServiceType.Tcp,
      'database': ServiceType.Database,
      'redis': ServiceType.Redis,
      'windowsservice': ServiceType.WindowsService, // после toLowerCase()
      'windows-service': ServiceType.WindowsService, // на случай kebab-case
      'kafka': ServiceType.Kafka,
      'custom': ServiceType.Custom,
    };
    
    return typeMap[typeLower] ?? ServiceType.Custom;
  };

  const [formData, setFormData] = useState<ServiceCreateDto>({
    name: '',
    description: '',
    url: '',
    type: ServiceType.Http,
    checkInterval: 60,
    timeout: 5000,
    retryCount: 2,
    isEnabled: true,
    isPublic: false,
    isCritical: false,
  });

  const [httpConfig, setHttpConfig] = useState<HttpConfig>({
    expectedStatusCode: 200,
    headers: {},
    parseModules: false,
    criticalModules: [],
  });

  const [databaseConfig, setDatabaseConfig] = useState<DatabaseConfig>({
    connectionString: '',
    testQuery: 'SELECT 1',
    checkDatabaseSize: false,
    checkActiveConnections: false,
  });

  const [redisConfig, setRedisConfig] = useState<RedisConfig>({
    redisConnection: '',
    useSsl: false,
    checkMemoryUsage: false,
    checkConnectedClients: false,
  });

  const [windowsServiceConfig, setWindowsServiceConfig] = useState<WindowsServiceConfig>({
    serviceName: '',
    machineName: '.',
    checkStartType: false,
    expectedStartType: 'Automatic',
  });

  const [headersJson, setHeadersJson] = useState('{}');
  const [criticalModulesStr, setCriticalModulesStr] = useState('');

  useEffect(() => {
    if (service) {
      // Преобразуем тип сервиса в enum
      const serviceType = parseServiceType(service.type);
      
      setFormData({
        name: service.name,
        description: service.description,
        url: service.url,
        type: serviceType,
        checkInterval: service.checkInterval,
        timeout: service.timeout,
        retryCount: service.retryCount,
        isEnabled: service.isEnabled,
        isPublic: service.isPublic,
        isCritical: service.isCritical ?? false,
      });

      // Load configuration from service if available
      if (service.configuration) {
        const config = service.configuration;
        
        // Parse configuration based on service type (используем преобразованный тип)
        if (serviceType === ServiceType.Http) {
          try {
            const headers = config.headers ? JSON.parse(config.headers) : {};
            const params = config.parameters ? JSON.parse(config.parameters) : {};
            
            setHttpConfig({
              expectedStatusCode: config.expectedStatusCode || 200,
              headers: headers,
              parseModules: params.parseModules || false,
              criticalModules: params.criticalModules || [],
            });
            setHeadersJson(JSON.stringify(headers, null, 2));
            setCriticalModulesStr((params.criticalModules || []).join(', '));
          } catch (error) {
            console.error('Error parsing HTTP config:', error);
            setHttpConfig({
              expectedStatusCode: config.expectedStatusCode || 200,
              headers: {},
              parseModules: false,
              criticalModules: [],
            });
            setHeadersJson('{}');
            setCriticalModulesStr('');
          }
        } else if (serviceType === ServiceType.Database) {
          try {
            const params = config.parameters ? JSON.parse(config.parameters) : {};
            setDatabaseConfig({
              connectionString: params.connectionString || '',
              testQuery: params.testQuery || 'SELECT 1',
              checkDatabaseSize: params.checkDatabaseSize || false,
              checkActiveConnections: params.checkActiveConnections || false,
              checkPerformance: params.checkPerformance || false,
              checkTableInfo: params.checkTableInfo || false,
              checkBackupInfo: params.checkBackupInfo || false,
              checkServerInfo: params.checkServerInfo || false,
              checkResourceUsage: params.checkResourceUsage || false,
            });
          } catch (error) {
            console.error('Error parsing Database config:', error);
            setDatabaseConfig({
              connectionString: '',
              testQuery: 'SELECT 1',
              checkDatabaseSize: false,
              checkActiveConnections: false,
            });
          }
        } else if (serviceType === ServiceType.Redis) {
          try {
            const params = config.parameters ? JSON.parse(config.parameters) : {};
            setRedisConfig({
              redisConnection: params.redisConnection || '',
              useSsl: params.useSsl || false,
              checkMemoryUsage: params.checkMemoryUsage || false,
              checkConnectedClients: params.checkConnectedClients || false,
            });
          } catch (error) {
            console.error('Error parsing Redis config:', error);
            setRedisConfig({
              redisConnection: '',
              useSsl: false,
              checkMemoryUsage: false,
              checkConnectedClients: false,
            });
          }
        } else if (serviceType === ServiceType.WindowsService) {
          try {
            const params = config.parameters ? JSON.parse(config.parameters) : {};
            setWindowsServiceConfig({
              serviceName: params.serviceName || '',
              machineName: params.machineName || '.',
              checkStartType: params.checkStartType || false,
              expectedStartType: params.expectedStartType || 'Automatic',
            });
          } catch (error) {
            console.error('Error parsing Windows Service config:', error);
            setWindowsServiceConfig({
              serviceName: '',
              machineName: '.',
              checkStartType: false,
              expectedStartType: 'Automatic',
            });
          }
        } else {
          // Reset to defaults for other types
          setHttpConfig({
            expectedStatusCode: 200,
            headers: {},
            parseModules: false,
            criticalModules: [],
          });
          setDatabaseConfig({
            connectionString: '',
            testQuery: 'SELECT 1',
            checkDatabaseSize: false,
            checkActiveConnections: false,
          });
          setRedisConfig({
            redisConnection: '',
            useSsl: false,
            checkMemoryUsage: false,
            checkConnectedClients: false,
          });
          setWindowsServiceConfig({
            serviceName: '',
            machineName: '.',
            checkStartType: false,
            expectedStartType: 'Automatic',
          });
          setHeadersJson('{}');
          setCriticalModulesStr('');
        }
      } else {
        // No configuration, reset to defaults
        setHttpConfig({
          expectedStatusCode: 200,
          headers: {},
          parseModules: false,
          criticalModules: [],
        });
        setDatabaseConfig({
          connectionString: '',
          testQuery: 'SELECT 1',
          checkDatabaseSize: false,
          checkActiveConnections: false,
        });
        setRedisConfig({
          redisConnection: '',
          useSsl: false,
          checkMemoryUsage: false,
          checkConnectedClients: false,
        });
        setWindowsServiceConfig({
          serviceName: '',
          machineName: '.',
          checkStartType: false,
          expectedStartType: 'Automatic',
        });
        setHeadersJson('{}');
        setCriticalModulesStr('');
      }
    } else {
      // Reset form when creating new service
      setFormData({
        name: '',
        description: '',
        url: '',
        type: ServiceType.Http,
        checkInterval: 60,
        timeout: 5000,
        retryCount: 2,
        isEnabled: true,
        isPublic: false,
        isCritical: false,
      });
      setHttpConfig({
        expectedStatusCode: 200,
        headers: {},
        parseModules: false,
        criticalModules: [],
      });
      setDatabaseConfig({
        connectionString: '',
        testQuery: 'SELECT 1',
        checkDatabaseSize: false,
        checkActiveConnections: false,
      });
      setRedisConfig({
        redisConnection: '',
        useSsl: false,
        checkMemoryUsage: false,
        checkConnectedClients: false,
      });
      setWindowsServiceConfig({
        serviceName: '',
        machineName: '.',
        checkStartType: false,
        expectedStartType: 'Automatic',
      });
      setHeadersJson('{}');
      setCriticalModulesStr('');
    }
  }, [service]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // Validate required fields based on service type
    if (formData.type === ServiceType.Database && !databaseConfig.connectionString?.trim()) {
      toast.error('Необходимо указать строку подключения к базе данных');
      return;
    }

    if (formData.type === ServiceType.Redis && !redisConfig.redisConnection?.trim()) {
      toast.error('Необходимо указать подключение к Redis (host:port)');
      return;
    }

    if (formData.type === ServiceType.WindowsService && !windowsServiceConfig.serviceName?.trim()) {
      toast.error('Необходимо указать имя Windows службы');
      return;
    }

    // Map ServiceType enum to string name
    const getCheckTypeName = (type: ServiceType): string => {
      switch (type) {
        case ServiceType.Http:
          return 'Http';
        case ServiceType.Tcp:
          return 'Tcp';
        case ServiceType.Database:
          return 'SqlServer'; // Backend uses SqlServer for Database type
        case ServiceType.Redis:
          return 'Redis';
        case ServiceType.WindowsService:
          return 'WindowsService';
        case ServiceType.Kafka:
          return 'Kafka';
        case ServiceType.Custom:
          return 'Custom';
        default:
          return 'Http';
      }
    };

    const config: ServiceConfigurationDto = {
      checkType: getCheckTypeName(formData.type),
    };

    // Build configuration based on service type
    switch (formData.type) {
      case ServiceType.Http:
        try {
          const headers = headersJson.trim() ? JSON.parse(headersJson) : {};
          const criticalModules = criticalModulesStr
            .split(',')
            .map((m) => m.trim())
            .filter((m) => m.length > 0);

          config.expectedStatusCode = httpConfig.expectedStatusCode;
          config.headers = JSON.stringify(headers);
          
          // Store additional HTTP config in Parameters
          const httpParams: any = {};
          if (httpConfig.parseModules) httpParams.parseModules = true;
          if (criticalModules.length > 0) httpParams.criticalModules = criticalModules;
          if (Object.keys(httpParams).length > 0) {
            config.parameters = JSON.stringify(httpParams);
          }
        } catch (error) {
          toast.error('Ошибка в JSON заголовков. Проверьте формат.');
          return;
        }
        break;

      case ServiceType.Database:
        const dbParams: any = {
          connectionString: databaseConfig.connectionString,
          testQuery: databaseConfig.testQuery || 'SELECT 1',
        };
        if (databaseConfig.checkDatabaseSize) dbParams.checkDatabaseSize = true;
        if (databaseConfig.checkActiveConnections) dbParams.checkActiveConnections = true;
        if (databaseConfig.checkPerformance) dbParams.checkPerformance = true;
        if (databaseConfig.checkTableInfo) dbParams.checkTableInfo = true;
        if (databaseConfig.checkBackupInfo) dbParams.checkBackupInfo = true;
        if (databaseConfig.checkServerInfo) dbParams.checkServerInfo = true;
        if (databaseConfig.checkResourceUsage) dbParams.checkResourceUsage = true;
        config.parameters = JSON.stringify(dbParams);
        break;

      case ServiceType.Redis:
        const redisParams: any = {
          redisConnection: redisConfig.redisConnection,
        };
        if (redisConfig.useSsl) redisParams.useSsl = true;
        if (redisConfig.checkMemoryUsage) redisParams.checkMemoryUsage = true;
        if (redisConfig.checkConnectedClients) redisParams.checkConnectedClients = true;
        config.parameters = JSON.stringify(redisParams);
        break;

      case ServiceType.WindowsService:
        const wsParams: any = {
          serviceName: windowsServiceConfig.serviceName,
          machineName: windowsServiceConfig.machineName || '.',
        };
        if (windowsServiceConfig.checkStartType) {
          wsParams.checkStartType = true;
          if (windowsServiceConfig.expectedStartType) {
            wsParams.expectedStartType = windowsServiceConfig.expectedStartType;
          }
        }
        config.parameters = JSON.stringify(wsParams);
        break;
    }

    const submitData = {
      ...formData,
      configuration: config,
    };

    onSubmit(submitData);
  };

  const renderTypeSpecificFields = () => {
    switch (formData.type) {
      case ServiceType.Http:
        return (
          <div className="config-section">
            <h3>Настройки HTTP</h3>
            <div className="form-group">
              <label htmlFor="expectedStatusCode">Ожидаемый HTTP статус код</label>
              <input
                id="expectedStatusCode"
                type="number"
                value={httpConfig.expectedStatusCode || 200}
                onChange={(e) =>
                  setHttpConfig({ ...httpConfig, expectedStatusCode: parseInt(e.target.value) || 200 })
                }
              />
            </div>
            <div className="form-group">
              <label htmlFor="headers">HTTP заголовки (JSON)</label>
              <textarea
                id="headers"
                value={headersJson}
                onChange={(e) => setHeadersJson(e.target.value)}
                placeholder='{"Authorization": "Bearer token"}'
                rows={3}
              />
              <small>Формат: JSON объект с парами ключ-значение</small>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={httpConfig.parseModules || false}
                  onChange={(e) => setHttpConfig({ ...httpConfig, parseModules: e.target.checked })}
                />
                Парсить детальную информацию о модулях (для X-Lab API)
              </label>
            </div>
            {httpConfig.parseModules && (
              <div className="form-group">
                <label htmlFor="criticalModules">Критические модули (через запятую)</label>
                <input
                  id="criticalModules"
                  type="text"
                  value={criticalModulesStr}
                  onChange={(e) => setCriticalModulesStr(e.target.value)}
                  placeholder="database, cache"
                />
                <small>Список модулей, которые считаются критическими для определения статуса</small>
              </div>
            )}
          </div>
        );

      case ServiceType.Database:
        return (
          <div className="config-section">
            <h3>Настройки базы данных</h3>
            <div className="form-group">
              <label htmlFor="connectionString">Строка подключения *</label>
              <textarea
                id="connectionString"
                value={databaseConfig.connectionString}
                onChange={(e) => setDatabaseConfig({ ...databaseConfig, connectionString: e.target.value })}
                placeholder="Data Source=server;Initial Catalog=db;User ID=user;Password=pass;..."
                rows={3}
              />
              <small>Строка подключения к базе данных MS SQL Server</small>
            </div>
            <div className="form-group">
              <label htmlFor="testQuery">Тестовый SQL запрос</label>
              <input
                id="testQuery"
                type="text"
                value={databaseConfig.testQuery || 'SELECT 1'}
                onChange={(e) => setDatabaseConfig({ ...databaseConfig, testQuery: e.target.value })}
              />
              <small>SQL запрос для проверки доступности БД</small>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={databaseConfig.checkDatabaseSize || false}
                  onChange={(e) =>
                    setDatabaseConfig({ ...databaseConfig, checkDatabaseSize: e.target.checked })
                  }
                />
                Проверять размер базы данных
              </label>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={databaseConfig.checkActiveConnections || false}
                  onChange={(e) =>
                    setDatabaseConfig({ ...databaseConfig, checkActiveConnections: e.target.checked })
                  }
                />
                Проверять активные соединения
              </label>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={databaseConfig.checkPerformance || false}
                  onChange={(e) =>
                    setDatabaseConfig({ ...databaseConfig, checkPerformance: e.target.checked })
                  }
                />
                Проверять производительность
              </label>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={databaseConfig.checkBackupInfo || false}
                  onChange={(e) =>
                    setDatabaseConfig({ ...databaseConfig, checkBackupInfo: e.target.checked })
                  }
                />
                Проверять информацию о резервном копировании
              </label>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={databaseConfig.checkServerInfo || false}
                  onChange={(e) =>
                    setDatabaseConfig({ ...databaseConfig, checkServerInfo: e.target.checked })
                  }
                />
                Проверять информацию о сервере
              </label>
            </div>
          </div>
        );

      case ServiceType.Redis:
        return (
          <div className="config-section">
            <h3>Настройки Redis</h3>
            <div className="form-group">
              <label htmlFor="redisConnection">Подключение Redis (host:port) *</label>
              <input
                id="redisConnection"
                type="text"
                value={redisConfig.redisConnection}
                onChange={(e) => setRedisConfig({ ...redisConfig, redisConnection: e.target.value })}
                placeholder="192.168.20.7:6379"
              />
              <small>Адрес и порт Redis сервера</small>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={redisConfig.useSsl || false}
                  onChange={(e) => setRedisConfig({ ...redisConfig, useSsl: e.target.checked })}
                />
                Использовать SSL/TLS
              </label>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={redisConfig.checkMemoryUsage || false}
                  onChange={(e) => setRedisConfig({ ...redisConfig, checkMemoryUsage: e.target.checked })}
                />
                Проверять использование памяти
              </label>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={redisConfig.checkConnectedClients || false}
                  onChange={(e) =>
                    setRedisConfig({ ...redisConfig, checkConnectedClients: e.target.checked })
                  }
                />
                Проверять подключенных клиентов
              </label>
            </div>
          </div>
        );

      case ServiceType.WindowsService:
        return (
          <div className="config-section">
            <h3>Настройки Windows службы</h3>
            <div className="form-group">
              <label htmlFor="serviceName">Имя службы *</label>
              <input
                id="serviceName"
                type="text"
                value={windowsServiceConfig.serviceName}
                onChange={(e) =>
                  setWindowsServiceConfig({ ...windowsServiceConfig, serviceName: e.target.value })
                }
                placeholder="XLabNotificationService"
              />
              <small>Точное имя Windows службы (как в services.msc)</small>
            </div>
            <div className="form-group">
              <label htmlFor="machineName">Имя компьютера</label>
              <input
                id="machineName"
                type="text"
                value={windowsServiceConfig.machineName || '.'}
                onChange={(e) =>
                  setWindowsServiceConfig({ ...windowsServiceConfig, machineName: e.target.value })
                }
                placeholder=". (точка для локальной машины)"
              />
              <small>Имя компьютера или "." для локальной машины</small>
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  checked={windowsServiceConfig.checkStartType || false}
                  onChange={(e) =>
                    setWindowsServiceConfig({ ...windowsServiceConfig, checkStartType: e.target.checked })
                  }
                />
                Проверять тип запуска службы
              </label>
            </div>
            {windowsServiceConfig.checkStartType && (
              <div className="form-group">
                <label htmlFor="expectedStartType">Ожидаемый тип запуска</label>
                <select
                  id="expectedStartType"
                  value={windowsServiceConfig.expectedStartType || 'Automatic'}
                  onChange={(e) =>
                    setWindowsServiceConfig({ ...windowsServiceConfig, expectedStartType: e.target.value })
                  }
                >
                  <option value="Automatic">Automatic (Автоматически)</option>
                  <option value="Manual">Manual (Вручную)</option>
                  <option value="Disabled">Disabled (Отключено)</option>
                </select>
              </div>
            )}
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <form onSubmit={handleSubmit} className="service-form">
      <div className="form-group">
        <label htmlFor="name">Название *</label>
        <input
          id="name"
          type="text"
          required
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          maxLength={200}
        />
      </div>

      <div className="form-group">
        <label htmlFor="description">Описание</label>
        <textarea
          id="description"
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          maxLength={1000}
          rows={3}
        />
      </div>

      <div className="form-group">
        <label htmlFor="type">Тип сервиса *</label>
        <select
          id="type"
          required
          value={formData.type}
          onChange={(e) => setFormData({ ...formData, type: parseInt(e.target.value) as ServiceType })}
        >
          <option value={ServiceType.Http}>HTTP/HTTPS</option>
          <option value={ServiceType.Tcp}>TCP</option>
          <option value={ServiceType.Database}>База данных (MS SQL Server)</option>
          <option value={ServiceType.Redis}>Redis</option>
          <option value={ServiceType.WindowsService}>Windows служба</option>
          <option value={ServiceType.Kafka}>Apache Kafka</option>
          <option value={ServiceType.Custom}>Кастомный</option>
        </select>
      </div>

      <div className="form-group">
        <label htmlFor="url">URL / Endpoint *</label>
        <input
          id="url"
          type="text"
          required
          value={formData.url}
          onChange={(e) => setFormData({ ...formData, url: e.target.value })}
          placeholder={
            formData.type === ServiceType.Http
              ? 'https://api.example.com/health'
              : formData.type === ServiceType.Database
                ? 'db.x-lab.by'
                : formData.type === ServiceType.Redis
                  ? 'redis://192.168.20.7:6379'
                  : formData.type === ServiceType.WindowsService
                    ? 'XLabNotificationService'
                    : 'Endpoint для проверки'
          }
          maxLength={500}
        />
        <small>
          {formData.type === ServiceType.Http && 'URL health check endpoint'}
          {formData.type === ServiceType.Database && 'Имя сервера базы данных'}
          {formData.type === ServiceType.Redis && 'Адрес Redis сервера'}
          {formData.type === ServiceType.WindowsService && 'Имя службы Windows'}
        </small>
      </div>

      {renderTypeSpecificFields()}

      <div className="form-section">
        <h3>Общие настройки</h3>
        <div className="form-group">
          <label htmlFor="checkInterval">Интервал проверки (секунды) *</label>
          <input
            id="checkInterval"
            type="number"
            required
            min={1}
            max={3600}
            value={formData.checkInterval}
            onChange={(e) => setFormData({ ...formData, checkInterval: parseInt(e.target.value) || 60 })}
          />
          <small>Минимальный интервал: 1 секунда, максимальный: 3600 секунд (1 час)</small>
        </div>

        <div className="form-group">
          <label htmlFor="timeout">Таймаут (миллисекунды) *</label>
          <input
            id="timeout"
            type="number"
            required
            min={1}
            max={60000}
            value={formData.timeout}
            onChange={(e) => setFormData({ ...formData, timeout: parseInt(e.target.value) || 5000 })}
          />
          <small>Максимальное время ожидания ответа (макс: 60000 мс)</small>
        </div>

        <div className="form-group">
          <label htmlFor="retryCount">Количество повторов *</label>
          <input
            id="retryCount"
            type="number"
            required
            min={0}
            max={10}
            value={formData.retryCount}
            onChange={(e) => setFormData({ ...formData, retryCount: parseInt(e.target.value) || 0 })}
          />
          <small>Количество повторных попыток при ошибке (0-10)</small>
        </div>

        <div className="form-group">
          <label>
            <input
              type="checkbox"
              checked={formData.isEnabled}
              onChange={(e) => setFormData({ ...formData, isEnabled: e.target.checked })}
            />
            Включен (мониторинг активен)
          </label>
        </div>

        <div className="form-group">
          <label>
            <input
              type="checkbox"
              checked={formData.isPublic}
              onChange={(e) => setFormData({ ...formData, isPublic: e.target.checked })}
            />
            Публичный (отображается в публичном API)
          </label>
        </div>

        <div className="form-group">
          <label>
            <input
              type="checkbox"
              checked={formData.isCritical}
              onChange={(e) => setFormData({ ...formData, isCritical: e.target.checked })}
            />
            Критичный (при сбое — вся система не работает)
          </label>
        </div>
      </div>

      <div className="form-actions">
        <button type="button" className="btn-secondary" onClick={onCancel} disabled={isLoading}>
          Отмена
        </button>
        <button type="submit" className="btn-primary" disabled={isLoading}>
          {isLoading ? 'Сохранение...' : service ? 'Сохранить' : 'Создать'}
        </button>
      </div>
    </form>
  );
};

export default ServiceForm;

