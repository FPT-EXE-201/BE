using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FPT.EXE201.Application.DTOs.Auth;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services
{
    /// <summary>
    /// Authentication service implementation
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUserRoleService _userRoleService;
        private readonly IMapper _mapper;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IUserRoleService userRoleService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _userRoleService = userRoleService;
            _mapper = mapper;
        }

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request, 
            string? ipAddress = null, 
            string? userAgent = null, 
            CancellationToken ct = default)
        {
            // 1. Normalize email for storage
            var emailNormalized = NormalizeEmail(request.Email);

            // 2. Check if email already exists (case-insensitive via Email field)
            if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email, includeDeleted: false, ct))
            {
                throw new ConflictException("Email already exists");
            }

            // 2b. Check if phone already exists
            if (!string.IsNullOrEmpty(request.Phone) 
                && await _unitOfWork.Users.ExistsByPhoneAsync(request.Phone, includeDeleted: false, ct))
            {
                throw new ConflictException("Phone number already exists");
            }

            // 3. Validate preferred language (optional - fallback to "vi" if invalid)
            var language = await _unitOfWork.Languages.GetByCodeAsync(request.PreferredLanguage ?? "vi", ct);
            if (language == null || !language.IsActive)
            {
                language = await _unitOfWork.Languages.GetDefaultAsync(ct);
            }

            // 4. Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 5. Create User entity using AutoMapper
            var user = _mapper.Map<User>(request);
            user.PasswordHash = passwordHash; // Set after mapping
            user.EmailNormalized = emailNormalized; // Ensure normalized

            await _unitOfWork.Users.AddAsync(user, ct);

            // 6. Create UserProfile entity using AutoMapper
            var profile = _mapper.Map<UserProfile>(request);
            profile.UserId = user.Id;
            profile.PreferredLang = language?.Code ?? "vi";

            await _unitOfWork.UserProfiles.AddAsync(profile, ct);

            // 7. Save changes
            await _unitOfWork.SaveChangesAsync(ct);

            // 7b. Assign default USER role
            var userRole = await _unitOfWork.Roles.GetByCodeAsync("USER", includeDeleted: false, ct);
            if (userRole != null)
            {
                await _userRoleService.AssignRolesToUserAsync(user.Id, new List<Guid> { userRole.Id }, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            // 8. Attach profile to user for response mapping
            user.Profile = profile;

            // 9. Query user permissions and roles (newly registered user may have no roles yet)
            var permissions = await _userRoleService.GetUserPermissionCodesAsync(user.Id, ct);
            var roles = await _userRoleService.GetUserRoleCodesAsync(user.Id, ct);

            // 10. Issue refresh token first (need tokenId for access token claims)
            var (refreshToken, tokenId, _) = await _jwtTokenService.IssueRefreshTokenAsync(
                user.Id, 
                ipAddress, 
                userAgent, 
                ct: ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 11. Generate access token WITH permissions and roles
            var accessToken = _jwtTokenService.GenerateAccessTokenWithPermissions(user, permissions, roles, tokenId);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtTokenService.GetTokenExpirationSeconds(),
                User = _mapper.Map<UserResponseDto>(user)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request, 
            string? ipAddress = null, 
            string? userAgent = null, 
            CancellationToken ct = default)
        {
            // 1. Determine if input is email or phone
            User? user = null;

            if (request.EmailOrPhone.Contains("@"))
            {
                // Email login
                var email = request.EmailOrPhone;
                user = await _unitOfWork.Users.GetByEmailAsync(
                    email,
                    includeProfile: true,
                    includeDeleted: false,
                    ct);
            }
            else
            {
                // Phone login
                user = await _unitOfWork.Users.GetByPhoneAsync(
                    request.EmailOrPhone,
                    includeProfile: true,
                    includeDeleted: false,
                    ct);
            }

            // 2. Validate user exists
            if (user == null)
            {
                throw new UnauthorizedException("Invalid credentials");
            }

            // 3. Check user status
            if (user.Status != UserStatus.Active)
            {
                throw new UnauthorizedException($"Account is {user.Status}");
            }

            // 4. Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid credentials");
            }

            // 5. Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);

            // 6. Query user permissions and roles (Approach 2 - 1 time only at login)
            var permissions = await _userRoleService.GetUserPermissionCodesAsync(user.Id, ct);
            var roles = await _userRoleService.GetUserRoleCodesAsync(user.Id, ct);

            // 7. Issue refresh token first (need tokenId for access token claims)
            var (refreshToken, tokenId, _) = await _jwtTokenService.IssueRefreshTokenAsync(
                user.Id, 
                ipAddress, 
                userAgent, 
                ct: ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 8. Generate access token WITH permissions and roles
            var accessToken = _jwtTokenService.GenerateAccessTokenWithPermissions(user, permissions, roles, tokenId);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtTokenService.GetTokenExpirationSeconds(),
                User = _mapper.Map<UserResponseDto>(user)
            };
        }

        public async Task<UserResponseDto> GetMeAsync(Guid userId, CancellationToken ct = default)
        {
            // Get user with profile
            var user = await _unitOfWork.Users.GetByIdWithProfileAsync(userId, includeDeleted: false, ct);

            if (user == null)
            {
                throw new NotFoundException("User not found");
            }

            // Map to UserResponseDto using AutoMapper
            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<RefreshTokenResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto request, 
            string? ipAddress = null, 
            string? userAgent = null, 
            CancellationToken ct = default)
        {
            // Rotate the refresh token (validates and issues new one)
            var (newRefreshToken, newTokenId, user) = await _jwtTokenService.RotateRefreshTokenAsync(
                request.RefreshToken,
                ipAddress,
                userAgent,
                ct);

            // Query latest permissions and roles (in case admin assigned new roles)
            var permissions = await _userRoleService.GetUserPermissionCodesAsync(user.Id, ct);
            var roles = await _userRoleService.GetUserRoleCodesAsync(user.Id, ct);

            // Generate new access token WITH latest permissions and roles
            var newAccessToken = _jwtTokenService.GenerateAccessTokenWithPermissions(user, permissions, roles, newTokenId);

            return new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtTokenService.GetTokenExpirationSeconds()
            };
        }

        public async Task LogoutAsync(Guid userId, Guid refreshTokenId, CancellationToken ct = default)
        {
            await _jwtTokenService.RevokeByIdAsync(refreshTokenId, ct);
        }

        #region Helper Methods

        private string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        #endregion
    }
}

