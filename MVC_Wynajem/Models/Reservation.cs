using System.ComponentModel.DataAnnotations;

namespace Reservo.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Data rozpoczęcia")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [Required]
        [Display(Name = "Data zakończenia")]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }
        
        [Display(Name = "Cel rezerwacji")]
        public string? Purpose { get; set; }
        
        [Display(Name = "Status")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Active;
        
        [Display(Name = "Data utworzenia")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Klucze obce
        [Required]
        [Display(Name = "Użytkownik")]
        public int UserId { get; set; }
        public User? User { get; set; }
        
        [Required]
        [Display(Name = "Zasób")]
        public int ResourceId { get; set; }
        public Resource? Resource { get; set; }
    }
    
    public enum ReservationStatus
    {
        [Display(Name = "Aktywna")]
        Active,
        [Display(Name = "Zakończona")]
        Completed,
        [Display(Name = "Anulowana")]
        Cancelled
    }
}