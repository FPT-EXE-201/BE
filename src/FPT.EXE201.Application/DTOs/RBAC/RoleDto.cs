namespace FPT.EXE201.Application.DTOs.RBAC
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Optional: include permissions when needed
        public List<PermissionDto>? Permissions { get; set; }
    }
}
