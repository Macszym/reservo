using System.ComponentModel.DataAnnotations;

namespace Reservo.Models
{
    public class Resource
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Nazwa zasobu")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Opis")]
        public string? Description { get; set; }
        
        [Display(Name = "Lokalizacja")]
        public string? Location { get; set; }
        
        [Display(Name = "Dostępny")]
        public bool IsAvailable { get; set; } = true;
        
        [Display(Name = "Maksymalny czas rezerwacji (godz.)")]
        public int MaxReservationHours { get; set; } = 24;
        
        [Display(Name = "Data utworzenia")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Klucz obcy do kategorii
        [Display(Name = "Kategoria")]
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        
        // Relacja z rezerwacjami
        public ICollection<Reservation>? Reservations { get; set; }
    }
}