using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Users;

namespace FPT.EXE201.Application.IServices
{
    public interface IUserService
    {
        /// <summary>
        /// Lấy danh sách người dùng phân trang cho bảng quản lý (hỗ trợ search, sort).
        /// </summary>
        Task<PagedResult<UserListItemDto>> GetPagedUsersAsync(QueryOptions options, CancellationToken ct = default);

        /// <summary>
        /// Lấy thống kê 6 thẻ đầu trang: Tổng / Đã xóa / Chờ xác thực / Free / Còn hạn Pro / Hết hạn Pro.
        /// </summary>
        Task<UserStatsDto> GetStatsAsync(CancellationToken ct = default);
    }
}
