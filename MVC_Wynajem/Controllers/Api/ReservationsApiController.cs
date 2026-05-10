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
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetReservations(
            [FromQuery] int? resourceId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var currentUser = GetCurrentUser();
            var query = _context.Reservations
                .Include(r => r.Resource)
                .Include(r => r.User)
                .AsQueryable();

            // Admin widzi wszystkie, użytkownik tylko swoje
            if (currentUser.Role != "Admin")
            {
                query = query.Where(r => r.UserId == currentUser.Id);
            }

            if (resourceId.HasValue)
                query = query.Where(r => r.ResourceId == resourceId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReservationStatus>(status, true, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);

            if (startDate.HasValue)
                query = query.Where(r => r.EndDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(r => r.StartDate <= endDate);

            var reservations = await query.Select(r => new ReservationDTO
            {
                Id = r.Id,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                Purpose = r.Purpose,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                UserId = r.UserId,
                Username = r.User != null ? r.User.Username : null,
                ResourceId = r.ResourceId,
                ResourceName = r.Resource != null ? r.Resource.Name : null,
                ResourceLocation = r.Resource != null ? r.Resource.Location : null
            }).OrderByDescending(r => r.CreatedAt).ToListAsync();

            return Ok(reservations);
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDTO>> GetReservation(int id)
        {
            var currentUser = GetCurrentUser();
            var reservation = await _context.Reservations
                .Include(r => r.Resource)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound(new { error = "Rezerwacja nie została znaleziona" });
            }

            // Sprawdź uprawnienia
            if (currentUser.Role != "Admin" && reservation.UserId != currentUser.Id)
            {
                return Forbid();
            }

            var dto = new ReservationDTO
            {
                Id = reservation.Id,
                StartDate = reservation.StartDate,
                EndDate = reservation.EndDate,
                Purpose = reservation.Purpose,
                Status = reservation.Status.ToString(),
                CreatedAt = reservation.CreatedAt,
                UserId = reservation.UserId,
                Username = reservation.User?.Username,
                ResourceId = reservation.ResourceId,
                ResourceName = reservation.Resource?.Name,
                ResourceLocation = reservation.Resource?.Location
            };

            return Ok(dto);
        }

        // POST: api/Reservations
        [HttpPost]
        public async Task<ActionResult<ReservationDTO>> CreateReservation(CreateReservationDTO dto)
        {
            var currentUser = GetCurrentUser();
            
            var reservation = new Reservation
            {
                ResourceId = dto.ResourceId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Purpose = dto.Purpose,
                UserId = currentUser.Id,
                Status = ReservationStatus.Active,
                CreatedAt = DateTime.Now
            };

            // Walidacja
            var validationResult = await ValidateReservation(reservation);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors });
            }

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Pobierz utworzoną rezerwację z pełnymi danymi
            var createdReservation = await _context.Reservations
                .Include(r => r.Resource)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reservation.Id);

            var responseDto = new ReservationDTO
            {
                Id = createdReservation.Id,
                StartDate = createdReservation.StartDate,
                EndDate = createdReservation.EndDate,
                Purpose = createdReservation.Purpose,
                Status = createdReservation.Status.ToString(),
                CreatedAt = createdReservation.CreatedAt,
                UserId = createdReservation.UserId,
                Username = createdReservation.User?.Username,
                ResourceId = createdReservation.ResourceId,
                ResourceName = createdReservation.Resource?.Name,
                ResourceLocation = createdReservation.Resource?.Location
            };

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, responseDto);
        }

        // PUT: api/Reservations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReservation(int id, CreateReservationDTO dto)
        {
            var currentUser = GetCurrentUser();
            var reservation = await _context.Reservations.FindAsync(id);
            
            if (reservation == null)
            {
                return NotFound(new { error = "Rezerwacja nie została znaleziona" });
            }

            // Sprawdź uprawnienia
            if (currentUser.Role != "Admin" && reservation.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // Nie można edytować zakończonych rezerwacji
            if (reservation.Status == ReservationStatus.Completed || reservation.EndDate < DateTime.Now)
            {
                return BadRequest(new { error = "Nie można edytować zakończonych rezerwacji" });
            }

            reservation.ResourceId = dto.ResourceId;
            reservation.StartDate = dto.StartDate;
            reservation.EndDate = dto.EndDate;
            reservation.Purpose = dto.Purpose;

            // Walidacja
            var validationResult = await ValidateReservation(reservation, id);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
                {
                    return NotFound(new { error = "Rezerwacja nie została znaleziona" });
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Reservations/5 (anulowanie)
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var currentUser = GetCurrentUser();
            var reservation = await _context.Reservations.FindAsync(id);
            
            if (reservation == null)
            {
                return NotFound(new { error = "Rezerwacja nie została znaleziona" });
            }

            // Sprawdź uprawnienia
            if (currentUser.Role != "Admin" && reservation.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // Nie można anulować zakończonych rezerwacji
            if (reservation.Status == ReservationStatus.Completed)
            {
                return BadRequest(new { error = "Nie można anulować zakończonych rezerwacji" });
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private User GetCurrentUser()
        {
            return (User)HttpContext.Items["ApiUser"]!;
        }

        private async Task<(bool IsValid, List<string> Errors)> ValidateReservation(Reservation reservation, int? excludeId = null)
        {
            var errors = new List<string>();

            // Sprawdź czy zasób istnieje i jest dostępny
            var resource = await _context.Resources.FindAsync(reservation.ResourceId);
            if (resource == null)
            {
                errors.Add("Wybrany zasób nie istnieje");
                return (false, errors);
            }

            if (!resource.IsAvailable)
            {
                errors.Add("Wybrany zasób jest obecnie niedostępny");
            }

            // Sprawdź daty
            if (reservation.StartDate >= reservation.EndDate)
            {
                errors.Add("Data zakończenia musi być późniejsza niż data rozpoczęcia");
            }

            if (reservation.StartDate < DateTime.Now.AddMinutes(-5))
            {
                errors.Add("Nie można utworzyć rezerwacji w przeszłości");
            }

            // Sprawdź maksymalny czas rezerwacji
            var duration = reservation.EndDate - reservation.StartDate;
            if (duration.TotalHours > resource.MaxReservationHours)
            {
                errors.Add($"Maksymalny czas rezerwacji dla tego zasobu to {resource.MaxReservationHours} godzin");
            }

            // Sprawdź kolizje z innymi rezerwacjami
            var conflictingReservations = await _context.Reservations
                .Where(r => r.ResourceId == reservation.ResourceId &&
                           r.Status == ReservationStatus.Active &&
                           (excludeId == null || r.Id != excludeId) &&
                           r.StartDate < reservation.EndDate &&
                           r.EndDate > reservation.StartDate)
                .ToListAsync();

            if (conflictingReservations.Any())
            {
                errors.Add("Wybrany termin koliduje z istniejącą rezerwacją");
            }

            return (errors.Count == 0, errors);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}