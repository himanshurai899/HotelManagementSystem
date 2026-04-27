using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Models;

namespace HotelManagementSystem.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDTO>()
                .ForMember(dest => dest.Username,        opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.FirstName,       opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName,        opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.ProfilePhotoUrl))
                .ReverseMap()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username));
            CreateMap<Role, RoleDTO>().ReverseMap();
            CreateMap<Room, RoomDTO>().ReverseMap();
            CreateMap<RoomType, RoomTypeDTO>().ReverseMap();
            CreateMap<Booking, BookingDTO>().ReverseMap();
            CreateMap<Payment, PaymentDTO>().ReverseMap();
            CreateMap<Amenity, AmenityDTO>().ReverseMap();
            CreateMap<Staff, StaffDTO>().ReverseMap();
        }
    }
}
