namespace FPT.EXE201.Application.DTOs.Users
{
    /// <summary>
    /// Thống kê 6 thẻ đầu trang Quản lý người dùng
    /// </summary>
    public class UserStatsDto
    {
        public int Total { get; set; }
        public int Deleted { get; set; }
        public int Pending { get; set; }
        public int Free { get; set; }
        public int ActivePro { get; set; }
        public int ExpiredPro { get; set; }
    }

    /// <summary>
    /// Các tham số truyền vào API GET /api/users
    /// Giấu bớt các field rườm rà không cần thiết của QueryOptions
    /// </summary>
    public class UserQueryOptions
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }
    }

    /// <summary>
    /// Một dòng trong bảng danh sách người dùng
    /// </summary>
    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gói đang dùng: "Monthly" / "SixMonths" / "Yearly" — null nếu không có
        /// </summary>
        public string? SubscriptionPlan { get; set; }
        
        public DateTime? SubscriptionEndDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
