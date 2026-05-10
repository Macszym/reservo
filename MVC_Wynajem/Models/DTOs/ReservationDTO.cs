namespace Reservo.Models.DTOs
{
    public class ReservationDTO
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Purpose { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public int ResourceId { get; set; }
        public string? ResourceName { get; set; }
        public string? ResourceLocation { get; set; }
    }
    
    public class CreateReservationDTO
    {
        public int ResourceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Purpose { get; set; }
    }
}