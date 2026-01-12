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
        private readonly IMapper _mapper;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
        {
            // 1. Normalize email for storage
            var emailNormalized = NormalizeEmail(request.Email);

            // 2. Check if email already exists (case-insensitive via Email field)
            if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email, includeDeleted: false, ct))
            {
                throw new ConflictException("Email already exists");
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

            // 8. Attach profile to user for response mapping
            user.Profile = profile;

            // 9. Generate token and return response using AutoMapper
            var token = _jwtTokenService.GenerateAccessToken(user);

            return new AuthResponseDto
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = _jwtTokenService.GetTokenExpirationSeconds(),
                User = _mapper.Map<UserResponseDto>(user)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
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
            await _unitOfWork.SaveChangesAsync(ct);

            // 6. Generate token and return response using AutoMapper
            var token = _jwtTokenService.GenerateAccessToken(user);

            return new AuthResponseDto
            {
                AccessToken = token,
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

        #region Helper Methods

        private string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        #endregion
    }
}
