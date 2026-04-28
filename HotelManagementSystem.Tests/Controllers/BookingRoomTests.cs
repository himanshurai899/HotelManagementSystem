using System.Security.Claims;
using AutoMapper;
using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Enums;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelManagementSystem.Tests.Controllers
{
    /// <summary>
    /// Phase 12a — Multi-Room Booking behavioural tests.
    ///
    /// These tests assert the new contract for BookingsController.Create:
    ///   1. POST accepts a List&lt;BookingRoomDTO&gt; under BookingDTO.Rooms.
    ///   2. The server (not the client) snapshots PriceAtBooking from Room.PricePerNight
    ///      at booking time — security improvement aligned with Phase 17 pricing engine.
    ///   3. The overlap guard runs **per room** — if ANY requested room collides with
    ///      a Pending/Confirmed booking on overlapping dates, the entire request is
    ///      rejected with 409 Conflict (atomic — no partial bookings).
    ///   4. Server computes TotalPrice from sum(PriceAtBooking × nights) — client values ignored.
    /// </summary>
    public class BookingRoomTests
    {
        // ---------- helpers ----------

        private static BookingsController BuildController(
            Mock<IRepository<Booking>> bookingRepo,
            Mock<IRepository<Room>> roomRepo,
            Mock<IMapper> mapper,
            int userId,
            params string[] roles)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            // BookingsController constructor signature changes in Phase 12a to also inject
            // IRepository<Room> so the server can resolve PriceAtBooking authoritatively.
            var controller = new BookingsController(bookingRepo.Object, roomRepo.Object, mapper.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
            return controller;
        }

        private static BookingDTO MakeDto(DateTime checkIn, DateTime checkOut, params int[] roomIds)
        {
            return new BookingDTO
            {
                // Use NightStay (multi-night) — tests here exercise multi-room logic, not booking type.
                BookingType  = BookingType.NightStay,
                CheckInDate  = checkIn,
                CheckOutDate = checkOut,
                Rooms        = roomIds.Select(id => new BookingRoomDTO { RoomId = id }).ToList()
            };
        }

        // =====================================================================
        //  MULTI-ROOM CREATE — happy path
        // =====================================================================

        [Fact]
        public async Task Create_MultiRoom_PersistsOneBookingRoomPerRequestedRoom()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 6, 1);
            var checkOut = new DateTime(2026, 6, 4); // 3 nights
            var dtoIn    = MakeDto(checkIn, checkOut, roomIds: new[] { 10, 11 });

            // Two rooms with different nightly prices.
            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Room { Id = 10, PricePerNight = 100m });
            roomRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Room { Id = 11, PricePerNight = 150m });

            // Mapper produces a fresh entity (BookingRooms collection assembled by controller).
            mapper.Setup(m => m.Map<Booking>(dtoIn))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut });
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var sut = BuildController(bookingRepo, roomRepo, mapper, userId: 7, "Customer");

            var result = await sut.Create(dtoIn) as CreatedAtActionResult;

            Assert.NotNull(result);
            bookingRepo.Verify(
                r => r.AddAsync(It.Is<Booking>(b =>
                    b.BookingRooms.Count == 2
                 && b.BookingRooms.Any(br => br.RoomId == 10 && br.PriceAtBooking == 100m)
                 && b.BookingRooms.Any(br => br.RoomId == 11 && br.PriceAtBooking == 150m))),
                Times.Once);
        }

        [Fact]
        public async Task Create_MultiRoom_ServerComputesTotalPrice_IgnoringClientValue()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 6, 1);
            var checkOut = new DateTime(2026, 6, 4); // 3 nights
            var dtoIn    = MakeDto(checkIn, checkOut, roomIds: new[] { 10, 11 });
            dtoIn.TotalPrice = 1m; // client tries to underpay

            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Room { Id = 10, PricePerNight = 100m });
            roomRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Room { Id = 11, PricePerNight = 150m });

            mapper.Setup(m => m.Map<Booking>(dtoIn))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut, TotalPrice = 1m });
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var sut = BuildController(bookingRepo, roomRepo, mapper, userId: 7, "Customer");

            await sut.Create(dtoIn);

            // (100 + 150) × 3 nights = 750, regardless of dtoIn.TotalPrice = 1.
            bookingRepo.Verify(r => r.AddAsync(It.Is<Booking>(b => b.TotalPrice == 750m)), Times.Once);
        }

        // =====================================================================
        //  OVERLAP GUARD — per-room, atomic
        // =====================================================================

        [Fact]
        public async Task Create_MultiRoom_RejectsWhenAnyRoomConflicts()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 6, 1);
            var checkOut = new DateTime(2026, 6, 4);
            var dtoIn    = MakeDto(checkIn, checkOut, roomIds: new[] { 10, 11 });

            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Room { Id = 10, PricePerNight = 100m });
            roomRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Room { Id = 11, PricePerNight = 150m });

            mapper.Setup(m => m.Map<Booking>(dtoIn))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut });

            // Existing booking holds Room 11 across the requested window — must reject.
            var existing = new Booking
            {
                Id = 99,
                Status = BookingStatus.Confirmed,
                CheckInDate  = new DateTime(2026, 6, 2),
                CheckOutDate = new DateTime(2026, 6, 3),
                BookingRooms = new List<BookingRoom>
                {
                    new() { RoomId = 11, BookingId = 99 }
                }
            };
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> { existing });

            var sut = BuildController(bookingRepo, roomRepo, mapper, userId: 7, "Customer");

            var result = await sut.Create(dtoIn);

            Assert.IsType<ConflictObjectResult>(result);
            // Atomic: no partial booking persisted.
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task Create_MultiRoom_AcceptsWhenCancelledBookingHoldsRoom()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 6, 1);
            var checkOut = new DateTime(2026, 6, 4);
            var dtoIn    = MakeDto(checkIn, checkOut, roomIds: new[] { 10 });

            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Room { Id = 10, PricePerNight = 100m });

            mapper.Setup(m => m.Map<Booking>(dtoIn))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut });
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            // Cancelled booking on the same room — must NOT block the new booking.
            var cancelled = new Booking
            {
                Id = 50,
                Status = BookingStatus.Cancelled,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                BookingRooms = new List<BookingRoom> { new() { RoomId = 10, BookingId = 50 } }
            };
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> { cancelled });

            var sut = BuildController(bookingRepo, roomRepo, mapper, userId: 7, "Customer");

            var result = await sut.Create(dtoIn);

            Assert.IsType<CreatedAtActionResult>(result);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        // =====================================================================
        //  VALIDATION
        // =====================================================================

        [Fact]
        public async Task Create_RejectsEmptyRoomsList()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var mapper      = new Mock<IMapper>();

            var dtoIn = new BookingDTO
            {
                CheckInDate  = new DateTime(2026, 6, 1),
                CheckOutDate = new DateTime(2026, 6, 4),
                Rooms        = new List<BookingRoomDTO>() // empty
            };

            mapper.Setup(m => m.Map<Booking>(dtoIn)).Returns(new Booking());

            var sut = BuildController(bookingRepo, roomRepo, mapper, userId: 7, "Customer");

            var result = await sut.Create(dtoIn);

            Assert.IsType<BadRequestObjectResult>(result);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task Create_RejectsWhenRoomNotFound()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var mapper      = new Mock<IMapper>();

            var dtoIn = MakeDto(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 4),
                roomIds: new[] { 999 });

            roomRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Room?)null!);
            mapper.Setup(m => m.Map<Booking>(dtoIn)).Returns(new Booking());
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var sut = BuildController(bookingRepo, roomRepo, mapper, userId: 7, "Customer");

            var result = await sut.Create(dtoIn);

            Assert.IsType<BadRequestObjectResult>(result);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}
