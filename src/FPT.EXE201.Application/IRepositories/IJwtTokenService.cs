using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories
{
    /// <summary>
    /// JWT token generation and validation service
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generate access token for authenticated user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT access token string</returns>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Get token expiration time in seconds
        /// </summary>
        int GetTokenExpirationSeconds();

        /// <summary>
        /// Validate token and extract claims (optional, usually done by middleware)
        /// </summary>
        ClaimsPrincipal? ValidateToken(string token);
    }
}
