using AutoMapper;
using XLabStatusService.Application.DTOs;
using XLabStatusService.Core.Entities;

namespace XLabStatusService.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Service mappings
        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.LastStatus, opt => opt.Ignore())
            .ForMember(dest => dest.LastCheckedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Configuration, opt => opt.MapFrom(src => src.Configuration != null 
                ? new ServiceConfigurationDto
                {
                    CheckType = src.Configuration.CheckType,
                    Parameters = src.Configuration.Parameters,
                    Headers = src.Configuration.Headers,
                    ExpectedStatusCode = src.Configuration.ExpectedStatusCode,
                    ExpectedResponse = src.Configuration.ExpectedResponse
                }
                : null));

        CreateMap<Service, PublicServiceDto>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.LastCheckedAt, opt => opt.Ignore());

        CreateMap<ServiceCreateDto, Service>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HealthCheckResults, opt => opt.Ignore())
            .ForMember(dest => dest.Configuration, opt => opt.Ignore());

        CreateMap<ServiceUpdateDto, Service>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HealthCheckResults, opt => opt.Ignore())
            .ForMember(dest => dest.Configuration, opt => opt.Ignore());

        // ServiceConfiguration mappings
        CreateMap<ServiceConfigurationDto, ServiceConfiguration>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceId, opt => opt.Ignore())
            .ForMember(dest => dest.Service, opt => opt.Ignore());

        CreateMap<ServiceConfiguration, ServiceConfigurationDto>();

        // HealthCheckResult mappings
        CreateMap<HealthCheckResult, HealthCheckResultDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.Ignore());

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<UserCreateDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore());

        // Webhook mappings
        CreateMap<Webhook, WebhookDto>();
        CreateMap<WebhookCreateDto, Webhook>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Service, opt => opt.Ignore());

        CreateMap<WebhookUpdateDto, Webhook>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Service, opt => opt.Ignore());
    }
}

