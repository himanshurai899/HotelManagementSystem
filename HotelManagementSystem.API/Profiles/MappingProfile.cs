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
                .ForMember(dest => dest.TenantName,
                    opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.RoomType, opt => opt.Ignore())
                // Prevent AutoMapper from reverse-mapping TenantName → Tenant.Name,
                // which would create a phantom Tenant { Name = null } that EF tries to INSERT.
                .ForMember(dest => dest.Tenant, opt => opt.Ignore());
            CreateMap<RoomType, RoomTypeDTO>()
                .ForMember(dest => dest.TenantName,
                    opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.Tenant, opt => opt.Ignore());
            // Phase 12a — BookingRoom join entity
            CreateMap<BookingRoom, BookingRoomDTO>()
                .ForMember(dest => dest.RoomNumber,
                    opt => opt.MapFrom(src => src.Room != null ? src.Room.RoomNumber : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.Room,    opt => opt.Ignore())
                .ForMember(dest => dest.Booking, opt => opt.Ignore());
            // Phase 12d — RoomBlock
            CreateMap<RoomBlock, RoomBlockDTO>()
                .ForMember(dest => dest.RoomNumber,
                    opt => opt.MapFrom(src => src.Room != null ? src.Room.RoomNumber : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.Room, opt => opt.Ignore());

            CreateMap<Booking, BookingDTO>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty))
                .ForMember(dest => dest.Rooms,
                    opt => opt.MapFrom(src => src.BookingRooms))
                // Legacy derived fields — pulled from the first BookingRoom for backward compat.
                .ForMember(dest => dest.RoomId,
                    opt => opt.MapFrom(src => src.BookingRooms.FirstOrDefault() != null
                        ? src.BookingRooms.First().RoomId : 0))
                .ForMember(dest => dest.RoomNumber,
                    opt => opt.MapFrom(src => src.BookingRooms.FirstOrDefault() != null && src.BookingRooms.First().Room != null
                        ? src.BookingRooms.First().Room.RoomNumber : string.Empty))
                .ForMember(dest => dest.TenantName,
                    opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                // Phase 12c — TimeSpan → "HH:mm" string
                .ForMember(dest => dest.CheckInTime,
                    opt => opt.MapFrom(src => src.CheckInTime != null
                        ? src.CheckInTime.Value.ToString(@"hh\:mm") : null))
                .ForMember(dest => dest.CheckOutTime,
                    opt => opt.MapFrom(src => src.CheckOutTime != null
                        ? src.CheckOutTime.Value.ToString(@"hh\:mm") : null))
                .ReverseMap()
                .ForMember(dest => dest.User,         opt => opt.Ignore())
                .ForMember(dest => dest.Tenant,       opt => opt.Ignore())
                // Controller assembles BookingRooms server-side with authoritative PriceAtBooking.
                .ForMember(dest => dest.BookingRooms, opt => opt.Ignore())
                // Phase 12c — "HH:mm" string → TimeSpan (controller handles Hourly-specific logic)
                .ForMember(dest => dest.CheckInTime,
                    opt => opt.MapFrom(src => string.IsNullOrEmpty(src.CheckInTime)
                        ? (TimeSpan?)null : TimeSpan.Parse(src.CheckInTime)))
                .ForMember(dest => dest.CheckOutTime,
                    opt => opt.MapFrom(src => string.IsNullOrEmpty(src.CheckOutTime)
                        ? (TimeSpan?)null : TimeSpan.Parse(src.CheckOutTime)));
            CreateMap<Payment, PaymentDTO>().ReverseMap();
            CreateMap<Amenity, AmenityDTO>()
                .ForMember(dest => dest.TenantName,
                    opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.Tenant, opt => opt.Ignore());
            CreateMap<Staff, StaffDTO>()
                .ForMember(dest => dest.TenantName,
                    opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.Tenant, opt => opt.Ignore());

            // ── Billing & Payments (Phase 8) ──────────────────────────────────
            CreateMap<Invoice, InvoiceDTO>()
                .ForMember(dest => dest.GuestName,
                    opt => opt.MapFrom(src => src.Booking != null && src.Booking.User != null
                        ? src.Booking.User.UserName : string.Empty))
                // Phase 12a — invoice room number derived from the first BookingRoom.
                .ForMember(dest => dest.RoomNumber,
                    opt => opt.MapFrom(src =>
                        src.Booking != null
                     && src.Booking.BookingRooms != null
                     && src.Booking.BookingRooms.Any()
                     && src.Booking.BookingRooms.First().Room != null
                            ? src.Booking.BookingRooms.First().Room.RoomNumber
                            : string.Empty))
                .ForMember(dest => dest.TenantName,
                    opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.Name : string.Empty))
                .ReverseMap()
                .ForMember(dest => dest.Booking, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant,  opt => opt.Ignore());
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
