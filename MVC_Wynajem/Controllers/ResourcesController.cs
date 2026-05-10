using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Reservo.Models;
using Reservo.Attributes;

namespace Reservo.Controllers
{
    [Authorize]
    public class ResourcesController : Controller
    {
        private readonly AppDbContext _context;

        public ResourcesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Resources
        public async Task<IActionResult> Index(int? categoryId, string searchString)
        {
            var resources = _context.Resources.Include(r => r.Category).AsQueryable();

            // Filtrowanie po kategorii
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                resources = resources.Where(r => r.CategoryId == categoryId.Value);
            }

            // Wyszukiwanie po nazwie lub opisie
            if (!string.IsNullOrEmpty(searchString))
            {
                resources = resources.Where(r => r.Name.Contains(searchString) || 
                                               (r.Description != null && r.Description.Contains(searchString)));
            }

            // Dane dla filtrów
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", categoryId);
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentCategory = categoryId;

            return View(await resources.OrderBy(r => r.Name).ToListAsync());
        }

        // GET: Resources/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .Include(r => r.Category)
                .Include(r => r.Reservations)
                    .ThenInclude(res => res.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resource == null)
            {
                return NotFound();
            }

            // Aktywne rezerwacje dla tego zasobu
            ViewBag.ActiveReservations = resource.Reservations?
                .Where(r => r.Status == ReservationStatus.Active && r.EndDate > DateTime.Now)
                .OrderBy(r => r.StartDate)
                .ToList();

            return View(resource);
        }

        // GET: Resources/Create
        [Authorize("Admin")]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Resources/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("Admin")]
        public async Task<IActionResult> Create([Bind("Name,Description,Location,IsAvailable,MaxReservationHours,CategoryId")] Resource resource)
        {
            if (ModelState.IsValid)
            {
                resource.CreatedAt = DateTime.Now;
                _context.Add(resource);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", resource.CategoryId);
            return View(resource);
        }

        // GET: Resources/Edit/5
        [Authorize("Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", resource.CategoryId);
            return View(resource);
        }

        // POST: Resources/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Location,IsAvailable,MaxReservationHours,CategoryId,CreatedAt")] Resource resource)
        {
            if (id != resource.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(resource);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResourceExists(resource.Id))
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
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", resource.CategoryId);
            return View(resource);
        }

        // GET: Resources/Delete/5
        [Authorize("Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .Include(r => r.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resource == null)
            {
                return NotFound();
            }

            return View(resource);
        }

        // POST: Resources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize("Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                _context.Resources.Remove(resource);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }

        // GET: Resources/Availability/5 - sprawdza dostępność zasobu
        public async Task<IActionResult> Availability(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .Include(r => r.Category)
                .Include(r => r.Reservations)
                    .ThenInclude(res => res.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resource == null)
            {
                return NotFound();
            }

            // Rezerwacje na następne 30 dni
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(30);
            
            var reservations = resource.Reservations?
                .Where(r => r.Status == ReservationStatus.Active && 
                           r.StartDate < endDate && r.EndDate > startDate)
                .OrderBy(r => r.StartDate)
                .ToList() ?? new List<Reservation>();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Reservations = reservations;

            return View(resource);
        }
    }
}