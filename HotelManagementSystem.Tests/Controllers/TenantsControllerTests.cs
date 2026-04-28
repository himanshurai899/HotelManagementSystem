using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.Shared.DTOs;
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
    public class TenantsControllerTests
    {
        // ---------- helpers ----------

        private static Mock<UserManager<User>> BuildUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private static TenantsController BuildController(
            Mock<IRepository<Tenant>> repo,
            Mock<IRepository<UserTenant>> userTenantRepo,
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

            var controller = new TenantsController(repo.Object, userTenantRepo.Object, BuildUserManagerMock().Object, mapper.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
            return controller;
        }

        private static MethodInfo GetAction(string name) =>
            typeof(TenantsController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Action {name} not found");

        private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttrs(string action) =>
            GetAction(action).GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(typeof(TenantsController).GetCustomAttributes<AuthorizeAttribute>(inherit: true));

        // =====================================================================
        //  AUTHORIZATION METADATA
        // =====================================================================

        [Theory]
        [InlineData(nameof(TenantsController.GetAll))]
        [InlineData(nameof(TenantsController.GetById))]
        [InlineData(nameof(TenantsController.Create))]
        [InlineData(nameof(TenantsController.Update))]
        [InlineData(nameof(TenantsController.Delete))]
        [InlineData(nameof(TenantsController.GetUsersInTenant))]
        [InlineData(nameof(TenantsController.AddUserToTenant))]
        [InlineData(nameof(TenantsController.RemoveUserFromTenant))]
        public void Action_RequiresSuperAdminRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.Contains("SuperAdmin", roles);
        }

        [Theory]
        [InlineData(nameof(TenantsController.GetAll))]
        [InlineData(nameof(TenantsController.GetById))]
        [InlineData(nameof(TenantsController.Create))]
        [InlineData(nameof(TenantsController.Update))]
        [InlineData(nameof(TenantsController.Delete))]
        [InlineData(nameof(TenantsController.GetUsersInTenant))]
        [InlineData(nameof(TenantsController.AddUserToTenant))]
        [InlineData(nameof(TenantsController.RemoveUserFromTenant))]
        public void Action_DoesNotAllowCustomerRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.DoesNotContain("Customer", roles);
        }

        [Theory]
        [InlineData(nameof(TenantsController.GetAll))]
        [InlineData(nameof(TenantsController.GetById))]
        [InlineData(nameof(TenantsController.Create))]
        [InlineData(nameof(TenantsController.Update))]
        [InlineData(nameof(TenantsController.Delete))]
        [InlineData(nameof(TenantsController.GetUsersInTenant))]
        [InlineData(nameof(TenantsController.AddUserToTenant))]
        [InlineData(nameof(TenantsController.RemoveUserFromTenant))]
        public void Action_DoesNotAllowAdministratorRole(string actionName)
        {
            var roles = GetAuthorizeAttrs(actionName)
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries));

            Assert.DoesNotContain("Administrator", roles);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetAll
        // =====================================================================

        [Fact]
        public async Task GetAll_ReturnsOkWithMappedTenants()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var data = new List<Tenant> { new() { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" } };
            var dto = new List<TenantDTO> { new() { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" } };

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(data);
            mapper.Setup(m => m.Map<IEnumerable<TenantDTO>>(data)).Returns(dto);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.GetAll() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal(dto, result.Value);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetById
        // =====================================================================

        [Fact]
        public async Task GetById_ExistingId_ReturnsOk()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var tenant = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };
            var dto = new TenantDTO { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tenant);
            mapper.Setup(m => m.Map<TenantDTO>(tenant)).Returns(dto);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.GetById(1) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal(dto, result.Value);
        }

        [Fact]
        public async Task GetById_MissingId_ReturnsNotFound()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tenant)null!);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.GetById(99);

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Create
        // =====================================================================

        [Fact]
        public async Task Create_ValidDto_Returns201WithLocation()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var dto = new TenantDTO { Name = "Hotel Beta", Subdomain = "beta", Plan = "Pro", IsActive = true };
            var entity = new Tenant { Id = 2, Name = "Hotel Beta", Subdomain = "beta" };
            var returnDto = new TenantDTO { Id = 2, Name = "Hotel Beta", Subdomain = "beta" };

            mapper.Setup(m => m.Map<Tenant>(dto)).Returns(entity);
            repo.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            mapper.Setup(m => m.Map<TenantDTO>(entity)).Returns(returnDto);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.Create(dto) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
            Assert.Equal(returnDto, result.Value);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Update
        // =====================================================================

        [Fact]
        public async Task Update_ExistingId_ReturnsNoContent()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var existing = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };
            var dto = new TenantDTO { Id = 1, Name = "Hotel Alpha Updated", Subdomain = "alpha" };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            mapper.Setup(m => m.Map(dto, existing));
            repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.Update(1, dto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Update_MissingId_ReturnsNotFound()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tenant)null!);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.Update(99, new TenantDTO());

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — Delete
        // =====================================================================

        [Fact]
        public async Task Delete_ExistingId_ReturnsNoContent()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var existing = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            repo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.Delete(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_MissingId_ReturnsNotFound()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tenant)null!);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.Delete(99);

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — GetUsersInTenant
        // =====================================================================

        [Fact]
        public async Task GetUsersInTenant_ExistingTenant_ReturnsOkWithUsers()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var tenant = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };
            var userTenants = new List<UserTenant>
            {
                new() { UserId = 10, TenantId = 1, TenantRole = "Administrator", JoinedAt = DateTime.UtcNow }
            };
            var dtos = new List<UserTenantDTO>
            {
                new() { UserId = 10, TenantId = 1, TenantRole = "Administrator" }
            };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tenant);
            utRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(userTenants);
            mapper.Setup(m => m.Map<IEnumerable<UserTenantDTO>>(It.IsAny<IEnumerable<UserTenant>>())).Returns(dtos);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.GetUsersInTenant(1) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
        }

        [Fact]
        public async Task GetUsersInTenant_MissingTenant_ReturnsNotFound()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tenant)null!);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.GetUsersInTenant(99);

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — AddUserToTenant
        // =====================================================================

        [Fact]
        public async Task AddUserToTenant_ValidRequest_ReturnsCreated()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var tenant = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };
            var utDto = new UserTenantDTO { UserId = 10, TenantId = 1, TenantRole = "Administrator" };
            var entity = new UserTenant { UserId = 10, TenantId = 1, TenantRole = "Administrator", JoinedAt = DateTime.UtcNow };
            var returnDto = new UserTenantDTO { UserId = 10, TenantId = 1, TenantRole = "Administrator" };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tenant);
            utRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserTenant>());
            mapper.Setup(m => m.Map<UserTenant>(utDto)).Returns(entity);
            utRepo.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
            mapper.Setup(m => m.Map<UserTenantDTO>(entity)).Returns(returnDto);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.AddUserToTenant(1, utDto) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
        }

        [Fact]
        public async Task AddUserToTenant_MissingTenant_ReturnsNotFound()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tenant)null!);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.AddUserToTenant(99, new UserTenantDTO());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddUserToTenant_DuplicateUser_ReturnsBadRequest()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var tenant = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };
            var existing = new List<UserTenant>
            {
                new() { UserId = 10, TenantId = 1 }
            };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tenant);
            utRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.AddUserToTenant(1, new UserTenantDTO { UserId = 10, TenantId = 1 });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // =====================================================================
        //  BEHAVIOURAL TESTS — RemoveUserFromTenant
        // =====================================================================

        [Fact]
        public async Task RemoveUserFromTenant_ExistingMembership_ReturnsNoContent()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var tenant = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };
            var userTenants = new List<UserTenant>
            {
                new() { UserId = 10, TenantId = 1 }
            };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tenant);
            utRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(userTenants);
            utRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.RemoveUserFromTenant(1, 10);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task RemoveUserFromTenant_UserNotInTenant_ReturnsNotFound()
        {
            var repo = new Mock<IRepository<Tenant>>();
            var utRepo = new Mock<IRepository<UserTenant>>();
            var mapper = new Mock<IMapper>();
            var tenant = new Tenant { Id = 1, Name = "Hotel Alpha", Subdomain = "alpha" };

            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tenant);
            utRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserTenant>());

            var sut = BuildController(repo, utRepo, mapper, userId: 3, "SuperAdmin");
            var result = await sut.RemoveUserFromTenant(1, 99);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
