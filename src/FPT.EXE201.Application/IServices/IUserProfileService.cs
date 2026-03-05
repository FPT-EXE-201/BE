using FPT.EXE201.Application.DTOs.Auth;

namespace FPT.EXE201.Application.IServices
{
    public interface IUserProfileService
    {
        /// <summary>
        /// Cập nhật thông tin profile. Hỗ trợ upload avatar qua IFormFile.
        /// </summary>
        Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, AvatarUploadInfo? avatarUpload = null, CancellationToken ct = default);

        /// <summary>
        /// Đổi mật khẩu — yêu cầu xác nhận mật khẩu cũ
        /// </summary>
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken ct = default);
    }
}

