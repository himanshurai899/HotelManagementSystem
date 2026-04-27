using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace HotelManagementSystem.Tests.Controllers
{
    public class RolesControllerTests
    {
        private static Mock<RoleManager<Role>> CreateRoleManagerMock()
        {
            var store = new Mock<IRoleStore<Role>>();
            return new Mock<RoleManager<Role>>(store.Object, null!, null!, null!, null!);
        }

        [Fact]
        public async Task GetAll_Returns200_WithRoleList()
        {
            var rm = CreateRoleManagerMock();
            rm.Setup(x => x.Roles).Returns(new List<Role> { new() { Id = 4, Name = "SuperAdmin" } }.AsQueryable());
            rm.Setup(x => x.GetClaimsAsync(It.IsAny<Role>())).ReturnsAsync(new List<Claim>());
            var result = await new RolesController(rm.Object).GetAll() as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetAll_Returns_SuperAdmin_InList()
        {
            var rm = CreateRoleManagerMock();
            rm.Setup(x => x.Roles).Returns(new List<Role> { new() { Id = 4, Name = "SuperAdmin" } }.AsQueryable());
            rm.Setup(x => x.GetClaimsAsync(It.IsAny<Role>())).ReturnsAsync(new List<Claim>());
            var result = (await new RolesController(rm.Object).GetAll() as OkObjectResult)!.Value;
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            Assert.Contains("SuperAdmin", json);
        }

        [Fact]
        public async Task Create_Returns201_OnSuccess()
        {
            var rm = CreateRoleManagerMock();
            rm.Setup(x => x.RoleExistsAsync("TestRole")).ReturnsAsync(false);
            rm.Setup(x => x.CreateAsync(It.IsAny<Role>())).ReturnsAsync(IdentityResult.Success);
            var result = await new RolesController(rm.Object).Create("TestRole") as CreatedAtActionResult;
            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task Create_Returns400_WhenNameEmpty()
        {
            var rm = CreateRoleManagerMock();
            var result = await new RolesController(rm.Object).Create("") as BadRequestObjectResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Delete_Returns204_OnSuccess()
        {
            var rm = CreateRoleManagerMock();
            var role = new Role { Id = 99, Name = "Temp" };
            rm.Setup(x => x.FindByIdAsync("99")).ReturnsAsync(role);
            rm.Setup(x => x.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);
            var result = await new RolesController(rm.Object).Delete(99) as NoContentResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Delete_Returns404_WhenRoleNotFound()
        {
            var rm = CreateRoleManagerMock();
            rm.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((Role?)null);
            var result = await new RolesController(rm.Object).Delete(999) as NotFoundResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPermissions_Returns200_WithClaimList()
        {
            var rm = CreateRoleManagerMock();
            var role = new Role { Id = 1, Name = "Administrator" };
            rm.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(role);
            rm.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim> { new Claim("Permission", "ManageUsers") });
            var result = await new RolesController(rm.Object).GetPermissions(1) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task AddPermission_Returns204_OnSuccess()
        {
            var rm = CreateRoleManagerMock();
            var role = new Role { Id = 1, Name = "Administrator" };
            rm.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(role);
            rm.Setup(x => x.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
            var result = await new RolesController(rm.Object).AddPermission(1, "ManageRooms") as NoContentResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RemovePermission_Returns204_OnSuccess()
        {
            var rm = CreateRoleManagerMock();
            var role = new Role { Id = 1, Name = "Administrator" };
            rm.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(role);
            rm.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim> { new Claim("Permission", "ManageRooms") });
            rm.Setup(x => x.RemoveClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
            var result = await new RolesController(rm.Object).RemovePermission(1, "ManageRooms") as NoContentResult;
            Assert.NotNull(result);
        }
    }
}
