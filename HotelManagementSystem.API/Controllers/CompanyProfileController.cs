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
    public class CompanyProfileController(
        IRepository<CompanyProfile> repo,
        IMapper mapper,
        IFileStorageService fileStorage) : ControllerBase
    {
        private readonly IRepository<CompanyProfile> _repo        = repo;
        private readonly IMapper                     _mapper      = mapper;
        private readonly IFileStorageService         _fileStorage = fileStorage;

        // GET api/companyprofile
        // Returns the single company profile, or 204 if none has been created yet.
        [HttpGet]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Get()
        {
            var all = await _repo.GetAllAsync();
            var profile = all.FirstOrDefault();
            if (profile is null) return NoContent();
            return Ok(_mapper.Map<CompanyProfileDTO>(profile));
        }

        // PUT api/companyprofile
        // Creates the profile if none exists; updates it if one does.
        [HttpPut]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> Upsert([FromBody] CompanyProfileDTO dto)
        {
            var all = await _repo.GetAllAsync();
            var existing = all.FirstOrDefault();

            if (existing is null)
            {
                // First-time setup — create
                var newProfile = _mapper.Map<CompanyProfile>(dto);
                await _repo.AddAsync(newProfile);
                return Ok(_mapper.Map<CompanyProfileDTO>(newProfile));
            }

            // Update in-place — map DTO fields onto the tracked entity
            _mapper.Map(dto, existing);
            await _repo.UpdateAsync(existing);
            return Ok(_mapper.Map<CompanyProfileDTO>(existing));
        }

        // POST api/companyprofile/logo
        // Uploads the company logo and stores it under uploads/{CompanyName}/assets/.
        // Returns the saved URL so the client can display a preview.
        [HttpPost("logo")]
        [Authorize(Roles = "Administrator,SuperAdmin")]
        public async Task<IActionResult> UploadLogo(IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return BadRequest("No file provided.");

            var all = await _repo.GetAllAsync();
            var profile = all.FirstOrDefault();
            if (profile is null)
                return BadRequest("Company profile must be created before uploading a logo.");

            // Build path: uploads/{sanitized company name}/assets/{filename}
            var safeName = string.Concat(profile.CompanyName
                .Split(Path.GetInvalidFileNameChars()))
                .Replace(" ", string.Empty);
            var folder   = $"uploads/{safeName}/assets";
            var fileName = $"{folder}/{Path.GetFileName(file.FileName)}";

            await using var stream = file.OpenReadStream();
            var logoUrl = await _fileStorage.SaveAsync(stream, fileName, file.ContentType);

            profile.LogoUrl = logoUrl;
            await _repo.UpdateAsync(profile);

            return Ok(new { logoUrl });
        }
    }
}
