using System.ComponentModel.DataAnnotations;

namespace Reservo.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Nazwa kategorii")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Opis")]
        public string? Description { get; set; }
        
        [Display(Name = "Kolor")]
        public string Color { get; set; } = "#007bff";
        
        // Relacja z zasobami
        public ICollection<Resource>? Resources { get; set; }
    }
}