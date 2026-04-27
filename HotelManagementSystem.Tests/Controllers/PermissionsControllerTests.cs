using HotelManagementSystem.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HotelManagementSystem.Tests.Controllers
{
    public class PermissionsControllerTests
    {
        private readonly PermissionsController _sut = new();

        [Fact]
        public void GetAll_Returns200() 
        {
            var result = _sut.GetAll() as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public void GetAll_ReturnsNonEmptyList()
        {
            var result = (_sut.GetAll() as OkObjectResult)!.Value;
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            Assert.NotEmpty(json);
        }

        [Fact]
        public void GetAll_ContainsManageUsers()
        {
            var result = (_sut.GetAll() as OkObjectResult)!.Value;
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            Assert.Contains("ManageUsers", json);
        }

        [Fact]
        public void GetAll_ContainsManagePermissions()
        {
            var result = (_sut.GetAll() as OkObjectResult)!.Value;
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            Assert.Contains("ManagePermissions", json);
        }

        [Fact]
        public void GetAll_Contains11OrMorePermissions()
        {
            var result = (_sut.GetAll() as OkObjectResult)!.Value;
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            var count = System.Text.RegularExpressions.Regex.Matches(json, "\"Name\"").Count;
            Assert.True(count >= 11, $"Expected >= 11 permissions, got {count}");
        }
    }
}
