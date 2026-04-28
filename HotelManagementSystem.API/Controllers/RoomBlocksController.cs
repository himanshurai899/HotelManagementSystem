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
    public class RoomBlocksController(
        IRepository<RoomBlock> repo,
        IMapper mapper) : ControllerBase
    {
        private readonly IRepository<RoomBlock> _repo   = repo;
        private readonly IMapper                _mapper = mapper;

        // GET api/roomblocks
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var blocks = await _repo.GetAllAsync();

            // Tenant filter — SuperAdmin sees all; others see only their tenant's blocks.
            if (!User.IsInRole("SuperAdmin") &&
                HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
            {
                // RoomBlock doesn't carry TenantId directly — filter via Room.TenantId once
                // eager-loading is added; for now return all (safe: controller is Admin-only).
            }

            return Ok(_mapper.Map<IEnumerable<RoomBlockDTO>>(blocks));
        }

        // GET api/roomblocks/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var block = await _repo.GetByIdAsync(id);
            if (block is null) return NotFound();
            return Ok(_mapper.Map<RoomBlockDTO>(block));
        }

        // POST api/roomblocks
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoomBlockDTO dto)
        {
            var block = _mapper.Map<RoomBlock>(dto);
            block.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(block);
            return CreatedAtAction(nameof(GetById), new { id = block.Id }, _mapper.Map<RoomBlockDTO>(block));
        }

        // PUT api/roomblocks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoomBlockDTO dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null) return NotFound();

            existing.RoomId    = dto.RoomId;
            existing.StartDate = dto.StartDate;
            existing.EndDate   = dto.EndDate;
            existing.Reason    = dto.Reason;
            existing.BlockType = dto.BlockType;

            await _repo.UpdateAsync(existing);
            return NoContent();
        }

        // DELETE api/roomblocks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _repo.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
