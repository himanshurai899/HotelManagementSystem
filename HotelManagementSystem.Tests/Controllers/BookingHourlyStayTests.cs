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
    /// Phase 12c — Hourly Stay &amp; BookingType validation tests.
    ///
    /// Covered contracts:
    ///   1. Hourly booking: TotalPrice = sum(HourlyRate) × ceil(hours) (server-computed).
    ///   2. Hourly booking rejected when any selected room lacks AllowHourlyStay.
    ///   3. Hourly booking rejected when any selected room has null HourlyRate.
    ///   4. Hourly booking rejected when CheckInTime >= CheckOutTime.
    ///   5. Hourly booking rejected when CheckInDate != CheckOutDate.
    ///   6. FullDay booking: exactly 1 night enforced (fractional nights rounded → must equal 1).
    ///   7. Yearly booking: duration must be an exact multiple of 365 days.
    ///   8. NightStay/LongTerm: no minimum duration enforced.
    ///   9. Hourly: CheckInTime and CheckOutTime are required.
    ///  10. NightStay: time fields are stripped server-side (null on saved entity).
    /// </summary>
    public class BookingHourlyStayTests
    {
        // ---------- helpers ----------

        private static BookingsController BuildController(
            Mock<IRepository<Booking>> repo,
            Mock<IRepository<Room>> roomRepo,
            Mock<IMapper> mapper,
            int userId = 1,
            string role = "Customer")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = new BookingsController(repo.Object, roomRepo.Object, mapper.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
            return controller;
        }

        /// <summary>
        /// Stub every method BookingsController.Create calls on the booking repo so tests
        /// that don't exercise the repo don't fail with NullReferenceException.
        /// </summary>
        private static void StubEmptyBookings(Mock<IRepository<Booking>> repo)
        {
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
        }

        private static BookingDTO HourlyDto(int roomId = 10,
            string checkInTime = "14:00", string checkOutTime = "16:00",
            DateTime? checkInDate = null, DateTime? checkOutDate = null)
        {
            return new BookingDTO
            {
                BookingType  = BookingType.Hourly,
                CheckInDate  = checkInDate  ?? DateTime.Today,
                CheckOutDate = checkOutDate ?? DateTime.Today,
                CheckInTime  = checkInTime,
                CheckOutTime = checkOutTime,
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = roomId } }
            };
        }

        private static Room HourlyRoom(int id = 10, decimal hourlyRate = 50m) =>
            new Room { Id = id, RoomNumber = $"H-{id}", PricePerNight = 0m,
                       AllowHourlyStay = true, HourlyRate = hourlyRate };

        // =====================================================================
        //  TEST 1 — Hourly: TotalPrice = sum(HourlyRate) × ceil(hours)
        // =====================================================================

        [Fact]
        public async Task Create_HourlyBooking_TotalPrice_IsHourlyRate_Times_Hours()
        {
            // 14:00 → 17:30 = 3.5 hrs → ceil = 4 hrs; HourlyRate = 50 → TotalPrice = 200
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            var room = HourlyRoom(10, 50m);
            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(room);

            var dto    = HourlyDto(10, "14:00", "17:30");
            var entity = new Booking();
            mapper.Setup(m => m.Map<Booking>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
            // 4 hours × 50/hr = 200
            Assert.Equal(200m, entity.TotalPrice);
        }

        // =====================================================================
        //  TEST 2 — Hourly rejected when room does not allow hourly stay
        // =====================================================================

        [Fact]
        public async Task Create_HourlyBooking_Rejected_WhenRoomDoesNotAllowHourly()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            // Room exists but AllowHourlyStay = false
            roomRepo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new Room { Id = 10, RoomNumber = "R-10",
                                         AllowHourlyStay = false, HourlyRate = 50m });
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today });

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(HourlyDto(10));

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("hourly", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        //  TEST 3 — Hourly rejected when room has null HourlyRate
        // =====================================================================

        [Fact]
        public async Task Create_HourlyBooking_Rejected_WhenRoomHasNoHourlyRate()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            roomRepo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new Room { Id = 10, RoomNumber = "R-10",
                                         AllowHourlyStay = true, HourlyRate = null });
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today });

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(HourlyDto(10));

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("hourly rate", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        //  TEST 4 — Hourly rejected when CheckInTime >= CheckOutTime
        // =====================================================================

        [Fact]
        public async Task Create_HourlyBooking_Rejected_WhenCheckInTime_GteCheckOutTime()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(HourlyRoom(10));
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today });

            var dto = HourlyDto(10, checkInTime: "16:00", checkOutTime: "14:00");
            var sut = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // =====================================================================
        //  TEST 5 — Hourly rejected when CheckInDate != CheckOutDate
        // =====================================================================

        [Fact]
        public async Task Create_HourlyBooking_Rejected_WhenCheckInDate_NotSameDay()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(HourlyRoom(10));
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(1) });

            // CheckOutDate is next day — invalid for hourly
            var dto = HourlyDto(10,
                checkInDate:  DateTime.Today,
                checkOutDate: DateTime.Today.AddDays(1));
            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // =====================================================================
        //  TEST 6 — FullDay: exactly 1 night must be selected
        // =====================================================================

        [Fact]
        public async Task Create_FullDayBooking_Rejected_WhenMoreThanOneNight()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            var room = new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m };
            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(room);
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(3) });

            var dto = new BookingDTO
            {
                BookingType  = BookingType.FullDay,
                CheckInDate  = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(3),   // 3 nights — invalid
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("1 night", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        //  TEST 7 — Yearly: must be exact multiple of 365 days
        // =====================================================================

        [Fact]
        public async Task Create_YearlyBooking_Rejected_WhenNotMultipleOf365Days()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            var room = new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m, YearlyRate = 20000m };
            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(room);
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(400) });

            var dto = new BookingDTO
            {
                BookingType  = BookingType.Yearly,
                CheckInDate  = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(400),  // not a multiple of 365
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("365", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        //  TEST 8 — NightStay: no minimum; 1 night is valid
        // =====================================================================

        [Fact]
        public async Task Create_NightStay_AcceptsOneNight()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            var room = new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m };
            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(room);

            var entity = new Booking();
            var dto = new BookingDTO
            {
                BookingType  = BookingType.NightStay,
                CheckInDate  = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };
            mapper.Setup(m => m.Map<Booking>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        // =====================================================================
        //  TEST 9 — Hourly: CheckInTime and CheckOutTime are required
        // =====================================================================

        [Fact]
        public async Task Create_HourlyBooking_Rejected_WhenTimesMissing()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(HourlyRoom(10));
            mapper.Setup(m => m.Map<Booking>(It.IsAny<BookingDTO>()))
                  .Returns(new Booking { CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today });

            var dto = new BookingDTO
            {
                BookingType  = BookingType.Hourly,
                CheckInDate  = DateTime.Today,
                CheckOutDate = DateTime.Today,
                CheckInTime  = null,   // missing
                CheckOutTime = null,   // missing
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };

            var sut    = BuildController(repo, roomRepo, mapper);
            var result = await sut.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // =====================================================================
        //  TEST 10 — NightStay: CheckInTime / CheckOutTime stripped server-side
        // =====================================================================

        [Fact]
        public async Task Create_NightStay_StripTimesServerSide()
        {
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();
            StubEmptyBookings(repo);

            var room = new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m };
            roomRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(room);

            var entity = new Booking
            {
                CheckInDate  = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(2),
                CheckInTime  = TimeSpan.FromHours(10),
                CheckOutTime = TimeSpan.FromHours(11)
            };
            var dto = new BookingDTO
            {
                BookingType  = BookingType.NightStay,
                CheckInDate  = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(2),
                CheckInTime  = "10:00",
                CheckOutTime = "11:00",
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };
            mapper.Setup(m => m.Map<Booking>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            var sut    = BuildController(repo, roomRepo, mapper);
            await sut.Create(dto);

            // Server must have nulled the time fields on non-hourly bookings
            Assert.Null(entity.CheckInTime);
            Assert.Null(entity.CheckOutTime);
        }
    }
}
