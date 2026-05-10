namespace Reservo.Models.DTOs
{
    public class ResourceDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsAvailable { get; set; }
        public int MaxReservationHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
    }
    
    public class CreateResourceDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int MaxReservationHours { get; set; } = 24;
        public int? CategoryId { get; set; }
    }
}