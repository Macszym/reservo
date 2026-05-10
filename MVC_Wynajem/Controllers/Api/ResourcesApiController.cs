using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reservo.Models;
using Reservo.Models.DTOs;
using Reservo.Attributes;

namespace Reservo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    public class ResourcesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResourcesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Resources
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResourceDTO>>> GetResources(
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? isAvailable = null,
            [FromQuery] string? search = null)
        {
            var query = _context.Resources.Include(r => r.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(r => r.CategoryId == categoryId);

            if (isAvailable.HasValue)
                query = query.Where(r => r.IsAvailable == isAvailable);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.Name.Contains(search) || 
                                       (r.Description != null && r.Description.Contains(search)));

            var resources = await query.Select(r => new ResourceDTO
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Location = r.Location,
                IsAvailable = r.IsAvailable,
                MaxReservationHours = r.MaxReservationHours,
                CreatedAt = r.CreatedAt,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : null,
                CategoryColor = r.Category != null ? r.Category.Color : null
            }).ToListAsync();

            return Ok(resources);
        }

        // GET: api/Resources/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ResourceDTO>> GetResource(int id)
        {
            var resource = await _context.Resources
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null)
            {
                return NotFound(new { error = "Zasób nie został znaleziony" });
            }

            var dto = new ResourceDTO
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = resource.Description,
                Location = resource.Location,
                IsAvailable = resource.IsAvailable,
                MaxReservationHours = resource.MaxReservationHours,
                CreatedAt = resource.CreatedAt,
                CategoryId = resource.CategoryId,
                CategoryName = resource.Category?.Name,
                CategoryColor = resource.Category?.Color
            };

            return Ok(dto);
        }

        // POST: api/Resources
        [HttpPost]
        [ApiKeyAuth("Admin")]
        public async Task<ActionResult<ResourceDTO>> CreateResource(CreateResourceDTO dto)
        {
            var resource = new Resource
            {
                Name = dto.Name,
                Description = dto.Description,
                Location = dto.Location,
                IsAvailable = dto.IsAvailable,
                MaxReservationHours = dto.MaxReservationHours,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.Now
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // Pobierz utworzony zasób z kategorią
            var createdResource = await _context.Resources
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == resource.Id);

            var responseDto = new ResourceDTO
            {
                Id = createdResource.Id,
                Name = createdResource.Name,
                Description = createdResource.Description,
                Location = createdResource.Location,
                IsAvailable = createdResource.IsAvailable,
                MaxReservationHours = createdResource.MaxReservationHours,
                CreatedAt = createdResource.CreatedAt,
                CategoryId = createdResource.CategoryId,
                CategoryName = createdResource.Category?.Name,
                CategoryColor = createdResource.Category?.Color
            };

            return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, responseDto);
        }

        // PUT: api/Resources/5
        [HttpPut("{id}")]
        [ApiKeyAuth("Admin")]
        public async Task<IActionResult> UpdateResource(int id, CreateResourceDTO dto)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { error = "Zasób nie został znaleziony" });
            }

            resource.Name = dto.Name;
            resource.Description = dto.Description;
            resource.Location = dto.Location;
            resource.IsAvailable = dto.IsAvailable;
            resource.MaxReservationHours = dto.MaxReservationHours;
            resource.CategoryId = dto.CategoryId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResourceExists(id))
                {
                    return NotFound(new { error = "Zasób nie został znaleziony" });
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Resources/5
        [HttpDelete("{id}")]
        [ApiKeyAuth("Admin")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { error = "Zasób nie został znaleziony" });
            }

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Resources/5/availability
        [HttpGet("{id}/availability")]
        public async Task<ActionResult> GetResourceAvailability(int id, 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { error = "Zasób nie został znaleziony" });
            }

            var start = startDate ?? DateTime.Today;
            var end = endDate ?? start.AddDays(30);

            var reservations = await _context.Reservations
                .Where(r => r.ResourceId == id &&
                           r.Status == ReservationStatus.Active &&
                           r.StartDate < end &&
                           r.EndDate > start)
                .Select(r => new
                {
                    r.Id,
                    r.StartDate,
                    r.EndDate,
                    r.Purpose,
                    Username = r.User != null ? r.User.Username : null
                })
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            return Ok(new
            {
                resourceId = id,
                resourceName = resource.Name,
                isAvailable = resource.IsAvailable,
                periodStart = start,
                periodEnd = end,
                reservations
            });
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }
    }
}