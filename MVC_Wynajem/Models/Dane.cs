using System.ComponentModel.DataAnnotations;

namespace Reservo.Models
{
    public class Dane
    {
        [Key]
        public int    Id   { get; set; }
        public string Text { get; set; }
    }
}
