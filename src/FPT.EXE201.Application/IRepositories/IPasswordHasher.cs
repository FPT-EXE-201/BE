using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPT.EXE201.Application.IRepositories
{
    /// <summary>
    /// Password hashing service interface
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hash a plain text password
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password as byte array</returns>
        byte[] HashPassword(string password);

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="passwordHash">Stored password hash</param>
        /// <returns>True if password matches, false otherwise</returns>
        bool VerifyPassword(string password, byte[] passwordHash);
    }
}
