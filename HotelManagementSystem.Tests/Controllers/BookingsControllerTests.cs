using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.Shared.DTOs;
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
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            var controller = new BookingsController(repo.Object, mapper.Object)
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

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(data);
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
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { Id = 1, UserId = 999 });

            var sut = BuildController(repo, mapper, userId: 1, "Customer");

            var result = await sut.GetById(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_OverwritesUserIdFromToken()
        {
            var repo = new Mock<IRepository<Booking>>();
            var mapper = new Mock<IMapper>();

            var dtoIn = new BookingDTO { UserId = 999, RoomId = 5 }; // attempted spoof
            var entity = new Booking { UserId = 999, RoomId = 5 };

            mapper.Setup(m => m.Map<Booking>(dtoIn)).Returns(entity);
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            var sut = BuildController(repo, mapper, userId: 7, "Customer");

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
    }
}
