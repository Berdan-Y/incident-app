using Shared.Models.Dtos;
using Shared.Models.Enums;
using API.Models.Classes;
using AutoMapper;
using IncidentCreateDto = Shared.Models.Dtos.IncidentCreateDto;
using IncidentPhotoDto = Shared.Models.Dtos.IncidentPhotoDto;
using IncidentResponseDto = Shared.Models.Dtos.IncidentResponseDto;
using UserDto = Shared.Models.Dtos.UserDto;

namespace API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Incident, IncidentResponseDto>()
            .ForMember(dest => dest.ReportedById, opt => opt.MapFrom(src => src.ReportedById))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.AssignedTo));

        CreateMap<IncidentCreateDto, Incident>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Status.Todo))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => Priority.Unknown))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<IncidentUpdateDto, Incident>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<User, UserDto>();

        CreateMap<IncidentPhoto, IncidentPhotoDto>();
    }
} 