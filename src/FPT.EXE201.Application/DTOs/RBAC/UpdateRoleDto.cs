namespace FPT.EXE201.Application.DTOs.RBAC
{
    public class UpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }
}
