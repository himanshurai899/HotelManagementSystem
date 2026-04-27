using System.Security.Claims;
using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.API.Models;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelManagementSystem.Tests.Controllers
{
    public class ProfileControllerTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static Mock<UserManager<User>> BuildUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private static ProfileController BuildController(
            Mock<UserManager<User>> userManager,
            Mock<IFileStorageService> fileStorage,
            int userId = 1,
            string userName = "testuser")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new("sub", userName)
            };
            var identity  = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            return new ProfileController(userManager.Object, fileStorage.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
        }

        // =====================================================================
        //  GET /api/profile
        // =====================================================================

        [Fact]
        public async Task GetProfile_ReturnsOk_WhenUserExists()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1, UserName = "testuser", Email = "t@t.com", FirstName = "Test", LastName = "User" };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });
            um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            var result = await BuildController(um, fs).GetProfile();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_ReturnsNotFound_WhenUserMissing()
        {
            var um = BuildUserManagerMock();
            var fs = new Mock<IFileStorageService>();
            um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((User?)null);

            var result = await BuildController(um, fs, userId: 99).GetProfile();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetProfile_ResponseContainsFirstAndLastName()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1, UserName = "testuser", FirstName = "Ada", LastName = "Lovelace", Email = "ada@test.com" };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            var result  = await BuildController(um, fs).GetProfile() as OkObjectResult;
            var dto     = result!.Value as UserDTO;

            Assert.Equal("Ada",      dto!.FirstName);
            Assert.Equal("Lovelace", dto.LastName);
        }

        // =====================================================================
        //  PUT /api/profile
        // =====================================================================

        [Fact]
        public async Task UpdateProfile_ReturnsOk_WhenSuccessful()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1, UserName = "testuser", Email = "t@t.com" };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            var dto    = new UpdateProfileDTO { UserName = "newname", FirstName = "New", LastName = "Name", Email = "new@t.com", PhoneNumber = "1234567890" };
            var result = await BuildController(um, fs).UpdateProfile(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsBadRequest_WhenIdentityFails()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1 };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.UpdateAsync(user)).ReturnsAsync(
                IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            var result = await BuildController(um, fs).UpdateProfile(new UpdateProfileDTO());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // =====================================================================
        //  POST /api/profile/photo
        // =====================================================================

        [Fact]
        public async Task UploadPhoto_ReturnsBadRequest_WhenNoFile()
        {
            var result = await BuildController(BuildUserManagerMock(), new Mock<IFileStorageService>())
                             .UploadPhoto(new PhotoUploadRequest { Photo = null! });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadPhoto_ReturnsBadRequest_WhenFileTooLarge()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(3 * 1024 * 1024); // 3 MB > 2 MB limit
            fileMock.Setup(f => f.FileName).Returns("photo.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            var result = await BuildController(BuildUserManagerMock(), new Mock<IFileStorageService>())
                             .UploadPhoto(new PhotoUploadRequest { Photo = fileMock.Object });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadPhoto_ReturnsBadRequest_WhenInvalidContentType()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(500 * 1024);
            fileMock.Setup(f => f.FileName).Returns("photo.gif");
            fileMock.Setup(f => f.ContentType).Returns("image/gif");

            var result = await BuildController(BuildUserManagerMock(), new Mock<IFileStorageService>())
                             .UploadPhoto(new PhotoUploadRequest { Photo = fileMock.Object });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadPhoto_ReturnsOk_WhenValidFile()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1 };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            fs.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync("/uploads/test.jpg");

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(500 * 1024);
            fileMock.Setup(f => f.FileName).Returns("photo.jpg");
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

            var result = await BuildController(um, fs).UploadPhoto(new PhotoUploadRequest { Photo = fileMock.Object });
            Assert.IsType<OkObjectResult>(result);
        }

        // =====================================================================
        //  PUT /api/profile/change-password
        // =====================================================================

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_WhenPasswordsMismatch()
        {
            var dto = new ChangePasswordDTO
            {
                CurrentPassword    = "Old@1",
                NewPassword        = "New@123",
                ConfirmNewPassword = "Different@123"
            };
            var result = await BuildController(BuildUserManagerMock(), new Mock<IFileStorageService>())
                             .ChangePassword(dto);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsOk_WhenSuccessful()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1, UserName = "testuser" };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.ChangePasswordAsync(user, "Old@1", "New@1A")).ReturnsAsync(IdentityResult.Success);

            var dto = new ChangePasswordDTO { CurrentPassword = "Old@1", NewPassword = "New@1A", ConfirmNewPassword = "New@1A" };
            var result = await BuildController(um, fs).ChangePassword(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_WhenCurrentPasswordWrong()
        {
            var um   = BuildUserManagerMock();
            var fs   = new Mock<IFileStorageService>();
            var user = new User { Id = 1 };

            um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            um.Setup(m => m.ChangePasswordAsync(user, "Wrong@1", "New@1A")).ReturnsAsync(
                IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));

            var dto = new ChangePasswordDTO { CurrentPassword = "Wrong@1", NewPassword = "New@1A", ConfirmNewPassword = "New@1A" };
            var result = await BuildController(um, fs).ChangePassword(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
