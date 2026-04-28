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
            CreateMap<Room, RoomDTO>()
                .ForMember(dest => dest.RoomTypeName,
                    opt => opt.MapFrom(src => src.RoomType != null ? src.RoomType.Name : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.RoomType, opt => opt.Ignore());
            CreateMap<RoomType, RoomTypeDTO>().ReverseMap();
            CreateMap<Booking, BookingDTO>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty))
                .ForMember(dest => dest.RoomNumber,
                    opt => opt.MapFrom(src => src.Room != null ? src.Room.RoomNumber : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore());
            CreateMap<Payment, PaymentDTO>().ReverseMap();
            CreateMap<Amenity, AmenityDTO>().ReverseMap();
            CreateMap<Staff, StaffDTO>().ReverseMap();

            // ── Billing & Payments (Phase 8) ──────────────────────────────────
            CreateMap<Invoice, InvoiceDTO>()
                .ForMember(dest => dest.GuestName,
                    opt => opt.MapFrom(src => src.Booking != null ? src.Booking.User.UserName : string.Empty))
                .ForMember(dest => dest.RoomNumber,
                    opt => opt.MapFrom(src => src.Booking != null ? src.Booking.Room.RoomNumber : string.Empty))
                .ReverseMap();
            CreateMap<InvoiceItem, InvoiceItemDTO>().ReverseMap();
            CreateMap<CompanyProfile, CompanyProfileDTO>().ReverseMap();

            // ── Multi-Tenant SaaS (Phase 9) ───────────────────────────────────
            CreateMap<Tenant, TenantDTO>().ReverseMap();
            CreateMap<UserTenant, UserTenantDTO>()
                .ForMember(dest => dest.UserName,   opt => opt.MapFrom(src => src.User != null ? src.User.UserName  : string.Empty))
                .ForMember(dest => dest.FirstName,  opt => opt.MapFrom(src => src.User != null ? src.User.FirstName ?? string.Empty : string.Empty))
                .ForMember(dest => dest.LastName,   opt => opt.MapFrom(src => src.User != null ? src.User.LastName  ?? string.Empty : string.Empty))
                .ForMember(dest => dest.Email,      opt => opt.MapFrom(src => src.User != null ? src.User.Email     ?? string.Empty : string.Empty))
                .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty));
        }
    }
}
