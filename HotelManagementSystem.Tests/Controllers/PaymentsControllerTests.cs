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
    public class PaymentsControllerTests
    {
        // ---------- helpers ----------

        private static PaymentsController BuildController(
            Mock<IRepository<Payment>> repo,
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

            var controller = new PaymentsController(repo.Object, mapper.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        private static MethodInfo GetAction(string name) =>
            typeof(PaymentsController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Action {name} not found");

        private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttrs(string action) =>
            GetAction(action).GetCustomAttributes<AuthorizeAttribute>(inherit: true);

        // =====================================================================
        //  AUTHORIZATION METADATA
        // =====================================================================

        [Theory]
        [InlineData(nameof(PaymentsController.GetAll))]
        [InlineData(nameof(PaymentsController.GetById))]
        [InlineData(nameof(PaymentsController.GetMyPayments))]
        [InlineData(nameof(PaymentsController.Create))]
        [InlineData(nameof(PaymentsController.Update))]
        [InlineData(nameof(PaymentsController.Delete))]
        public void Action_AllowsAdministratorRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("Administrator", roles);
        }

        [Theory]
        [InlineData(nameof(PaymentsController.GetMyPayments))]
        [InlineData(nameof(PaymentsController.GetById))]
        public void Action_AllowsCustomerRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("Customer", roles);
        }

        [Theory]
        [InlineData(nameof(PaymentsController.Create))]
        [InlineData(nameof(PaymentsController.Update))]
        [InlineData(nameof(PaymentsController.Delete))]
        public void AdminOnlyActions_DoNotAllowCustomerRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.DoesNotContain("Customer", roles);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetAll
        // =====================================================================

        [Fact]
        public async Task GetAll_ReturnsOkWithMappedPayments()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            var data = new List<Payment> { new() { Id = 1, BookingId = 1, Amount = 200 } };
            var dto = new List<PaymentDTO> { new() { Id = 1, BookingId = 1, Amount = 200 } };

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(data);
            mapper.Setup(m => m.Map<IEnumerable<PaymentDTO>>(data)).Returns(dto);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.GetAll() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal(dto, result.Value);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetById
        // =====================================================================

        [Fact]
        public async Task GetById_Admin_ReturnsOk_ForAnyPayment()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            var payment = new Payment { Id = 1, BookingId = 1, Booking = new Booking { UserId = 99 } };
            var dto = new PaymentDTO { Id = 1 };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            mapper.Setup(m => m.Map<PaymentDTO>(payment)).Returns(dto);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.GetById(1) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Payment?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.GetById(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_Customer_GettingOthersPayment_ReturnsForbid()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            var payment = new Payment
            {
                Id = 1,
                BookingId = 5,
                Booking = new Booking { Id = 5, UserId = 999 } // belongs to user 999
            };
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

            var sut = BuildController(repo, mapper, userId: 1, "Customer"); // logged in as user 1

            var result = await sut.GetById(1);

            Assert.IsType<ForbidResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetMyPayments
        // =====================================================================

        [Fact]
        public async Task GetMyPayments_ReturnsOnlyCurrentUsersPayments()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();

            var myPayment    = new Payment { Id = 1, Booking = new Booking { UserId = 7 } };
            var otherPayment = new Payment { Id = 2, Booking = new Booking { UserId = 99 } };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { myPayment, otherPayment });
            mapper.Setup(m => m.Map<IEnumerable<PaymentDTO>>(It.IsAny<IEnumerable<Payment>>()))
                  .Returns((IEnumerable<Payment> src) => src.Select(p => new PaymentDTO { Id = p.Id }));

            var sut = BuildController(repo, mapper, userId: 7, "Customer");

            var result = await sut.GetMyPayments() as OkObjectResult;

            Assert.NotNull(result);
            var items = Assert.IsAssignableFrom<IEnumerable<PaymentDTO>>(result!.Value);
            Assert.Single(items);
            Assert.Equal(1, items.First().Id);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Create
        // =====================================================================

        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            var dto = new PaymentDTO { BookingId = 1, Amount = 300 };
            var entity = new Payment { BookingId = 1, Amount = 300 };
            var returnDto = new PaymentDTO { Id = 1, BookingId = 1 };

            mapper.Setup(m => m.Map<Payment>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<PaymentDTO>(entity)).Returns(returnDto);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Create(dto) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
            repo.Verify(r => r.AddAsync(entity), Times.Once);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Update
        // =====================================================================

        [Fact]
        public async Task Update_ReturnsNoContent_OnSuccess()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            var existing = new Payment { Id = 3, BookingId = 1 };
            repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Update(3, new PaymentDTO { Id = 3 });

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Update(99, new PaymentDTO { Id = 99 });

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Delete
        // =====================================================================

        [Fact]
        public async Task Delete_ReturnsNoContent_OnSuccess()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Payment { Id = 5 });

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Delete(5);

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Payment>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Payment?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Delete(404);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
