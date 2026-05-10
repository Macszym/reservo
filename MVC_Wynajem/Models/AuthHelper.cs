using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace Reservo.Models
{
    public static class AuthHelper
    {
        private static readonly PasswordHasher<User> _passwordHasher = new();
        
        public static string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }
        
        public static bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
        
        public static string GenerateApiKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}