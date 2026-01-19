using System;
using System.Collections.Generic;
using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
