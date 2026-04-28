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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelManagementSystem.Tests.Controllers
{
    public class TenantCurrencyTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static Mock<UserManager<User>> BuildUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private static TenantsController BuildController(
            Mock<IRepository<Tenant>> repo,
            Mock<IRepository<UserTenant>> utRepo,
            Mock<IMapper> mapper)
        {
            return new TenantsController(repo.Object, utRepo.Object, BuildUserManagerMock().Object, mapper.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        // ── GetDefaultCurrency ────────────────────────────────────────────────

        [Fact]
        public async Task GetDefaultCurrency_ActiveTenantExists_ReturnsOkWithCurrencyAndLocale()
        {
            var repo   = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Tenant>
            {
                new() { Id = 1, Name = "Default", Subdomain = "default", IsActive = true,
                        CurrencyCode = "INR", Locale = "en-IN", Plan = TenantPlan.Enterprise }
            });

            var sut    = BuildController(repo, utRepo, mapper);
            var result = await sut.GetDefaultCurrency() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            var dto = result.Value as DefaultCurrencyDTO;
            Assert.NotNull(dto);
            Assert.Equal("INR",   dto!.CurrencyCode);
            Assert.Equal("en-IN", dto.Locale);
        }

        [Fact]
        public async Task GetDefaultCurrency_NoTenants_ReturnsFallbackINR()
        {
            var repo   = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Tenant>());

            var sut    = BuildController(repo, utRepo, mapper);
            var result = await sut.GetDefaultCurrency() as OkObjectResult;

            Assert.NotNull(result);
            var dto = result!.Value as DefaultCurrencyDTO;
            Assert.NotNull(dto);
            Assert.Equal("INR",   dto!.CurrencyCode);
            Assert.Equal("en-IN", dto.Locale);
        }

        [Fact]
        public void GetDefaultCurrency_IsDecoratedWithAllowAnonymous()
        {
            var method = typeof(TenantsController)
                .GetMethod(nameof(TenantsController.GetDefaultCurrency));
            var attr = method?.GetCustomAttribute<AllowAnonymousAttribute>();
            Assert.NotNull(attr);
        }

        // ── Tenant model ──────────────────────────────────────────────────────

        [Fact]
        public void Tenant_HasCurrencyCodeProperty_WithDefaultINR()
        {
            var tenant = new Tenant();
            Assert.Equal("INR", tenant.CurrencyCode);
        }

        [Fact]
        public void Tenant_HasLocaleProperty_WithDefaultEnIN()
        {
            var tenant = new Tenant();
            Assert.Equal("en-IN", tenant.Locale);
        }

        [Fact]
        public void Tenant_CurrencyCode_CanBeOverridden()
        {
            var tenant = new Tenant { CurrencyCode = "USD", Locale = "en-US" };
            Assert.Equal("USD", tenant.CurrencyCode);
            Assert.Equal("en-US", tenant.Locale);
        }

        // ── Room model ────────────────────────────────────────────────────────

        [Fact]
        public void Room_HasPricePerNightProperty()
        {
            var room = new Room { PricePerNight = 1500.75m };
            Assert.Equal(1500.75m, room.PricePerNight);
        }

        [Fact]
        public void Room_PricePerNight_DefaultsToZero()
        {
            var room = new Room();
            Assert.Equal(0m, room.PricePerNight);
        }

        // ── DefaultCurrencyDTO ────────────────────────────────────────────────

        [Fact]
        public void DefaultCurrencyDTO_CanBeConstructed()
        {
            var dto = new DefaultCurrencyDTO { CurrencyCode = "EUR", Locale = "de-DE" };
            Assert.Equal("EUR",   dto.CurrencyCode);
            Assert.Equal("de-DE", dto.Locale);
        }
    }
}
