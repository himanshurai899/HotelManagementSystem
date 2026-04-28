using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public class StaffController(IRepository<Staff> repo, IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Staff> _repo = repo;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var staff = await _repo.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<StaffDTO>>(staff));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var staff = await _repo.GetByIdAsync(id);
            return staff is null ? NotFound() : Ok(_mapper.Map<StaffDTO>(staff));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffDTO dto)
        {
            var staff = _mapper.Map<Staff>(dto);
            staff.Tenant = null!;
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
            {
                staff.TenantId = tenantId;
            }
            await _repo.AddAsync(staff);
            return CreatedAtAction(nameof(GetById), new { id = staff.Id }, _mapper.Map<StaffDTO>(staff));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StaffDTO dto)
        {
            var staffItem = await _repo.GetByIdAsync(id);
            if (staffItem is null) return NotFound();
            _mapper.Map(dto, staffItem);
            await _repo.UpdateAsync(staffItem);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _repo.GetByIdAsync(id) is null) return NotFound();
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
