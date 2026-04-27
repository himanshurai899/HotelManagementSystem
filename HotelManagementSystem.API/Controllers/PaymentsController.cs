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
    public class PaymentsController(IRepository<Payment> repo, IMapper mapper) : ControllerBase
    {
        private readonly IRepository<Payment> _repo = repo;
        private readonly IMapper _mapper = mapper;

        // GET api/payments  — Admin/SuperAdmin sees all
        [HttpGet]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var payments = await _repo.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<PaymentDTO>>(payments));
        }

        // GET api/payments/my  — Customer sees only their own
        [HttpGet("my")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var all = await _repo.GetAllAsync();
            var mine = all.Where(p => p.Booking != null && p.Booking.UserId == userId);
            return Ok(_mapper.Map<IEnumerable<PaymentDTO>>(mine));
        }

        // GET api/payments/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _repo.GetByIdAsync(id);
            if (payment is null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!User.IsInRole("Administrator") && !User.IsInRole("SuperAdmin")
                && (payment.Booking == null || payment.Booking.UserId != userId))
                return Forbid();

            return Ok(_mapper.Map<PaymentDTO>(payment));
        }

        // POST api/payments  — Admin only records payments
        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] PaymentDTO dto)
        {
            var payment = _mapper.Map<Payment>(dto);
            await _repo.AddAsync(payment);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, _mapper.Map<PaymentDTO>(payment));
        }

        // PUT api/payments/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] PaymentDTO dto)
        {
            var payment = await _repo.GetByIdAsync(id);
            if (payment is null) return NotFound();
            _mapper.Map(dto, payment);
            await _repo.UpdateAsync(payment);
            return NoContent();
        }

        // DELETE api/payments/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _repo.GetByIdAsync(id) is null) return NotFound();
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
