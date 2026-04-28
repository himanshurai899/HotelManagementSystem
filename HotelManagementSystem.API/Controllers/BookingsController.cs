using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController(IRepository<Booking> repo, IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Booking> _repo = repo;
        private readonly IMapper _mapper = mapper;
        // GET api/bookings  — Admin/SuperAdmin sees all
        [HttpGet]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _repo.GetAllWithIncludesAsync(b => b.User, b => b.Room);
            return Ok(_mapper.Map<IEnumerable<BookingDTO>>(bookings));
        }

        // GET api/bookings/my  — Customer sees only their own
        [HttpGet("my")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var all = await _repo.GetAllWithIncludesAsync(b => b.User, b => b.Room);
            var mine = all.Where(b => b.UserId == userId);
            return Ok(_mapper.Map<IEnumerable<BookingDTO>>(mine));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _repo.GetByIdWithIncludesAsync(id, b => b.User, b => b.Room);
            if (booking is null) return NotFound();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!User.IsInRole("Administrator") && !User.IsInRole("SuperAdmin") && booking.UserId != userId) return Forbid();
            return Ok(_mapper.Map<BookingDTO>(booking));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> Create([FromBody] BookingDTO dto)
        {
            var booking = _mapper.Map<Booking>(dto);
            booking.User = null!;
            booking.Room = null!;
            booking.Tenant = null!;
            // Always set UserId from token — prevents spoofing
            booking.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
            {
                booking.TenantId = tenantId;
            }
            await _repo.AddAsync(booking);
            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, _mapper.Map<BookingDTO>(booking));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageBookings")]
        public async Task<IActionResult> Update(int id, [FromBody] BookingDTO dto)
        {
            var booking = await _repo.GetByIdAsync(id);
            if (booking is null) return NotFound();
            _mapper.Map(dto, booking);
            await _repo.UpdateAsync(booking);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageBookings")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _repo.GetByIdAsync(id) is null) return NotFound();
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
