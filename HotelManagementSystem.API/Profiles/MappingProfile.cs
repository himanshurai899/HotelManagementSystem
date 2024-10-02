using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Models;

namespace HotelManagementSystem.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDTO>().ReverseMap();
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
