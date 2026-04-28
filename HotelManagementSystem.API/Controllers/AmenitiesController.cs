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
    public class AmenitiesController(IRepository<Amenity> repo, IMapper mapper) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var amenities = await repo.GetAllAsync();
            return Ok(mapper.Map<IEnumerable<AmenityDTO>>(amenities));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var amenity = await repo.GetByIdAsync(id);
            return amenity is null ? NotFound() : Ok(mapper.Map<AmenityDTO>(amenity));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] AmenityDTO dto)
        {
            var amenity = mapper.Map<Amenity>(dto);
            amenity.Tenant = null!;
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
            {
                amenity.TenantId = tenantId;
            }
            await repo.AddAsync(amenity);
            return CreatedAtAction(nameof(GetById), new { id = amenity.Id }, mapper.Map<AmenityDTO>(amenity));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] AmenityDTO dto)
        {
            var amenity = await repo.GetByIdAsync(id);
            if (amenity is null) return NotFound();
            mapper.Map(dto, amenity);
            await repo.UpdateAsync(amenity);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await repo.GetByIdAsync(id) is null) return NotFound();
            await repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
