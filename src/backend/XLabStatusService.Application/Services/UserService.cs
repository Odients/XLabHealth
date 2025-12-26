using AutoMapper;
using Microsoft.Extensions.Logging;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Entities;
using XLabStatusService.Core.Interfaces;

namespace XLabStatusService.Application.Services;

/// <summary>
/// Сервис для управления пользователями
/// </summary>
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Получить всех пользователей
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    public async Task<UserDto> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
    {
        // Проверка на существование пользователя с таким username
        if (await _userRepository.ExistsAsync(dto.Username, cancellationToken))
        {
            throw new InvalidOperationException($"User with username '{dto.Username}' already exists");
        }

        // Проверка на существование пользователя с таким email
        var existingUserByEmail = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (existingUserByEmail != null)
        {
            throw new InvalidOperationException($"User with email '{dto.Email}' already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);
        _logger.LogInformation("User created: {Username} ({UserId})", createdUser.Username, createdUser.Id);

        return _mapper.Map<UserDto>(createdUser);
    }

    /// <summary>
    /// Обновить пользователя
    /// </summary>
    public async Task<UserDto> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found");
        }

        // Проверка на существование другого пользователя с таким username
        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
        {
            if (await _userRepository.ExistsAsync(dto.Username, cancellationToken))
            {
                throw new InvalidOperationException($"User with username '{dto.Username}' already exists");
            }
            user.Username = dto.Username;
        }

        // Проверка на существование другого пользователя с таким email
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            var existingUserByEmail = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
            if (existingUserByEmail != null && existingUserByEmail.Id != id)
            {
                throw new InvalidOperationException($"User with email '{dto.Email}' already exists");
            }
            user.Email = dto.Email;
        }

        // Обновление пароля, если указан
        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        // Обновление роли
        if (!string.IsNullOrEmpty(dto.Role))
        {
            user.Role = dto.Role;
        }

        // Обновление статуса активности
        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
        }

        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("User updated: {Username} ({UserId})", updatedUser.Username, updatedUser.Id);

        return _mapper.Map<UserDto>(updatedUser);
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found");
        }

        await _userRepository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("User deleted: {Username} ({UserId})", user.Username, user.Id);
    }
}

