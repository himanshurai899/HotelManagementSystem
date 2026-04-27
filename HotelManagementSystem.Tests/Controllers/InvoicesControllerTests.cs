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
    public class InvoicesControllerTests
    {
        // ---------- helpers ----------

        private static InvoicesController BuildController(
            Mock<IRepository<Invoice>> repo,
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

            var companyRepo = new Mock<IRepository<CompanyProfile>>();
            companyRepo.Setup(r => r.GetAllAsync())
                       .ReturnsAsync(new List<CompanyProfile>());

            var controller = new InvoicesController(repo.Object, mapper.Object, companyRepo.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        private static MethodInfo GetAction(string name) =>
            typeof(InvoicesController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Action {name} not found");

        private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttrs(string action) =>
            GetAction(action).GetCustomAttributes<AuthorizeAttribute>(inherit: true);

        // =====================================================================
        //  AUTHORIZATION METADATA
        // =====================================================================

        [Theory]
        [InlineData(nameof(InvoicesController.GetAll))]
        [InlineData(nameof(InvoicesController.GetById))]
        [InlineData(nameof(InvoicesController.GetMyInvoices))]
        [InlineData(nameof(InvoicesController.Create))]
        [InlineData(nameof(InvoicesController.Update))]
        [InlineData(nameof(InvoicesController.Delete))]
        public void Action_AllowsAdministratorRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("Administrator", roles);
        }

        [Theory]
        [InlineData(nameof(InvoicesController.GetMyInvoices))]
        [InlineData(nameof(InvoicesController.GetById))]
        public void Action_AllowsCustomerRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("Customer", roles);
        }

        [Theory]
        [InlineData(nameof(InvoicesController.Create))]
        [InlineData(nameof(InvoicesController.Update))]
        [InlineData(nameof(InvoicesController.Delete))]
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
        public async Task GetAll_ReturnsOkWithMappedInvoices()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            var data = new List<Invoice> { new() { Id = 1, BookingId = 1 } };
            var dto = new List<InvoiceDTO> { new() { Id = 1, BookingId = 1, GuestName = "John" } };

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(data);
            mapper.Setup(m => m.Map<IEnumerable<InvoiceDTO>>(data)).Returns(dto);

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
        public async Task GetById_Admin_ReturnsOk_ForAnyInvoice()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            var invoice = new Invoice { Id = 1, BookingId = 1, Booking = new Booking { UserId = 99 } };
            var dto = new InvoiceDTO { Id = 1 };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invoice);
            mapper.Setup(m => m.Map<InvoiceDTO>(invoice)).Returns(dto);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.GetById(1) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Invoice?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.GetById(42);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_Customer_GettingOthersInvoice_ReturnsForbid()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            var invoice = new Invoice
            {
                Id = 1,
                BookingId = 5,
                Booking = new Booking { Id = 5, UserId = 999 } // belongs to user 999
            };
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(invoice);

            var sut = BuildController(repo, mapper, userId: 1, "Customer"); // logged in as user 1

            var result = await sut.GetById(1);

            Assert.IsType<ForbidResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetMyInvoices
        // =====================================================================

        [Fact]
        public async Task GetMyInvoices_ReturnsOnlyCurrentUsersInvoices()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();

            var userInvoice  = new Invoice { Id = 1, Booking = new Booking { UserId = 7 } };
            var otherInvoice = new Invoice { Id = 2, Booking = new Booking { UserId = 99 } };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Invoice> { userInvoice, otherInvoice });
            mapper.Setup(m => m.Map<IEnumerable<InvoiceDTO>>(It.IsAny<IEnumerable<Invoice>>()))
                  .Returns((IEnumerable<Invoice> src) => src.Select(i => new InvoiceDTO { Id = i.Id }));

            var sut = BuildController(repo, mapper, userId: 7, "Customer");

            var result = await sut.GetMyInvoices() as OkObjectResult;

            Assert.NotNull(result);
            var items = Assert.IsAssignableFrom<IEnumerable<InvoiceDTO>>(result!.Value);
            Assert.Single(items);
            Assert.Equal(1, items.First().Id);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Create
        // =====================================================================

        [Fact]
        public async Task Create_ReturnsCreatedAt_OnSuccess()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            var dto = new InvoiceDTO { BookingId = 1, TotalAmount = 500 };
            var entity = new Invoice { BookingId = 1, TotalAmount = 500 };
            var returnDto = new InvoiceDTO { Id = 1, BookingId = 1 };

            mapper.Setup(m => m.Map<Invoice>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<InvoiceDTO>(entity)).Returns(returnDto);

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
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            var existing = new Invoice { Id = 3, BookingId = 1 };
            repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Update(3, new InvoiceDTO { Id = 3 });

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Invoice?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Update(99, new InvoiceDTO { Id = 99 });

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Delete
        // =====================================================================

        [Fact]
        public async Task Delete_ReturnsNoContent_OnSuccess()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Invoice { Id = 5 });

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Delete(5);

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenMissing()
        {
            var repo = new Mock<IRepository<Invoice>>();
            var mapper = new Mock<IMapper>();
            repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Invoice?)null);

            var sut = BuildController(repo, mapper, userId: 1, "Administrator");

            var result = await sut.Delete(404);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
