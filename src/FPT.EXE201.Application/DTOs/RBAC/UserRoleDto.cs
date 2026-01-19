namespace FPT.EXE201.Application.DTOs.RBAC
{
    public class UserRoleDto
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
