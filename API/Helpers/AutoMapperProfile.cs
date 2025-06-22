using AutoMapper;
using Shared.Models.Classes;
using Shared.Models.Dtos;

namespace API.Helpers;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Incident, IncidentResponseDto>()
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.AssignedTo))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos));

        CreateMap<IncidentPhoto, IncidentPhotoDto>();

        CreateMap<User, UserDto>();

        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
            .ForMember(dest => dest.IncidentTitle, opt => opt.MapFrom(src => src.Incident.Title));
    }
}