using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Reservo.Models;
using Reservo.Attributes;

namespace Reservo.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("UserRole");

            IQueryable<Reservation> reservations;

            if (userRole == "Admin")
            {
                // Admin widzi wszystkie rezerwacje
                reservations = _context.Reservations
                    .Include(r => r.Resource)
                        .ThenInclude(res => res.Category)
                    .Include(r => r.User);
            }
            else
            {
                // Zwykły użytkownik widzi tylko swoje rezerwacje
                reservations = _context.Reservations
                    .Include(r => r.Resource)
                        .ThenInclude(res => res.Category)
                    .Include(r => r.User)
                    .Where(r => r.UserId == currentUserId);
            }

            return View(await reservations.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Resource)
                    .ThenInclude(res => res.Category)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Sprawdź uprawnienia
            var currentUserId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userRole != "Admin" && reservation.UserId != currentUserId)
            {
                return Forbid();
            }

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create(int? resourceId)
        {
            var availableResources = await _context.Resources
                .Where(r => r.IsAvailable)
                .Include(r => r.Category)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.Resources = new SelectList(availableResources, "Id", "Name", resourceId);
            ViewBag.SelectedResourceId = resourceId;

            if (resourceId.HasValue)
            {
                var selectedResource = await _context.Resources.FindAsync(resourceId.Value);
                ViewBag.SelectedResource = selectedResource;
            }

            return View();
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ResourceId,StartDate,EndDate,Purpose")] Reservation reservation)
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId == null)
            {
                return RedirectToAction("Logowanie", "IO");
            }

            reservation.UserId = currentUserId.Value;
            reservation.Status = ReservationStatus.Active;
            reservation.CreatedAt = DateTime.Now;

            // Walidacja
            var validationResult = await ValidateReservation(reservation);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Reload data for view
            var availableResources = await _context.Resources
                .Where(r => r.IsAvailable)
                .Include(r => r.Category)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.Resources = new SelectList(availableResources, "Id", "Name", reservation.ResourceId);

            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            // Sprawdź uprawnienia
            var currentUserId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userRole != "Admin" && reservation.UserId != currentUserId)
            {
                return Forbid();
            }

            // Nie można edytować zakończonych rezerwacji
            if (reservation.Status == ReservationStatus.Completed || reservation.EndDate < DateTime.Now)
            {
                ViewBag.Error = "Nie można edytować zakończonych rezerwacji.";
                return View("Details", await GetReservationWithIncludes(id.Value));
            }

            var availableResources = await _context.Resources
                .Where(r => r.IsAvailable || r.Id == reservation.ResourceId)
                .Include(r => r.Category)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.Resources = new SelectList(availableResources, "Id", "Name", reservation.ResourceId);

            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ResourceId,StartDate,EndDate,Purpose,Status,UserId,CreatedAt")] Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            // Sprawdź uprawnienia
            var currentUserId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userRole != "Admin" && reservation.UserId != currentUserId)
            {
                return Forbid();
            }

            // Walidacja
            var validationResult = await ValidateReservation(reservation, id);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var availableResources = await _context.Resources
                .Where(r => r.IsAvailable || r.Id == reservation.ResourceId)
                .Include(r => r.Category)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.Resources = new SelectList(availableResources, "Id", "Name", reservation.ResourceId);

            return View(reservation);
        }

        // POST: Reservations/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            // Sprawdź uprawnienia
            var currentUserId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("UserRole");
            
            if (userRole != "Admin" && reservation.UserId != currentUserId)
            {
                return Forbid();
            }

            // Nie można anulować zakończonych rezerwacji
            if (reservation.Status == ReservationStatus.Completed)
            {
                return BadRequest("Nie można anulować zakończonych rezerwacji.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            _context.Update(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return null;
        }

        private async Task<Reservation> GetReservationWithIncludes(int id)
        {
            return await _context.Reservations
                .Include(r => r.Resource)
                    .ThenInclude(res => res.Category)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        private async Task<(bool IsValid, List<string> Errors)> ValidateReservation(Reservation reservation, int? excludeId = null)
        {
            var errors = new List<string>();

            // Sprawdź czy zasób istnieje i jest dostępny
            var resource = await _context.Resources.FindAsync(reservation.ResourceId);
            if (resource == null)
            {
                errors.Add("Wybrany zasób nie istnieje.");
                return (false, errors);
            }

            if (!resource.IsAvailable)
            {
                errors.Add("Wybrany zasób jest obecnie niedostępny.");
            }

            // Sprawdź daty
            if (reservation.StartDate >= reservation.EndDate)
            {
                errors.Add("Data zakończenia musi być późniejsza niż data rozpoczęcia.");
            }

            if (reservation.StartDate < DateTime.Now.AddMinutes(-5)) // 5 minut tolerancji
            {
                errors.Add("Nie można utworzyć rezerwacji w przeszłości.");
            }

            // Sprawdź maksymalny czas rezerwacji
            var duration = reservation.EndDate - reservation.StartDate;
            if (duration.TotalHours > resource.MaxReservationHours)
            {
                errors.Add($"Maksymalny czas rezerwacji dla tego zasobu to {resource.MaxReservationHours} godzin.");
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
                errors.Add("Wybrany termin koliduje z istniejącą rezerwacją.");
            }

            return (errors.Count == 0, errors);
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}