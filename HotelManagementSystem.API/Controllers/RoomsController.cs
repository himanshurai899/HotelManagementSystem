using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController(IRepository<Room> roomRepository, IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Room> _roomRepository = roomRepository;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetRooms()
        {
            var rooms = await _roomRepository.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<RoomDTO>>(rooms));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<RoomDTO>> GetRoom(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);
            if (room == null) return NotFound();
            return Ok(_mapper.Map<RoomDTO>(room));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageRooms")]
        public async Task<ActionResult<RoomDTO>> CreateRoom([FromBody] RoomDTO dto)
        {
            var room = _mapper.Map<Room>(dto);
            await _roomRepository.AddAsync(room);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, _mapper.Map<RoomDTO>(room));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageRooms")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] RoomDTO dto)
        {
            if (id != dto.Id) return BadRequest();
            var room = _mapper.Map<Room>(dto);
            await _roomRepository.UpdateAsync(room);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageRooms")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            await _roomRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
