using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Users;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ─── Paged list ──────────────────────────────────────────────────────────

        public async Task<PagedResult<UserListItemDto>> GetPagedUsersAsync(
            QueryOptions options,
            CancellationToken ct = default)
        {
            // Lấy trang users (đã include Profile, UserRoles→Role trong repository)
            var paged = await _unitOfWork.Users.GetPagedUsersAsync(options, ct);

            // Lấy subscription Active cho tất cả userId trong trang này (1 query)
            var userIds = paged.Items.Select(u => u.Id).ToList();
            var activeSubs = await _unitOfWork.Subscriptions.GetActiveSubscriptionsByUserIdsAsync(userIds, ct);
            var subByUserId = activeSubs.ToDictionary(s => s.UserId);

            var items = paged.Items.Select(u =>
            {
                subByUserId.TryGetValue(u.Id, out var sub);

                return new UserListItemDto
                {
                    Id                  = u.Id,
                    FullName            = u.Profile?.FullName,
                    Email               = u.Email,
                    Status              = u.Status.ToString(),
                    SubscriptionPlan    = sub?.Plan.ToString() ?? "Free",
                    SubscriptionEndDate = sub?.EndDate,
                    CreatedAt           = u.CreatedAt,
                };
            }).ToList();

            return new PagedResult<UserListItemDto>(items, paged.Page, paged.PageSize, paged.TotalItems);
        }

        // ─── Stats ───────────────────────────────────────────────────────────────

        public async Task<UserStatsDto> GetStatsAsync(CancellationToken ct = default)
        {
            var total = (int)await _unitOfWork.Users.CountAsync(includeDeleted: true, cancellationToken: ct);

            var deleted = (int)await _unitOfWork.Users.CountAsync(
                u => u.DeletedAt != null,
                includeDeleted: true,
                cancellationToken: ct);

            var pending = (int)await _unitOfWork.Users.CountAsync(
                u => u.Status == UserStatus.Pending && u.DeletedAt == null,
                includeDeleted: true,
                cancellationToken: ct);

            var activePro  = await _unitOfWork.Subscriptions.CountActiveProUsersAsync(ct);
            var expiredPro = await _unitOfWork.Subscriptions.CountExpiredProUsersAsync(ct);

            var activeTotal = (int)await _unitOfWork.Users.CountAsync(
                u => u.DeletedAt == null,
                includeDeleted: true,
                cancellationToken: ct);

            var free = Math.Max(0, activeTotal - pending - activePro);

            return new UserStatsDto
            {
                Total      = total,
                Deleted    = deleted,
                Pending    = pending,
                Free       = free,
                ActivePro  = activePro,
                ExpiredPro = expiredPro,
            };
        }
    }
}
