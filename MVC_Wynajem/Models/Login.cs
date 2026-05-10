using System.ComponentModel.DataAnnotations;

namespace Reservo.Models;

public class Login
{
    [Key]
    public int    Id       { get; set; }
    public string User     { get; set; }
    public string Password { get; set; }
}