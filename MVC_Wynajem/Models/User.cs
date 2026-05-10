using System.ComponentModel.DataAnnotations;

namespace Reservo.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Nazwa użytkownika")]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Rola")]
        public string Role { get; set; } = "User"; // "Admin" lub "User"
        
        [Display(Name = "Klucz API")]
        public string? ApiKey { get; set; }
        
        [Display(Name = "Data utworzenia")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Relacja z rezerwacjami
        public ICollection<Reservation>? Reservations { get; set; }
    }
}