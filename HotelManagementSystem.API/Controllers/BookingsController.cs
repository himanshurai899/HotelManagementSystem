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
    public class BookingsController(
        IRepository<Booking> repo,
        IRepository<Room> roomRepo,
        IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Booking> _repo = repo;
        private readonly IRepository<Room> _roomRepo = roomRepo;
        private readonly IMapper _mapper = mapper;

        // GET api/bookings  — Admin/SuperAdmin sees all
        [HttpGet]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            // Phase 12a — eager-load BookingRooms so the mapper can populate
            // the legacy RoomId/RoomNumber fields and the new Rooms list.
            var bookings = await _repo.GetAllWithIncludesAsync(b => b.User, b => b.BookingRooms);
            return Ok(_mapper.Map<IEnumerable<BookingDTO>>(bookings));
        }

        // GET api/bookings/my  — Customer sees only their own
        [HttpGet("my")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var all = await _repo.GetAllWithIncludesAsync(b => b.User, b => b.BookingRooms);
            var mine = all.Where(b => b.UserId == userId);
            return Ok(_mapper.Map<IEnumerable<BookingDTO>>(mine));
        }

        // GET api/bookings/calendar?year=2026&month=4
        // Returns one entry per (booking × booked-room) overlapping the requested month.
        [HttpGet("calendar")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month)
        {
            if (year == 0) year = DateTime.UtcNow.Year;
            if (month == 0) month = DateTime.UtcNow.Month;

            var firstDay = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDay = firstDay.AddMonths(1);

            var all = await _repo.GetAllWithIncludesAsync(b => b.User, b => b.BookingRooms);
            var rooms = (await _roomRepo.GetAllAsync()).ToDictionary(r => r.Id, r => r.RoomNumber);

            var inMonth = all
                .Where(b => b.Status != BookingStatus.Cancelled
                         && b.CheckInDate < lastDay
                         && b.CheckOutDate > firstDay)
                .ToList();

            // Phase 12a — flatten BookingRooms so the calendar still shows one row per room.
            var result = inMonth.SelectMany(b => b.BookingRooms.Select(br => new
            {
                b.Id,
                br.RoomId,
                RoomNumber = rooms.TryGetValue(br.RoomId, out var rn) ? rn : string.Empty,
                CustomerName = b.User != null
                    ? $"{b.User.FirstName} {b.User.LastName}".Trim()
                    : b.UserId.ToString(),
                CheckInDate = b.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = b.CheckOutDate.ToString("yyyy-MM-dd"),
                Status = b.Status.ToString()
            }));

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _repo.GetByIdWithIncludesAsync(id, b => b.User, b => b.BookingRooms);
            if (booking is null) return NotFound();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!User.IsInRole("Administrator") && !User.IsInRole("SuperAdmin") && booking.UserId != userId)
                return Forbid();
            return Ok(_mapper.Map<BookingDTO>(booking));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> Create([FromBody] BookingDTO dto)
        {
            // 1) Validate the Rooms list (authoritative; legacy RoomId is ignored).
            if (dto.Rooms is null || dto.Rooms.Count == 0)
                return BadRequest(new { message = "At least one room must be selected." });

            if (dto.CheckInDate >= dto.CheckOutDate && dto.BookingType != BookingType.Hourly)
                return BadRequest(new { message = "Check-out must be after check-in." });

            var booking = _mapper.Map<Booking>(dto);
            booking.User   = null!;
            booking.Tenant = null!;
            booking.Status = BookingStatus.Pending;
            booking.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
                booking.TenantId = tenantId;

            // 2) Resolve every requested room — server is authoritative for all price snapshots.
            var requestedRoomIds = dto.Rooms.Select(r => r.RoomId).Distinct().ToList();
            var resolvedRooms    = new List<Room>();
            foreach (var roomId in requestedRoomIds)
            {
                var room = await _roomRepo.GetByIdAsync(roomId);
                if (room is null)
                    return BadRequest(new { message = $"Room {roomId} not found." });
                resolvedRooms.Add(room);
            }

            // 3) Phase 12c — BookingType-specific validation + pricing
            switch (dto.BookingType)
            {
                case BookingType.Hourly:
                {
                    // All rooms must support hourly stay and have an hourly rate set.
                    foreach (var room in resolvedRooms)
                    {
                        if (!room.AllowHourlyStay)
                            return BadRequest(new { message = $"Room {room.RoomNumber} does not support hourly bookings." });
                        if (room.HourlyRate is null)
                            return BadRequest(new { message = $"Room {room.RoomNumber} has no hourly rate configured." });
                    }
                    // CheckInTime and CheckOutTime are required.
                    if (string.IsNullOrEmpty(dto.CheckInTime) || string.IsNullOrEmpty(dto.CheckOutTime))
                        return BadRequest(new { message = "CheckInTime and CheckOutTime are required for hourly bookings." });

                    var checkIn  = TimeSpan.Parse(dto.CheckInTime);
                    var checkOut = TimeSpan.Parse(dto.CheckOutTime);
                    if (checkIn >= checkOut)
                        return BadRequest(new { message = "Check-in time must be before check-out time." });

                    // Must be same calendar day.
                    if (dto.CheckInDate.Date != dto.CheckOutDate.Date)
                        return BadRequest(new { message = "Hourly bookings must check in and check out on the same calendar day." });

                    // Server sets the time fields from the parsed values.
                    booking.CheckInTime  = checkIn;
                    booking.CheckOutTime = checkOut;

                    // Pricing: sum(HourlyRate) × ceil(hours); snapshot HourlyRate as PriceAtBooking.
                    booking.BookingRooms = resolvedRooms
                        .Select(r => new BookingRoom { RoomId = r.Id, PriceAtBooking = r.HourlyRate!.Value })
                        .ToList();
                    var hours = (int)Math.Ceiling((checkOut - checkIn).TotalHours);
                    hours = Math.Max(1, hours);
                    booking.TotalPrice = booking.BookingRooms.Sum(br => br.PriceAtBooking) * hours;
                    break;
                }

                case BookingType.FullDay:
                {
                    // Exactly 1 night.
                    var nights = (dto.CheckOutDate - dto.CheckInDate).TotalDays;
                    if (Math.Round(nights) != 1)
                        return BadRequest(new { message = "FullDay bookings must be exactly 1 night." });
                    booking.CheckInTime  = null;
                    booking.CheckOutTime = null;
                    booking.BookingRooms = resolvedRooms
                        .Select(r => new BookingRoom { RoomId = r.Id, PriceAtBooking = r.PricePerNight })
                        .ToList();
                    booking.TotalPrice = booking.BookingRooms.Sum(br => br.PriceAtBooking);
                    break;
                }

                case BookingType.Yearly:
                {
                    // Must be an exact multiple of 365 days.
                    var days = (int)Math.Round((dto.CheckOutDate - dto.CheckInDate).TotalDays);
                    if (days < 365 || days % 365 != 0)
                        return BadRequest(new { message = "Yearly bookings must be an exact multiple of 365 days." });
                    booking.CheckInTime  = null;
                    booking.CheckOutTime = null;
                    var years = days / 365;
                    booking.BookingRooms = resolvedRooms
                        .Select(r => new BookingRoom { RoomId = r.Id, PriceAtBooking = r.PricePerNight })
                        .ToList();
                    booking.TotalPrice = booking.BookingRooms.Sum(br => br.PriceAtBooking) * days;
                    break;
                }

                default: // NightStay, LongTerm — no minimum, PricePerNight × nights
                {
                    if (dto.CheckInDate >= dto.CheckOutDate)
                        return BadRequest(new { message = "Check-out must be after check-in." });
                    booking.CheckInTime  = null;
                    booking.CheckOutTime = null;
                    booking.BookingRooms = resolvedRooms
                        .Select(r => new BookingRoom { RoomId = r.Id, PriceAtBooking = r.PricePerNight })
                        .ToList();
                    var nights = Math.Max(1, (int)Math.Ceiling((dto.CheckOutDate - dto.CheckInDate).TotalDays));
                    booking.TotalPrice = booking.BookingRooms.Sum(br => br.PriceAtBooking) * nights;
                    break;
                }
            }

            // 4) Per-room overlap guard — atomic across all booking types.
            var existing = await _repo.GetAllAsync();
            foreach (var roomId in requestedRoomIds)
            {
                var conflict = existing.Any(b =>
                    b.Status != BookingStatus.Cancelled
                    && b.BookingRooms != null
                    && b.BookingRooms.Any(br => br.RoomId == roomId)
                    && b.CheckInDate < booking.CheckOutDate
                    && b.CheckOutDate > booking.CheckInDate);
                if (conflict)
                    return Conflict(new { message = $"Room {roomId} is already booked for the selected dates." });
            }

            await _repo.AddAsync(booking);
            return CreatedAtAction(nameof(GetById), new { id = booking.Id }, _mapper.Map<BookingDTO>(booking));
        }

        // PUT api/bookings/{id}/approve
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

        // PUT api/bookings/{id}/reject
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
            // Phase 12a — Update only mutates booking-level fields. The room set is immutable
            // post-creation; to change rooms a customer must cancel and rebook.
            booking.CheckInDate = dto.CheckInDate;
            booking.CheckOutDate = dto.CheckOutDate;
            booking.Status = dto.Status;
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
