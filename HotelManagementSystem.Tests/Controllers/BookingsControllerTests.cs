using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Enums;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelManagementSystem.Tests.Controllers
{
    public class BookingsControllerTests
    {
        // ---------- helpers ----------

        private static BookingsController BuildController(
            Mock<IRepository<Booking>> repo,
            Mock<IMapper> mapper,
            int userId,
            params string[] roles)
            => BuildController(repo, mapper, new Mock<IRepository<Room>>(), userId, roles);

        private static BookingsController BuildController(
            Mock<IRepository<Booking>> repo,
            Mock<IMapper> mapper,
            Mock<IRepository<Room>> roomRepo,
            int userId,
            params string[] roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            var controller = new BookingsController(repo.Object, roomRepo.Object, mapper.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        private static MethodInfo GetAction(string name) =>
            typeof(BookingsController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Action {name} not found");

        private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttrs(string action) =>
            GetAction(action).GetCustomAttributes<AuthorizeAttribute>(inherit: true);

        // =====================================================================
        //  AUTHORIZATION METADATA — SuperAdmin must be allowed on every action
        // =====================================================================

        [Theory]
        [InlineData(nameof(BookingsController.GetAll))]
        [InlineData(nameof(BookingsController.GetMyBookings))]
        [InlineData(nameof(BookingsController.GetById))]
        [InlineData(nameof(BookingsController.Create))]
        [InlineData(nameof(BookingsController.Update))]
        [InlineData(nameof(BookingsController.Delete))]
        public void Action_AllowsSuperAdminRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("SuperAdmin", roles);
        }

        [Theory]
        [InlineData(nameof(BookingsController.GetAll))]
        [InlineData(nameof(BookingsController.Update))]
        [InlineData(nameof(BookingsController.Delete))]
        public void Action_AllowsAdministratorRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("Administrator", roles);
        }

        // =====================================================================
        //  AUTHORIZATION METADATA — ManageBookings policy stacked on modify ops
        // =====================================================================

        [Theory]
        [InlineData(nameof(BookingsController.Update))]
        [InlineData(nameof(BookingsController.Delete))]
        public void ModifyAction_RequiresManageBookingsPolicy(string actionName)
        {
            var policies = GetAuthorizeAttrs(actionName)
                .Select(a => a.Policy)
                .Where(p => !string.IsNullOrEmpty(p));

            Assert.Contains("ManageBookings", policies);
        }

        [Fact]
        public void Create_DoesNotRequireManageBookingsPolicy()
        {
            // Per Gate 0 decision (#2): POST stays open to any Administrator/Customer/SuperAdmin.
            var policies = GetAuthorizeAttrs(nameof(BookingsController.Create))
                .Select(a => a.Policy);

            Assert.DoesNotContain("ManageBookings", policies);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS
        // =====================================================================

        [Fact]
        public async Task GetAll_ReturnsOkWithMappedBookings()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            var data = new List<Booking> { new() { Id = 1, UserId = 1 } };
            var dto = new List<BookingDTO> { new() { Id = 1, UserId = 1 } };

            repo.Setup(r => r.GetAllWithIncludesAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(data);
            mapper.Setup(m => m.Map<IEnumerable<BookingDTO>>(data)).Returns(dto);

            var sut = BuildController(repo, mapper, userId: 99, "SuperAdmin");

            var result = await sut.GetAll() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal(dto, result.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Booking?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.GetById(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_Customer_GettingOthersBooking_ReturnsForbid()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            // Phase 12a — controller now uses GetByIdWithIncludesAsync to load BookingRooms.
            repo.Setup(r => r.GetByIdWithIncludesAsync(
                    1,
                    It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(new Booking { Id = 1, UserId = 999 });

            var sut = BuildController(repo, mapper, userId: 1, "Customer");

            var result = await sut.GetById(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_OverwritesUserIdFromToken()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();

            // Phase 12a — multi-room: spoofed UserId in DTO must be overwritten from the JWT.
            var dtoIn = new BookingDTO
            {
                UserId       = 999,
                BookingType  = BookingType.NightStay,
                CheckInDate  = new DateTime(2026, 6, 1),
                CheckOutDate = new DateTime(2026, 6, 3),
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 5 } }
            };
            var entity = new Booking { UserId = 999 };

            mapper.Setup(m => m.Map<Booking>(dtoIn)).Returns(entity);
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            // Stub: empty existing-bookings list so the overlap guard passes.
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            // Stub: room 5 must resolve so the controller can snapshot PriceAtBooking.
            var roomRepo = new Mock<IRepository<Room>>();
            roomRepo.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(new Room { Id = 5, RoomNumber = "R-5", PricePerNight = 100m });

            var sut = BuildController(repo, mapper, roomRepo, userId: 7, "Customer");

            var result = await sut.Create(dtoIn) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(7, entity.UserId); // overwritten from token, not 999
            repo.Verify(r => r.AddAsync(entity), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNoContent_OnSuccess()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            var existing = new Booking { Id = 5, UserId = 1 };
            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            var sut = BuildController(repo, mapper, userId: 1, "SuperAdmin");

            var result = await sut.Update(5, new BookingDTO { Id = 5 });

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Update(99, new BookingDTO { Id = 99 });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_OnSuccess()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(new Booking { Id = 8 });

            var sut = BuildController(repo, mapper, userId: 1, "SuperAdmin");

            var result = await sut.Delete(8);

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.DeleteAsync(8), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Booking?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Delete(404);

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  PHASE 12b — History-Based Rebooking
        // =====================================================================

        [Fact]
        public async Task GetRebookHistory_Returns200_WithSuggestions()
        {
            // Arrange — one CheckedOut booking for the current user (userId=1)
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();

            var bookingRooms = new List<BookingRoom> { new() { RoomId = 10 } };
            var booking = new Booking
            {
                Id          = 1,
                UserId      = 1,
                Status      = BookingStatus.Completed,
                CheckInDate = new DateTime(2026, 1, 1),
                CheckOutDate= new DateTime(2026, 1, 3),
                TotalPrice  = 200m,
                BookingRooms= bookingRooms
            };

            repo.Setup(r => r.GetAllWithIncludesAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(new List<Booking> { booking });

            roomRepo.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(new Room { Id = 10, RoomNumber = "101" });

            var sut = BuildController(repo, mapper, roomRepo, userId: 1, "Customer");

            // Act
            var result = await sut.GetRebookHistory() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            var suggestions = Assert.IsAssignableFrom<IEnumerable<HotelManagementSystem.Shared.DTOs.RebookSuggestionDTO>>(result.Value);
            Assert.Single(suggestions);
            var s = suggestions.First();
            Assert.Equal(1, s.OriginalBookingId);
            Assert.Contains(10, s.RoomIds);
            Assert.Equal(2, s.DurationDays);
            Assert.Equal(200m, s.TotalPrice);
        }

        [Fact]
        public async Task GetRebookHistory_ReturnsEmpty_WhenNoCheckedOutBookings()
        {
            // Arrange — user has only a Pending booking
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();

            repo.Setup(r => r.GetAllWithIncludesAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(new List<Booking>
                {
                    new() { Id = 1, UserId = 1, Status = BookingStatus.Pending, BookingRooms = [] }
                });

            var sut = BuildController(repo, mapper, roomRepo, userId: 1, "Customer");

            // Act
            var result = await sut.GetRebookHistory() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var suggestions = Assert.IsAssignableFrom<IEnumerable<HotelManagementSystem.Shared.DTOs.RebookSuggestionDTO>>(result.Value);
            Assert.Empty(suggestions);
        }

        [Fact]
        public async Task GetRebookHistory_DeduplicatesIdenticalRoomSets()
        {
            // Arrange — two CheckedOut bookings with the same room (should yield only 1 suggestion)
            var repo     = new Mock<IRepository<Booking>>();
            var roomRepo = new Mock<IRepository<Room>>();
            var mapper   = new Mock<IMapper>();

            var rooms = new List<BookingRoom> { new() { RoomId = 5 } };
            var bookings = new List<Booking>
            {
                new() { Id=1, UserId=1, Status=BookingStatus.Completed,
                        CheckInDate=new DateTime(2026,3,1), CheckOutDate=new DateTime(2026,3,2),
                        TotalPrice=100m, BookingRooms=rooms },
                new() { Id=2, UserId=1, Status=BookingStatus.Completed,
                        CheckInDate=new DateTime(2026,2,1), CheckOutDate=new DateTime(2026,2,2),
                        TotalPrice=100m, BookingRooms=rooms }
            };

            repo.Setup(r => r.GetAllWithIncludesAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
                .ReturnsAsync(bookings);

            roomRepo.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(new Room { Id = 5, RoomNumber = "205" });

            var sut = BuildController(repo, mapper, roomRepo, userId: 1, "Customer");

            // Act
            var result = await sut.GetRebookHistory() as OkObjectResult;

            // Assert — only 1 suggestion (deduplicated); most recent stay is booking id=1
            Assert.NotNull(result);
            var suggestions = Assert.IsAssignableFrom<IEnumerable<HotelManagementSystem.Shared.DTOs.RebookSuggestionDTO>>(result.Value)
                .ToList();
            Assert.Single(suggestions);
            Assert.Equal(1, suggestions[0].OriginalBookingId);
        }

        [Fact]
        public void GetRebookHistory_AllowsSuperAdminRole()
        {
            var roles = GetAuthorizeAttrs(nameof(BookingsController.GetRebookHistory))
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));
            Assert.Contains("SuperAdmin", roles);
        }
    }
}
