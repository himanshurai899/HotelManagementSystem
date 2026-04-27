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
    public class CompanyProfileControllerTests
    {
        // ---------- helpers ----------

        private static CompanyProfileController BuildController(
            Mock<IRepository<CompanyProfile>> repo,
            Mock<IMapper> mapper,
            Mock<IFileStorageService> storage,
            params string[] roles)
        {
            var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            var controller = new CompanyProfileController(repo.Object, mapper.Object, storage.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        private static MethodInfo GetAction(string name) =>
            typeof(CompanyProfileController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Action {name} not found");

        private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttrs(string action) =>
            GetAction(action).GetCustomAttributes<AuthorizeAttribute>(inherit: true);

        // =====================================================================
        //  AUTHORIZATION METADATA
        // =====================================================================

        [Theory]
        [InlineData(nameof(CompanyProfileController.Get))]
        [InlineData(nameof(CompanyProfileController.Upsert))]
        [InlineData(nameof(CompanyProfileController.UploadLogo))]
        public void Action_RequiresAdministratorRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("Administrator", roles);
        }

        [Theory]
        [InlineData(nameof(CompanyProfileController.Upsert))]
        [InlineData(nameof(CompanyProfileController.UploadLogo))]
        public void WriteActions_DoNotAllowCustomerRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.DoesNotContain("Customer", roles);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Get
        // =====================================================================

        [Fact]
        public async Task Get_ReturnsOk_WhenProfileExists()
        {
            var repo = new Mock<IRepository<CompanyProfile>>();
            var mapper = new Mock<IMapper>();
            var storage = new Mock<IFileStorageService>();

            var profile = new CompanyProfile { Id = 1, CompanyName = "Grand Hotel" };
            var dto = new CompanyProfileDTO { Id = 1, CompanyName = "Grand Hotel" };

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CompanyProfile> { profile });
            mapper.Setup(m => m.Map<CompanyProfileDTO>(profile)).Returns(dto);

            var sut = BuildController(repo, mapper, storage, "Administrator");

            var result = await sut.Get() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal(dto, result.Value);
        }

        [Fact]
        public async Task Get_ReturnsNoContent_WhenNoProfileExists()
        {
            var repo = new Mock<IRepository<CompanyProfile>>();
            var mapper = new Mock<IMapper>();
            var storage = new Mock<IFileStorageService>();

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CompanyProfile>());

            var sut = BuildController(repo, mapper, storage, "Administrator");

            var result = await sut.Get();

            Assert.IsType<NoContentResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Upsert (Create when none exists)
        // =====================================================================

        [Fact]
        public async Task Upsert_CreatesNewProfile_WhenNoneExists()
        {
            var repo = new Mock<IRepository<CompanyProfile>>();
            var mapper = new Mock<IMapper>();
            var storage = new Mock<IFileStorageService>();

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CompanyProfile>());
            var dto = new CompanyProfileDTO { CompanyName = "New Hotel", PrimaryColor = "#FF0000" };
            var entity = new CompanyProfile { CompanyName = "New Hotel" };
            mapper.Setup(m => m.Map<CompanyProfile>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<CompanyProfileDTO>(entity)).Returns(dto);

            var sut = BuildController(repo, mapper, storage, "Administrator");

            var result = await sut.Upsert(dto) as OkObjectResult;

            Assert.NotNull(result);
            repo.Verify(r => r.AddAsync(entity), Times.Once);
            repo.Verify(r => r.UpdateAsync(It.IsAny<CompanyProfile>()), Times.Never);
        }

        [Fact]
        public async Task Upsert_UpdatesExistingProfile_WhenOneExists()
        {
            var repo = new Mock<IRepository<CompanyProfile>>();
            var mapper = new Mock<IMapper>();
            var storage = new Mock<IFileStorageService>();

            var existing = new CompanyProfile { Id = 1, CompanyName = "Old Hotel" };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CompanyProfile> { existing });

            var dto = new CompanyProfileDTO { CompanyName = "Updated Hotel", PrimaryColor = "#0000FF" };
            mapper.Setup(m => m.Map<CompanyProfileDTO>(existing)).Returns(dto);

            var sut = BuildController(repo, mapper, storage, "Administrator");

            var result = await sut.Upsert(dto) as OkObjectResult;

            Assert.NotNull(result);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
            repo.Verify(r => r.AddAsync(It.IsAny<CompanyProfile>()), Times.Never);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — UploadLogo
        // =====================================================================

        [Fact]
        public async Task UploadLogo_ReturnsOk_WithLogoUrl()
        {
            var repo = new Mock<IRepository<CompanyProfile>>();
            var mapper = new Mock<IMapper>();
            var storage = new Mock<IFileStorageService>();

            var existing = new CompanyProfile { Id = 1, CompanyName = "Grand Hotel" };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CompanyProfile> { existing });

            var logoUrl = "uploads/GrandHotel/assets/logo.png";
            storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(logoUrl);

            // Build a fake IFormFile
            var content = System.Text.Encoding.UTF8.GetBytes("fake-image");
            var ms = new MemoryStream(content);
            var formFile = new FormFile(ms, 0, content.Length, "logo", "logo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var sut = BuildController(repo, mapper, storage, "Administrator");

            var result = await sut.UploadLogo(formFile) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png"), Times.Once);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
            Assert.Equal(logoUrl, existing.LogoUrl);
        }

        [Fact]
        public async Task UploadLogo_ReturnsBadRequest_WhenNoFileProvided()
        {
            var repo = new Mock<IRepository<CompanyProfile>>();
            var mapper = new Mock<IMapper>();
            var storage = new Mock<IFileStorageService>();

            var sut = BuildController(repo, mapper, storage, "Administrator");

            var result = await sut.UploadLogo(null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
