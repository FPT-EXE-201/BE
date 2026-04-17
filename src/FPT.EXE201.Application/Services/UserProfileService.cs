using AutoMapper;
using FPT.EXE201.Application.DTOs.Auth;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services
{
    public class UserProfileService : IUserProfileService
    {
        private static readonly string[] AllowedAvatarTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxAvatarSizeBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMapper _mapper;

        public UserProfileService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IFileStorageService fileStorageService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _fileStorageService = fileStorageService;
            _mapper = mapper;
        }

        public async Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, AvatarUploadInfo? avatarUpload = null, CancellationToken ct = default)
        {
            // 1. Lấy profile tracked (đã include User navigation) — 1 query duy nhất
            //    Tránh load User riêng vì sẽ gây EF tracking conflict (2 instance cùng Id)
            var profile = await _unitOfWork.UserProfiles.GetByUserIdTrackedAsync(userId, ct)
                ?? throw new NotFoundException("User profile not found");

            var user = profile.User
                ?? throw new NotFoundException("User not found");

            if (user.Status != UserStatus.Active)
                throw new UnauthorizedException($"Account is {user.Status}");

            // 2. Cập nhật phone trên User nếu có thay đổi
            if (request.Phone is not null)
            {
                var normalizedPhone = request.Phone.Trim();
                if (normalizedPhone != user.Phone &&
                    await _unitOfWork.Users.ExistsByPhoneAsync(normalizedPhone, includeDeleted: false, ct))
                {
                    throw new ConflictException("Phone number already used by another account");
                }
                user.Phone = normalizedPhone;
                // User đã được track qua profile.User — EF tự detect change, không cần Update()
            }

            // 3. Cập nhật profile fields (chỉ cập nhật field không null)
            if (request.FullName is not null)
                profile.FullName = request.FullName.Trim();

            if (request.DateOfBirth.HasValue)
                profile.DateOfBirth = request.DateOfBirth.Value;

            // 4. Upload avatar lên Supabase nếu có file
            if (avatarUpload is not null)
            {
                if (!AllowedAvatarTypes.Contains(avatarUpload.ContentType.ToLowerInvariant()))
                    throw new BadRequestException("Avatar must be a JPEG, PNG, or WebP image");

                if (avatarUpload.SizeBytes > MaxAvatarSizeBytes)
                    throw new BadRequestException("Avatar file size must not exceed 5 MB");

                var uploadResult = await _fileStorageService.UploadAsync(
                    avatarUpload.FileStream,
                    $"avatars/{userId}/{Guid.NewGuid()}{Path.GetExtension(avatarUpload.FileName)}",
                    avatarUpload.ContentType,
                    avatarUpload.SizeBytes,
                    ownerUserId: userId,
                    cancellationToken: ct);

                profile.AvatarUrl = uploadResult.PublicUrl;
            }

            if (request.PreferredLanguage is not null)
            {
                var language = await _unitOfWork.Languages.GetByCodeAsync(request.PreferredLanguage.Trim(), ct);
                if (language == null || !language.IsActive)
                    throw new BadRequestException($"Language '{request.PreferredLanguage}' is not supported");
                profile.PreferredLang = language.Code;
            }

            // Profile và User đều đang tracked → SaveChanges tự detect mọi thay đổi
            await _unitOfWork.SaveChangesAsync(ct);

            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken ct = default)
        {
            // 1. Validate ConfirmNewPassword
            if (request.NewPassword != request.ConfirmNewPassword)
                throw new BadRequestException("New password and confirmation do not match");

            // 2. Validate độ dài mật khẩu tối thiểu
            if (request.NewPassword.Length < 6)
                throw new BadRequestException("New password must be at least 6 characters");

            // 3. Lấy user (tracked)
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: ct)
                ?? throw new NotFoundException("User not found");

            if (user.Status != UserStatus.Active)
                throw new UnauthorizedException($"Account is {user.Status}");

            // 4. Xác nhận mật khẩu cũ
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                throw new BadRequestException("Current password is incorrect");

            // 5. Đảm bảo mật khẩu mới khác mật khẩu cũ
            if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
                throw new BadRequestException("New password must be different from current password");

            // 6. Hash và lưu mật khẩu mới
            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}

