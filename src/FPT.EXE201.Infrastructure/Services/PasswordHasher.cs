using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.IRepositories;

namespace FPT.EXE201.Infrastructure.Services
{
    /// <summary>
    /// Password hashing implementation using BCrypt
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12; // BCrypt work factor (cost)

        public byte[] HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Generate BCrypt hash with salt
            var hash = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
            
            // Convert to byte array for storage
            return Encoding.UTF8.GetBytes(hash);
        }

        public bool VerifyPassword(string password, byte[] passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (passwordHash == null || passwordHash.Length == 0)
                return false;

            try
            {
                // Convert byte array back to hash string
                var hashString = Encoding.UTF8.GetString(passwordHash);
                
                // Verify password against hash
                return BCrypt.Net.BCrypt.Verify(password, hashString);
            }
            catch
            {
                return false;
            }
        }
    }
}
