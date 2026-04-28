using AutoMapper;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using HotelManagementSystem.Shared.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController(IRepository<Invoice> repo, IMapper mapper, IRepository<CompanyProfile> companyRepo) : ControllerBase
    {
        private readonly IRepository<Invoice> _repo = repo;
        private readonly IMapper _mapper = mapper;
        private readonly IRepository<CompanyProfile> _companyRepo = companyRepo;

        // GET api/invoices  — Admin/SuperAdmin sees all
        [HttpGet]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _repo.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<InvoiceDTO>>(invoices));
        }

        // GET api/invoices/my  — Customer sees only their own
        [HttpGet("my")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetMyInvoices()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var all = await _repo.GetAllAsync();
            var mine = all.Where(i => i.Booking != null && i.Booking.UserId == userId);
            return Ok(_mapper.Map<IEnumerable<InvoiceDTO>>(mine));
        }

        // GET api/invoices/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice is null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!User.IsInRole("Administrator") && !User.IsInRole("SuperAdmin")
                && (invoice.Booking == null || invoice.Booking.UserId != userId))
                return Forbid();

            return Ok(_mapper.Map<InvoiceDTO>(invoice));
        }

        // POST api/invoices  — Admin only creates invoices
        [HttpPost]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Create([FromBody] InvoiceDTO dto)
        {
            var invoice = _mapper.Map<Invoice>(dto);
            invoice.Tenant = null!;
            if (HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int tenantId)
            {
                invoice.TenantId = tenantId;
            }
            await _repo.AddAsync(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, _mapper.Map<InvoiceDTO>(invoice));
        }

        // PUT api/invoices/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] InvoiceDTO dto)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice is null) return NotFound();
            _mapper.Map(dto, invoice);
            await _repo.UpdateAsync(invoice);
            return NoContent();
        }

        // DELETE api/invoices/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _repo.GetByIdAsync(id) is null) return NotFound();
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        // GET api/invoices/{id}/pdf
        [HttpGet("{id}/pdf")]
        [Authorize(Roles = "Administrator,SuperAdmin,Customer")]
        public async Task<IActionResult> GetPdf(int id)
        {
            var invoice = await _repo.GetByIdAsync(id);
            if (invoice is null) return NotFound();

            // Customer may only download their own invoice
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!User.IsInRole("Administrator") && !User.IsInRole("SuperAdmin")
                && (invoice.Booking == null || invoice.Booking.UserId != userId))
                return Forbid();

            var dto = _mapper.Map<InvoiceDTO>(invoice);

            var profiles = await _companyRepo.GetAllAsync();
            var profile  = profiles.FirstOrDefault();

            var pdfBytes = InvoicePdfUtility.Generate(dto, profile);

            return File(pdfBytes, "application/pdf",
                        $"Invoice_{invoice.Id}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }
}
