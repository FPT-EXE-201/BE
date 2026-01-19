namespace FPT.EXE201.Application.DTOs.RBAC
{
    public class CreateRoleDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }
}
