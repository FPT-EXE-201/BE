using System.Security.Claims;
using FPT.EXE201.Application.DTOs.Auth;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers
{
    [Route("api/[controller]")]
    [Tags("Authentication")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register new user account
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var response = await _authService.RegisterAsync(request, ipAddress, userAgent, ct);
            return Created(response, "User registered successfully");
        }

        /// <summary>
        /// Login with email/phone and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var response = await _authService.LoginAsync(request, ipAddress, userAgent, ct);
            return Success(response, "Login successful");
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
        {
            // Get client IP and UserAgent for security tracking
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var response = await _authService.RefreshTokenAsync(request, ipAddress, userAgent, ct);
            return Success(response, "Token refreshed successfully");
        }

        /// <summary>
        /// Logout and revoke refresh token (current device only)
        /// </summary>
        [HttpPost("logout")]
        [Authorize] // Changed from [AllowAnonymous] - now requires valid access token
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            // Extract userId from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid token");

            // Extract refresh token ID from claims (Option 3 - Hybrid approach)
            var rtidClaim = User.FindFirst("rtid")?.Value;

            if (string.IsNullOrEmpty(rtidClaim) || !Guid.TryParse(rtidClaim, out var refreshTokenId))
                throw new UnauthorizedException("Refresh token ID not found in access token");

            await _authService.LogoutAsync(userId, refreshTokenId, ct);
            return Success("Logged out successfully");
        }

        /// <summary>
        /// Get current authenticated user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid token");

            var user = await _authService.GetMeAsync(userId, ct);
            return Success(user, "User information retrieved successfully");
        }
    }
}
