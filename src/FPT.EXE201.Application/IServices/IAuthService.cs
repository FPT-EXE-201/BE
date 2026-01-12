using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPT.EXE201.Application.DTOs.Auth;

namespace FPT.EXE201.Application.IServices
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Register new user account
        /// </summary>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);

        /// <summary>
        /// Login with email/phone and password
        /// </summary>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);

        /// <summary>
        /// Get current user info by userId
        /// </summary>
        Task<UserResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default);
    }
}
