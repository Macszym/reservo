using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reservo.Models;
using Reservo.Attributes;

namespace Reservo.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Podstawowe statystyki
            var totalResources = await _context.Resources.CountAsync();
            var availableResources = await _context.Resources.CountAsync(r => r.IsAvailable);
            var totalUsers = await _context.Users.CountAsync();
            
            var activeReservationsToday = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.Active && 
                               r.StartDate.Date <= today && r.EndDate.Date >= today);

            var reservationsThisMonth = await _context.Reservations
                .CountAsync(r => r.CreatedAt >= startOfMonth && r.CreatedAt <= endOfMonth);

            ViewBag.TotalResources = totalResources;
            ViewBag.AvailableResources = availableResources;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveReservationsToday = activeReservationsToday;
            ViewBag.ReservationsThisMonth = reservationsThisMonth;

            // Najbardziej popularne zasoby (w tym miesiącu)
            var reservationsThisMonthData = await _context.Reservations
                .Where(r => r.CreatedAt >= startOfMonth && r.CreatedAt <= endOfMonth)
                .Include(r => r.Resource)
                .Include(r => r.User)
                .ToListAsync();

            var popularResources = reservationsThisMonthData
                .GroupBy(r => r.Resource)
                .Select(g => new
                {
                    Resource = g.Key,
                    Count = g.Count(),
                    TotalHours = g.Sum(r => (r.EndDate - r.StartDate).TotalHours)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            ViewBag.PopularResources = popularResources;

            // Najbardziej aktywni użytkownicy (w tym miesiącu)
            var activeUsers = reservationsThisMonthData
                .GroupBy(r => r.User)
                .Select(g => new
                {
                    User = g.Key,
                    Count = g.Count(),
                    TotalHours = g.Sum(r => (r.EndDate - r.StartDate).TotalHours)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            ViewBag.ActiveUsers = activeUsers;

            return View();
        }

        // GET: Reports/ResourceUsage
        public async Task<IActionResult> ResourceUsage(int? resourceId, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var resourcesQuery = _context.Resources.Include(r => r.Category).AsQueryable();
            
            if (resourceId.HasValue)
            {
                resourcesQuery = resourcesQuery.Where(r => r.Id == resourceId.Value);
            }

            var resources = await resourcesQuery.ToListAsync();
            var resourceUsage = new List<object>();

            foreach (var resource in resources)
            {
                var reservations = await _context.Reservations
                    .Where(r => r.ResourceId == resource.Id &&
                               r.StartDate >= start && r.EndDate <= end)
                    .Include(r => r.User)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();

                var totalHours = reservations.Sum(r => (r.EndDate - r.StartDate).TotalHours);
                var avgHoursPerReservation = reservations.Any() ? totalHours / reservations.Count : 0;

                resourceUsage.Add(new
                {
                    Resource = resource,
                    Reservations = reservations,
                    TotalReservations = reservations.Count,
                    TotalHours = totalHours,
                    AverageHoursPerReservation = avgHoursPerReservation,
                    UtilizationPercentage = CalculateUtilization(totalHours, start, end)
                });
            }

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.SelectedResourceId = resourceId;
            ViewBag.Resources = await _context.Resources.OrderBy(r => r.Name).ToListAsync();
            ViewBag.ResourceUsage = resourceUsage;

            return View();
        }

        // GET: Reports/Calendar
        public async Task<IActionResult> Calendar(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var startOfWeek = selectedDate.AddDays(-(int)selectedDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);

            var weekDays = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                weekDays.Add(startOfWeek.AddDays(i));
            }

            var reservations = await _context.Reservations
                .Include(r => r.Resource)
                    .ThenInclude(res => res.Category)
                .Include(r => r.User)
                .Where(r => r.Status == ReservationStatus.Active &&
                           r.StartDate.Date <= endOfWeek &&
                           r.EndDate.Date >= startOfWeek)
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            // Grupuj rezerwacje po dniach
            var dailyReservations = weekDays.ToDictionary(
                day => day,
                day => reservations.Where(r => 
                    r.StartDate.Date <= day && r.EndDate.Date >= day).ToList()
            );

            ViewBag.SelectedDate = selectedDate;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.WeekDays = weekDays;
            ViewBag.DailyReservations = dailyReservations;

            return View();
        }

        // GET: Reports/Availability
        public async Task<IActionResult> Availability(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? start.AddDays(30);

            var resources = await _context.Resources
                .Include(r => r.Category)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var availability = new List<object>();

            foreach (var resource in resources)
            {
                var reservations = await _context.Reservations
                    .Where(r => r.ResourceId == resource.Id &&
                               r.Status == ReservationStatus.Active &&
                               r.StartDate < end &&
                               r.EndDate > start)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();

                // Znajdź wolne terminy
                var freeSlots = FindFreeSlots(start, end, reservations);
                var totalFreeHours = freeSlots.Sum(slot => (slot.End - slot.Start).TotalHours);
                var totalPeriodHours = (end - start).TotalHours;
                var availabilityPercentage = (totalFreeHours / totalPeriodHours) * 100;

                availability.Add(new
                {
                    Resource = resource,
                    Reservations = reservations,
                    FreeSlots = freeSlots,
                    AvailabilityPercentage = availabilityPercentage,
                    TotalFreeHours = totalFreeHours
                });
            }

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.Availability = availability;

            return View();
        }

        private double CalculateUtilization(double totalHours, DateTime start, DateTime end)
        {
            var totalPeriodHours = (end - start).TotalHours;
            return totalPeriodHours > 0 ? (totalHours / totalPeriodHours) * 100 : 0;
        }

        private List<(DateTime Start, DateTime End)> FindFreeSlots(DateTime start, DateTime end, List<Reservation> reservations)
        {
            var freeSlots = new List<(DateTime Start, DateTime End)>();
            var current = start;

            var sortedReservations = reservations.OrderBy(r => r.StartDate).ToList();

            foreach (var reservation in sortedReservations)
            {
                if (current < reservation.StartDate)
                {
                    freeSlots.Add((current, reservation.StartDate));
                }
                current = reservation.EndDate > current ? reservation.EndDate : current;
            }

            if (current < end)
            {
                freeSlots.Add((current, end));
            }

            return freeSlots;
        }
    }
}