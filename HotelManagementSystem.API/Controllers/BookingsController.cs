using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Enums;
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

        // GET api/bookings/calendar?year=2026&month=4
        // Returns all non-cancelled bookings that overlap the requested month.
        [HttpGet("calendar")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month)
        {
            if (year == 0) year = DateTime.UtcNow.Year;
            if (month == 0) month = DateTime.UtcNow.Month;

            var firstDay = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDay  = firstDay.AddMonths(1);

            var all = await _repo.GetAllWithIncludesAsync(b => b.User, b => b.Room);

            // Include bookings whose date range overlaps the month window
            var inMonth = all
                .Where(b => b.Status != BookingStatus.Cancelled
                         && b.CheckInDate  < lastDay
                         && b.CheckOutDate > firstDay)
                .ToList();

            var result = inMonth.Select(b => new
            {
                b.Id,
                b.RoomId,
                RoomNumber = b.Room?.RoomNumber ?? string.Empty,
                CustomerName = b.User != null
                    ? $"{b.User.FirstName} {b.User.LastName}".Trim()
                    : b.UserId.ToString(),
                CheckInDate  = b.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = b.CheckOutDate.ToString("yyyy-MM-dd"),
                Status = b.Status.ToString()
            });

            return Ok(result);
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
            // Always start as Pending — admin must approve
            var booking = _mapper.Map<Booking>(dto);
            booking.User   = null!;
            booking.Room   = null!;
            booking.Tenant = null!;
            booking.Status = BookingStatus.Pending;
            // Always set UserId from token — prevents spoofing
            booking.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
                booking.TenantId = tenantId;

            // Overlap guard — reject if any Pending or Confirmed booking occupies the same room
            var existing = await _repo.GetAllAsync();
            var conflict = existing.Any(b =>
                b.RoomId == booking.RoomId
                && b.Status != BookingStatus.Cancelled
                && b.CheckInDate  < booking.CheckOutDate
                && b.CheckOutDate > booking.CheckInDate);

            if (conflict)
                return Conflict(new { message = "Room is already booked for the selected dates." });

            await _repo.AddAsync(booking);
            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, _mapper.Map<BookingDTO>(booking));
        }

        // PUT api/bookings/{id}/approve  — Admin/SuperAdmin with ManageBookings
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageBookings")]
        public async Task<IActionResult> Approve(int id)
        {
            var booking = await _repo.GetByIdAsync(id);
            if (booking is null) return NotFound();
            if (booking.Status != BookingStatus.Pending)
                return BadRequest(new { message = "Only Pending bookings can be approved." });

            booking.Status = BookingStatus.Confirmed;
            await _repo.UpdateAsync(booking);
            return NoContent();
        }

        // PUT api/bookings/{id}/reject  — Admin/SuperAdmin with ManageBookings
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        [Authorize(Policy = "ManageBookings")]
        public async Task<IActionResult> Reject(int id)
        {
            var booking = await _repo.GetByIdAsync(id);
            if (booking is null) return NotFound();
            if (booking.Status != BookingStatus.Pending)
                return BadRequest(new { message = "Only Pending bookings can be rejected." });

            booking.Status = BookingStatus.Cancelled;
            await _repo.UpdateAsync(booking);
            return NoContent();
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
