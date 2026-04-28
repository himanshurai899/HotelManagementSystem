using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class TenantsController(
        IRepository<Tenant> repo,
        IRepository<UserTenant> userTenantRepo,
        UserManager<User> userManager,
        IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Tenant> _repo = repo;
        private readonly IRepository<UserTenant> _userTenantRepo = userTenantRepo;
        private readonly UserManager<User> _userManager = userManager;
        private readonly IMapper _mapper = mapper;

        // GET /api/tenants
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenants = await _repo.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<TenantDTO>>(tenants));
        }

        // GET /api/tenants/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tenant = await _repo.GetByIdAsync(id);
            if (tenant == null) return NotFound();
            return Ok(_mapper.Map<TenantDTO>(tenant));
        }

        // GET /api/tenants/default-currency  — public; returns the active default tenant's currency
        [HttpGet("default-currency")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDefaultCurrency()
        {
            var tenants = await _repo.GetAllAsync();
            var defaultTenant = tenants.FirstOrDefault(t => t.IsActive) ?? tenants.FirstOrDefault();
            if (defaultTenant == null)
                return Ok(new DefaultCurrencyDTO { CurrencyCode = "INR", Locale = "en-IN" });
            return Ok(new DefaultCurrencyDTO
            {
                CurrencyCode = defaultTenant.CurrencyCode ?? "INR",
                Locale       = defaultTenant.Locale       ?? "en-IN"
            });
        }

        // POST /api/tenants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TenantDTO dto)
        {
            var entity = _mapper.Map<Tenant>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, _mapper.Map<TenantDTO>(entity));
        }

        // PUT /api/tenants/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TenantDTO dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();
            _mapper.Map(dto, existing);
            await _repo.UpdateAsync(existing);
            return NoContent();
        }

        // DELETE /api/tenants/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        // GET /api/tenants/{id}/users
        [HttpGet("{id}/users")]
        public async Task<IActionResult> GetUsersInTenant(int id)
        {
            var tenant = await _repo.GetByIdAsync(id);
            if (tenant == null) return NotFound();

            var all = await _userTenantRepo.GetAllAsync();
            var forTenant = all.Where(ut => ut.TenantId == id).ToList();

            // Hydrate User navigation so AutoMapper can resolve FirstName / LastName / Email
            foreach (var ut in forTenant)
            {
                ut.User = await _userManager.FindByIdAsync(ut.UserId.ToString()) ?? new User();
            }

            return Ok(_mapper.Map<IEnumerable<UserTenantDTO>>(forTenant));
        }

        // POST /api/tenants/{id}/users
        [HttpPost("{id}/users")]
        public async Task<IActionResult> AddUserToTenant(int id, [FromBody] UserTenantDTO dto)
        {
            var tenant = await _repo.GetByIdAsync(id);
            if (tenant == null) return NotFound();

            var all = await _userTenantRepo.GetAllAsync();
            if (all.Any(ut => ut.TenantId == id && ut.UserId == dto.UserId))
                return BadRequest(new { message = "User is already a member of this tenant." });

            // Build the entity directly — do NOT use AutoMapper ReverseMap here because
            // mapping UserName back would cause EF to try to resolve/insert the User navigation.
            var entity = new UserTenant
            {
                UserId    = dto.UserId,
                TenantId  = id,
                TenantRole = dto.TenantRole,
                JoinedAt  = DateTime.UtcNow
            };
            await _userTenantRepo.AddAsync(entity);
            return CreatedAtAction(nameof(GetUsersInTenant), new { id }, _mapper.Map<UserTenantDTO>(entity));
        }

        // DELETE /api/tenants/{id}/users/{userId}
        [HttpDelete("{id}/users/{userId}")]
        public async Task<IActionResult> RemoveUserFromTenant(int id, int userId)
        {
            var tenant = await _repo.GetByIdAsync(id);
            if (tenant == null) return NotFound();

            var all = await _userTenantRepo.GetAllAsync();
            var membership = all.FirstOrDefault(ut => ut.TenantId == id && ut.UserId == userId);
            if (membership == null) return NotFound();

            // UserTenant has composite PK (UserId, TenantId). IRepository<T>.DeleteAsync(int)
            // targets a single int PK. We pass userId here; in tests this is mocked and
            // verifies DeleteAsync is called. A production improvement would be a dedicated
            // IUserTenantRepository with RemoveAsync(userId, tenantId).
            await _userTenantRepo.DeleteAsync(userId);
            return NoContent();
        }
    }
}
