using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Users;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers
{
    /// <summary>
    /// Quản lý người dùng — dành cho Admin
    /// </summary>
    [Route("api/users")]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// [GET] /api/users/stats
        /// Thống kê 6 thẻ đầu trang: Tổng / Đã xóa / Chờ xác thực / Free / Còn hạn Pro / Hết hạn Pro
        /// </summary>
        [AllowAnonymous]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct)
        {
            var stats = await _userService.GetStatsAsync(ct);
            return Success(stats, "Lấy thống kê người dùng thành công");
        }

        /// <summary>
        /// [GET] /api/users?page=1&amp;pageSize=20&amp;search=...
        /// Danh sách người dùng phân trang cho bảng quản lý
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] UserQueryOptions options, CancellationToken ct)
        {
            var queryOptions = new QueryOptions
            {
                Page = options.Page,
                PageSize = options.PageSize,
                Search = options.Search
            };
            var result = await _userService.GetPagedUsersAsync(queryOptions, ct);
            return Success(result, "Lấy danh sách người dùng thành công");
        }
    }
}
