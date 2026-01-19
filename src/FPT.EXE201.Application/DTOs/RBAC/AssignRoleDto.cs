namespace FPT.EXE201.Application.DTOs.RBAC
{
    public class AssignRoleDto
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; } = new();
    }
}
