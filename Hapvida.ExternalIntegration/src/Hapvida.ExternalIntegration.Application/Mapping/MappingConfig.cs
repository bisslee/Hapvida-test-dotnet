using AutoMapper;
using Hapvida.ExternalIntegration.Application.Models;
using Hapvida.ExternalIntegration.Domain.Entities;

namespace Hapvida.ExternalIntegration.Application.Mapping;

public class MappingConfig : Profile
{
    public MappingConfig()
    {
        // Mapeamento de CepResult para CepResponseDto
        CreateMap<CepResult, CepResponseDto>()
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.ZipCode.Value))
            .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location != null
                ? new LocationDto
                {
                    Lat = src.Location.Latitude,
                    Lon = src.Location.Longitude
                }
                : null));

        // Mapeamento de ZipCodeLookup para ZipCodeLookupDto
        CreateMap<ZipCodeLookup, ZipCodeLookupDto>()
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => 
                src.Latitude.HasValue && src.Longitude.HasValue
                    ? new LocationDto
                    {
                        Lat = src.Latitude.Value,
                        Lon = src.Longitude.Value
                    }
                    : null))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.CreatedAt));
    }
}

